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
            Check(Find<PanelContainer>("NewCampaignRegion").GetMeta("frame_tier").AsString() == nameof(UiKit.FrameTier.Premium), "the new-campaign form sits in a premium region");
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
            SectionChromeCheck(id);
            RailCheck(id);
        }
        // The league view of Competition (reference 05_league): every team in the derived table, the company's row selected,
        // under the same chrome rules as every section.
        Navigate(ShellSections.Competition); await Settle(); await Press("View_League");
        var standings = Find<Tree>("Standings");
        Check(standings.GetRoot().GetChildCount() == view.Standings.Length && standings.GetSelected()?.GetMetadata(0).AsString() == view.CompanyId,
            "the league view lists every team, with the company's row selected");
        var rivalSquad = Find<Tree>("RivalSquad");
        Check(view.RivalProfiles.Any(p => p.Players.Length == rivalSquad.GetRoot().GetChildCount() && p.Players.All(x => x.RatingEstimate.Contains("(estimate)"))),
            "the league view profiles a rival: its squad as scouted, every rating an estimate");
        LayoutCheck("competition-league"); SectionChromeCheck("competition-league");
        await Press("View_Match");
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
        // The result reaches the player as world news and the inbox, not as narration of the advance.
        Check(view.Inbox.Any(r => r.Kind == "Match") && !(toast.Visible && message.Text.Contains("Advanced to day", StringComparison.Ordinal)) && navCounts[ShellSections.Inbox].Visible
            && Descendants(content).OfType<Label>().Any(l => l.Text == "NEW"), "the result arrives as unread news and inbox, with no advancement narration");
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
            CurrencyTextCheck(overlay, "confirmation");
            Check(Find<PanelContainer>("ConfirmPanel").GetMeta("frame_tier").AsString() == nameof(UiKit.FrameTier.Premium) && Descendants(overlay).OfType<UiKit.HeaderPlate>().Any(),
                "the confirmation is a premium region whose header plate names the commitment");
            await Press("ConfirmCancel");
            Check(!overlay.Visible && view.Revision == revision, "cancelled confirmation changes nothing");
        }
        await SigningStory();
        Navigate(ShellSections.Commercial); await Settle();
        var review = Descendants(this).OfType<Button>().FirstOrDefault(b => b.Name.ToString().StartsWith("Review_", StringComparison.Ordinal) && !b.Disabled);
        if (review is not null)
        {
            review.EmitSignal(BaseButton.SignalName.Pressed); await Settle(6);
            Check(overlay.Visible && overlay.GetNodeOrNull<SponsorWorkspaceView>("OfferWorkspace") is not null, "offer opens the sponsor decision workspace");
            RegionChrome(overlay.GetNode<Control>("OfferWorkspace"), "offer workspace");
            await Tap(Godot.Key.Escape); await Settle(4);
            Check(!overlay.Visible && section == ShellSections.Commercial, "workspace returns to Commercial");
        }
        revision = view.Revision; await Tap(Godot.Key.Enter, ctrl: true);
        Check(view.Revision > revision || section == ShellSections.Competition, "Ctrl+Enter continues or routes to the blocking task");
        var stories = portal.Transfers;
        await Press("Save"); Check(System.IO.File.Exists(SavePath), "top bar saves the campaign");
        var hash = Composition.Hash(session!); await Press("Load");
        Check(Composition.Hash(session!) == hash, "top bar loads the saved campaign");
        Check(!stories.IsEmpty && portal.Transfers.SequenceEqual(stories), "transfer news reads the same after Save and Load: no story lost, added or repeated");
        var width = nav.Size.X; await Press("NavToggle"); await Settle();
        Check(nav.Size.X < width, "navigation rail collapses to its art");
        RailCheck("collapsed");
        // A collapsed item still navigates and takes the selection; expanding restores labels at the same section.
        await Press("Nav_" + ShellSections.Finance);
        Check(section == ShellSections.Finance && navButtons[ShellSections.Finance].ThemeTypeVariation == "NavSelected", "a collapsed rail item navigates and shows the selection");
        LayoutCheck("collapsed rail"); RailCheck("collapsed, Finance");
        await Press("NavToggle"); await Settle();
        Check(Math.Abs(nav.Size.X - width) <= .5f && section == ShellSections.Finance, "the rail expands back to its width at the same section");
        RailCheck("expanded again");
        LayoutCheck("after verification");
    }

    /// <summary>A real signing, made the way a player makes it (Squad profile, Sign, confirm), becomes one confirmed transfer
    /// story from its record; the story opens the person where the squad owns them.</summary>
    private async Task SigningStory()
    {
        var before = portal.Transfers;
        Check(view.Candidates.FirstOrDefault() is not null, "the free-agent market stays operational in Squad");
        var signing = view.Candidates[0];
        OpenTarget(ShellSections.Squad, signing.Id); await Settle();
        Check(Find<Tree>("FreeAgents") is not null && Find<Button>("Sign") is { Disabled: false }, "a free agent opens in the Squad profile with the signing action");
        await Press("Sign"); await Press("ConfirmAccept"); await Settle();
        Check(view.People.Any(p => p.Id == signing.Id), "the signing is committed through the Squad market");
        Navigate(ShellSections.Portal); await Settle(6);
        var stories = portal.Transfers.Where(t => t.PersonId == signing.Id).ToArray();
        Check(stories.Length == 1 && stories[0] is { Kind: MoveKind.Signing, Status: StoryStatus.Confirmed, Source: StorySource.Simulation } story
            && story.From.Standing == Standing.FreeAgent && story.To.OrganizationId == view.CompanyId && story.Headline == $"{signing.Name} signs with {view.Company}"
            && view.Moves.Any(m => m.Id == story.Id && m.Day == view.Day) && portal.Transfers.Length == Math.Min(PortalProjection.TransferStoryLimit, before.Length + 1)
            && Descendants(content).OfType<UiKit.CardButton>().Any(c => c.HasMeta("story_id") && c.GetMeta("story_id").AsString() == story.Id),
            "a real signing appears once in transfer news, confirmed, from its record, as Free agent → the company");
        PortalLayoutCheck("after a signing", portal);
        var row = Descendants(content).OfType<UiKit.CardButton>().First(c => c.HasMeta("story_id") && c.GetMeta("story_id").AsString() == stories[0].Id);
        row.EmitSignal(BaseButton.SignalName.Pressed); await Settle();
        Check(section == ShellSections.Squad && squadSelection == signing.Id, "a transfer story opens its person in the owning section");
        Navigate(ShellSections.Portal); await Settle();
        Check(portal.Transfers.SequenceEqual(PortalProjection.Build(session!.Observe()).Transfers), "opening the Portal again tells the same stories, none regenerated");
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
        Navigate(ShellSections.Competition); await Press("View_League"); await Shot("competition-league"); await Press("View_Match");
        if (view.Offers.FirstOrDefault(o => o.Availability == "Available") is { } offer) { Navigate(ShellSections.Commercial); OpenOffer(offer.Id); await Shot("offer"); CloseOverlay(); }
        // A material commitment's confirmation (hiring a coach opens it without committing anything).
        if (!view.CoachCandidates.IsEmpty) { Navigate(ShellSections.Staff); await Press("HireCoach"); await Shot("confirm"); CloseOverlay(); }
        Navigate(ShellSections.Portal); await Settle(4);
        await RailStates(output);
        if (OS.GetCmdlineUserArgs().Contains("--shell-cases")) await PortalCases(output);
        if (OS.GetCmdlineUserArgs().Contains("--shell-transfers")) await TransferEvidence(output);
    }

    /// <summary>Live transfer-news evidence: plays the real campaign on (the coach's plans, one open sponsor offer taken, a
    /// missing role covered) until contracts end at the season's close, then re-signs a departed player, signs a free
    /// agent, appoints a head coach and releases a player, as a player would. The Portal is captured after the
    /// departures, after the moves and again after Save and Load; every story is a real event of this campaign, and every
    /// story told before the later moves is still told the same way after them.</summary>
    private async Task TransferEvidence(string output)
    {
        // The company's own departures (rivals' contracts end the day before, at the season's close).
        for (var guard = 0; guard < 400 && !view.Moves.Any(m => m.Kind == MoveKind.ContractEnd && m.From == view.CompanyId); guard++)
        {
            if (view.Offers.FirstOrDefault(o => o.Availability == "Available") is { } offer) Send(new SponsorDecision(offer.Id));
            if (view.CommittedPlan is null && view.Recommendation is { } plan && view.NextMatchDay == view.Day)
                Send(new PreparationDecision(plan.FixtureId, plan.Execution, plan.Opponent, plan.Meta, plan.Posture, plan.Lineup));
            else if (!Send(new AdvanceDecision()).Accepted) break;
        }
        Navigate(ShellSections.Portal); await Settle(6);
        Check(portal.Transfers.Any(t => t.Kind == MoveKind.ContractEnd && t.From.OrganizationId == view.CompanyId && t.To.Standing == Standing.FreeAgent),
            "a contract-end departure reaches transfer news as the company → Free agent");
        PortalLayoutCheck("live departures", portal);
        await Snap(output, "portal-transfers-departures");
        var told = PortalProjection.TransferStories(view);
        // One departed player re-signs (a return), one free agent who never played here joins, a head coach is appointed
        // (the previous one is replaced) and a player is released.
        var departures = view.Moves.Where(m => m.Kind == MoveKind.ContractEnd && !m.Coach && m.From == view.CompanyId).ToArray();
        var back = departures.FirstOrDefault(m => view.Candidates.Any(c => c.Id == m.PersonId));
        if (back is not null) Send(new SigningDecision(back.PersonId, 2));
        if (view.Candidates.FirstOrDefault(c => !departures.Any(m => m.PersonId == c.Id)) is { } fresh) Send(new SigningDecision(fresh.Id, 2));
        var replaced = view.Coach;
        if (view.CoachCandidates.FirstOrDefault() is { } coach) Send(new HireCoachDecision(coach.Id));
        var released = view.People.FirstOrDefault(p => p.CanRelease && p.Id != back?.PersonId) is { } cut && Send(new ReleaseDecision(cut.Id)).Accepted ? cut : null;
        Navigate(ShellSections.Portal); await Settle(6);
        var all = PortalProjection.TransferStories(view);
        Check(all.Any(t => t.Kind == MoveKind.Signing) && all.Any(t => t.Kind == MoveKind.Appointment && t.From.Standing == Standing.FreeAgent)
            && all.Any(t => t.Kind == MoveKind.Replacement && t.PersonName == replaced && t.To.Standing == Standing.FreeAgent && t.Summary.StartsWith("Head coach; replaced by ", StringComparison.Ordinal))
            && portal.Transfers[0].Significance == StorySignificance.HeadCoach, "a signing, an appointment and the replaced coach's departure join the feed, head-coach stories leading");
        Check(released is null || all.Any(t => t.Kind == MoveKind.Release && t.PersonId == released.Id && t.From.OrganizationId == view.CompanyId && t.To.Standing == Standing.FreeAgent
            && t.Headline == $"{released.Name} released by {view.Company}"), "a release is told as the company → Free agent, by name");
        Check(back is null || all.SingleOrDefault(t => t.Kind == MoveKind.Signing && t.PersonId == back.PersonId) is { } comeback && comeback.From.Standing == Standing.FreeAgent
            && comeback.Headline == $"{back.Name} re-signs with {view.Company}" && all.Count(t => t.Records.Contains(back.Id)) == 1,
            "a player who left and re-signed has a return story from the free-agent market, and the departure is still told");
        Check(told.All(all.Contains), "every story told before the later moves is still told exactly the same way after them");
        PortalLayoutCheck("live moves", portal);
        await Snap(output, "portal-transfers-moves");
        var stories = portal.Transfers;
        session!.Save("campaign"); session.Load("campaign"); Refresh(); await Settle(6);
        Check(portal.Transfers.SequenceEqual(stories) && PortalProjection.TransferStories(view).SequenceEqual(all), "after Save and Load the same stories, none repeated");
        await Snap(output, "portal-transfers-reloaded");
        Navigate(ShellSections.Squad); await Snap(output, "squad-market-after");
        Navigate(ShellSections.Staff); await Snap(output, "staff-market-after");
        Navigate(ShellSections.Portal); await Settle(4);
    }
}
