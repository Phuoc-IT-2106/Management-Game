using System.Collections.Immutable;
using Godot;
using ManagementGame.Application;
using ManagementGame.Formatting;
using ManagementGame.SponsorWorkspace;
using ManagementGame.UiKit;

namespace ManagementGame.Shell;

/// <summary>Native Portal checks. Edge cases are built from the live observation and projection and altered here only:
/// DEVELOPMENT / NONCANONICAL verification fixtures, never shown in play and never written to a campaign. The top bar
/// and the Portal always render the same case model, exactly as in play.</summary>
public partial class GameShell
{
    private static readonly string[] PortalRegions = ["Portal_Decisions", "Portal_News", "Portal_Transfers", "Portal_NextMatch", "Portal_Record", "Portal_Money"];

    /// <summary>Every Portal region is fully inside the content area (no scrolling at this viewport) and no
    /// single-line label is clipped.</summary>
    /// <param name="mustFit">Normal states must fit the first viewport; extreme-name cases may scroll vertically but must keep
    /// the decisions and next match in the first viewport and never overflow horizontally.</param>
    private void PortalLayoutCheck(string label, PortalView model, bool mustFit = true)
    {
        var area = content.GetGlobalRect().Grow(1);
        foreach (var name in PortalRegions)
        {
            var region = Descendants(content).OfType<Control>().FirstOrDefault(c => c.Name == name && c.IsVisibleInTree())
                ?? throw new InvalidOperationException($"Portal region missing: {name} ({label})");
            var rect = region.GetGlobalRect();
            if (mustFit || name is "Portal_Decisions")
                Check(area.Encloses(rect), $"portal region visible without scrolling: {name} ({label}, {rect.End.Y:0} of {area.End.Y:0})");
            else Check(rect.Position.X >= area.Position.X && rect.End.X <= area.End.X, $"portal region stays within the width: {name} ({label})");
        }
        Check(Descendants(content).All(c => c.Name != "Portal_Horizon"), $"four-week operations are no longer part of the Portal ({label})");
        var clipped = Descendants(content).OfType<Label>().Where(l => l.IsVisibleInTree() && l.AutowrapMode == TextServer.AutowrapMode.Off
            && l.TextOverrunBehavior == TextServer.OverrunBehavior.NoTrimming && l.Text.Length > 0)
            .FirstOrDefault(l => l.GetMinimumSize().X > l.Size.X + 1 || (mustFit && !area.Encloses(l.GetGlobalRect())));
        Check(clipped is null, $"no clipped single-line portal text ({label}{(clipped is null ? "" : ": " + clipped.Text)})");
        if (mustFit && Descendants(content).OfType<ScrollContainer>().First().GetVScrollBar() is { } bar)
            Check(!bar.Visible || bar.MaxValue - bar.Page <= 1, $"the whole Portal fits the first viewport without a scrollbar ({label}, overflow {Math.Max(0, bar.MaxValue - bar.Page):0} px)");
        var small = Descendants(content).OfType<Label>().FirstOrDefault(l => l.IsVisibleInTree() && l.Text.Length > 0 && l.GetThemeFontSize("font_size") < ui.Tokens.FontSize(TypographyRole.Caption));
        Check(small is null, $"no portal text below the 12 px caption step ({label}{(small is null ? "" : ": " + small.Text)})");
        var marks = Descendants(this).OfType<AssetMark>().Where(m => m.IsVisibleInTree()).ToArray();
        Check(marks.All(m => m.Resolution.Length > 0 && m.Size.X > 0), $"every identity mark resolves to an asset or a neutral fallback ({label})");
        SemanticArtCheck(label);
        var top = Descendants(topBar).OfType<Control>().Where(c => c.IsVisibleInTree() && c.Size.X > 0).ToArray();
        Check(top.All(c => topBar.GetGlobalRect().Grow(2).Encloses(c.GetGlobalRect())), $"top bar contents stay in one row ({label})");
        CurrencyTextCheck(topBar, "top bar, " + label); CurrencyTextCheck(nav, "rail, " + label);
        // One authoritative pending-decision total: the top bar badge and the Portal header make the same claim.
        var badge = Find<Label>("TopBarDecisionCount").Text;
        var header = Descendants(content).OfType<Label>().FirstOrDefault(l => l.Name == "PortalDecisionCount");
        Check(badge == Figures.Capped(model.Decisions.Length)
            && (model.Decisions.IsEmpty ? header is null : header?.Text.EndsWith(PortalScreen.PendingText(model.Decisions.Length), StringComparison.Ordinal) == true),
            $"top bar and Portal state the same pending-decision total ({label}: {badge} / {header?.Text ?? "none"})");
        Check(Find<Label>("CompanyName").Text == model.Company.Name.ToUpperInvariant() && nav.GetGlobalRect().Grow(1).Encloses(Find<Label>("CompanyName").GetGlobalRect()),
            $"the rail heads with the company's display name from the model ({label})");
        ChromeCheck(label);
        PortalRedesignCheck(label, model);
        TransferNewsCheck(label, model, mustFit);
    }

    /// <summary>Transfer news is an editorial feed of the model's stories, not a market: a few stories in the model's order,
    /// all with one treatment; each with its certainty and kind, a headline that leads with the person's whole name, and
    /// its journey (origin → destination, in that order, each side the model's organization, the free-agent status or an
    /// explicit unknown); people drawn bare, never on a box; and nothing of the market (filters, a market route, fees,
    /// wages, ability or availability).</summary>
    private void TransferNewsCheck(string label, PortalView model, bool mustFit)
    {
        var roomy = ShellLayout.Roomy(GetViewportRect().Size);
        var panel = Descendants(content).OfType<Control>().First(c => c.Name == "Portal_Transfers");
        var rows = Descendants(panel).OfType<CardButton>().Where(c => c.Name.ToString().StartsWith("Transfer_", StringComparison.Ordinal)).ToArray();
        Check(rows.Length == Math.Min(model.Transfers.Length, ShellLayout.TransferItems(roomy))
            && rows.Select((r, i) => (r, i)).All(x => x.r.GetMeta("story_id").AsString() == model.Transfers[x.i].Id && x.r.GetMeta("target_id").AsString() == model.Transfers[x.i].TargetId),
            $"transfer news shows the model's stories in order, a few at most ({label})");
        Check(model.Transfers.IsEmpty == Descendants(panel).OfType<Label>().Any(l => l.Text == "No major transfer developments to report."),
            $"transfer news without a story says so, and shows nothing invented ({label})");
        Check(rows.All(r => Descendants(r).OfType<Label>().Any(l => l.Text == PortalScreen.StatusLabel(Enum.Parse<StoryStatus>(r.GetMeta("status").AsString())).Text))
            && rows.Select((r, i) => (r, i)).All(x => Descendants(x.r).OfType<Label>().Single(l => l.Name == "StoryKind").Text == PortalScreen.KindLabel(model.Transfers[x.i]).ToUpperInvariant()),
            $"every transfer story carries its certainty label and its kind of move, player or staff ({label})");
        // The person's name is the headline's prefix, so it is whole when it wraps into no more lines than the headline shows.
        static bool NameWhole(Label h, string name)
        {
            var font = h.GetThemeFont("font"); var size = h.GetThemeFontSize("font_size");
            return font.GetMultilineStringSize(name, HorizontalAlignment.Left, h.Size.X, size).Y <= h.MaxLinesVisible * font.GetHeight(size) + 1;
        }
        static bool NameFits(Label h, string name)
        {
            var font = h.GetThemeFont("font"); var size = h.GetThemeFontSize("font_size");
            return font.GetMultilineStringSize(name, HorizontalAlignment.Left, h.Size.X, size).Y <= font.GetHeight(size) + 1;
        }
        Check(rows.Select((r, i) => (r, i)).All(x => Descendants(x.r).OfType<Label>().Single(l => l.Name == "StoryHeadline") is var h
            && (model.Transfers[x.i].PersonName is not { Length: > 0 } person || h.Text.StartsWith(person, StringComparison.Ordinal) && NameWhole(h, person))
            && h.TooltipText == model.Transfers[x.i].Headline && h.MaxLinesVisible == (NameFits(h, model.Transfers[x.i].PersonName) ? 1 : 2)),
            $"a headline is one line, two only for a name that needs them: a person's whole name leads it and is never cut; a longer headline shortens only the organization named whole in the journey, its full text in the tooltip ({label})");
        // One treatment for every story: the same flat material, identity column, headline size and journey row; no lead
        // plate, no box behind a person.
        Check(rows.All(r => r.ThemeTypeVariation == "Entity") && rows.Select(r => Descendants(r).OfType<Label>().Single(l => l.Name == "StoryHeadline").GetThemeFontSize("font_size")).Distinct().Count() <= 1
            && rows.Select(r => Descendants(r).OfType<Control>().First(c => c.Name.ToString().StartsWith("TransferPortrait_", StringComparison.Ordinal)
                || c.Name.ToString().StartsWith("TransferCount_", StringComparison.Ordinal)).Size.X).Distinct().Count() <= 1
            && rows.All(r => !Descendants(r).OfType<PanelContainer>().Any(p => p.Name != "StoryStatus")),
            $"every transfer story has the same treatment and size, with no enlarged lead and no nested plates ({label})");
        Check(rows.Select((r, i) => (r, i)).All(x => model.Transfers[x.i].PortraitAssetId.Length > 0
            ? Descendants(x.r).OfType<AssetMark>().FirstOrDefault(m => m.Name == "TransferPortrait_" + x.i) is { Plate: false } mark
                && (mark.Resolution == IdentityAssets.DefaultPortrait || mark.Resolution == "semantic:" + SemanticAssets.ForPortrait(model.Transfers[x.i].PortraitAssetId))
            : Descendants(x.r).OfType<Label>().Any(l => l.Name == "TransferCountValue" && l.Text == Figures.Count(model.Transfers[x.i].Records.Length))),
            $"a person is drawn bare with the generic art of their kind (player or staff) or the neutral glyph, a roundup with its count ({label})");
        // The journey: origin, arrow, destination from left to right, side by side without overlap, each side what the model says.
        static bool Side(Control box, JourneySide side, bool origin)
        {
            var crest = Descendants(box).OfType<AssetMark>().SingleOrDefault();
            var name = Descendants(box).OfType<Label>().Single(l => l.Name == "JourneyName");
            return box.GetMeta("standing").AsString() == side.Standing.ToString() && box.GetMeta("organization_id").AsString() == side.OrganizationId && side.Standing switch
            {
                Standing.Organization => crest?.AssetId == side.CrestAssetId && name.Text == side.Name && name.Size.X >= 40,
                Standing.FreeAgent or Standing.Retired => crest is null && name.Text == (side.Standing == Standing.Retired ? PortalScreen.RetiredText : PortalScreen.FreeAgentText)
                    && name.GetThemeFont("font").GetStringSize(name.Text, HorizontalAlignment.Left, -1, name.GetThemeFontSize("font_size")).X <= name.Size.X + 1,
                _ => crest is null && name.Text == PortalScreen.SideText(side, origin) && name.Text.StartsWith(PresentationText.InformationMarker(InformationState.Unknown), StringComparison.Ordinal)
                    && name.TooltipText.EndsWith(origin ? "Previous team unknown" : "New team unknown", StringComparison.Ordinal)
                    && name.GetThemeFont("font").GetStringSize(name.Text, HorizontalAlignment.Left, -1, name.GetThemeFontSize("font_size")).X <= name.Size.X + 1
            };
        }
        Check(rows.Select((r, i) => (r, i)).All(x => Descendants(x.r).OfType<Control>().SingleOrDefault(c => c.Name == "TransferJourney") is { } journey
            && Descendants(journey).OfType<Control>().Single(c => c.Name == "JourneyFrom") is var from && Descendants(journey).OfType<Control>().Single(c => c.Name == "JourneyTo") is var to
            && Descendants(journey).OfType<Control>().Single(c => c.Name == "JourneyArrow") is var arrow
            && from.GetGlobalRect().End.X <= arrow.GetGlobalRect().Position.X + .5f && arrow.GetGlobalRect().End.X <= to.GetGlobalRect().Position.X + .5f
            && x.r.GetGlobalRect().Grow(.5f).Encloses(journey.GetGlobalRect())
            && Side(from, model.Transfers[x.i].From, true) && Side(to, model.Transfers[x.i].To, false)),
            $"every story shows its journey origin → destination, in that order and inside the story, with the model's organizations, free agency or unknown ({label})");
        var market = Descendants(panel).OfType<Label>().FirstOrDefault(l => l.IsVisibleInTree() && (l.Text.Contains('$') || l.Text.Contains("/day", StringComparison.Ordinal)
            || l.Text.Contains("fee", StringComparison.OrdinalIgnoreCase) || l.Text.Contains("wage", StringComparison.OrdinalIgnoreCase)
            || l.Text.Contains("ability", StringComparison.OrdinalIgnoreCase) || l.Text.Contains("days left", StringComparison.Ordinal) || l.Text.StartsWith("Showing", StringComparison.Ordinal)));
        Check(market is null && !Descendants(panel).Any(n => n.Name.ToString().StartsWith("TransferFilter", StringComparison.Ordinal) || n.Name == "PortalTransfersAll"),
            $"transfer news shows no market listing, filter, terms or market route ({label}{(market is null ? "" : ": " + market.Text)})");
        Check(model.Transfers.All(t => !model.News.Any(n => n.Id == t.Id)) && model.Transfers.Select(t => t.Id).Distinct().Count() == model.Transfers.Length,
            $"each move is one story, never repeated in world news ({label})");
    }

    /// <summary>Every approved semantic asset on screen stands for its own concept only: module art in the rail for its
    /// section, decision art for its decision kind, generic person art for a portrait ID of that kind. Nothing else.</summary>
    private void SemanticArtCheck(string label)
    {
        var misuse = new List<string>();
        foreach (var art in Descendants(this).OfType<SemanticArt>().Where(a => a.IsVisibleInTree()))
        {
            var owner = art.GetParent();
            while (owner is not null && owner is not CardButton && owner is not AssetMark) owner = owner.GetParent();
            var expected = owner switch
            {
                AssetMark mark => SemanticAssets.ForPortrait(mark.AssetId),
                CardButton nav when nav.Name.ToString().StartsWith("Nav_", StringComparison.Ordinal) => SemanticAssets.ForSection(nav.Name.ToString()["Nav_".Length..]),
                CardButton card when card.HasMeta("kind") && card.Name.ToString().StartsWith("Task_", StringComparison.Ordinal)
                    => PortalScreen.DecisionArt(Enum.Parse<DecisionKind>(card.GetMeta("kind").AsString())),
                _ => null
            };
            if (art.Texture is null || expected != art.Concept) misuse.Add($"{art.Concept} in {owner?.Name ?? art.GetParent().Name}");
        }
        Check(misuse.Count == 0, $"semantic assets appear only for their own concept ({label}{(misuse.Count == 0 ? "" : ": " + string.Join(", ", misuse))})");
    }

    private async Task PortalChecks()
    {
        Navigate(ShellSections.Portal); await Settle(6);
        var model = portal;
        var roomy = ShellLayout.Roomy(GetViewportRect().Size);
        PortalLayoutCheck("live", model);
        Check(Descendants(topBar).OfType<Label>().Any(l => l.Text == Currency.Compact(view.Cash)), "top bar binds cash from the observation, in dollars");
        Check(Descendants(this).OfType<AssetMark>().Any(m => m.AssetId == AssetIds.Crest(view.CompanyId) && m.Resolution == m.AssetId), "company crest resolves by stable company ID");
        Check(model.NextMatch is null || Descendants(this).OfType<AssetMark>().Any(m => m.AssetId == AssetIds.Crest(view.OpponentId)), "opponent crest resolves by stable rival ID");
        // One fallback policy: every approved concept names a vector icon of the shared family, and none is in use while the art loads.
        Check(SemanticAssets.All.All(a => UiIcon.Keys.Contains(a.FallbackIcon) && SemanticArt.Load(a.Concept) is not null), "every approved asset loads and names a vector fallback");
        var probe = SemanticArt.Fallback(SemanticConcept.ContractDecision, new Vector2(48, 48));
        Check(probe.Key == SemanticAssets.Get(SemanticConcept.ContractDecision).FallbackIcon, "missing art degrades to the concept's own vector icon, never other art");
        probe.Free();
        Check(!Descendants(this).OfType<Node>().Any(n => n.HasMeta("semantic_fallback")), "no fallback is shown while the approved art exists");
        // Rail: approved module art exactly for the sections that have it, the vector family elsewhere.
        foreach (var (id, _, _) in Sections)
        {
            var art = Descendants(navButtons[id]).OfType<SemanticArt>().SingleOrDefault();
            Check(art?.Concept == SemanticAssets.ForSection(id) && (art is not null || Descendants(navButtons[id]).OfType<UiIcon>().Any()),
                $"rail item {id} uses {(SemanticAssets.ForSection(id) is { } c ? "approved art " + c : "the vector icon family")}");
        }
        // Decisions: bounded, typed, with the art of their kind (approved art, the subject's mark, or the category icon).
        var cards = Descendants(content).OfType<CardButton>().Where(c => c.Name.ToString().StartsWith("Task_", StringComparison.Ordinal)).ToArray();
        Check(cards.Length == Math.Min(model.Decisions.Length, ShellLayout.DecisionCards(roomy)), "decision cards are bounded and bound to the typed decision list");
        Check(cards.All(c => model.Decisions.Single(d => d.Id == c.GetMeta("decision_id").AsString()) is var d
            && (PortalScreen.DecisionArt(d.Kind) is { } concept ? Descendants(c).OfType<SemanticArt>().Single().Concept == concept : !Descendants(c).OfType<SemanticArt>().Any())),
            "each decision card shows the approved art of its kind, and no approved art otherwise");
        Check(model.Decisions.All(d => d.Kind != DecisionKind.MatchPreparation) || cards.Any(c => Descendants(c).OfType<SemanticArt>().Any(a => a.Concept == SemanticConcept.MatchPreparationDecision)),
            "match preparation uses the approved match-preparation art");
        // World news: the world's messages only, each with its subject's catalog image.
        var newsRows = Descendants(content).OfType<CardButton>().Where(c => c.Name.ToString().StartsWith("News_", StringComparison.Ordinal)).ToArray();
        var newsArt = Descendants(content).OfType<AssetMark>().Where(m => m.Name.ToString().StartsWith("NewsImage_", StringComparison.Ordinal)).ToArray();
        Check(newsRows.Length == Math.Min(model.News.Length, ShellLayout.NewsItems(roomy)) && newsArt.Length == newsRows.Length
            && newsArt.All(a => model.News.Any(n => n.ImageAssetId == a.AssetId)), "world news rows are the model's stories with their subject's catalog image");
        Check(model.News.All(n => !PortalProjection.Personnel(n.Category)), "personnel notices are not world news");
        if (roomy && newsArt.Length > 0)
            Check(Find<Control>("NewsArt_0").Size.X >= Find<Control>("Portal_News").Size.X * .8f, "the newest story leads with a wide art panel on a roomy viewport");
        // Transfer news: stories traceable to a completed move in the observation; the market is not on the Portal.
        Check(model.Transfers.All(t => !t.Records.IsEmpty && t.Records.All(id => view.Moves.Any(m => m.Id == id && (t.PersonId.Length == 0 || m.PersonId == t.PersonId)))
            && view.Moves.Any(m => m.Id == t.Records[^1] && m.Kind == t.Kind)), "every transfer story reports completed moves in the records");
        Check(view.Candidates.Select(c => c.Id).Concat(view.CoachCandidates.Select(c => c.Id)).Where(id => !view.Moves.Any(m => m.PersonId == id))
            .All(id => !model.Transfers.Any(t => t.PersonId == id)), "a free agent or coach on the market is not transfer news by being listed");
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
        if (Descendants(content).OfType<CardButton>().FirstOrDefault(c => c.Name == "Transfer_0") is { } transfer)
        {
            var target = transfer.GetMeta("section").AsString(); var id = transfer.GetMeta("target_id").AsString();
            transfer.EmitSignal(BaseButton.SignalName.Pressed); await Settle();
            Check(section == target && (target != ShellSections.Squad || squadSelection == id), $"a transfer item opens its person in the owning section ({id})");
            Navigate(ShellSections.Portal); await Settle();
        }
        if (Descendants(content).OfType<CardButton>().FirstOrDefault(c => c.Name.ToString().StartsWith("Task_", StringComparison.Ordinal) && c.GetMeta("section").AsString() == ShellSections.Commercial
            && c.GetMeta("kind").AsString() == nameof(DecisionKind.SponsorOffer)) is { } offer)
        {
            var id = offer.GetMeta("target_id").AsString();
            offer.EmitSignal(BaseButton.SignalName.Pressed); await Settle(6);
            Check(section == ShellSections.Commercial && overlay.Visible && overlay.GetNodeOrNull<SponsorWorkspaceView>("OfferWorkspace") is not null, $"offer decision opens its sponsor workspace ({id})");
            await Tap(Key.Escape); await Settle(4);
            Navigate(ShellSections.Portal); await Settle();
        }
        // Keyboard: cards, links, filters and Continue take focus; Alt shortcuts still leave the Portal.
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
        const string longPerson = "DEV Maximilian Alexander Konstantinopoulos-Vanderbilt";
        const string longSponsor = "DEV Extraordinarily Long Sponsor Brand International Holdings";
        PortalView From(Situation s) => PortalProjection.Build(s);
        var distress = liveView with { Status = "Distress" };
        var warning = liveView with { Status = "Warning" };
        // DEVELOPMENT / NONCANONICAL fixture moves for the transfer-news cases: the campaign's own people renamed "DEV …", with
        // "dev:" record IDs, one per day going back (fixture days may precede day 1), never written to a campaign. The
        // production projection still selects, ranks and words them.
        ImmutableArray<MoveRow> DevMoves(int count, string? person = null)
        {
            var kinds = new[] { MoveKind.Signing, MoveKind.ContractEnd, MoveKind.Release, MoveKind.Appointment, MoveKind.Signing, MoveKind.ContractEnd, MoveKind.Signing };
            return [.. Enumerable.Range(0, count).Select(i =>
            {
                var kind = kinds[i % kinds.Length]; var coach = i % kinds.Length is 1 or 3; var joins = kind is MoveKind.Signing or MoveKind.Appointment;
                var p = liveView.People[i % liveView.People.Length];
                return new MoveRow("dev:move:" + i, kind, liveView.Day - i, coach ? liveView.CoachContract.Id : p.Id, person ?? "DEV " + (coach ? liveView.CoachContract.Name : p.Name),
                    coach, coach ? "" : p.Role, joins ? "" : liveView.CompanyId, joins ? liveView.CompanyId : "", "dev:cause:" + i);
            })];
        }
        // Journey sides the live rules cannot produce yet (a move between organizations, an organization no longer known),
        // built from a real rival's ID and the campaign's own people renamed "DEV …".
        MoveRow Dev(int i, MoveKind kind, int day, string from, string to)
        {
            var p = liveView.People[i % liveView.People.Length];
            return new MoveRow("dev:journey:" + i, kind, day, p.Id, "DEV " + p.Name, false, p.Role, from, to, "dev:journey-cause:" + i);
        }
        var devRival = liveView.Rivals.IsEmpty ? "rival:dev" : liveView.Rivals[0].Id;
        static JourneySide NoCrest(JourneySide side) => side.Standing == Standing.Organization ? side with { CrestAssetId = "crest:" } : side;
        var manyStories = From(liveView with { Moves = DevMoves(7) });
        var cases = new (string Name, PortalView Model)[]
        {
            ("long-names", From(liveView with { Company = longCompany, Opponent = longRival, Offers = [.. liveView.Offers.Select(o => o with { Name = longSponsor })],
                Candidates = [.. liveView.Candidates.Select(c => c with { Name = longPerson })], People = [.. liveView.People.Select(p => p with { Name = longPerson, CanRenew = true })],
                Moves = DevMoves(7, longPerson) })),
            ("no-decisions", live with { Decisions = [] }),
            // Rules v3: one player per role, one below the league's registered-roster minimum.
            ("roster-shortfall", From(liveView with { People = [.. liveView.People.GroupBy(p => p.Role).Select(g => g.First())] })),
            ("one-decision", live with { Decisions = live.Decisions.IsEmpty ? [] : [live.Decisions[0]] }),
            ("many-decisions", live with { Decisions = many }),
            // Beyond what a status bar can count at a glance: the count caps at 99+, and cash in the millions is compact.
            ("large-values", live with { Decisions = [.. Enumerable.Range(0, 120).Select(i => many[i % many.Length] with { Id = many[i % many.Length].Id + ":x" + i })],
                Finance = live.Finance with { Cash = 1_234_567_89 } }),
            ("no-match", live with { NextMatch = null, Time = live.Time with { Phase = SeasonPhase.Complete } }),
            ("empty-news", live with { News = [] }),
            ("many-news", live.News.IsEmpty ? live : live with { News = [.. Enumerable.Range(0, PortalProjection.NewsLimit)
                .Select(i => live.News[i % live.News.Length] with { Id = live.News[i % live.News.Length].Id + ":dev" + i })] }),
            ("empty-transfers", live with { Transfers = [] }),
            ("one-story", From(liveView with { Moves = DevMoves(1) })),
            // Contracts ending together: four players leaving on one day are one roundup story beside a single signing.
            ("roundup", From(liveView with { Moves = [.. liveView.People.Take(4).Select((p, i) => new MoveRow("dev:roundup:" + i, MoveKind.ContractEnd, liveView.Day, p.Id,
                "DEV " + p.Name, false, p.Role, liveView.CompanyId, "", "dev:roundup-cause:" + i)), .. DevMoves(1).Select(m => m with { Day = liveView.Day - 1 })] })),
            // Every journey side: a move from a rival (team to team), from an organization the company cannot name, and to one.
            ("journeys", From(liveView with { Moves = [Dev(0, MoveKind.Signing, liveView.Day, devRival, liveView.CompanyId),
                Dev(1, MoveKind.Signing, liveView.Day - 1, "rival:dev-unknown", liveView.CompanyId), Dev(2, MoveKind.ContractEnd, liveView.Day - 2, liveView.CompanyId, "rival:dev-unknown")] })),
            // A player who left at contract end and re-signed: the departure story stays as told, the return is a new story
            // from the free-agent market.
            ("return", From(liveView with { Moves = [Dev(3, MoveKind.Signing, liveView.Day, "", liveView.CompanyId) with { Id = "dev:return:back" },
                Dev(3, MoveKind.ContractEnd, liveView.Day - 2, liveView.CompanyId, "") with { Id = "dev:return:left" }] })),
            ("many-transfers", manyStories),
            ("warning", From(warning) with { Finance = From(warning).Finance with { Forecast = live.Finance.Cash - 400_00 } }),
            ("distress", From(distress) with { Finance = From(distress).Finance with { Arrears = 125_00, Forecast = -350_00 } }),
            ("missing-art", live with { Company = live.Company with { CrestAssetId = "crest:" },
                NextMatch = live.NextMatch is null ? null : live.NextMatch with { Us = live.NextMatch.Us with { CrestAssetId = "crest:" }, Opponent = live.NextMatch.Opponent with { CrestAssetId = "crest:" } },
                News = [.. live.News.Select(n => n with { ImageAssetId = n.ImageAssetId.StartsWith("crest:", StringComparison.Ordinal) ? "crest:" : AssetIds.NewsFallback(n.Category) })],
                Transfers = [.. From(liveView with { Moves = DevMoves(3) }).Transfers.Select(t => t with { PortraitAssetId = "portrait:", From = NoCrest(t.From), To = NoCrest(t.To) })] })
        };
        foreach (var (name, model) in cases)
        {
            BuildTopBar(model);
            ClearChildren(content);
            var screen = PortalScreen.Create(ui, assets, model, roomy, new PortalScreen.Routes((_, _) => { }, _ => false, () => { }, false));
            screen.Name = "Screen_portal"; content.AddChild(screen); screen.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            await Settle(8);
            PortalLayoutCheck("case " + name, model, name != "long-names");
            if (name == "missing-art")
            {
                Check(Descendants(content).OfType<AssetMark>().Where(m => m.AssetId == "crest:").All(m => m.Resolution == IdentityAssets.DefaultCrest), "missing crests fall back to the neutral default crest");
                Check(Descendants(content).OfType<AssetMark>().Where(m => m.Name.ToString().StartsWith("NewsImage_", StringComparison.Ordinal)).All(m => m.Resolution is IdentityAssets.DefaultNews or IdentityAssets.DefaultCrest),
                    "news without specific art uses the deterministic category fallback");
                Check(Descendants(content).OfType<AssetMark>().Where(m => m.Name.ToString().StartsWith("TransferPortrait_", StringComparison.Ordinal))
                    .All(m => m.Resolution == IdentityAssets.DefaultPortrait && !m.Plate && !Descendants(m).OfType<SemanticArt>().Any()),
                    "a portrait ID without a known person kind falls back to the neutral glyph alone: no plate, no invented image");
            }
            if (name == "no-decisions") Check(Descendants(content).OfType<Label>().Any(l => l.Text.StartsWith("Nothing currently requires", StringComparison.Ordinal))
                && Find<Label>("TopBarDecisionCount").Text == "0", "zero decisions shows the calm empty state and a zero badge");
            if (name == "roster-shortfall")
                Check(model.Decisions.FirstOrDefault(d => d.Kind == DecisionKind.RosterShortfall) is { Urgency: TaskUrgency.Blocking } shortfall
                    && Descendants(content).OfType<CardButton>().FirstOrDefault(c => c.HasMeta("decision_id") && c.GetMeta("decision_id").AsString() == shortfall.Id) is { } shortfallCard
                    && Descendants(shortfallCard).OfType<SemanticArt>().Single().Concept == SemanticConcept.RosterMoveDecision,
                    "a roster below the league minimum leads the decisions as a blocking roster move");
            if (name == "one-decision" && !live.Decisions.IsEmpty) Check(Descendants(content).OfType<Label>().Any(l => l.Text == "1 pending decision"), "one decision uses the singular wording");
            if (name == "no-match") Check(Descendants(content).OfType<Label>().Any(l => l.Text.StartsWith("No upcoming match", StringComparison.Ordinal)), "no match shows an empty state, no invented opponent");
            if (name == "empty-news") Check(Descendants(content).OfType<Label>().Any(l => l.Text == "No world news yet."), "empty world news shows an empty state, no invented stories");
            if (name == "empty-transfers") Check(Descendants(content).OfType<Label>().Any(l => l.Text == "No major transfer developments to report."), "empty transfer news shows an empty state, no invented movement");
            if (name == "one-story") Check(model.Transfers.Length == 1 && Find<CardButton>("Transfer_0").ThemeTypeVariation == "Entity"
                && !Descendants(content).OfType<CardButton>().Any(c => c.Name == "Transfer_1"), "a single transfer story is the same article as any other, not an enlarged lead");
            if (name == "roundup") Check(model.Transfers.Length == 2 && model.Transfers[0] is { Kind: MoveKind.ContractEnd, PersonName: "" } roundup && roundup.Records.Length == 4
                && roundup.Headline.StartsWith("4 players leave ", StringComparison.Ordinal) && roundup.From.OrganizationId == liveView.CompanyId && roundup.To.Standing == Standing.FreeAgent
                && Find<Label>("TransferCountValue").Text == "4", "players leaving together are one roundup story with their count and shared journey, not a run of near-identical rows");
            if (name == "journeys") Check(model.Transfers.Select(t => (t.From.Standing, t.To.Standing)).SequenceEqual([(Standing.Organization, Standing.Organization),
                    (Standing.Unknown, Standing.Organization), (Standing.Organization, Standing.Unknown)]) && model.Transfers[0].From.OrganizationId == devRival
                && model.Transfers[0].To.OrganizationId == liveView.CompanyId && model.Transfers[1].From.OrganizationId == "rival:dev-unknown",
                "a team-to-team move keeps its direction, and an organization the company cannot name stays unknown, never replaced");
            if (name == "return") Check(model.Transfers.Length == 2 && model.Transfers[0] is { Id: "dev:return:back", Kind: MoveKind.Signing } back && back.From.Standing == Standing.FreeAgent
                && back.Headline.Contains(" re-signs with ", StringComparison.Ordinal) && back.Summary.Contains("back 2 days after", StringComparison.Ordinal)
                && model.Transfers[1] is { Id: "dev:return:left", Kind: MoveKind.ContractEnd } left && left.To.Standing == Standing.FreeAgent && left.From.OrganizationId == liveView.CompanyId,
                "a return comes from the free-agent market with the earlier departure as context, and the departure story stays as it was told");
            if (name == "many-transfers")
                Check(model.Transfers.Length == PortalProjection.TransferStoryLimit
                    && Descendants(content).OfType<CardButton>().Count(c => c.Name.ToString().StartsWith("Transfer_", StringComparison.Ordinal)) == ShellLayout.TransferItems(roomy)
                    && model.Transfers[0].Significance == StorySignificance.HeadCoach && model.Transfers.Skip(1).All(t => t.Significance <= model.Transfers[0].Significance),
                    "many moves become a few stories, the head-coach move leading");
            if (name == "many-decisions") Check(Descendants(content).OfType<CardButton>().Count(c => c.Name.ToString().StartsWith("Task_", StringComparison.Ordinal)) == ShellLayout.DecisionCards(roomy)
                && Descendants(content).Any(c => c.Name == "PortalDecisionsMore"), "many decisions stay bounded with a route to the rest");
            if (name == "distress")
                Check(Find<CardButton>("Task_0").GetMeta("decision_id").AsString() == "distress" && Descendants(topBar).OfType<Label>().Any(l => l.Text.EndsWith("Distress", StringComparison.Ordinal))
                    && Descendants(content).OfType<Label>().Any(l => l.Text.Contains("Arrears", StringComparison.Ordinal)), "distress leads the decisions, the top bar and the money snapshot");
            if (name == "long-names") Check(Descendants(content).OfType<Label>().Any(l => l.Text.StartsWith(longSponsor, StringComparison.Ordinal)), "long sponsor names render in decisions and news");
            if (name == "long-names") Check(Descendants(content).OfType<Label>().Where(l => l.Name == "StoryHeadline").ToArray() is { Length: > 0 } headlines
                && headlines.All(l => l.Text.StartsWith(longPerson + " ", StringComparison.Ordinal) && l.TooltipText == l.Text) && model.Transfers.All(t => t.PersonName == longPerson),
                "long person and company names: every headline leads with the whole person name, the full text in the tooltip");
            if (name == "distress") Check(Descendants(content).OfType<Label>().Any(l => l.Text.Contains(Currency.Whole(-350_00), StringComparison.Ordinal)) && Currency.Whole(-350_00) == "−$350", "a negative estimate shows its sign before the dollar sign");
            if (name == "large-values") Check(Find<Label>("TopBarDecisionCount").Text == "99+" && Find<Label>("TopBarCashValue").Text == "$1.23M"
                && Find<Control>("TopBarCash").TooltipText.Contains("$1,234,567.89", StringComparison.Ordinal), "a count above 99 reads 99+, and millions read compact with the exact amount in the tooltip");
            if (name == "warning") Check(Descendants(topBar).OfType<Label>().Any(l => l.Text == PresentationText.WarningMarker + " Warning"), "warning condition shows its marker and wording");
            if (output is not null) await Snap(output, "portal-case-" + name);
        }
        await SurfaceStates(output);
        view = liveView; Refresh();
    }

    /// <summary>The surface system's button materials in every state (DEVELOPMENT board for evidence; never shown in play):
    /// each state is a layered material, hover differs from normal, and a press sinks the content.</summary>
    private async Task SurfaceStates(string? output)
    {
        ChromeStateChecks();
        var kinds = new[] { "PrimaryAction", "Button", "SecondaryAction", "AccentOutline", "Card", "Nav", "NavSelected", "Segment", "SegmentSelected", "Entity" };
        var states = new[] { "normal", "hover", "pressed", "disabled" };
        StyleBox Box(string state, string kind) => ui.Theme.GetStylebox(state, kind);
        foreach (var kind in new[] { "PrimaryAction", "Button", "SecondaryAction", "AccentOutline", "Card" })
        {
            Check(states.All(state => Box(state, kind) is SurfaceStyle), $"{kind}: every state uses the layered surface material");
            Check(((SurfaceStyle)Box("hover", kind)).Spec != ((SurfaceStyle)Box("normal", kind)).Spec, $"{kind}: hover is visibly different from normal");
            Check(Box("pressed", kind).ContentMarginTop == Box("normal", kind).ContentMarginTop + Surfaces.PressShift
                && Box("pressed", kind).ContentMarginBottom == Box("normal", kind).ContentMarginBottom - Surfaces.PressShift, $"{kind}: a press sinks the content without changing its size");
        }
        Check(Box("normal", "NavSelected") is SurfaceStyle { Spec.AccentEdge: AccentEdge.Left } && Box("normal", "SegmentSelected") is SurfaceStyle,
            "the current selection is a raised material with a brass leading edge");
        ClearChildren(content);
        var board = new PanelContainer { Name = "Screen_surface_states" };
        board.AddThemeStyleboxOverride("panel", Surfaces.Region(ui.Tokens, 16));
        var grid = new GridContainer { Columns = states.Length + 2 };
        grid.AddThemeConstantOverride("h_separation", 14); grid.AddThemeConstantOverride("v_separation", 10);
        board.AddChild(grid);
        grid.AddChild(SemanticText.Create(ui, "DEV surface states", TypographyRole.Caption, ColorRole.TextMuted));
        foreach (var header in states.Append("focused")) grid.AddChild(SemanticText.Create(ui, header, TypographyRole.Caption, ColorRole.TextMuted));
        foreach (var kind in kinds)
        {
            grid.AddChild(SemanticText.Create(ui, kind, TypographyRole.Annotation, ColorRole.TextSecondary));
            foreach (var state in states.Append("focused"))
            {
                var sample = new Button { Text = kind == "PrimaryAction" ? "Continue" : "Action", ThemeTypeVariation = kind, FocusMode = FocusModeEnum.None,
                    CustomMinimumSize = new Vector2(132, 36), Disabled = state == "disabled" };
                if (state is "hover" or "pressed") sample.AddThemeStyleboxOverride("normal", Box(state, kind));
                if (state == "focused")
                {
                    var ring = new Panel { MouseFilter = MouseFilterEnum.Ignore };
                    ring.AddThemeStyleboxOverride("panel", ui.Theme.GetStylebox("focus", kind));
                    sample.AddChild(ring); ring.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
                }
                grid.AddChild(sample);
            }
        }
        // Frame tiers (functional hierarchy): a premium and a standard region with their header plates, and the utility
        // plates set into them. DEVELOPMENT samples only.
        var tiers = new HBoxContainer(); tiers.AddThemeConstantOverride("separation", 14);
        foreach (var tier in new[] { FrameTier.Premium, FrameTier.Standard })
        {
            var region = PanelFrame.Major(ui, "Tier_" + tier, SectionHeader.Region(ui, "alert", $"DEV {tier} frame", false, "context", null, ColorRole.ActionPrimary, tier),
                8, out var body, 6, tier);
            region.CustomMinimumSize = new Vector2(330, 0);
            var plate = PanelFrame.Card(ui, 10, 6); plate.AddChild(SemanticText.Create(ui, "DEV nested plate", TypographyRole.Annotation, ColorRole.TextSecondary)); body.AddChild(plate);
            body.AddChild(new Button { Text = "DEV utility row", ThemeTypeVariation = "Row", FocusMode = FocusModeEnum.None, Alignment = HorizontalAlignment.Left });
            tiers.AddChild(region);
        }
        var stack = new VBoxContainer(); stack.AddThemeConstantOverride("separation", 16); stack.AddChild(board); stack.AddChild(tiers);
        var margin = new MarginContainer(); foreach (var edge in new[] { "left", "top", "right" }) margin.AddThemeConstantOverride("margin_" + edge, 24);
        margin.AddChild(stack); content.AddChild(margin); margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        await Settle(6);
        if (output is not null) await Snap(output, "surface-states");
    }

    /// <summary>The rail in its interactive states (DEVELOPMENT evidence, never shown in play): the current section selected,
    /// Inbox in its hover material, Squad holding keyboard focus; then the collapsed rail.</summary>
    private async Task RailStates(string output)
    {
        var hover = navButtons[ShellSections.Inbox];
        hover.AddThemeStyleboxOverride("normal", ui.Theme.GetStylebox("hover", hover.ThemeTypeVariation));
        navButtons[ShellSections.Squad].GrabFocus();
        await Snap(output, "rail-states", keepFocus: true);
        hover.RemoveThemeStyleboxOverride("normal");
        await Press("NavToggle");
        await Snap(output, "rail-collapsed");
        await Press("NavToggle");
    }

    private async Task Snap(string output, string key, bool keepFocus = false)
    {
        Input.WarpMouse(Vector2.Zero); if (!keepFocus) GetViewport().GuiReleaseFocus(); await Settle(8);
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
