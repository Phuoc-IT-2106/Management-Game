using Godot;
using ManagementGame.SponsorWorkspace;
using ManagementGame.UiKit;
using System.Text.Json;

// Bounded development evidence, following the existing UI Lab/native capture convention.
public partial class CompanyOperatingField
{
    private async Task Settle(int count = UiTokens.CaptureSettleFrames)
    { for (var i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private static IEnumerable<Node> Descendants(Node node)
    {
        foreach (var child in node.GetChildren())
        { yield return child; foreach (var nested in Descendants(child)) yield return nested; }
    }
    private static void Check(bool condition, string name)
    { if (!condition) throw new InvalidOperationException(name); GD.Print("PASS " + name); }
    private void CheckLayout()
    {
        var bounds = GetViewportRect();
        var controls = Descendants(page).OfType<Control>().Where(c => c.IsVisibleInTree()).ToArray();
        Check(controls.All(c => c.GetGlobalRect().Position.X >= -2 && c.GetGlobalRect().End.X <= bounds.End.X + 2), "primary shell has no horizontal overflow");
        foreach (var control in controls.Where(c => c.GetGlobalRect().Position.Y < -2 || c.GetGlobalRect().End.Y > bounds.End.Y + 2))
            GD.Print($"OUTSIDE {control.GetType().Name} {(control is Label l ? l.Text : control is Godot.Button b ? b.Text : control.Name)} {control.GetGlobalRect()} viewport={bounds} root={Size}");
        Check(controls.All(c => c.GetGlobalRect().Position.Y >= -2 && c.GetGlobalRect().End.Y <= bounds.End.Y + 2), "primary content fits viewport vertically");
        Check(Descendants(page).OfType<Label>().Where(l => l.HasMeta("field_text")).All(l => l.GetMinimumSize().Y <= l.Size.Y + 2), "field text fits its allocated height");
        Check(bounds.Encloses(entry.GetGlobalRect()) && bounds.Encloses(targets["timing"].GetGlobalRect()), "decision and Affairs actions remain visible");
        var leaves = controls.Where(c => c is Label or Godot.Button).ToArray();
        for (var i = 0; i < leaves.Length; i++)
            for (var j = i + 1; j < leaves.Length; j++)
                if (leaves[i].GetGlobalRect().Grow(-2).Intersects(leaves[j].GetGlobalRect().Grow(-2)))
                    throw new InvalidOperationException($"Text/action overlap: {leaves[i].Get("text")} {leaves[i].GetGlobalRect()} / {leaves[j].Get("text")} {leaves[j].GetGlobalRect()}");
        GD.Print("PASS primary text/actions do not overlap");
    }
    private async Task Capture(string output)
    {
        Input.WarpMouse(Vector2.Zero); await Settle(); CheckLayout();
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        var viewport = GetViewportRect().Size; var key = $"company-field-{(int)viewport.X}x{(int)viewport.Y}";
        Directory.CreateDirectory(output);
        using var capture = GetViewport().GetTexture().GetImage();
        Check(capture.SavePng(Path.Combine(output, key + ".png")) == Error.Ok, "PNG capture saved");
        File.WriteAllText(Path.Combine(output, key + ".json"), JsonSerializer.Serialize(new
        {
            Prototype = "COS-05 A / DEVELOPMENT / NONCANONICAL", StartingHead = Arg("--field-commit"),
            SourceDigest = Arg("--field-source-digest"), Viewport = new { Width = capture.GetWidth(), Height = capture.GetHeight() },
            StateHash = Composition.Hash(session), snapshot.CampaignId, snapshot.CompanyId, snapshot.Revision, snapshot.Day,
            Offer = new { Offer.OfferId, Offer.Name, Offer.Availability, Offer.Load, Offer.LoadIfAccepted, Offer.ForecastIfAccepted, Offer.Deadline, Offer.ScheduledPayments },
            snapshot.Load, snapshot.Capacity, snapshot.Cash, snapshot.NextCheckpoint,
            PlayerReadBoundary = "SponsorSnapshot + Situation; no hidden World inputs",
            Layout = Wide ? "field + contextual DayTrack and financial evidence" : "field + timing disclosure"
        }, new JsonSerializerOptions { WriteIndented = true }));
    }
    private async Task KeyPress(Key key, bool shift = false)
    {
        Input.ParseInputEvent(new InputEventKey { Keycode = key, Pressed = true, ShiftPressed = shift }); await Settle(2);
        Input.ParseInputEvent(new InputEventKey { Keycode = key, Pressed = false, ShiftPressed = shift }); await Settle(2);
    }
    private async Task Verify()
    {
        CheckLayout(); var hash = Composition.Hash(session);
        targets["matter:" + offerId].GrabFocus(); await KeyPress(Key.Enter);
        Check(navigation.SituationId == "matter:" + offerId, "keyboard selects stable Sponsor matter");
        targets["structure"].EmitSignal(BaseButton.SignalName.Pressed); await Settle();
        var team = Descendants(page).OfType<EntityLabel>().Single(e => e.Binding.Current?.Id == ManagementGame.OperatingMap.MapFixtures.Team);
        team.GrabFocus(); await KeyPress(Key.Enter); await Settle();
        Check(disclosure.Length == 0 && navigation.ScopeId == ManagementGame.OperatingMap.MapFixtures.Team, "structured selection uses the same company scope");
        targets["timing"].EmitSignal(BaseButton.SignalName.Pressed); await Settle(); await KeyPress(Key.Escape);
        Check(disclosure.Length == 0 && targets["timing"].HasFocus(), "timing disclosure closes and restores focus");
        entry.GrabFocus(); await KeyPress(Key.Enter); await Settle();
        Check(workspace is not null && !page.Visible && workspace.Presenter.OfferId == offerId, "native Enter opens existing Sponsor Workspace");
        workspace!.ReviewButton.GrabFocus(); await KeyPress(Key.Space);
        Check(workspace.Presenter.Phase == SponsorPhase.Confirm, "workspace still requires deliberate confirmation");
        await KeyPress(Key.Escape); await KeyPress(Key.Escape); await Settle();
        Check(workspace is null && entry.HasFocus() && navigation.SituationId == "matter:" + offerId, "cancel restores exact company matter and entry focus");
        Check(Composition.Hash(session) == hash, "inspection and cancel do not mutate gameplay");
        // Real in-memory acceptance and refreshed return, using the unchanged presenter/command.
        entry.EmitSignal(BaseButton.SignalName.Pressed); await Settle();
        var cash = snapshot.Cash; var expectedLoad = Offer.LoadIfAccepted;
        workspace!.Act("review"); workspace.Act("commit"); await Settle();
        Check(workspace.Presenter.Phase == SponsorPhase.Accepted, "existing Sponsor command accepts in isolated session");
        workspace.Act("back"); await Settle();
        Check(workspace is null && Offer.Availability == "Signed" && snapshot.Load == expectedLoad && snapshot.Cash == cash,
            "return refreshes signed obligation and load without immediate cash");
        CheckLayout();
        GD.Print("COMPANY_FIELD_VERIFY_PASS");
    }
}
