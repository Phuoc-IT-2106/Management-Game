using Godot;
using Qualification.Application;
using Qualification.Domain;
using System.Globalization;
using System.Text.Json;

public partial class DeterminismHost : Node
{
    private Session _session=null!;
    private string _initial="";
    private readonly List<string> _hashes=[];
    private int _cursor;
    private readonly System.Collections.Immutable.ImmutableArray<AdvanceCommand> _journal=Fixture.Journal();
    public override void _Ready()
    {
        var args=OS.GetCmdlineUserArgs();
        CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo(args.Contains("--tr")?"tr-TR":"en-US");
        CultureInfo.CurrentUICulture=CultureInfo.CurrentCulture;
        _session=new Session(Fixture.Initial(reversed:args.Contains("--reverse")),args.Contains("--log")?s=>GD.Print(s):null);
        _initial=Fixture.Hash(_session.Current);
    }
    public override void _Process(double delta)
    {
        // One command per actual engine frame; delta never reaches Application/Domain.
        try
        {
            _hashes.Add(_session.Submit(_journal[_cursor++]));
            if(_cursor!=_journal.Length) return;
            string path=OS.GetCmdlineUserArgs().Single(a=>a.StartsWith("--output=",StringComparison.Ordinal))[9..];
            System.IO.File.WriteAllText(path,JsonSerializer.Serialize(new ReplayResult(_initial,_hashes.ToArray(),Fixture.Hash(_session.Current)),new JsonSerializerOptions {WriteIndented=true}));
            GD.Print($"QG03_HOST_PASS {_cursor} {Fixture.Hash(_session.Current)}"); GetTree().Quit();
            SetProcess(false);
        }
        catch(Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); SetProcess(false); }
    }
}
