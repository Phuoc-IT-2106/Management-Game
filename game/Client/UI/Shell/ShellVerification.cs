using Godot;
using ManagementGame.Application;
using ManagementGame.SponsorWorkspace;

namespace ManagementGame.Shell;

/// <summary>Native shell harness: synthetic pointer/keyboard checks and deterministic captures.</summary>
public partial class GameShell
{
    private readonly List<string> checks = [];
    private async Task Settle(int frames = 4) { for (var i = 0; i < frames; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private void Check(bool passed, string name) { if (!passed) throw new InvalidOperationException(name); checks.Add(name); GD.Print("PASS " + name); }
    private static IEnumerable<Node> Descendants(Node node) { foreach (var child in node.GetChildren()) { yield return child; foreach (var x in Descendants(child)) yield return x; } }
    private T Find<T>(string name) where T : Node =>
        Descendants(this).OfType<T>().FirstOrDefault(x => x.Name == name && (x is not CanvasItem c || c.IsVisibleInTree())) ?? throw new InvalidOperationException("Missing control " + name);
    private async Task Press(string name) { Find<BaseButton>(name).EmitSignal(BaseButton.SignalName.Pressed); await Settle(); }
    private async Task Tap(Key key, bool alt = false, bool ctrl = false)
    {
        Input.ParseInputEvent(new InputEventKey { Keycode = key, Pressed = true, AltPressed = alt, CtrlPressed = ctrl }); await Settle(1);
        Input.ParseInputEvent(new InputEventKey { Keycode = key, Pressed = false, AltPressed = alt, CtrlPressed = ctrl }); await Settle(2);
    }
    private void LayoutCheck(string label)
    {
        var bounds = GetViewportRect();
        var overflow = Descendants(this).OfType<Control>().Where(c => c.IsVisibleInTree() && c.Size.X > 0)
            .FirstOrDefault(c => c.GetGlobalRect().Position.X < -2 || c.GetGlobalRect().End.X > bounds.End.X + 2);
        Check(overflow is null, $"no horizontal overflow: {label}{(overflow is null ? "" : " (" + overflow.Name + ")")}");
        Check(bounds.Encloses(continueButton.GetGlobalRect()), "Continue visible: " + label);
        // The top bar is a single row; content keeps most of the height (S1/S3).
        Check(topBar.Size.Y <= 2.5f * ui.Tokens.ControlHeight && content.Size.Y >= bounds.Size.Y * 0.6f, $"top bar stays one row and content keeps the height: {label} (bar {topBar.Size.Y:0}, content {content.Size.Y:0})");
    }

    private async Task RunHarness()
    {
        try
        {
            await Settle(6);
            if (OS.GetCmdlineUserArgs().Contains("--shell-verify")) await Verify();
            if (Arg("--shell-output") is { } output) await CaptureAll(output);
            GD.Print($"SHELL_PASS {checks.Count}");
            GetTree().Quit();
        }
        catch (Exception e) { GD.PrintErr("SHELL_FAIL " + e); GetTree().Quit(1); }
    }

    private async Task Verify()
    {
        if (session is null)
        {
            Check(Find<Button>("NewCampaign").IsVisibleInTree(), "start screen offers a new campaign");
            Find<LineEdit>("CompanyName").Text = "  ";
            await Press("NewCampaign");
            Check(session is null && Find<Button>("NewCampaign").IsVisibleInTree(), "a campaign does not start without a player-chosen company name");
            Find<LineEdit>("CompanyName").Text = "Verify Signal";
            await Press("NewCampaign");
            Check(view.Company == "Verify Signal", "new campaign uses the player's company name");
        }
        Check(section == ShellSections.Portal && Find<Control>("Screen_portal") is not null, "Portal is the home screen");
        await PortalChecks();
        await PortalCases(null);
        foreach (var (id, _, _) in Sections)
        {
            Navigate(id); await Settle();
            Check(content.GetChildCount() == 1 && navButtons[id].ThemeTypeVariation == "NavSelected", "section opens with rail selection: " + id);
            LayoutCheck(id);
        }
        Navigate(ShellSections.Portal); await Settle();
        var tasks = PortalTasks.Build(view);
        var prepare = tasks.Select((t, i) => (t, i)).First(x => x.t.Section == ShellSections.Competition);
        await Press("Task_" + prepare.i);
        Check(section == ShellSections.Competition, "Portal task routes to the section that resolves it");
        await Press("UseRecommendation"); await Press("CommitPlan");
        Check(view.CommittedPlan is not null, "plan committed from the competition screen");
        var played = view.Results.Length;
        for (var i = 0; i < 15 && view.Results.Length == played; i++) await Press("Continue");
        Check(view.Results.Length == played + 1, "Continue advances to a match result");
        Navigate(ShellSections.Portal); await Settle(6);
        PortalLayoutCheck("after the first result", portal);
        Check(view.Inbox.Any(r => r.Kind == "Match") && message.Visible && message.Text.Length > 2, "result reaches the inbox and the stop reason is shown");
        await Tap(Godot.Key.Key2, alt: true); Check(section == ShellSections.Inbox, "Alt+2 opens the inbox");
        await Tap(Godot.Key.Key3, alt: true); Check(section == ShellSections.Squad, "Alt+3 opens the squad");
        var table = Find<Tree>("SquadTable"); table.GrabFocus(); table.GetRoot().GetChild(0).Select(0); await Settle(1);
        var first = squadSelection; await Tap(Godot.Key.Down);
        Check(squadSelection is not null && squadSelection != first, "keyboard row flipping changes the profile");
        var selectedName = view.People.Single(p => p.Id == squadSelection).Name;
        Check(Descendants(Find<VBoxContainer>("Profile")).OfType<Label>().Any(l => l.Text == selectedName), "profile shows the selected player");
        var revision = view.Revision;
        var commitment = Find<Button>("Release").Disabled ? "Renew" : "Release";
        if (commitment == "Release" || Descendants(this).OfType<Button>().Any(b => b.Name == "Renew"))
        {
            await Press(commitment);
            Check(overlay.Visible && Find<Button>("ConfirmAccept") is not null, "material commitment asks for confirmation");
            await Press("ConfirmCancel");
            Check(!overlay.Visible && view.Revision == revision, "cancelled confirmation changes nothing");
        }
        Navigate(ShellSections.Commercial); await Settle();
        var review = Descendants(this).OfType<Button>().FirstOrDefault(b => b.Name.ToString().StartsWith("Review_", StringComparison.Ordinal) && !b.Disabled);
        if (review is not null)
        {
            review.EmitSignal(BaseButton.SignalName.Pressed); await Settle(6);
            Check(overlay.Visible && overlay.GetNodeOrNull<SponsorWorkspaceView>("OfferWorkspace") is not null, "offer opens the sponsor decision workspace");
            await Tap(Godot.Key.Escape); await Settle(4);
            Check(!overlay.Visible && section == ShellSections.Commercial, "workspace returns to Commercial");
        }
        revision = view.Revision; await Tap(Godot.Key.Enter, ctrl: true);
        Check(view.Revision > revision || section == ShellSections.Competition, "Ctrl+Enter continues or routes to the blocking task");
        await Press("Save"); Check(System.IO.File.Exists(SavePath), "top bar saves the campaign");
        var hash = Composition.Hash(session!); await Press("Load");
        Check(Composition.Hash(session!) == hash, "top bar loads the saved campaign");
        var width = nav.Size.X; await Press("NavToggle"); await Settle();
        Check(nav.Size.X < width, "navigation rail collapses to glyphs"); await Press("NavToggle");
        LayoutCheck("after verification");
    }

    private async Task CaptureAll(string output)
    {
        System.IO.Directory.CreateDirectory(output);
        var size = $"{GetViewportRect().Size.X}x{GetViewportRect().Size.Y}";
        async Task Shot(string key)
        {
            // Resting state: no hover and no keyboard focus ring (focus is verified separately), so the selected styles show.
            Input.WarpMouse(Vector2.Zero); GetViewport().GuiReleaseFocus(); await Settle(8);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using var image = GetViewport().GetTexture().GetImage();
            if (image.SavePng(System.IO.Path.Combine(output, $"{key}-{size}.png")) != Error.Ok) throw new IOException("PNG capture failed");
            GD.Print("CAPTURED " + key);
        }
        // Captures start the way a player does: with a company name they chose (harness input), never the content placeholder.
        if (session is null) { await Shot("start"); StartCampaign(Arg("--shell-company") ?? "", 20261004); }
        if (Arg("--shell-results") is { } results && int.TryParse(results, out var played)) PlayForward(played);
        foreach (var (id, _, _) in Sections) { Navigate(id); await Shot(id); }
        if (view.Offers.FirstOrDefault(o => o.Availability == "Available") is { } offer) { Navigate(ShellSections.Commercial); OpenOffer(offer.Id); await Shot("offer"); CloseOverlay(); }
        Navigate(ShellSections.Portal); await Settle(4);
        if (OS.GetCmdlineUserArgs().Contains("--shell-cases")) await PortalCases(output);
    }
}
