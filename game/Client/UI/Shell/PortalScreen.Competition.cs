using System.Globalization;
using Godot;
using ManagementGame.Application;
using ManagementGame.Formatting;
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
            match is null ? $"Season {m.Time.Season}" : $"Day {match.DayOfSeason} ({Until(match.Day, m.Time.Day)})");
        if (match is null)
        {
            body.AddChild(Empty(ui, "calendar", "No upcoming match scheduled.",
                m.Time.Phase == SeasonPhase.Complete ? "The next season's fixtures are drawn at rollover." : ""));
            return panel;
        }
        body.AddChild(Stage(ui, assets, m, match, roomy));
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
        // The matchup is the premium region's screen: a plate cut like the decision cards, its washes clipped to the cuts.
        var stage = PanelFrame.Mask(ui, new PanelContainer { Name = "MatchStage", SizeFlagsVertical = Control.SizeFlags.ExpandFill, ClipContents = true }, Surfaces.CardCut);
        var inset = Tone(ui, ColorRole.SurfaceInset);
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
        var h2h = "Head to head " + Figures.Record(match.HeadToHeadWon, match.HeadToHeadLost);
        row.AddChild(Side(ui, assets, match.Us, roomy, "MatchUs", (planText, planTone), roomy ? (record, ColorRole.TextSecondary) : null));
        var middle = Stack(2); middle.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter; middle.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        // VS sits in its own recessed badge: the hinge of the matchup.
        var vsWell = PanelFrame.Well(ui, roomy ? 8 : 4); vsWell.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        var versus = Text(ui, "VS", TypographyRole.SectionTitle, ColorRole.ActionPrimary, wrap: false, size: roomy ? TypographyRole.CompanyIdentity : TypographyRole.Label);
        versus.HorizontalAlignment = HorizontalAlignment.Center; vsWell.AddChild(versus); middle.AddChild(vsWell);
        // Facts the simulation owns sit under VS: stakes and where the match falls in the season. No venue or series format exists.
        if (roomy || match.Importance > 1)
            middle.AddChild(Text(ui, match.Importance > 1 ? "Double stakes" : "Regular stakes", TypographyRole.Caption, match.Importance > 1 ? ColorRole.StateWarning : ColorRole.TextSecondary, wrap: false));
        if (roomy) middle.AddChild(Text(ui, $"Match {match.MatchNumber} of {match.SeasonMatches}", TypographyRole.Caption, ColorRole.TextSecondary, wrap: false));
        foreach (var line in middle.GetChildren().OfType<Label>()) line.HorizontalAlignment = HorizontalAlignment.Center;
        row.AddChild(middle);
        row.AddChild(Side(ui, assets, match.Opponent, roomy, "MatchThem",
            ($"{PresentationText.InformationMarker(InformationState.Estimated)} Strength {match.StrengthEstimate}", ColorRole.InformationEstimated),
            roomy ? (h2h, ColorRole.TextSecondary) : null));
        stage.AddChild(PanelFrame.Overlay(ui, Surfaces.CardCut));
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
    /// <summary>"How is this season going?" The record leads, recent form (oldest to newest, letters not colour alone)
    /// and the matches still to play support it. Once a match is played, the league place joins them (rules v4: an
    /// eight-team league whose top teams reach the playoffs).</summary>
    private static Control Record(UiContext ui, PortalView m, bool roomy)
    {
        var s = m.Season;
        var panel = Region(ui, "Portal_Record", "record", "Season record", roomy, out var body, $"Season {s.Season}");
        var played = s.Won + s.Lost;
        var row = Row(roomy ? 18 : 12); row.Alignment = BoxContainer.AlignmentMode.Center; row.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        var size = ShellLayout.RecordRing(roomy);
        // Primary record in a recessed data well; the supporting metrics on a level-2 plate beside it.
        var ringWell = PanelFrame.Well(ui, roomy ? 8 : 4); ringWell.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        var ring = new Control { Name = "RecordRing", CustomMinimumSize = new(size, size), MouseFilter = Control.MouseFilterEnum.Ignore, SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        ringWell.AddChild(ring);
        var gauge = RingGauge.Create(ui, size, s.Won, played, ColorRole.StatePositive); ring.AddChild(gauge); gauge.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var centre = Stack(0); centre.Alignment = BoxContainer.AlignmentMode.Center; ring.AddChild(centre); centre.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var score = Text(ui, Figures.Record(s.Won, s.Lost), TypographyRole.Data, wrap: false, size: roomy ? TypographyRole.WorkspaceTitle : TypographyRole.Data);
        score.Name = "RecordScore";
        var caption = Text(ui, "W–L", TypographyRole.Caption, ColorRole.TextMuted, wrap: false);
        foreach (var label in new[] { score, caption }) { label.HorizontalAlignment = HorizontalAlignment.Center; label.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter; centre.AddChild(label); }
        row.AddChild(ringWell);
        var metrics = PanelFrame.Card(ui, roomy ? 12 : 8, roomy ? 8 : 4); metrics.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; metrics.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        var list = Stack(roomy ? 8 : 3); list.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter; metrics.AddChild(list);
        var form = Row(4); form.Name = "RecordForm";
        if (s.Recent.IsEmpty) form.AddChild(Text(ui, "No results yet", TypographyRole.Caption, ColorRole.TextMuted, wrap: false));
        foreach (var r in s.Recent.Reverse())
        {
            var (letter, tone) = r.Outcome switch { "Victory" => ("W", ColorRole.StatePositive), "Forfeit" => ("F", ColorRole.StateCritical), _ => ("L", ColorRole.StateCritical) };
            var pill = Chip(ui, letter, tone); pill.TooltipText = $"{r.Outcome} vs {r.Opponent}, day {r.Day}"; pill.MouseFilter = Control.MouseFilterEnum.Pass;
            form.AddChild(pill);
        }
        if (roomy) list.AddChild(Caps(ui, "Recent form", ColorRole.TextMuted));
        list.AddChild(form);
        var rows = new List<(string, string, ColorRole)>();
        // The place reads against the playoff line, both as text (where there is room), never by colour alone.
        if (s.Place > 0) rows.Add(("League place", $"{Figures.Ordinal(s.Place)} of {Figures.Count(s.Teams)}", ColorRole.TextPrimary));
        if (s.Place > 0 && roomy && s.PlayoffTeams > 0) rows.Add(("Playoff places", $"Top {Figures.Count(s.PlayoffTeams)}", ColorRole.TextSecondary));
        rows.Add(("To play", Figures.Count(s.ToPlay), ColorRole.TextPrimary));
        // The win rate supports the record where there is room; it never leads it.
        if (roomy) rows.Add(("Win rate", Figures.Percent(s.Won, played), ColorRole.TextSecondary));
        foreach (var (label, value, tone) in rows)
        {
            var line = Row(12);
            line.AddChild(Clamp(Text(ui, label, TypographyRole.Annotation, ColorRole.TextSecondary), 1));
            line.AddChild(Text(ui, value, TypographyRole.Strong, tone, wrap: false, size: roomy ? TypographyRole.Label : TypographyRole.Annotation));
            list.AddChild(line);
        }
        row.AddChild(metrics);
        body.AddChild(row);
        return panel;
    }
}
