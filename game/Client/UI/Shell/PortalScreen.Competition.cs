using System.Globalization;
using Godot;
using ManagementGame.Application;
using ManagementGame.UiKit;

namespace ManagementGame.Shell;

/// <summary>Next match and season record. The match is staged like a broadcast card: both sides' generated arms and
/// names by stable ID over a procedural light wash in each club's colour; only facts the simulation owns appear (no venue,
/// series format or rival form).</summary>
public static partial class PortalScreen
{
    private static Control Match(UiContext ui, IdentityAssets assets, PortalView m, bool roomy, Routes routes)
    {
        var match = m.NextMatch;
        var panel = Region(ui, "Portal_NextMatch", "competition", "Next match", roomy, out var body,
            match is null ? $"Season {m.Time.Season}" : $"Season {m.Time.Season} · {Phase(m.Time.Phase)}");
        if (match is null)
        {
            body.AddChild(Empty(ui, "calendar", "No upcoming match scheduled.",
                m.Time.Phase == SeasonPhase.Complete ? "The next season's fixtures are drawn at rollover." : ""));
            return panel;
        }
        body.AddChild(Stage(ui, assets, m, match, roomy));
        // Facts row: when, what is at stake and where in the season. The simulation owns no venue or series format.
        var facts = new HFlowContainer { Name = "MatchFacts", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        facts.AddThemeConstantOverride("h_separation", 16);
        var list = new List<(string Icon, string Text, ColorRole Tone)>
        {
            ("calendar", $"Day {match.DayOfSeason} ({Until(match.Day, m.Time.Day)})", match.Day == m.Time.Day ? ColorRole.StateWarning : ColorRole.TextPrimary),
            ("stakes", match.Importance > 1 ? "Double stakes" : "Regular stakes", ColorRole.TextPrimary)
        };
        if (roomy) list.Add(("record", $"Match {match.MatchNumber} of {match.SeasonMatches}", ColorRole.TextPrimary));
        foreach (var (icon, text, tone) in list)
        {
            var fact = Row(6); fact.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
            fact.AddChild(UiIcon.Create(ui, icon, 16, ColorRole.TextSecondary));
            fact.AddChild(Text(ui, text, TypographyRole.Annotation, tone, wrap: false));
            facts.AddChild(fact);
        }
        body.AddChild(facts);
        // The route to preparation is a strong secondary action while no plan exists; Continue stays the one gold action.
        var missing = match.Plan == PlanState.Missing;
        var open = new CardButton(12, roomy ? 6 : 4) { Name = "PortalMatchOpen", ThemeTypeVariation = missing ? "SecondaryAction" : "AccentOutline" };
        var label = CardButton.Passive(Row(8)); label.Alignment = BoxContainer.AlignmentMode.Center;
        label.AddChild(Text(ui, match.Plan switch { PlanState.Committed => "Review match plan", PlanState.CoachWillPlan => "Review coach's plan", _ => "Prepare match" },
            TypographyRole.Strong, wrap: false, size: roomy ? TypographyRole.Label : TypographyRole.Annotation));
        label.AddChild(UiIcon.Create(ui, "arrow", 16));
        open.Body.AddChild(label);
        open.Pressed += () => routes.Open(ShellSections.Competition, match.FixtureId);
        body.AddChild(open);
        return panel;
    }
    private static Control Stage(UiContext ui, IdentityAssets assets, PortalView m, PortalMatch match, bool roomy)
    {
        var stage = new PanelContainer { Name = "MatchStage", SizeFlagsVertical = Control.SizeFlags.ExpandFill, ClipContents = true };
        var inset = Tone(ui, ColorRole.SurfaceInset);
        stage.AddThemeStyleboxOverride("panel", Fill(inset, 0));
        var us = UiTheme.ToGodot(assets.TeamColor(match.Us.CrestAssetId)); var them = UiTheme.ToGodot(assets.TeamColor(match.Opponent.CrestAssetId));
        stage.AddChild(Backdrop.Create(inset.Lerp(us, .4f), inset.Lerp(them, .4f), Tone(ui, ColorRole.SurfaceBase) with { A = .7f },
            new Backdrop.Glow(new(.25f, .38f), .8f, us.Lightened(.3f) with { A = .38f }),
            new Backdrop.Glow(new(.75f, .38f), .8f, them.Lightened(.3f) with { A = .38f }),
            new Backdrop.Glow(new(.5f, .5f), .55f, inset with { A = .9f })));
        var row = Row(roomy ? 8 : 2); row.Alignment = BoxContainer.AlignmentMode.Center;
        var inner = new MarginContainer(); foreach (var edge in new[] { "left", "right", "top", "bottom" }) inner.AddThemeConstantOverride("margin_" + edge, roomy ? 12 : 4);
        inner.AddChild(row); stage.AddChild(inner);
        // Under our name: our readiness for this match; under theirs: the estimate, marked as an estimate.
        var (planText, planTone) = match.Plan switch
        {
            PlanState.Committed => ($"{PresentationText.PositiveMarker} Plan committed", ColorRole.StatePositive),
            PlanState.CoachWillPlan => ($"{PresentationText.NeutralMarker} Coach will plan", ColorRole.TextSecondary),
            _ => ($"{PresentationText.WarningMarker} No plan yet", ColorRole.StateWarning)
        };
        var record = $"Season {m.Season.Won}W – {m.Season.Lost}L";
        var h2h = $"Head to head {match.HeadToHeadWon}–{match.HeadToHeadLost}";
        row.AddChild(Side(ui, assets, match.Us, roomy, "MatchUs", (planText, planTone), roomy ? (record, ColorRole.TextSecondary) : null));
        var versus = Text(ui, "VS", TypographyRole.SectionTitle, ColorRole.TextSecondary, wrap: false, size: roomy ? TypographyRole.CompanyIdentity : TypographyRole.Label);
        versus.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter; versus.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        row.AddChild(versus);
        row.AddChild(Side(ui, assets, match.Opponent, roomy, "MatchThem",
            ($"{PresentationText.InformationMarker(InformationState.Estimated)} Strength {match.StrengthEstimate}", ColorRole.InformationEstimated),
            roomy ? (h2h, ColorRole.TextSecondary) : null));
        stage.TooltipText = $"{match.Us.Name} vs {match.Opponent.Name}. {h2h}. Usually {match.Tendency.ToLowerInvariant()}, {match.Confidence.ToLowerInvariant()} (estimate).";
        return stage;
    }
    private static Control Side(UiContext ui, IdentityAssets assets, PortalTeam team, bool roomy, string name, (string Text, ColorRole Tone) first, (string Text, ColorRole Tone)? second)
    {
        var box = Stack(roomy ? 4 : 2); box.Name = name; box.Alignment = BoxContainer.AlignmentMode.Center; box.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        box.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        var size = ShellLayout.MatchCrest(roomy);
        var crest = assets.Create(team.CrestAssetId, new(size * 100f / 120, size)); crest.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        box.AddChild(crest);
        // Broadcast caps where there is room; mixed case on the compact stage keeps ordinary names on one line.
        var label = Text(ui, roomy ? team.Name.ToUpper(CultureInfo.InvariantCulture) : team.Name, TypographyRole.Strong, size: roomy ? TypographyRole.Label : TypographyRole.Annotation);
        label.HorizontalAlignment = HorizontalAlignment.Center; label.MaxLinesVisible = 2; label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        label.TooltipText = team.Name; label.MouseFilter = Control.MouseFilterEnum.Pass; box.AddChild(label);
        foreach (var (text, tone) in second is { } extra ? new[] { first, extra } : [first])
        {
            var line = Text(ui, text, TypographyRole.Caption, tone); line.HorizontalAlignment = HorizontalAlignment.Center; box.AddChild(line);
        }
        return box;
    }

    // ---------- season record ----------
    /// <summary>"How is this season going?" The record leads; the win rate is one supporting row, not the headline.</summary>
    private static Control Record(UiContext ui, IdentityAssets assets, PortalView m, bool roomy)
    {
        var s = m.Season;
        var panel = Region(ui, "Portal_Record", "record", "Season record", roomy, out var body, $"Season {s.Season}");
        var played = s.Won + s.Lost;
        var row = Row(roomy ? 18 : 16); row.Alignment = BoxContainer.AlignmentMode.Center; row.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        var size = ShellLayout.RecordRing(roomy);
        var ring = new Control { Name = "RecordRing", CustomMinimumSize = new(size, size), MouseFilter = Control.MouseFilterEnum.Ignore };
        var gauge = RingGauge.Create(ui, size, s.Won, played, ColorRole.StatePositive); ring.AddChild(gauge); gauge.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var centre = Stack(0); centre.Alignment = BoxContainer.AlignmentMode.Center; ring.AddChild(centre); centre.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var score = Text(ui, $"{s.Won}–{s.Lost}", TypographyRole.Data, wrap: false, size: roomy ? TypographyRole.WorkspaceTitle : TypographyRole.Data);
        score.Name = "RecordScore";
        var caption = Text(ui, "W–L", TypographyRole.Caption, ColorRole.TextMuted, wrap: false);
        foreach (var label in new[] { score, caption }) { label.HorizontalAlignment = HorizontalAlignment.Center; label.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter; centre.AddChild(label); }
        row.AddChild(ring);
        var list = Stack(roomy ? 4 : 0); list.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        var rate = played == 0 ? "—" : (s.Won / (double)played).ToString("P0", CultureInfo.InvariantCulture).Replace(" ", "", StringComparison.Ordinal);
        var rows = new List<(string, string, ColorRole)>
        {
            ("Wins", s.Won.ToString(CultureInfo.InvariantCulture), ColorRole.StatePositive), ("Losses", s.Lost.ToString(CultureInfo.InvariantCulture), ColorRole.StateCritical),
            ("To play", s.ToPlay.ToString(CultureInfo.InvariantCulture), ColorRole.TextPrimary)
        };
        // The win rate supports the record where there is room; it never leads it.
        if (roomy) rows.Add(("Win rate", rate, ColorRole.TextSecondary));
        foreach (var (label, value, tone) in rows)
        {
            var line = Row(12);
            line.AddChild(Clamp(Text(ui, label, TypographyRole.Annotation, ColorRole.TextSecondary), 1));
            line.AddChild(Text(ui, value, TypographyRole.Strong, tone, wrap: false, size: roomy ? TypographyRole.Label : TypographyRole.Annotation));
            list.AddChild(line);
        }
        list.SizeFlagsStretchRatio = .8f;
        row.AddChild(list);
        // Head to head sits beside the record where there is width, so the region stays one band tall.
        if (roomy && !s.HeadToHead.IsEmpty)
        {
            row.AddChild(Rule(ui, vertical: true));
            var rivals = Stack(5); rivals.Name = "HeadToHead"; rivals.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            rivals.AddChild(Caps(ui, "Head to head", ColorRole.TextMuted));
            foreach (var rival in s.HeadToHead)
            {
                var line = Row(8);
                line.AddChild(assets.Create(AssetIds.Crest(rival.Id), new(17, 20)));
                line.AddChild(Clamp(Text(ui, rival.Name, TypographyRole.Annotation, m.NextMatch?.Opponent.Id == rival.Id ? ColorRole.TextPrimary : ColorRole.TextSecondary), 1));
                line.AddChild(Text(ui, $"{rival.Wins}–{rival.Losses}", TypographyRole.Strong, wrap: false, size: TypographyRole.Annotation));
                if (m.NextMatch?.Opponent.Id == rival.Id) line.TooltipText = "Next opponent";
                rivals.AddChild(line);
            }
            row.AddChild(rivals);
        }
        body.AddChild(row);
        return panel;
    }
}
