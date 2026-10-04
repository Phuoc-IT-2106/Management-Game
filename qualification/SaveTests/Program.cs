using Qualification.Persistence;
using System.Diagnostics;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;

if(args.Length>0 && args[0]=="--child")
{
    var childStore=new SnapshotStore(args[1]); string stage=args[2];
    childStore.Save(SnapshotStore.Create(4),point=>
    {
        if(point!=stage) return;
        File.WriteAllText(Path.Combine(args[1],"checkpoint.txt"),point);
        Thread.Sleep(Timeout.Infinite); // Parent kills this process: no Dispose/finally recovery.
    });
    return;
}

string root=Path.GetFullPath(args[0]); Directory.CreateDirectory(root);
var results=new List<object>(); int cases=0;
void Check(bool condition,string name) {if(!condition) throw new Exception(name); results.Add(new {test=name,result="PASS"}); Console.WriteLine("PASS "+name);}
SnapshotStore Fresh(string name)
{
    var store=new SnapshotStore(Path.Combine(root,$"{cases++:D3}-{name}"));
    store.Save(SnapshotStore.Create(1)); store.Save(SnapshotStore.Create(2)); store.Save(SnapshotStore.Create(3));
    return store;
}
void Reject(Action action,string name)
{
    try {action();} catch(IOException) {Check(true,name);return;} catch(UnauthorizedAccessException) {Check(true,name);return;}
    throw new Exception("Expected failure: "+name);
}
void Unchanged(SnapshotStore store,byte[] primary,byte[] backup,string name)
{
    Check(File.ReadAllBytes(store.Primary).SequenceEqual(primary) && File.ReadAllBytes(store.Backup).SequenceEqual(backup),name);
}

var round=Fresh("roundtrip");
Check(round.LoadPrimary()==SnapshotStore.Create(3),"valid snapshot round trip");
Check(SnapshotStore.Read(round.Backup).Revision==2 && SnapshotStore.Read(round.OlderBackup).Revision==1,"two valid backup generations");
foreach(string corruption in new[] {"truncated","malformed","new-schema","old-schema","checksum","content","duplicate","missing","invalid-day"})
{
    var store=Fresh(corruption); byte[] goodBackup=File.ReadAllBytes(store.Backup);
    string original=File.ReadAllText(store.Primary);
    string bad=corruption switch
    {
        "truncated"=>original[..(original.Length/2)], "malformed"=>"{oops",
        "new-schema"=>JsonSerializer.Serialize(SnapshotStore.Seal(SnapshotStore.Create(3) with {SchemaVersion=2})),
        "old-schema"=>JsonSerializer.Serialize(SnapshotStore.Seal(SnapshotStore.Create(3) with {SchemaVersion=0})),
        "checksum"=>original.Replace("\"CompanyCounter\":21","\"CompanyCounter\":22"),
        "content"=>JsonSerializer.Serialize(SnapshotStore.Seal(SnapshotStore.Create(3) with {ContentId="newer-content"})),
        "duplicate"=>original.Insert(1,"\"Revision\":3,"), "missing"=>"{}",
        _=>JsonSerializer.Serialize(SnapshotStore.Seal(SnapshotStore.Create(3) with {WorldDay=99}))
    };
    File.WriteAllText(store.Primary,bad); byte[] badPrimary=File.ReadAllBytes(store.Primary);
    Reject(()=>store.LoadPrimary(),$"reject {corruption}");
    Check(store.FindRecoveryCandidates().Select(c=>c.Revision).SequenceEqual(new long[] {2,1}),$"{corruption}: explicit recovery candidates");
    Check(SnapshotStore.Read(store.Backup)==SnapshotStore.Create(2),$"{corruption}: explicit backup load");
    Reject(()=>store.Save(SnapshotStore.Create(4)),$"{corruption}: corrupt primary cannot overwrite backup");
    Unchanged(store,badPrimary,goodBackup,$"{corruption}: originals untouched");
}

var locked=Fresh("locked"); byte[] lockedPrimary=File.ReadAllBytes(locked.Primary),lockedBackup=File.ReadAllBytes(locked.Backup);
using(var exclusive=new FileStream(locked.Primary,FileMode.Open,FileAccess.Read,FileShare.None))
    Reject(()=>locked.Save(SnapshotStore.Create(4)),"actual locked primary write rejected");
Unchanged(locked,lockedPrimary,lockedBackup,"locked primary preserves both generations");
using(var slot=new FileStream(locked.Primary+".lock",FileMode.Open,FileAccess.ReadWrite,FileShare.None))
    Reject(()=>locked.Save(SnapshotStore.Create(4)),"concurrent slot lock rejected");

var disk=Fresh("full-disk"); byte[] diskPrimary=File.ReadAllBytes(disk.Primary),diskBackup=File.ReadAllBytes(disk.Backup);
Reject(()=>disk.Save(SnapshotStore.Create(4),simulatedDiskLimit:37),"simulated disk full after 37 bytes");
Unchanged(disk,diskPrimary,diskBackup,"simulated disk full preserves committed generations");
Check(disk.LoadPrimary().Revision==3,"partial temp never selected as primary");
disk.Save(SnapshotStore.Create(4)); Check(disk.LoadPrimary().Revision==4,"subsequent save safely replaces abandoned temp");

// Actual Windows file ACL denial, scoped to an owned disposable fixture temp file.
if(OperatingSystem.IsWindows())
{
    var denied=Fresh("denied"); File.WriteAllText(denied.Temp,"abandoned");
    var fileInfo=new FileInfo(denied.Temp); var originalAcl=fileInfo.GetAccessControl();
    var acl=fileInfo.GetAccessControl();
    var identity=WindowsIdentity.GetCurrent().User ?? throw new Exception("No Windows identity");
    acl.AddAccessRule(new FileSystemAccessRule(identity,FileSystemRights.WriteData,AccessControlType.Deny));
    byte[] primary=File.ReadAllBytes(denied.Primary),backup=File.ReadAllBytes(denied.Backup);
    try
    {
        fileInfo.SetAccessControl(acl);
        Reject(()=>denied.Save(SnapshotStore.Create(4)),"actual ACL-denied temporary write rejected");
    }
    finally {fileInfo.SetAccessControl(originalAcl);}
    Unchanged(denied,primary,backup,"denied write preserves committed generations");
}

var corruptBackup=Fresh("corrupt-backup"); byte[] older=File.ReadAllBytes(corruptBackup.OlderBackup);
File.WriteAllText(corruptBackup.Backup,"corrupt"); corruptBackup.Save(SnapshotStore.Create(4));
Check(File.ReadAllBytes(corruptBackup.OlderBackup).SequenceEqual(older),"corrupt backup never rotates over older valid backup");
Check(SnapshotStore.Read(corruptBackup.Backup).Revision==3,"validated primary becomes new good backup");

string[] stages=["before-temp","partial-temp","after-temp-write","after-flush","after-validation","before-backup-publish","after-backup","before-publish","after-publish"];
foreach(string stage in stages)
for(int repeat=0;repeat<3;repeat++)
{
    var store=Fresh($"kill-{stage}-{repeat}"); string directory=Path.GetDirectoryName(store.Primary)!;
    var start=new ProcessStartInfo(Environment.ProcessPath!) {UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden};
    // Running as `dotnet SaveTests.dll` requires retaining the managed entry assembly.
    if(string.Equals(Path.GetFileNameWithoutExtension(Environment.ProcessPath),"dotnet",StringComparison.OrdinalIgnoreCase))
        start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
    start.ArgumentList.Add("--child"); start.ArgumentList.Add(directory); start.ArgumentList.Add(stage);
    using var process=Process.Start(start) ?? throw new Exception("Child launch failed");
    var wait=Stopwatch.StartNew(); string marker=Path.Combine(directory,"checkpoint.txt");
    while(!File.Exists(marker) && !process.HasExited && wait.Elapsed.TotalSeconds<15) Thread.Sleep(10);
    bool reached=File.Exists(marker);
    if(!process.HasExited) process.Kill(entireProcessTree:true);
    process.WaitForExit();
    Check(reached,$"kill {stage}/{repeat}: checkpoint reached");
    Snapshot primary=store.LoadPrimary();
    Check(primary==SnapshotStore.Create(stage=="after-publish"?4:3),$"kill {stage}/{repeat}: exact committed primary survived");
    Check(store.FindRecoveryCandidates().Length>=1,$"kill {stage}/{repeat}: valid committed backup survived");
    // Recover operationally without accepting the abandoned temporary file.
    store.Save(SnapshotStore.Create(5)); Check(store.LoadPrimary().Revision==5,$"kill {stage}/{repeat}: subsequent save succeeds");
}

File.WriteAllText(Path.Combine(root,"results.json"),JsonSerializer.Serialize(new {filesystem=new DriveInfo(Path.GetPathRoot(root)!).DriveFormat,tests=results.Count,results},new JsonSerializerOptions {WriteIndented=true}));
Console.WriteLine($"QG04_TESTS_PASS {results.Count}");
