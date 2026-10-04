using Godot;
using ManagementGame.UiKit;
using ManagementGame.OperatingMap;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

public partial class OperatingMap
{
    private readonly List<string> checks = [];
    private int retainedNodesBefore, retainedNodesAfter;
    private void Check(bool passed, string name)
    { if (!passed) throw new InvalidOperationException(name); checks.Add(name); GD.Print("PASS " + name); }
    private async Task Settle() { for (var i = 0; i < UiTokens.CaptureSettleFrames; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private static IEnumerable<Node> Descendants(Node node)
    { foreach (var child in node.GetChildren()) { yield return child; foreach (var nested in Descendants(child)) yield return nested; } }
    private async Task KeyPress(Key key, bool shift = false)
    {
        Input.ParseInputEvent(new InputEventKey { Keycode = key, ShiftPressed = shift, Pressed = true });
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Input.ParseInputEvent(new InputEventKey { Keycode = key, ShiftPressed = shift, Pressed = false });
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
    private async Task VerifyNative()
    {
        navigation = new(MapFixtures.Create()); Build(); await Settle();
        var clickPoint = targets["map:" + MapFixtures.Portfolio].GetGlobalRect().GetCenter();
        GetViewport().PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Position = clickPoint, GlobalPosition = clickPoint, Pressed = true }, true);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetViewport().PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Position = clickPoint, GlobalPosition = clickPoint, Pressed = false }, true);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Check(navigation.ScopeId == MapFixtures.Portfolio, "synthetic pointer click inspects portfolio");
        var company = targets["map:" + MapFixtures.Company]; company.GrabFocus();
        await KeyPress(Key.Down);
        Check(targets["map:" + MapFixtures.Portfolio].HasFocus(), "synthetic Down moves map focus");
        await KeyPress(Key.Enter);
        Check(navigation.ScopeId == MapFixtures.Portfolio, "synthetic Enter inspects focused scope");
        await KeyPress(Key.Tab); Check(!targets["map:" + MapFixtures.Portfolio].HasFocus(), "synthetic Tab traverses");
        await KeyPress(Key.Tab, true); Check(targets["map:" + MapFixtures.Portfolio].HasFocus(), "synthetic Shift+Tab returns focus");
        targets["map:matter:sponsor"].GrabFocus(); await KeyPress(Key.Space);
        Check(navigation.SituationId == "matter:sponsor" && navigation.ScopeId == MapFixtures.Business, "synthetic Space selects sponsor and affected scope");
        Check(targets["map:matter:sponsor"].Binding.Selected && targets["list:matter:sponsor"].Binding.Selected, "map/list selection synchronized by stable ID");
        ToggleMode(); await Settle(); Check(targets["list:matter:sponsor"].HasFocus(), "mode change restores same situation focus");
        Check(mapScroll.GetGlobalRect().Encloses(targets["list:matter:sponsor"].GetGlobalRect()), $"selected list situation is visible: region={mapScroll.GetGlobalRect()} target={targets["list:matter:sponsor"].GetGlobalRect()} scroll={mapScroll.ScrollVertical}");
        openEntry.GrabFocus(); await KeyPress(Key.Enter); await Settle();
        Check(isWorkspace && navigation.Return?.SituationId == "matter:sponsor" && navigation.Return.ListMode, "sponsor keyboard entry retains origin and mode");
        await KeyPress(Key.Escape); await Settle();
        Check(!isWorkspace && navigation.SituationId == "matter:sponsor" && targets["list:matter:sponsor"].HasFocus(), "Escape restores sponsor list origin and focus");
        ToggleMode(); targets["map:matter:preparation"].EmitSignal(BaseButton.SignalName.Pressed);
        Enter(); Check(navigation.Situation?.WorkspaceKind == "Preparation review", "secondary non-commercial entry"); Back();
        Check(navigation.ScopeId == MapFixtures.Team && navigation.SituationId == "matter:preparation", "secondary return preserves team and situation");
        affairs.Visible = true; targets["affairs:matter:obligation"].EmitSignal(BaseButton.SignalName.Pressed);
        Check(navigation.ScopeId == MapFixtures.Business && navigation.SituationId == "matter:obligation", "affair selects owning map context");
        Enter(); Back(); Check(targets["affairs:matter:obligation"].HasFocus(), "affair entry return restores affair focus"); affairs.Visible = false;
        targets["map:matter:sponsor"].EmitSignal(BaseButton.SignalName.Pressed); await Settle();
        retainedNodesBefore = Descendants(this).Count(); var calls = intentCount;
        // Warmup separated from the timed repeated native workflow.
        for (var i = 0; i < 5; i++) { Select("matter:sponsor"); Enter(); Back(); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
        var initialBind = timings["initial-bind"].ToList(); timings.Clear(); timings["initial-bind"] = initialBind;
        for (var i = 0; i < 50; i++)
        {
            var start = Stopwatch.GetTimestamp();
            targets["map:matter:sponsor"].EmitSignal(BaseButton.SignalName.Pressed);
            ToggleMode(); ToggleMode(); Enter(); Back();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Measure("navigation-work-plus-frame", start);
        }
        await Settle(); retainedNodesAfter = Descendants(this).Count();
        Check(retainedNodesBefore == retainedNodesAfter, "50 selection/mode/entry/return cycles retain constant nodes");
        Check(intentCount - calls == 50, "one callback per activation after repeated navigation");
        Check(targets["map:matter:sponsor"].GetThemeStylebox("focus") is StyleBoxFlat s && s.BorderWidthLeft >= UiTokens.FocusWidth, "canonical visible focus remains separate from selection");
        inspectorScroll.GrabFocus(); await KeyPress(Key.End);
        Check(inspectorScroll.ScrollVertical > 0 || inspector.Size.Y <= inspectorScroll.Size.Y, "keyboard End reaches inspector evidence");
        Select(Data.Organization.Id); targets["inspector:matter:sponsor"].EmitSignal(BaseButton.SignalName.Pressed);
        Check(openEntry.HasFocus(), "inspector-origin selection moves focus to decision entry");
        Enter(); Back(); Check(openEntry.HasFocus(), "inspector-origin return restores contextual action focus");
        foreach (var key in MapFixtures.Cases)
        {
            scenario = key; ApplyScenario(); await Settle(); VerifyLayout();
            Check(true, "no horizontal overflow: " + key);
        }
        navigation = new(MapFixtures.Create()); navigation.Select("matter:preparation", 7); navigation.Enter("entry:preparation", 7, "map:matter:preparation", 0);
        navigation.Replace(Data with { Revision = 8, Scopes = [.. Data.Scopes.Where(x => x.Id != MapFixtures.Team)],
            Situations = [.. Data.Situations.Where(x => x.ScopeId != MapFixtures.Team)], Links = [.. Data.Links.Where(x => x.To != MapFixtures.Team)] });
        Build(); await Settle();
        Check(navigation.ScopeId == MapFixtures.Discipline && !isWorkspace && navigation.SituationId is null && notice.Visible, "deleted scope clears entry and explains nearest parent fallback");
        Check(Descendants(inspector).OfType<Button>().All(x => x.Text != "Open decision entry"), "no stale decision control after replacement");
        scenario = Arg("--map-case") ?? "normal";
        GD.Print("OPERATING_MAP_NATIVE_PASS " + checks.Count);
    }
    private void VerifyLayout()
    {
        var viewport = GetViewportRect();
        foreach (var control in Descendants(layout).OfType<Control>().Where(c => c.IsVisibleInTree() && c is Label or Button or PanelContainer))
        {
            var rect = control.GetGlobalRect();
            if (rect.Position.X < -UiTokens.LayoutTolerance || rect.End.X > viewport.Size.X + UiTokens.LayoutTolerance)
                throw new InvalidOperationException($"Horizontal overflow {control.GetPath()} {rect} / {viewport}");
        }
        if (mapScroll.Size.Y < ui.Tokens.ControlHeight || inspectorScroll.Size.Y < ui.Tokens.ControlHeight)
            throw new InvalidOperationException("Reading regions no longer reachable.");
    }
    private void Capture()
    {
        var output = Arg("--map-output")!; Directory.CreateDirectory(output);
        using var pixels = GetViewport().GetTexture().GetImage();
        var key = $"{scenario}-{pixels.GetWidth()}x{pixels.GetHeight()}";
        var imagePath = Path.Combine(output, key + ".png");
        if (pixels.SavePng(imagePath) != Error.Ok) throw new IOException("Capture write failed.");
        var manifest = new
        {
            Status = "GOLDEN CANDIDATE — NOT APPROVED", Case = scenario,
            Commit = Arg("--map-commit") ?? "unrecorded", SourceDigest = Arg("--map-source-digest") ?? "unrecorded", Dirty = true,
            FixtureVersion = MapFixtures.Version, FixtureDigest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(Data)))).ToLowerInvariant(),
            Viewport = new { Width = pixels.GetWidth(), Height = pixels.GetHeight() }, NativeWindow = new { Width = GetWindow().Size.X, Height = GetWindow().Size.Y },
            CompanyId = Data.Organization.Id, Data.SessionKey, Data.Revision, Data.Day, Seed = "none; presentation-only", Locale = System.Globalization.CultureInfo.CurrentCulture.Name,
            navigation.ScopeId, navigation.SituationId, navigation.ListMode, navigation.Return, OriginFocus = originFocus, Workspace = isWorkspace,
            ThemeVersion = UiTokens.Version, Density = ui.Tokens.Density.ToString(), TextScale = ui.Tokens.TextScale, ReducedMotion = true,
            Engine = Engine.GetVersionInfo()["string"].AsString(), Runtime = System.Environment.Version.ToString(), OS = OS.GetName(),
            Renderer = RenderingServer.GetCurrentRenderingMethod(), Font = ui.Theme.DefaultFont.GetFontName(),
            CaptureTrigger = "explicit output; settled process frames; frame_post_draw", SettleFrames = UiTokens.CaptureSettleFrames,
            PhysicalInputVerified = false, DpiGatePassed = false, Checks = checks,
            ControlCount = Descendants(this).OfType<Control>().Count(), RetainedNodesBefore = retainedNodesBefore, RetainedNodesAfter = retainedNodesAfter,
            Timings = timings.ToDictionary(x => x.Key, x => new { Samples = x.Value.Count, P95Ms = x.Value.Order().ElementAt((int)Math.Ceiling(x.Value.Count * .95) - 1), RawMs = x.Value }),
            Image = Path.GetFileName(imagePath), ImageSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(imagePath))).ToLowerInvariant()
        };
        File.WriteAllText(Path.Combine(output, key + ".json"), JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
        GD.Print("OPERATING_MAP_CAPTURE " + key);
    }
}
