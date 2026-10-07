using System.Collections.Immutable;
using Godot;
using ManagementGame.Application;
using ManagementGame.SponsorWorkspace;
using ManagementGame.UiKit;

namespace ManagementGame.Shell;

/// <summary>Native Portal checks. Edge cases are built from the live observation and projection and altered here only:
/// DEVELOPMENT / NONCANONICAL verification fixtures, never shown in play and never written to a campaign. The top bar
/// and the Portal always render the same case model, exactly as in play.</summary>
public partial class GameShell
{
    private static readonly string[] PortalRegions = ["Portal_Header", "Portal_Decisions", "Portal_News", "Portal_NextMatch", "Portal_Record", "Portal_Horizon", "Portal_Money"];

    /// <summary>Every Portal region is fully inside the content area (no scrolling at this viewport) and no
    /// single-line label is clipped.</summary>
    /// <param name="mustFit">Normal states must fit the first viewport; extreme-name cases may scroll vertically but must keep
    /// the header, decisions and next match in the first viewport and never overflow horizontally.</param>
    private void PortalLayoutCheck(string label, PortalView model, bool mustFit = true)
    {
        var area = content.GetGlobalRect().Grow(1);
        foreach (var name in PortalRegions)
        {
            var region = Descendants(content).OfType<Control>().FirstOrDefault(c => c.Name == name && c.IsVisibleInTree())
                ?? throw new InvalidOperationException($"Portal region missing: {name} ({label})");
            var rect = region.GetGlobalRect();
            if (mustFit || name is "Portal_Header" or "Portal_Decisions")
                Check(area.Encloses(rect), $"portal region visible without scrolling: {name} ({label}, {rect.End.Y:0} of {area.End.Y:0})");
            else if (name == "Portal_NextMatch") Check(rect.Position.Y < area.End.Y && rect.Position.X >= area.Position.X && rect.End.X <= area.End.X, $"next match starts in the first viewport ({label})");
            else Check(rect.Position.X >= area.Position.X && rect.End.X <= area.End.X, $"portal region stays within the width: {name} ({label})");
        }
        var clipped = Descendants(content).OfType<Label>().Where(l => l.IsVisibleInTree() && l.AutowrapMode == TextServer.AutowrapMode.Off
            && l.TextOverrunBehavior == TextServer.OverrunBehavior.NoTrimming && l.Text.Length > 0)
            .FirstOrDefault(l => l.GetMinimumSize().X > l.Size.X + 1 || (mustFit && !area.Encloses(l.GetGlobalRect())));
        Check(clipped is null, $"no clipped single-line portal text ({label}{(clipped is null ? "" : ": " + clipped.Text)})");
        if (mustFit) Check(Descendants(content).OfType<ScrollContainer>().First().GetVScrollBar() is { } bar && (!bar.Visible || bar.MaxValue - bar.Page <= 1),
            $"the whole Portal fits the first viewport without a scrollbar ({label})");
        var small = Descendants(content).OfType<Label>().FirstOrDefault(l => l.IsVisibleInTree() && l.Text.Length > 0 && l.GetThemeFontSize("font_size") < ui.Tokens.FontSize(TypographyRole.Caption));
        Check(small is null, $"no portal text below the 12 px caption step ({label}{(small is null ? "" : ": " + small.Text)})");
        var marks = Descendants(this).OfType<AssetMark>().Where(m => m.IsVisibleInTree()).ToArray();
        Check(marks.All(m => m.Resolution.Length > 0 && m.Size.X > 0), $"every identity mark resolves to an asset or a neutral fallback ({label})");
        var top = Descendants(topBar).OfType<Control>().Where(c => c.IsVisibleInTree() && c.Size.X > 0).ToArray();
        Check(top.All(c => topBar.GetGlobalRect().Grow(2).Encloses(c.GetGlobalRect())), $"top bar contents stay in one row ({label})");
        // One authoritative pending-decision total: the top bar badge and the Portal header make the same claim.
        var badge = Find<Label>("TopBarDecisionCount").Text;
        var header = Descendants(content).OfType<Label>().FirstOrDefault(l => l.Name == "PortalDecisionCount");
        Check(badge.EndsWith(model.Decisions.Length.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)
            && (model.Decisions.IsEmpty ? header is null : header?.Text.EndsWith(PortalScreen.PendingText(model.Decisions.Length), StringComparison.Ordinal) == true),
            $"top bar and Portal state the same pending-decision total ({label}: {badge} / {header?.Text ?? "none"})");
        Check(Find<Label>("CompanyName").Text == model.Company.Name.ToUpperInvariant(), $"top bar shows the company's display name from the model ({label})");
        // The development marker is debug-only and lives in the rail, never over gameplay content.
        if (Descendants(this).OfType<Control>().FirstOrDefault(c => c.Name == "DevelopmentMarker" && c.IsVisibleInTree()) is { } marker)
            Check(!marker.GetGlobalRect().Intersects(content.GetGlobalRect()) && nav.GetGlobalRect().Grow(1).Encloses(marker.GetGlobalRect()),
                $"development marker stays in the rail, outside the content ({label})");
        else Check(!OS.IsDebugBuild(), $"debug builds show the development marker ({label})");
    }

    private async Task PortalChecks()
    {
        Navigate(ShellSections.Portal); await Settle(6);
        var model = portal;
        var roomy = ShellLayout.Roomy(GetViewportRect().Size);
        PortalLayoutCheck("live", model);
        Check(Descendants(topBar).OfType<Label>().Any(l => l.Text == PortalScreen.Cu(view.Cash)), "top bar binds cash from the observation");
        Check(Descendants(this).OfType<AssetMark>().Any(m => m.AssetId == AssetIds.Crest(view.CompanyId) && m.Resolution == m.AssetId), "company crest resolves by stable company ID");
        Check(model.NextMatch is null || Descendants(this).OfType<AssetMark>().Any(m => m.AssetId == AssetIds.Crest(view.OpponentId)), "opponent crest resolves by stable rival ID");
        Check(Descendants(content).OfType<Control>().First(c => c.Name == "PortalEnvironment").GetMeta("asset_resolution").AsString() == IdentityAssets.DefaultEnvironment,
            "hero atmosphere resolves through the asset catalog to the procedural fallback (no bundled art)");
        Check(Descendants(content).OfType<CardButton>().Count(c => c.Name.ToString().StartsWith("Task_", StringComparison.Ordinal)) == Math.Min(model.Decisions.Length, ShellLayout.DecisionCards(roomy)),
            "decision cards are bounded and bound to the typed decision list");
        // News: subject art by asset ID; the newest story leads with a wide art panel when there is room.
        var newsArt = Descendants(content).OfType<AssetMark>().Where(m => m.Name.ToString().StartsWith("NewsImage_", StringComparison.Ordinal)).ToArray();
        Check(newsArt.Length == Math.Min(model.News.Length, ShellLayout.NewsItems(roomy)) && newsArt.All(a => model.News.Any(n => n.ImageAssetId == a.AssetId)),
            "every news row shows its subject's catalog image");
        if (roomy && newsArt.Length > 0)
            Check(newsArt[0].Size.X >= Find<Control>("Portal_News").Size.X * .8f, "the newest story leads with a wide art panel on a roomy viewport");
        // Operations: structured events with a typed kind, never raw sentences.
        var events = Descendants(content).OfType<CardButton>().Where(c => c.Name.ToString().StartsWith("Event_", StringComparison.Ordinal)).ToArray();
        Check(events.Length > 0 && events.All(e => Enum.TryParse<HorizonKind>(e.GetMeta("kind").AsString(), out _))
            && events.All(e => !Descendants(e).OfType<Label>().Any(l => model.Horizon.SelectMany(w => w.Events).Any(x => x.Kind != HorizonKind.Match && l.Text == x.Text))),
            "four-week events render as typed category, title and day, not raw event sentences");
        // Routes: by section plus stable ID.
        Check(model.NextMatch is null || Find<CardButton>("PortalMatchOpen").ThemeTypeVariation == (model.NextMatch.Plan == PlanState.Missing ? "SecondaryAction" : "AccentOutline"),
            "match preparation is a strong secondary action while no plan exists");
        await Press("PortalMatchOpen"); Check(section == ShellSections.Competition, "Next match routes to Competition"); Navigate(ShellSections.Portal); await Settle();
        await Press("PortalMoneyOpen"); Check(section == ShellSections.Finance, "Money routes to Finance"); Navigate(ShellSections.Portal); await Settle();
        await Press("PortalNewsAll"); Check(section == ShellSections.Inbox, "View all routes to the Inbox"); Navigate(ShellSections.Portal); await Settle();
        if (Descendants(content).OfType<CardButton>().FirstOrDefault(c => c.Name == "News_0") is { } news)
        {
            var target = news.GetMeta("section").AsString(); var id = news.GetMeta("target_id").AsString();
            news.EmitSignal(BaseButton.SignalName.Pressed); await Settle();
            Check(section == target && seenInbox.Contains(id), "news item routes to its owning section and is marked read");
            Navigate(ShellSections.Portal); await Settle();
        }
        // The screen was rebuilt by the routes above: look the event up again.
        if (Descendants(content).OfType<CardButton>().FirstOrDefault(c => c.Name.ToString().StartsWith("Event_", StringComparison.Ordinal)) is { } first)
        {
            var target = first.GetMeta("section").AsString();
            first.EmitSignal(BaseButton.SignalName.Pressed); await Settle();
            Check(section == target, "a four-week event routes to its owning section"); Navigate(ShellSections.Portal); await Settle();
        }
        if (Descendants(content).OfType<CardButton>().FirstOrDefault(c => c.Name.ToString().StartsWith("Task_", StringComparison.Ordinal) && c.GetMeta("section").AsString() == ShellSections.Commercial) is { } offer)
        {
            var id = offer.GetMeta("target_id").AsString();
            offer.EmitSignal(BaseButton.SignalName.Pressed); await Settle(6);
            Check(section == ShellSections.Commercial && overlay.Visible && overlay.GetNodeOrNull<SponsorWorkspaceView>("OfferWorkspace") is not null, $"offer decision opens its sponsor workspace ({id})");
            await Tap(Key.Escape); await Settle(4);
            Navigate(ShellSections.Portal); await Settle();
        }
        // Keyboard: cards, links and Continue take focus; Alt shortcuts still leave the Portal.
        var card = Find<CardButton>("Task_0"); card.GrabFocus(); await Settle(1);
        Check(card.HasFocus() && card.FocusMode == FocusModeEnum.All, "decision card takes keyboard focus");
        Check(Descendants(content).OfType<BaseButton>().Where(b => b.IsVisibleInTree()).All(b => b.FocusMode == FocusModeEnum.All) && continueButton.FocusMode == FocusModeEnum.All,
            "every Portal action and Continue are keyboard reachable");
        await Tap(Key.Key3, alt: true); Check(section == ShellSections.Squad, "Alt+3 leaves the Portal (no focus trap)");
        Navigate(ShellSections.Portal); await Settle();
    }

    /// <summary>Renders edge cases through the production top bar and Portal builders and checks the same layout rules.</summary>
    private async Task PortalCases(string? output)
    {
        var liveView = view;
        var live = portal;
        var roomy = ShellLayout.Roomy(GetViewportRect().Size);
        var many = Enumerable.Range(0, 7).Select(i => live.Decisions.IsEmpty
            ? new PortalDecision("dev:" + i, DecisionCategory.Commercial, "DEV decision " + i, "Development fixture", TaskUrgency.Due, live.Time.Day + i, ShellSections.Commercial, "")
            : live.Decisions[i % live.Decisions.Length] with { Id = live.Decisions[i % live.Decisions.Length].Id + ":dev" + i }).ToImmutableArray();
        const string longCompany = "DEV Northern Lights Interactive Entertainment Esports Holdings";
        const string longRival = "DEV Extraordinarily Long Rival Organization Name";
        PortalView From(Situation s) => PortalProjection.Build(s);
        var distress = liveView with { Status = "Distress" };
        var warning = liveView with { Status = "Warning" };
        var cases = new (string Name, PortalView Model)[]
        {
            ("long-names", From(liveView with { Company = longCompany, Opponent = longRival })),
            ("no-decisions", live with { Decisions = [] }),
            ("one-decision", live with { Decisions = live.Decisions.IsEmpty ? [] : [live.Decisions[0]] }),
            ("many-decisions", live with { Decisions = many }),
            ("no-match", live with { NextMatch = null, Time = live.Time with { Phase = SeasonPhase.Complete } }),
            ("empty-news", live with { News = [] }),
            ("many-news", live.News.IsEmpty ? live : live with { News = [.. Enumerable.Range(0, PortalProjection.NewsLimit)
                .Select(i => live.News[i % live.News.Length] with { Id = live.News[i % live.News.Length].Id + ":dev" + i })] }),
            ("warning", From(warning) with { Finance = From(warning).Finance with { Forecast = live.Finance.Cash - 400_00 } }),
            ("distress", From(distress) with { Finance = From(distress).Finance with { Arrears = 125_00, Forecast = live.Finance.Cash - 900_00 } }),
            ("missing-crests", live with { Company = live.Company with { CrestAssetId = "crest:" },
                NextMatch = live.NextMatch is null ? null : live.NextMatch with { Us = live.NextMatch.Us with { CrestAssetId = "crest:" }, Opponent = live.NextMatch.Opponent with { CrestAssetId = "crest:" } },
                News = [.. live.News.Select(n => n with { ImageAssetId = n.ImageAssetId.StartsWith("crest:", StringComparison.Ordinal) ? "crest:" : AssetIds.NewsFallback(n.Category) })] })
        };
        foreach (var (name, model) in cases)
        {
            BuildTopBar(model);
            ClearChildren(content);
            var screen = PortalScreen.Create(ui, assets, model, roomy, new PortalScreen.Routes((_, _) => { }, _ => false, () => { }, false));
            screen.Name = "Screen_portal"; content.AddChild(screen); screen.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            await Settle(8);
            PortalLayoutCheck("case " + name, model, name != "long-names");
            if (name == "missing-crests")
            {
                Check(Descendants(content).OfType<AssetMark>().Where(m => m.AssetId == "crest:").All(m => m.Resolution == IdentityAssets.DefaultCrest), "missing crests fall back to the neutral default crest");
                Check(Descendants(content).OfType<AssetMark>().Where(m => m.Name.ToString().StartsWith("NewsImage_", StringComparison.Ordinal)).All(m => m.Resolution is IdentityAssets.DefaultNews or IdentityAssets.DefaultCrest),
                    "news without specific art uses the deterministic category fallback");
            }
            if (name == "no-decisions") Check(Descendants(content).OfType<Label>().Any(l => l.Text.StartsWith("Nothing currently requires", StringComparison.Ordinal))
                && Find<Label>("TopBarDecisionCount").Text == "0", "zero decisions shows the calm empty state and a zero badge");
            if (name == "one-decision" && !live.Decisions.IsEmpty) Check(Descendants(content).OfType<Label>().Any(l => l.Text == "1 pending decision"), "one decision uses the singular wording");
            if (name == "no-match") Check(Descendants(content).OfType<Label>().Any(l => l.Text.StartsWith("No upcoming match", StringComparison.Ordinal)), "no match shows an empty state, no invented opponent");
            if (name == "empty-news") Check(Descendants(content).OfType<Label>().Any(l => l.Text == "No recent organization news."), "empty news shows an empty state, no invented stories");
            if (name == "many-decisions") Check(Descendants(content).OfType<CardButton>().Count(c => c.Name.ToString().StartsWith("Task_", StringComparison.Ordinal)) == ShellLayout.DecisionCards(roomy)
                && Descendants(content).Any(c => c.Name == "PortalDecisionsMore"), "many decisions stay bounded with a route to the rest");
            if (name == "distress")
                Check(Find<CardButton>("Task_0").GetMeta("decision_id").AsString() == "distress" && Descendants(topBar).OfType<Label>().Any(l => l.Text.EndsWith("Distress", StringComparison.Ordinal))
                    && Descendants(content).OfType<Label>().Any(l => l.Text.Contains("Arrears", StringComparison.Ordinal)), "distress leads the decisions, the top bar and the money snapshot");
            if (name == "warning") Check(Descendants(topBar).OfType<Label>().Any(l => l.Text == PresentationText.WarningMarker + " Warning"), "warning condition shows its marker and wording");
            if (output is not null) await Snap(output, "portal-case-" + name);
        }
        view = liveView; Refresh();
    }

    private async Task Snap(string output, string key)
    {
        Input.WarpMouse(Vector2.Zero); GetViewport().GuiReleaseFocus(); await Settle(8);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        using var image = GetViewport().GetTexture().GetImage();
        var size = $"{GetViewportRect().Size.X}x{GetViewportRect().Size.Y}";
        if (image.SavePng(System.IO.Path.Combine(output, $"{key}-{size}.png")) != Error.Ok) throw new IOException("PNG capture failed");
        GD.Print("CAPTURED " + key);
    }

    /// <summary>Plays the real campaign forward with the coach's recommended plan until the given number of results,
    /// so evidence shows a lived-in state (results, news, record) rather than day one.</summary>
    private void PlayForward(int results)
    {
        for (var guard = 0; guard < 60 && view.Results.Length < results; guard++)
        {
            if (view.CommittedPlan is null && view.Recommendation is { } plan && view.NextMatchDay == view.Day)
                Send(new PreparationDecision(plan.FixtureId, plan.Execution, plan.Opponent, plan.Meta, plan.Posture, plan.Lineup));
            else Send(new AdvanceDecision());
        }
    }
}
