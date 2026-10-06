using Godot;
using ManagementGame.Application;
using ManagementGame.OperatingMap;
using ManagementGame.SponsorWorkspace;
using ManagementGame.UiKit;

public partial class SponsorCompanyHost : Control
{
    private IGameSession gameSession = null!;
    private ISponsorSession sponsors = null!;
    private OperatingMap company = null!;
    private SponsorWorkspaceView? workspace;
    private readonly string sessionKey = Guid.NewGuid().ToString("N");
    private Action? close;
    private bool embedded;
    public void Configure(IGameSession session, Action onClose, bool embedded = false)
    { gameSession = session; sponsors = (ISponsorSession)session; close = onClose; this.embedded = embedded; }
    public override void _Ready()
    {
        if (gameSession is null)
        {
            Configure(Composition.Create(System.IO.Path.GetFullPath(System.IO.Path.Combine(ProjectSettings.GlobalizePath("res://"), "../../content/fixture.json")),
                ProjectSettings.GlobalizePath("user://saves"), 20261004), () => GetTree().ChangeSceneToFile("res://Main.tscn"));
        }
        var shell = new VBoxContainer(); AddChild(shell); shell.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var ui = new UiContext(new UiTokens(UiDensity.Compact)); Theme = ui.Theme;
        if (!embedded) shell.AddChild(ui.Button("Return to internal management UI", () => close?.Invoke()));
        company = new OperatingMap { SizeFlagsVertical = SizeFlags.ExpandFill, Embedded = embedded };
        company.ConfigureLive(SponsorPresentation.Company(sponsors.ObserveSponsors(), sessionKey), Open); shell.AddChild(company);
    }
    private void Open(MapSituation matter, MapReturn origin)
    {
        if (workspace is not null) return;
        company.GetParent<Control>().Visible = false;
        workspace = new SponsorWorkspaceView();
        workspace.Configure(new(sponsors, sponsors.ObserveSponsors(), matter.TargetId, origin), () =>
        {
            var old = workspace!; workspace = null; RemoveChild(old); old.QueueFree();
            company.GetParent<Control>().Visible = true;
            company.ReturnLive(SponsorPresentation.Company(sponsors.ObserveSponsors(), sessionKey));
        });
        AddChild(workspace); workspace.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
    }
    public override void _ExitTree() => close = null;
}
