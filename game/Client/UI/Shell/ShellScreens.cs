using System.Collections.Immutable;
using Godot;
using ManagementGame.Application;
using ManagementGame.Formatting;
using ManagementGame.SponsorWorkspace;
using ManagementGame.UiKit;

namespace ManagementGame.Shell;

/// <summary>Section screens (S3–S6). Each screen composes kit components from the current observation.</summary>
public partial class GameShell
{
    private string inboxFilter = "All";
    private string? squadSelection;
    private string? coachSelection;

    // ---------- Shared composition (UI polish standard §5, §11) ----------
    /// <summary>Section page: the display title and one supporting line, closed by an engraved rule with a short brass
    /// lead, then the screen's regions. Same outer padding and gaps as the Portal.</summary>
    private ScrollContainer Page(out VBoxContainer body, string title, string subtitle)
    {
        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, FollowFocus = true };
        var margin = new MarginContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        foreach (var edge in new[] { "left", "right", "top", "bottom" }) margin.AddThemeConstantOverride("margin_" + edge, ShellLayout.ContentPadding);
        scroll.AddChild(margin);
        body = ui.Stack(); body.AddThemeConstantOverride("separation", ShellLayout.SectionGap); margin.AddChild(body);
        var head = new VBoxContainer { Name = "PageHeader" }; head.AddThemeConstantOverride("separation", 2);
        head.AddChild(SemanticText.Create(ui, title, TypographyRole.WorkspaceTitle));
        head.AddChild(SemanticText.Create(ui, subtitle, TypographyRole.Label, ColorRole.TextSecondary));
        var rule = EngravedRule.Create(ui, accentLength: 56); rule.SizeFlagsHorizontal = SizeFlags.ExpandFill; head.AddChild(rule);
        body.AddChild(head);
        return scroll;
    }
    private BoxContainer Columns(VBoxContainer parent, out VBoxContainer left, out VBoxContainer right, float leftRatio = 1.4f)
    {
        BoxContainer box = GetViewportRect().Size.X >= 1200 ? new HBoxContainer() : new VBoxContainer();
        box.SizeFlagsHorizontal = SizeFlags.ExpandFill; box.AddThemeConstantOverride("separation", ShellLayout.SectionGap);
        left = ui.Stack(); right = ui.Stack(); left.SizeFlagsStretchRatio = leftRatio;
        foreach (var column in new[] { left, right }) column.AddThemeConstantOverride("separation", ShellLayout.SectionGap);
        box.AddChild(left); box.AddChild(right); parent.AddChild(box); return box;
    }
    /// <summary>A region answering one question (S6): the shared frame and header plate of its tier. The question is the
    /// header's context (omitted whole where it does not fit; always in the header's tooltip). Returns the region body.</summary>
    private VBoxContainer Card(VBoxContainer parent, string title, string question = "", string icon = "plan", FrameTier tier = FrameTier.Standard)
    {
        var roomy = ShellLayout.Roomy(GetViewportRect().Size);
        var header = SectionHeader.Region(ui, icon, title, roomy, question, null, ColorRole.ActionPrimary, tier);
        header.TooltipText = question;
        var region = PanelFrame.Major(ui, "Region_" + title.Replace(' ', '_').Replace("-", ""), header, ShellLayout.RegionPadding(roomy), out var body, roomy ? 10 : 6, tier);
        region.SizeFlagsVertical = SizeFlags.Fill;
        parent.AddChild(region); return body;
    }
    /// <summary>Category label in the reference style: uppercase semibold caption.</summary>
    private Label Caps(string text, ColorRole tone = ColorRole.TextSecondary)
    {
        var label = SemanticText.Create(ui, text.ToUpperInvariant(), TypographyRole.Strong, tone);
        label.AutowrapMode = TextServer.AutowrapMode.Off; label.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
        label.AddThemeFontSizeOverride("font_size", ui.Tokens.FontSize(TypographyRole.Caption));
        return label;
    }
    private Label Line(VBoxContainer parent, string text, TypographyRole role = TypographyRole.Label, ColorRole tone = ColorRole.TextPrimary)
    { var label = SemanticText.Create(ui, text, role, tone); parent.AddChild(label); return label; }
    /// <summary>A dense table (S5). <paramref name="primary"/> is the column that names the row and gets the most width
    /// (−1: equal columns). <paramref name="numeric"/> lists the amount columns: they and their titles are right-aligned in
    /// tabular figures, so amounts line up by place value. <paramref name="fit"/> lists short label columns (a day) that, like
    /// amounts, must never be cut: each such column keeps room for its widest cell and title. A cell of any other column
    /// too narrow for its text is trimmed by the table; every cell keeps its full text as a tooltip, so nothing is lost
    /// (polish standard §7).</summary>
    private Tree Table(string name, string[] columns, IEnumerable<(string Id, string[] Cells)> rows, Action<string>? select, string? selected = null, int primary = 0,
        int[]? numeric = null, int[]? fit = null)
    {
        bool Numeric(int column) => numeric is not null && numeric.Contains(column);
        int[] whole = [.. (numeric ?? []).Concat(fit ?? []).Distinct()];
        var tree = new Tree { Name = name, Columns = columns.Length, ColumnTitlesVisible = true, HideRoot = true, SelectMode = Tree.SelectModeEnum.Row,
            SizeFlagsHorizontal = SizeFlags.ExpandFill, FocusMode = FocusModeEnum.All };
        for (var i = 0; i < columns.Length; i++) { tree.SetColumnTitle(i, columns[i].ToUpperInvariant()); tree.SetColumnExpand(i, true); tree.SetColumnTitleAlignment(i, Numeric(i) ? HorizontalAlignment.Right : HorizontalAlignment.Left); }
        var root = tree.CreateItem(); var count = 0; TreeItem? chosen = null;
        var widest = new float[columns.Length];
        var cellFont = ui.Theme.GetFont("font", "Tree"); var cellSize = ui.Theme.GetFontSize("font_size", "Tree");
        foreach (var (id, cells) in rows)
        {
            var item = tree.CreateItem(root); item.SetMetadata(0, id); count++;
            for (var i = 0; i < cells.Length; i++) widest[i] = Math.Max(widest[i], cellFont.GetStringSize(cells[i], HorizontalAlignment.Left, -1, cellSize).X);
            for (var i = 0; i < cells.Length; i++)
            {
                item.SetText(i, cells[i]); item.SetTooltipText(i, cells[i]);
                if (Numeric(i)) item.SetTextAlignment(i, HorizontalAlignment.Right);
                if (whole.Contains(i)) item.SetTextOverrunBehavior(i, TextServer.OverrunBehavior.NoTrimming);
            }
            if (id == selected) chosen = item;
        }
        // Whole columns are as wide as their widest cell or title plus the cell margins (Tree.inner_item_margin_*) and the
        // separation on both sides; the right margin keeps a right-aligned figure clear of the next column's text.
        var titleFont = ui.Theme.GetFont("title_button_font", "Tree"); var titleSize = ui.Theme.GetFontSize("title_button_font_size", "Tree");
        var titleStyle = ui.Theme.GetStylebox("title_button_normal", "Tree");
        foreach (var i in whole)
        {
            var heading = titleFont.GetStringSize(columns[i].ToUpperInvariant(), HorizontalAlignment.Left, -1, titleSize).X + titleStyle.ContentMarginLeft + titleStyle.ContentMarginRight;
            tree.SetColumnCustomMinimumWidth(i, (int)MathF.Ceiling(Math.Max(widest[i] + CellPadding(i), heading)));
        }
        tree.SetMeta("whole_columns", new Godot.Collections.Array<int>(whole));
        // Tables show every row (S5): size to content so no squad fact hides behind an inner scroll.
        tree.ScrollVerticalEnabled = false; tree.ScrollHorizontalEnabled = false;
        // Height from the table's own metrics (title strip plus one line and its separation per row), so the region ends
        // with the last row instead of a band of empty space; a few pixels of slack keep the last row whole.
        var rowHeight = ui.Theme.GetFont("font", "Tree").GetHeight(ui.Theme.GetFontSize("font_size", "Tree")) + ui.Theme.GetConstant("v_separation", "Tree");
        var title = ui.Theme.GetStylebox("title_button_normal", "Tree");
        var titleHeight = ui.Theme.GetFont("title_button_font", "Tree").GetHeight(ui.Theme.GetFontSize("title_button_font_size", "Tree")) + title.ContentMarginTop + title.ContentMarginBottom;
        tree.CustomMinimumSize = new Vector2(0, MathF.Ceiling(titleHeight + count * rowHeight) + 6);
        // The naming column gets the most width.
        if (primary >= 0) { tree.SetColumnExpandRatio(primary, 3); tree.SetColumnCustomMinimumWidth(primary, (int)(ui.Tokens.ActionMinimumWidth * 1.15f)); }
        if (select is not null) tree.ItemSelected += () => { if (tree.GetSelected() is { } item) select(item.GetMetadata(0).AsString()); };
        chosen?.Select(0);
        // Rows are defined by tone (polish standard §6): alternate rows sit a step darker; the selected row shows the brass
        // selection instead, so its tint is cleared whenever the selection moves.
        var alternate = UiTheme.ToGodot(ui.Tokens.Color(ColorRole.FrameOuter)) with { A = .24f };
        void Paint()
        {
            var index = 0;
            foreach (var item in root.GetChildren())
            {
                var tinted = index++ % 2 == 1 && !item.IsSelected(0);
                for (var c = 0; c < columns.Length; c++) { if (tinted) item.SetCustomBgColor(c, alternate); else item.ClearCustomBgColor(c); }
            }
        }
        Paint(); tree.ItemSelected += Paint;
        return tree;
    }
    /// <summary>Horizontal space a table cell takes besides its text: the cell margins, the separation on both sides and, in
    /// the first column, the row indent.</summary>
    private float CellPadding(int column) => ui.Theme.GetConstant("inner_item_margin_left", "Tree") + ui.Theme.GetConstant("inner_item_margin_right", "Tree")
        + 2 * ui.Theme.GetConstant("h_separation", "Tree") + (column == 0 ? ui.Theme.GetConstant("item_margin", "Tree") : 0);
    private OptionButton Seasons(string name)
    {
        var choice = new OptionButton { Name = name };
        foreach (var length in new[] { 1, 2, 3 }) choice.AddItem(length == 1 ? "1 season" : length + " seasons");
        return choice;
    }
    // Amount cells show dollars and cents, so a column lines up by place value; estimate status lives once in the column title.
    private static string Amount(long minor) => Currency.Cents(minor);
    private static string Band(string estimate) => estimate.Replace(" (estimate)", "", StringComparison.Ordinal);
    private static string Marker(TaskUrgency urgency) => urgency switch
    { TaskUrgency.Blocking => PresentationText.WarningMarker, TaskUrgency.Due => PresentationText.NeutralMarker, _ => PresentationText.InformationMarker(InformationState.Unknown) };
    private string Due(int? day) => day is null ? "" : day == view.Day ? " · today" : $" · day {day}";
    private static string ResultMarker(string result) => result switch { "Victory" => PresentationText.PositiveMarker, "" => "", _ => PresentationText.CriticalMarker };

    // ---------- Inbox ----------
    private Control InboxScreen()
    {
        var page = Page(out var body, "Inbox", $"{view.Inbox.Length} recent messages · newest first");
        var messages = Card(body, "Messages", "What happened, and who does it affect?", "inbox");
        var groups = new (string Name, string[] Kinds)[] { ("All", []), ("Offers & deals", ["Offer", "Negotiation", "Market", "Commercial"]),
            ("Contracts", ["Contract", "Renewal"]), ("Matches & season", ["Match", "Season", "Meta", "League"]) };
        // Filters are segments on a recessed track; the active one carries the brass edge (polish standard §6).
        var track = PanelFrame.Well(ui, 2); track.Name = "InboxFilterTrack"; track.SizeFlagsHorizontal = SizeFlags.ExpandFill; track.MouseFilter = MouseFilterEnum.Pass;
        var filters = new HFlowContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; filters.AddThemeConstantOverride("h_separation", 2); track.AddChild(filters);
        foreach (var (name, groupKinds) in groups)
        {
            var active = name == inboxFilter;
            var filter = new CardButton(10, 2) { ThemeTypeVariation = active ? "SegmentSelected" : "Segment", Name = "Filter_" + name.Split(' ')[0], SizeFlagsHorizontal = SizeFlags.ShrinkBegin }.Compact();
            var label = CardButton.Passive(new HBoxContainer()); label.AddThemeConstantOverride("separation", 5);
            label.AddChild(Caps(name, active ? ColorRole.TextPrimary : ColorRole.TextSecondary));
            label.AddChild(Caps(view.Inbox.Count(r => groupKinds.Length == 0 || groupKinds.Contains(r.Kind)).ToString(System.Globalization.CultureInfo.InvariantCulture), ColorRole.TextMuted));
            filter.Body.AddChild(label);
            filter.Pressed += () => { inboxFilter = name; ShowSection(); }; filters.AddChild(filter);
        }
        messages.AddChild(track);
        var kinds = groups.Single(g => g.Name == inboxFilter).Kinds;
        var rows = view.Inbox.Where(r => kinds.Length == 0 || kinds.Contains(r.Kind)).ToArray();
        for (var i = 0; i < rows.Length; i++)
        {
            var row = rows[i];
            // Each message is a row plate: kind and unread state as caps, its day, then the message naming who it affects.
            var entry = new CardButton(10, 6) { Name = "Message_" + i, ThemeTypeVariation = i % 2 == 0 ? "Row" : "RowAlt", TooltipText = row.Text };
            var meta = CardButton.Passive(new HBoxContainer()); meta.AddThemeConstantOverride("separation", 8);
            meta.AddChild(Caps(row.Kind));
            if (!seenInbox.Contains(row.Id)) meta.AddChild(Caps("New", ColorRole.StatePositive));
            meta.AddChild(CardButton.Passive(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill }));
            var day = SemanticText.Create(ui, $"Day {row.Day}", TypographyRole.Caption, ColorRole.TextMuted); day.AutowrapMode = TextServer.AutowrapMode.Off; day.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            meta.AddChild(day);
            entry.Body.AddChild(meta);
            entry.Body.AddChild(SemanticText.Create(ui, row.Text, TypographyRole.Label));
            entry.Pressed += () => Navigate(ShellSections.ForInbox(row.Kind));
            messages.AddChild(entry);
        }
        if (rows.Length == 0) Line(messages, "No messages in this view.", tone: ColorRole.TextSecondary);
        foreach (var row in view.Inbox) seenInbox.Add(row.Id);
        UpdateNav();
        return page;
    }

    // ---------- Squad (S5) ----------
    private Control SquadScreen()
    {
        var missing = "ABCDE".Select(r => r.ToString()).Where(r => !view.People.Any(p => p.Role == r)).ToArray();
        var page = Page(out var body, "Squad", missing.Length == 0 ? $"{view.People.Length} players · every role A–E is covered" : $"{PresentationText.WarningMarker} No player for role {string.Join(", ", missing)}: matches will be forfeited");
        Columns(body, out var left, out var right, 2.2f);
        right.CustomMinimumSize = new Vector2(ui.Tokens.Size(SizeRole.InspectorWidth) * (GetViewportRect().Size.X >= 1500 ? 1f : 0.82f), 0);
        // The profile holds the screen's decisions (renew, release, sign): the premium region.
        var profile = ui.Stack(); profile.Name = "Profile"; Card(right, "Profile", "", "squad", FrameTier.Premium).AddChild(profile);
        squadSelection ??= view.People.FirstOrDefault()?.Id;
        void Show(string id) { squadSelection = id; Profile(profile, id); }
        var players = Card(left, "Players", "Who plays, what do they cost, and when do contracts end?", "squad");
        players.AddChild(Table("SquadTable", ["Player", "Role", "Execution", "Readiness", "Wage/day", "Contract ends"],
            view.People.Select(p => (p.Id, new[] { p.Name, p.Role, p.Execution.ToString(), p.Readiness.ToString(), Amount(p.Salary), (p.CanRenew ? "▲ " : "") + "day " + p.ContractEnd })), Show, squadSelection, numeric: [4], fit: [5]));
        var market = Card(left, "Free agents", "Who could we sign, and what would it cost?", "transfer");
        market.AddChild(Table("FreeAgents", ["Name", "Role", "Ability (est.)", "Fee", "Wage/day", "Available until"],
            view.Candidates.Select(c => (c.Id, new[] { c.Name, c.Role, Band(c.AbilityEstimate), Amount(c.Fee), Amount(c.Salary), "day " + c.Deadline })), Show, squadSelection, numeric: [3, 4], fit: [5]));
        if (squadSelection is not null) Profile(profile, squadSelection);
        return page;
    }
    private void Profile(VBoxContainer profile, string id)
    {
        foreach (var child in profile.GetChildren()) { profile.RemoveChild(child); child.QueueFree(); }
        if (view.People.FirstOrDefault(p => p.Id == id) is { } person)
        {
            profile.AddChild(SectionHeader.Create(ui, person.Name, $"Role {person.Role} · under contract"));
            Line(profile, $"Execution {person.Execution}", TypographyRole.Data);
            profile.AddChild(ConfidenceIndicator.Create(ui, InformationState.Known, "Observed in competition."));
            Line(profile, $"Readiness {person.Readiness} · wage {Currency.PerDay(person.Salary)} · contract ends day {person.ContractEnd}");
            if (person.CanRenew)
            {
                var seasons = Seasons("RenewSeasons");
                Line(profile, $"{PresentationText.WarningMarker} Asks {Currency.PerDay(person.RenewalSalary)} to renew (now {Currency.PerDay(person.Salary)}).", tone: ColorRole.StateWarning);
                profile.AddChild(seasons);
                var renew = ui.PrimaryButton("Renew contract", () => Confirm("Renew contract", $"Renew {person.Name} for {seasons.Selected + 1} more season(s) at {Currency.PerDay(person.RenewalSalary)}?",
                    "Renew", () => Send(new RenewalDecision(person.Id, seasons.Selected + 1))));
                renew.Name = "Renew"; profile.AddChild(renew);
            }
            else Line(profile, "Renewal talks open near the end of the contract.", tone: ColorRole.TextSecondary);
            var release = ui.Button("Release player", () => Confirm("Release player", $"Release {person.Name}? Pay {Currency.Format(person.ExitCost)} now; wages stop. Arrears remain payable.",
                "Release", () => Send(new ReleaseDecision(person.Id))));
            release.Name = "Release"; release.Disabled = !person.CanRelease; profile.AddChild(release);
            if (!person.CanRelease) Line(profile, $"Only player in role {person.Role}; sign a replacement before releasing.", tone: ColorRole.TextSecondary);
            return;
        }
        if (view.Candidates.FirstOrDefault(c => c.Id == id) is { } candidate)
        {
            profile.AddChild(SectionHeader.Create(ui, candidate.Name, $"Role {candidate.Role} · free agent until day {candidate.Deadline}"));
            profile.AddChild(ConfidenceIndicator.Create(ui, InformationState.Estimated, "Ability " + candidate.AbilityEstimate));
            Line(profile, $"Signing fee {Currency.Format(candidate.Fee)} · wage {Currency.PerDay(candidate.Salary)}");
            var seasons = Seasons("SignSeasons"); profile.AddChild(seasons);
            var sign = ui.PrimaryButton("Sign player", () => Confirm("Sign player", $"Sign {candidate.Name} for {seasons.Selected + 1} season(s)? Pay {Currency.Format(candidate.Fee)} now and {Currency.PerDay(candidate.Salary)}. New signings start with reduced readiness.",
                "Sign", () => Send(new SigningDecision(candidate.Id, seasons.Selected + 1))));
            sign.Name = "Sign"; profile.AddChild(sign);
        }
    }

    // ---------- Competition ----------
    private Control CompetitionScreen()
    {
        var page = Page(out var body, "Competition", $"Public meta: {view.Meta.ToLowerInvariant()} · coach control: {view.Delegation.ToString().ToLowerInvariant()}");
        Columns(body, out var left, out var right);
        var match = Card(left, "Next match", "Who do we face, and what do we know?", "competition", FrameTier.Premium);
        if (view.NextFixtureId.Length == 0) Line(match, "No match until next season. Use the time for contracts and sponsors.", tone: ColorRole.TextSecondary);
        else
        {
            Line(match, $"{view.Opponent} · {(view.NextMatchDay == view.Day ? "today" : "day " + view.NextMatchDay)}", TypographyRole.SectionTitle);
            match.AddChild(ConfidenceIndicator.Create(ui, InformationState.Estimated, $"Strength {view.OpponentEstimate}; likely {view.OpponentTendency.ToLowerInvariant()} · {view.Confidence.ToLowerInvariant()}"));
            match.AddChild(ConfidenceIndicator.Create(ui, InformationState.Unknown, "Hidden opponent preparation and match-day variance."));
            var plan = Card(left, "Preparation plan", "Where should this week's work go?", "plan", FrameTier.Premium);
            var execution = Spin("Execution", 50); var opponent = Spin("Opponent", 30); var meta = Spin("Meta", 20);
            var allocation = ui.Flow(); plan.AddChild(allocation);
            allocation.AddChild(Field("Team execution %", execution)); allocation.AddChild(Field("Opponent study %", opponent)); allocation.AddChild(Field("Meta adaptation %", meta));
            var posture = new OptionButton { Name = "Posture" }; foreach (var name in Enum.GetNames<Risk>()) posture.AddItem(name); posture.Selected = (int)Risk.Balanced;
            plan.AddChild(Field("Posture", posture));
            var lineup = new Dictionary<string, OptionButton>(); var roles = ui.Flow(); plan.AddChild(roles);
            foreach (var role in "ABCDE".Select(r => r.ToString()))
            {
                var choice = new OptionButton { Name = "Role_" + role };
                foreach (var p in view.People.Where(p => p.Role == role)) { choice.AddItem($"{p.Name} · {p.Execution}/{p.Readiness}"); choice.SetItemMetadata(choice.ItemCount - 1, p.Id); }
                lineup[role] = choice; roles.AddChild(Field("Role " + role, choice));
            }
            void Apply(PlanView source)
            {
                execution.Value = source.Execution; opponent.Value = source.Opponent; meta.Value = source.Meta; posture.Selected = (int)source.Posture;
                foreach (var (role, choice) in lineup)
                    for (var i = 0; i < choice.ItemCount; i++) if (source.Lineup.Contains(choice.GetItemMetadata(i).AsString())) choice.Select(i);
            }
            if ((view.CommittedPlan ?? view.Recommendation) is { } draft) Apply(draft);
            Line(plan, view.RecommendationReason, tone: ColorRole.TextSecondary);
            var buttons = ui.Flow(); plan.AddChild(buttons);
            var recommend = ui.Button("Use coach recommendation", () => { if (view.Recommendation is { } r) Apply(r); }); recommend.Name = "UseRecommendation"; buttons.AddChild(recommend);
            var commit = ui.PrimaryButton(view.CommittedPlan is null ? "Commit plan" : "Update plan", () =>
            {
                if (lineup.Values.Any(c => c.ItemCount == 0)) { Notify("Every role needs a player before a plan can be committed.", true); return; }
                Send(new PreparationDecision(view.NextFixtureId, (int)execution.Value, (int)opponent.Value, (int)meta.Value, (Risk)posture.Selected,
                    lineup.Values.Select(c => c.GetSelectedMetadata().AsString()).ToImmutableArray()));
            });
            commit.Name = "CommitPlan"; buttons.AddChild(commit);
            Line(plan, view.CommittedPlan is null ? "No plan committed: preparation work does not accrue." : "Plan committed; you may change future work before the match.", tone: ColorRole.TextSecondary);
        }
        var authority = Card(left, "Coach authority", "How much does the coach decide alone?", "staff");
        var mode = new OptionButton { Name = "Delegation" }; foreach (var name in Enum.GetNames<Delegation>()) mode.AddItem(name); mode.Selected = (int)view.Delegation;
        var ceiling = new OptionButton { Name = "Ceiling" }; foreach (var name in Enum.GetNames<Risk>()) ceiling.AddItem(name); ceiling.Selected = (int)view.Ceiling;
        var row = ui.Flow(); authority.AddChild(row); row.AddChild(Field("Coach control", mode)); row.AddChild(Field("Risk ceiling", ceiling));
        authority.AddChild(ui.Button("Apply authority", () => Send(new CoachDecision((Delegation)mode.Selected, (Risk)ceiling.Selected))));

        var fixtures = Card(right, "Season fixtures", "What is left this season, and what did we win?", "calendar");
        fixtures.AddChild(Table("Fixtures", ["Day", "Opponent", "Stakes", "Result"],
            view.Fixtures.Select(f => (f.Id, new[] { f.Day.ToString(), f.Opponent, f.Importance == 2 ? "Double" : "Regular", f.Result.Length == 0 ? "—" : $"{ResultMarker(f.Result)} {f.Result}" })), null, primary: 1, fit: [0]));
        var rivals = Card(right, "Head-to-head", "Which rivals have our number?", "record");
        foreach (var rival in view.Rivals) Line(rivals, $"{rival.Name}  {rival.Wins}–{rival.Losses}{(rival.Remaining > 0 ? $"  ({rival.Remaining} left)" : "")}");
        return page;
    }
    private SpinBox Spin(string name, int value) => new() { Name = name, MinValue = 0, MaxValue = 100, Step = 5, Value = value, CustomMinimumSize = new Vector2(ui.Tokens.ActionMinimumWidth * 0.8f, 0) };

    // ---------- Commercial ----------
    private Control CommercialScreen()
    {
        var page = Page(out var body, "Commercial", $"Sponsor slots in use {view.ActiveSponsors}/{view.SponsorSlots} · delivery load {view.Load}/{view.Capacity}");
        Columns(body, out var left, out var right);
        var active = Card(left, "Active agreements", "Who pays us, for what load, until when?", "contract");
        foreach (var agreement in view.Sponsors) Line(active, agreement);
        if (view.Sponsors.IsEmpty) Line(active, "No active sponsors.", tone: ColorRole.TextSecondary);
        var offers = Card(left, "Offers", "Which terms are on the table, and until when?", "commercial");
        foreach (var offer in view.Offers)
        {
            var entry = ui.Stack(); offers.AddChild(ui.Panel(entry, ColorRole.SurfaceInset));
            Line(entry, $"{offer.Name} · {offer.Origin.ToLowerInvariant()}", TypographyRole.SectionTitle);
            Line(entry, $"{Currency.Format(offer.Payment)} weekly for {offer.DurationDays} days · win bonus {Currency.Format(offer.WinBonus)} · +{offer.Load} load");
            Line(entry, $"{offer.Availability} · answer by day {offer.Deadline}", tone: offer.Availability == "Available" ? ColorRole.TextSecondary : ColorRole.StateWarning);
            var review = ui.Button("Review offer", () => OpenOffer(offer.Id)); review.Name = "Review_" + offer.Id.Replace(':', '_'); review.Disabled = offer.Availability != "Available";
            entry.AddChild(review);
        }
        if (view.Offers.IsEmpty) Line(offers, "No offers. Brands approach weekly when you meet their tier, or approach one yourself.", tone: ColorRole.TextSecondary);

        var approach = Card(right, "Approach a brand", "What terms should we propose?", "commercial", FrameTier.Premium);
        var brands = new OptionButton { Name = "Brand" };
        foreach (var b in view.Brands) { brands.AddItem($"{b.Name} · {b.Sector}"); brands.SetItemMetadata(brands.ItemCount - 1, b.Id); }
        approach.AddChild(brands);
        var status = Line(approach, ""); var guide = Line(approach, "", tone: ColorRole.InformationEstimated);
        var payment = new SpinBox { Name = "Payment", MinValue = 5, MaxValue = 3000, Step = 5, CustomMinimumSize = new Vector2(ui.Tokens.ActionMinimumWidth * 1.2f, 0) };
        var duration = new OptionButton { Name = "Duration" };
        var terms = ui.Flow(); approach.AddChild(terms); terms.AddChild(Field($"Weekly payment ({Currency.Code})", payment)); terms.AddChild(Field("Duration", duration));
        var send = ui.PrimaryButton("Send proposal", () =>
        {
            if (view.Brands.FirstOrDefault(x => x.Id == brands.GetSelectedMetadata().AsString()) is not { } brand || duration.ItemCount == 0) return;
            var minor = (long)Math.Round(payment.Value * Currency.MinorPerUnit); var days = duration.GetSelectedMetadata().AsInt32();
            Confirm("Send proposal", $"Propose {Currency.Format(minor)} per week for {days} days to {brand.Name}? Market guide {Currency.Format(brand.GuideLow)}–{Currency.Format(brand.GuideHigh)} (estimate). Asking more risks a counter-offer or a refusal with a cooling-off period.",
                "Send", () => Send(new ProposalDecision(brand.Id, minor, days)));
        });
        send.Name = "SendProposal"; approach.AddChild(send);
        void Bind()
        {
            if (brands.ItemCount == 0 || view.Brands.FirstOrDefault(x => x.Id == brands.GetSelectedMetadata().AsString()) is not { } brand) return;
            var open = brand.Availability.StartsWith("Open", StringComparison.Ordinal);
            status.Text = (open ? PresentationText.NeutralMarker : PresentationText.WarningMarker) + " " + brand.Availability;
            ui.Tone(status, open ? ColorRole.TextSecondary : ColorRole.StateWarning);
            guide.Text = $"{PresentationText.InformationMarker(InformationState.Estimated)} Market guide {Currency.Format(brand.GuideLow)}–{Currency.Format(brand.GuideHigh)} per week (estimate)";
            duration.Clear(); foreach (var days in brand.DurationDays) { duration.AddItem(days + " days"); duration.SetItemMetadata(duration.ItemCount - 1, days); }
            payment.Value = (brand.GuideLow + brand.GuideHigh) / (2.0 * Currency.MinorPerUnit); send.Disabled = !open || view.ActiveSponsors >= view.SponsorSlots;
        }
        brands.ItemSelected += _ => Bind();
        var firstOpen = view.Brands.Select((b, i) => (b, i)).FirstOrDefault(x => x.b.Availability == "Open to proposals");
        if (brands.ItemCount > 0) { brands.Select(firstOpen.b is null ? 0 : firstOpen.i); Bind(); }
        var pending = Card(right, "Proposals under review", "Who still owes us an answer?", "decisions");
        foreach (var n in view.Negotiations) Line(pending, $"{n.Brand}: {Currency.Format(n.Payment)} weekly for {n.DurationDays} days · answer on day {n.ResponseDay}");
        if (view.Negotiations.IsEmpty) Line(pending, "None.", tone: ColorRole.TextSecondary);
        return page;
    }

    // ---------- Finance ----------
    private Control FinanceScreen()
    {
        var page = Page(out var body, "Finance", "Each day receipts settle first, then obligations; unpaid amounts become arrears.");
        Columns(body, out var left, out var right);
        var position = Card(left, "Position", "Where does our money stand?", "cash");
        position.AddChild(ResourceValue.Create(ui, SponsorPresentation.Amount("Cash", view.Cash, InformationState.Known, "Settled today.")));
        position.AddChild(ResourceValue.Create(ui, SponsorPresentation.Amount("7-day forecast", view.Forecast, InformationState.Estimated, "Scheduled items, running wages and operations; unearned wins excluded.")));
        var arrears = view.Bills.Where(x => !x.Incoming && x.MissedDay is not null).Sum(x => x.Remaining);
        Line(position, arrears == 0 ? "No arrears." : $"{PresentationText.WarningMarker} Arrears {Currency.Format(arrears)}", tone: arrears == 0 ? ColorRole.TextSecondary : ColorRole.StateWarning);
        var wages = view.People.Select(p => (p.Salary, p.ContractEnd)).Append((view.CoachContract.Salary, view.CoachContract.ContractEnd)).ToArray();
        var outlook = Card(left, "Next 14 days", "When does money arrive and leave?", "calendar");
        var days = Table("Outlook", ["Day", "Receipts", "Obligations", "Running costs (est.)", "Net"],
            Enumerable.Range(view.Day, 14).Select(d =>
            {
                var receipts = view.Bills.Where(x => x.Incoming && x.DueDay == d).Sum(x => x.Remaining);
                var obligations = view.Bills.Where(x => !x.Incoming && (x.DueDay == d || d == view.Day && x.DueDay < d)).Sum(x => x.Remaining);
                var projected = d > view.Day ? wages.Where(w => w.ContractEnd >= d).Sum(w => w.Salary) + view.OperatingCost : 0;
                return ("day:" + d, new[] { d == view.Day ? "Today" : "Day " + d, Amount(receipts), Amount(obligations), Amount(projected), Amount(receipts - obligations - projected) });
            }), null, primary: -1, numeric: [1, 2, 3, 4], fit: [0]);
        // The day column keeps room for its whole label ("Today", "Day 120"); the amount columns share the rest.
        days.SetColumnExpand(0, false);
        outlook.AddChild(days);
        var income = Card(right, "Sponsor income", "Which agreements fund us?", "commercial");
        foreach (var agreement in view.Sponsors) Line(income, agreement);
        if (view.Sponsors.IsEmpty) Line(income, "No sponsor income.", tone: ColorRole.StateWarning);
        // Running costs: wages by headcount, and the fixed cost the company carries whatever the roster (rules v3).
        var payroll = Card(right, "Running costs", "What does the company cost per day?", "staff");
        // One cost per line: a rate never breaks across lines.
        Line(payroll, $"Players {Currency.PerDay(view.People.Sum(p => p.Salary))}", TypographyRole.Data);
        Line(payroll, $"Head coach {Currency.PerDay(view.CoachContract.Salary)}", TypographyRole.Data);
        if (view.OperatingCost > 0)
        {
            Line(payroll, $"Operations {Currency.PerDay(view.OperatingCost)}", TypographyRole.Data);
            Line(payroll, "Office and league registration, owed whatever the roster.", tone: ColorRole.TextSecondary);
        }
        if (view.MinimumRoster > 0)
            Line(payroll, view.People.Length < view.MinimumRoster
                ? $"{PresentationText.WarningMarker} Registered roster {view.People.Length} of {view.MinimumRoster}: each match owes its prize as a fine"
                : $"Registered roster {view.People.Length} of the league's minimum {view.MinimumRoster}",
                tone: view.People.Length < view.MinimumRoster ? ColorRole.StateWarning : ColorRole.TextSecondary);
        return page;
    }

    // ---------- Staff ----------
    private Control StaffScreen()
    {
        var page = Page(out var body, "Staff", "The head coach drives preparation throughput and load capacity.");
        Columns(body, out var left, out var right, 0.8f);
        var coach = Card(left, "Head coach", "Is our coach under contract, and at what price?", "staff");
        var current = view.CoachContract;
        Line(coach, current.Name, TypographyRole.SectionTitle);
        Line(coach, $"Wage {Currency.PerDay(current.Salary)} · contract ends day {current.ContractEnd} · capacity {view.Capacity}");
        if (current.CanRenew)
        {
            Line(coach, $"{PresentationText.WarningMarker} Asks {Currency.PerDay(current.RenewalSalary)} to renew.", tone: ColorRole.StateWarning);
            var seasons = Seasons("CoachSeasons"); coach.AddChild(seasons);
            var renew = ui.PrimaryButton("Renew head coach", () => Confirm("Renew head coach", $"Renew {current.Name} for {seasons.Selected + 1} more season(s) at {Currency.PerDay(current.RenewalSalary)}?",
                "Renew", () => Send(new RenewalDecision(current.Id, seasons.Selected + 1))));
            renew.Name = "RenewCoach"; coach.AddChild(renew);
        }
        else Line(coach, "Renewal talks open near the end of the contract.", tone: ColorRole.TextSecondary);
        var market = Card(right, "Coach market", "Who could lead preparation instead?", "transfer");
        coachSelection ??= view.CoachCandidates.FirstOrDefault()?.Id;
        market.AddChild(Table("CoachMarket", ["Name", "Skill (est.)", "Fee", "Wage/day", "Until"],
            view.CoachCandidates.Select(c => (c.Id, new[] { c.Name, Band(c.SkillEstimate), Amount(c.Fee), Amount(c.Salary), "day " + c.Deadline })), id => coachSelection = id, coachSelection, numeric: [2, 3], fit: [4]));
        var hire = ui.Button("Hire selected coach", () =>
        {
            if (view.CoachCandidates.FirstOrDefault(c => c.Id == coachSelection) is not { } candidate) return;
            Confirm("Hire head coach", $"Hire {candidate.Name} for two seasons? Pay {Currency.Format(candidate.Fee)} plus {current.Name}'s exit cost now; {Currency.PerDay(candidate.Salary)}.",
                "Hire", () => Send(new HireCoachDecision(candidate.Id)));
        });
        hire.Name = "HireCoach"; hire.Disabled = view.CoachCandidates.IsEmpty; market.AddChild(hire);
        return page;
    }

    // ---------- Company (optional operating-space view) ----------
    private Control CompanyScreen()
    {
        var host = new SponsorCompanyHost();
        // The rail already navigates; the embedded view drops its own back control and watermark.
        host.Configure(session!, () => Navigate(ShellSections.Portal), embedded: true);
        return host;
    }
}
