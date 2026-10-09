using Godot;
using ManagementGame.Application;
using ManagementGame.UiKit;

namespace ManagementGame.Shell;

/// <summary>Native checks of the Portal's layout and content (Portal redesign, 2026-10-08): sponsor offers show the brand
/// without commercial terms, every decision's action sits at the card's bottom right without meeting the decision text,
/// the top bar spreads its information with the actions grouped right, the rail shows recognisable art aligned with its
/// labels, and no developer or narration text appears in player space.</summary>
public partial class GameShell
{
    /// <summary>Text that belongs to developers or to the simulation's narration, never to player space.</summary>
    private static readonly string[] DiagnosticText = ["Development build", "Noncanonical fixture", PresentationText.FixtureNotice, PresentationText.DevelopmentWatermark, "Advanced to day"];

    /// <summary>Top bar (UI polish standard §6–§7): one row of metrics, each a caps label over a value line, typeset in the
    /// bundled family; labels subordinate to values; one baseline for every value line and one for every label; equal gaps
    /// across the bar and none wider before the actions; nothing clipped; and every value bound to the read model.</summary>
    private void TopBarCheck(string label, PortalView model)
    {
        var info = Find<HBoxContainer>("TopBarInfo"); var actions = Find<HBoxContainer>("TopBarActions");
        var metrics = info.GetChildren().OfType<Control>().Where(c => !c.Name.ToString().StartsWith("MetricGap", StringComparison.Ordinal)).ToArray();
        var rects = metrics.Select(c => c.GetGlobalRect()).OrderBy(r => r.Position.X).ToArray();
        var gaps = rects.Zip(rects.Skip(1)).Select(p => p.Second.Position.X - p.First.End.X).ToArray();
        var tail = actions.GetGlobalRect().Position.X - rects[^1].End.X;
        Check(gaps.Length > 0 && gaps.Min() >= ShellLayout.MetricMinGap - .5f && gaps.Max() - gaps.Min() <= 1.5f && tail <= gaps.Max() + 1,
            $"top-bar metrics span the bar with equal gaps and no wider gap before the actions ({label}, gaps {gaps.DefaultIfEmpty().Min():0}–{gaps.DefaultIfEmpty().Max():0} px, tail {tail:0} px)");
        var texts = Descendants(info).OfType<Label>().Where(l => l.IsVisibleInTree()).ToArray();
        Label[] Of(params TypographyRole[] roles) => texts.Where(l => roles.Any(r => l.ThemeTypeVariation == r.ToString())).ToArray();
        var captions = Of(TypographyRole.MetricLabel); var values = Of(TypographyRole.MetricValue, TypographyRole.StatusValue); var context = Of(TypographyRole.MetricContext);
        int Size(Label l) => l.GetThemeFontSize("font_size");
        Check(captions.Length == metrics.Length && values.Length == metrics.Length && texts.Length == captions.Length + values.Length + context.Length,
            $"every top-bar metric is a label and a value, with optional context, and nothing else ({label})");
        Check(texts.All(l => UiTheme.BundledFace(l.GetThemeFont("font"))),
            $"top-bar text is set in the bundled {UiTokens.FontFamily} family ({label}: {string.Join(", ", texts.Select(l => l.GetThemeFont("font")).Select(f => (f is FontVariation v ? v.BaseFont : f)?.GetFontName() ?? "none").Distinct())})");
        Check(captions.All(l => l.Text == l.Text.ToUpperInvariant() && Size(l) == ui.Tokens.FontSize(TypographyRole.MetricLabel))
            && values.All(v => Size(v) > ui.Tokens.FontSize(TypographyRole.MetricLabel)) && context.All(c => Size(c) < ui.Tokens.FontSize(TypographyRole.MetricValue))
            && captions.All(l => l.GetThemeColor("font_color") == UiTheme.ToGodot(ui.Tokens.Color(ColorRole.TextMuted))),
            $"metric labels are small muted caps, subordinate to their values; context is smaller than the value ({label})");
        // Baselines: the text of a single-line label starts at its top, so its baseline is the top plus the face's ascent.
        float Baseline(Label l) => l.GetGlobalRect().Position.Y + l.GetThemeFont("font").GetAscent(Size(l));
        var valueLines = values.Concat(context).Select(Baseline).ToArray(); var labelLines = captions.Select(Baseline).ToArray();
        Check(valueLines.Max() - valueLines.Min() <= 1.5f && labelLines.Max() - labelLines.Min() <= 1f && labelLines.Max() < valueLines.Min(),
            $"labels share one baseline, values and context share one below it ({label}, values ±{valueLines.Max() - valueLines.Min():0.0} px)");
        var cut = texts.FirstOrDefault(l => l.TextOverrunBehavior == TextServer.OverrunBehavior.NoTrimming
            && l.GetThemeFont("font").GetStringSize(l.Text, HorizontalAlignment.Left, -1, Size(l)).X > l.Size.X + .5f);
        Check(cut is null && texts.Where(l => l.TextOverrunBehavior != TextServer.OverrunBehavior.NoTrimming).All(l => l.GetParent()?.GetParent()?.GetParent() is Control { TooltipText.Length: > 0 }),
            $"no top-bar text is clipped; a bounded value keeps its whole text in the tooltip ({label}{(cut is null ? "" : ": " + cut.Text)})");
        // Values come from the read model through the shared formatters.
        Check(Find<Label>("TopBarCashValue").Text == ManagementGame.Formatting.Currency.Compact(model.Finance.Cash)
            && Find<Control>("TopBarCash").TooltipText.Contains(ManagementGame.Formatting.Currency.Format(model.Finance.Cash), StringComparison.Ordinal),
            $"top-bar cash is the model's cash in dollars, exact in the tooltip ({label})");
        Check(Find<Label>("TopBarWeek").Text == "Week " + ((model.Time.DayOfSeason - 1) / PortalProjection.WeekLength + 1)
            && Find<Label>("TopBarDayValue").Text == model.Time.DayOfSeason.ToString(System.Globalization.CultureInfo.InvariantCulture),
            $"season week and day come from the model's time ({label})");
        var blocking = model.Decisions.Count(d => d.Urgency == TaskUrgency.Blocking);
        var state = Find<Label>("TopBarDecisionState");
        var amber = UiTheme.ToGodot(ui.Tokens.Color(ColorRole.StateWarning));
        Check(Find<Label>("TopBarDecisionCount").Text == ManagementGame.Formatting.Figures.Capped(model.Decisions.Length)
            && (model.Decisions.IsEmpty ? state.Text == "all clear" : blocking > 0 ? state.Text.Contains("blocking", StringComparison.Ordinal) : !state.Text.Contains("blocking", StringComparison.Ordinal))
            && (state.GetThemeColor("font_color") == amber) == (blocking > 0),
            $"pending decisions: the model's count and its state, amber only when a decision blocks ({label}: {Find<Label>("TopBarDecisionCount").Text} {state.Text})");
    }

    private void PortalRedesignCheck(string label, PortalView model)
    {
        // Decision cards: the identity left, the decision in the centre, the action at the bottom right.
        var cards = Descendants(content).OfType<CardButton>().Where(c => c.IsVisibleInTree() && c.Name.ToString().StartsWith("Task_", StringComparison.Ordinal)).ToArray();
        var insets = new List<float>();
        foreach (var card in cards)
        {
            var decision = model.Decisions.Single(d => d.Id == card.GetMeta("decision_id").AsString());
            var box = card.GetGlobalRect();
            var verb = Descendants(card).OfType<Control>().First(c => c.Name == "Verb").GetGlobalRect();
            var subject = Descendants(card).OfType<Label>().First(l => l.Name == "Subject").GetGlobalRect();
            var art = Descendants(card).OfType<Control>().First(c => c.Name == "DecisionArt").GetGlobalRect();
            insets.Add(box.End.X - verb.End.X);
            Check(verb.GetCenter().X > box.GetCenter().X && box.End.Y - verb.End.Y <= 16 && box.End.X - verb.End.X >= 8,
                $"{card.Name}: its action sits at the bottom right, clear of the border ({label})");
            Check(!verb.Intersects(subject) && art.End.X <= subject.Position.X && subject.End.X <= box.End.X,
                $"{card.Name}: identity left, decision centre, and the action never meets the name ({label})");
            if (decision.Kind == DecisionKind.SponsorOffer)
            {
                var texts = Descendants(card).OfType<Label>().Where(l => l.IsVisibleInTree()).Select(l => l.Text).ToArray();
                Check(texts.Contains(decision.Subject) && (decision.Why.Length == 0 || !texts.Contains(decision.Why))
                    && Descendants(card).OfType<AssetMark>().Any(m => m.AssetId == decision.SubjectAssetId),
                    $"{card.Name}: a sponsor offer shows the brand's mark and name, not its commercial terms ({label})");
            }
        }
        Check(insets.Count < 2 || insets.Max() - insets.Min() <= 1, $"decision actions share one right alignment ({label})");
        TopBarCheck(label, model);
        var actions = Find<HBoxContainer>("TopBarActions");
        var bar = topBar.GetGlobalRect();
        Check(continueButton.GetGlobalRect().End.X >= bar.End.X - 2 && actions.GetChildren().OfType<Control>().Last() == continueButton
            && actions.GetChildren().OfType<Control>().Any(c => c.Name == "Save") && actions.GetChildren().OfType<Control>().Any(c => c.Name == "Load"),
            $"Save, Load and Continue stay grouped at the right, Continue last ({label})");
        RailCheck(label);
        // No developer, fixture or narration text anywhere in the shell's player space, and no notice over a region header.
        var leaked = Descendants(this).OfType<Label>().Where(l => l.IsVisibleInTree() && l.Text.Length > 0)
            .FirstOrDefault(l => DiagnosticText.Any(t => l.Text.Contains(t, StringComparison.Ordinal)));
        Check(leaked is null && Descendants(this).All(n => n.Name != "DevelopmentMarker"), $"no developer or narration text in player space ({label}{(leaked is null ? "" : ": " + leaked.Text)})");
        if (toast.Visible)
            Check(Descendants(content).OfType<HeaderPlate>().All(h => !h.GetGlobalRect().Intersects(toast.GetGlobalRect()))
                && (navCollapsed || nav.GetGlobalRect().Grow(1).Encloses(toast.GetGlobalRect())), $"notices dock in the rail and never cover content ({label})");
        Check(nav.GetGlobalRect().Grow(1).Encloses(Find<Button>("NavToggle").GetGlobalRect()), $"the rail keeps Collapse inside it ({label})");
    }

    /// <summary>The rail (sidebar redesign, 2026-10-08): the eight sections in order, one item height and one even gap,
    /// clear of the identity plate and Collapse; each section's own approved art at one perceived size inside its slot,
    /// on the item's own surface with nothing behind it; labels whole at 16 px on one column and centred with their art;
    /// the unread badge clear of its label and bound to the inbox; the current section selected; a collapsed rail shows
    /// the art alone, centred.</summary>
    private void RailCheck(string label)
    {
        var roomy = ShellLayout.Roomy(GetViewportRect().Size);
        var items = navList.GetChildren().OfType<CardButton>().ToArray();
        Check(items.Select(b => b.Name.ToString()).SequenceEqual(Sections.Select(s => "Nav_" + s.Id)), $"the rail lists the eight sections in order ({label})");
        var rects = items.Select(b => b.GetGlobalRect()).ToArray();
        Check(rects.All(r => Math.Abs(r.Size.Y - ShellLayout.NavItemHeight(roomy)) <= 1 && r.Size.X >= UiTokens.MinimumTarget),
            $"rail items share one {ShellLayout.NavItemHeight(roomy)} px click target ({label})");
        var gaps = rects.Zip(rects.Skip(1)).Select(p => p.Second.Position.Y - p.First.End.Y).ToArray();
        Check(gaps.Min() >= ShellLayout.NavMinGap - .5f && gaps.Max() - gaps.Min() <= 1, $"rail items are evenly spaced ({label}, gaps {gaps.Min():0.0}–{gaps.Max():0.0} px)");
        var identity = Find<PanelContainer>("RailIdentityPlate").GetGlobalRect();
        Check(identity.End.Y <= rects[0].Position.Y && rects[^1].End.Y <= navToggle.GetGlobalRect().Position.Y
            && (navList.Tail is null || rects[^1].End.Y <= navList.Tail.GetGlobalRect().Position.Y + .5f), $"rail items clear the identity plate, the notice dock and Collapse ({label})");
        Check(nav.GetGlobalRect().End.Y <= GetViewportRect().End.Y + .5f && navList.Size.Y >= navList.GetCombinedMinimumSize().Y - .5f,
            $"the rail fits the window without squeezing its items ({label}, rail ends at {nav.GetGlobalRect().End.Y:0}, navigation {navList.Size.Y:0} of {navList.GetCombinedMinimumSize().Y:0} px needed)");
        var width = navCollapsed ? ShellLayout.RailCollapsedWidth(roomy) : ShellLayout.RailWidth(roomy);
        Check(Math.Abs(nav.Size.X - width) <= .5f, $"the rail keeps its {width} px width: every item fits without widening it ({label}, {nav.Size.X:0} px)");
        foreach (var (id, _, _) in Sections)
        {
            var button = navButtons[id]; var box = button.GetGlobalRect();
            var slot = Descendants(button).OfType<Control>().First(c => c.Name == "NavIcon");
            Check(slot is not PanelContainer && !Descendants(button).OfType<PanelContainer>().Any() && !Descendants(button).OfType<ColorRect>().Any(),
                $"rail art for {id} sits on the item's own surface, with no housing or backdrop behind it ({label})");
            var art = Descendants(slot).OfType<SemanticArt>().SingleOrDefault();
            Check(art is not null && art.Concept == SemanticAssets.ForSection(id) && art.Texture is not null, $"rail item {id} shows its own approved module art ({label})");
            var size = art!.Size; var drawn = art.GetGlobalRect();
            Check(size.X <= ShellLayout.NavArt(roomy) + .5f && size.Y <= ShellLayout.NavArt(roomy) + .5f && MathF.Sqrt(size.X * size.Y) >= ShellLayout.NavOptical(roomy) * .9f
                && slot.GetGlobalRect().Grow(.5f).Encloses(drawn) && Inside(box, Cuts.All(Surfaces.CardCut), drawn),
                $"rail art for {id} reads at one perceived size inside its slot and the item's cuts ({label}, {size.X:0}×{size.Y:0})");
            if (navCollapsed)
            {
                Check(!navLabels[id].Visible && !navCounts[id].Visible && Math.Abs(drawn.GetCenter().X - box.GetCenter().X) <= 1,
                    $"collapsed rail item {id} shows its art alone, centred ({label})");
                continue;
            }
            var name = navLabels[id];
            Check(Math.Abs(slot.GetGlobalRect().GetCenter().Y - name.GetGlobalRect().GetCenter().Y) <= 2, $"rail art and label for {id} share one centre line ({label})");
            Check(name.GetThemeFontSize("font_size") == ui.Tokens.FontSize(TypographyRole.Label) && name.GetMinimumSize().X <= name.Size.X + .5f
                && Math.Abs(name.GetGlobalRect().Position.X - slot.GetGlobalRect().End.X - ShellLayout.NavGap(roomy)) <= .5f,
                $"rail label for {id} is whole, {ui.Tokens.FontSize(TypographyRole.Label)} px, {ShellLayout.NavGap(roomy)} px after its art ({label})");
            if (navCounts[id].Visible)
            {
                var badge = navCounts[id].GetGlobalRect();
                Check(badge.Position.X >= Ink(name).End.X + 8 && box.Encloses(badge) && Math.Abs(badge.GetCenter().Y - box.GetCenter().Y) <= 2,
                    $"the {id} badge sits clear of its label, centred inside the item ({label})");
            }
        }
        if (!navCollapsed)
            Check(Sections.Select(s => navLabels[s.Id].GetGlobalRect().Position.X).Distinct().Count() == 1, $"rail labels start on one column ({label})");
        var unread = view.Inbox.Count(x => !seenInbox.Contains(x.Id));
        var inbox = ShellSections.Inbox;
        Check((unread == 0 ? navCounts[inbox].Text.Length == 0 : navCounts[inbox].Text == unread.ToString(System.Globalization.CultureInfo.InvariantCulture))
            && navCounts[inbox].Visible == (!navCollapsed && unread > 0) && navButtons[inbox].TooltipText.Contains(unread > 0 ? $"{unread} unread" : "Inbox", StringComparison.Ordinal),
            $"the Inbox badge and tooltip carry the unread count from the inbox ({label}, {unread})");
        Check(Sections.All(s => (navButtons[s.Id].ThemeTypeVariation == "NavSelected") == (s.Id == section)
            && navLabels[s.Id].ThemeTypeVariation == (s.Id == section ? nameof(TypographyRole.Strong) : nameof(TypographyRole.Label))),
            $"only the current section is selected, its label semibold ({label})");
    }
}
