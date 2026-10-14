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
    private const string FreeAgentsView = "Free agents", AvailableView = "Available", UnderContractView = "Under contract";
    private string squadMarket = FreeAgentsView;
    private string coachMarket = AvailableView;
    /// <summary>Every player a rival employs, with the club, as the company scouts them (reference 03_squad: candidates
    /// with a status). They are shown, never offered: the company cannot sign a player under contract.</summary>
    private (string Club, RivalPlayerRow Player)[] RivalPlayers() =>
        [.. (view.RivalProfiles.IsDefault ? [] : view.RivalProfiles).SelectMany(r => r.Players.Select(p => (r.Name, p)))];
    /// <summary>Every rival head coach with the club (reference 09_staff: availability), shown as under contract.</summary>
    private RivalProfile[] RivalCoaches() => [.. (view.RivalProfiles.IsDefault ? [] : view.RivalProfiles).Where(r => r.CoachId.Length > 0)];

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
    /// <summary>Filters or views as segments on a recessed track; the active one is a lifted plate with the brass edge (polish
    /// standard §6). Each option may carry a muted count; pressing one calls <paramref name="choose"/>.</summary>
    private PanelContainer Segments(string trackName, string prefix, IEnumerable<(string Name, string Count)> options, string active, Action<string> choose)
    {
        var track = PanelFrame.Well(ui, 2); track.Name = trackName; track.SizeFlagsHorizontal = SizeFlags.ExpandFill; track.MouseFilter = MouseFilterEnum.Pass;
        var row = new HFlowContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; row.AddThemeConstantOverride("h_separation", 2); track.AddChild(row);
        foreach (var (name, count) in options)
        {
            var on = name == active;
            var segment = new CardButton(10, 2) { ThemeTypeVariation = on ? "SegmentSelected" : "Segment", Name = prefix + name.Split(' ')[0], SizeFlagsHorizontal = SizeFlags.ShrinkBegin }.Compact();
            var label = CardButton.Passive(new HBoxContainer()); label.AddThemeConstantOverride("separation", 5);
            label.AddChild(Caps(name, on ? ColorRole.TextPrimary : ColorRole.TextSecondary));
            if (count.Length > 0) label.AddChild(Caps(count, ColorRole.TextMuted));
            segment.Body.AddChild(label);
            segment.Pressed += () => choose(name); row.AddChild(segment);
        }
        return track;
    }
    private Label Line(VBoxContainer parent, string text, TypographyRole role = TypographyRole.Label, ColorRole tone = ColorRole.TextPrimary)
    { var label = SemanticText.Create(ui, text, role, tone); parent.AddChild(label); return label; }
    /// <summary>A dense table (S5). <paramref name="primary"/> is the column that names the row and gets the most width
    /// (−1: equal columns). <paramref name="numeric"/> lists the amount columns: they and their titles are right-aligned in
    /// tabular figures, so amounts line up by place value. <paramref name="fit"/> lists short label columns (a day) that, like
    /// amounts, must never be cut: each such column keeps room for its widest cell and title. A cell of any other column
    /// too narrow for its text is trimmed by the table; every cell keeps its full text as a tooltip, so nothing is lost
    /// (polish standard §7). <paramref name="crest"/> puts a team's crest before the text of one column, resolved from the
    /// row's stable ID (reference 05_league); the cell keeps its text, so the name is never replaced by the mark.</summary>
    private Tree Table(string name, string[] columns, IEnumerable<(string Id, string[] Cells)> rows, Action<string>? select, string? selected = null, int primary = 0,
        int[]? numeric = null, int[]? fit = null, (int Column, Func<string, string> AssetId)? crest = null)
    {
        bool Numeric(int column) => numeric is not null && numeric.Contains(column);
        int[] whole = [.. (numeric ?? []).Concat(fit ?? []).Distinct()];
        var tree = new Tree { Name = name, Columns = columns.Length, ColumnTitlesVisible = true, HideRoot = true, SelectMode = Tree.SelectModeEnum.Row,
            SizeFlagsHorizontal = SizeFlags.ExpandFill, FocusMode = FocusModeEnum.All };
        for (var i = 0; i < columns.Length; i++) { tree.SetColumnTitle(i, columns[i].ToUpperInvariant()); tree.SetColumnExpand(i, true); tree.SetColumnTitleAlignment(i, Numeric(i) ? HorizontalAlignment.Right : HorizontalAlignment.Left); }
        var root = tree.CreateItem(); var count = 0; TreeItem? chosen = null;
        var widest = new float[columns.Length];
        var cellFont = ui.Theme.GetFont("font", "Tree"); var cellSize = ui.Theme.GetFontSize("font_size", "Tree");
        // A crest is as tall as the cell's text, so rows keep the height the table is sized by. Each team's crest is drawn
        // once by the shared asset mark into a small transparent viewport, whose texture is the cell's icon: the tree draws
        // it like any icon, above the row's selection and before the text.
        var crestSize = new Vector2I((int)MathF.Round(MathF.Floor(cellFont.GetHeight(cellSize)) * 100f / 120), (int)MathF.Floor(cellFont.GetHeight(cellSize)));
        var crests = new Dictionary<string, Texture2D>();
        Texture2D Crest(string assetId)
        {
            if (crests.TryGetValue(assetId, out var texture)) return texture;
            var canvas = new SubViewport { Name = "Crest_" + crests.Count, Size = crestSize, TransparentBg = true, Disable3D = true,
                RenderTargetUpdateMode = SubViewport.UpdateMode.Once };
            canvas.AddChild(assets.Create(assetId, crestSize)); tree.AddChild(canvas);
            return crests[assetId] = canvas.GetTexture();
        }
        foreach (var (id, cells) in rows)
        {
            var item = tree.CreateItem(root); item.SetMetadata(0, id); count++;
            if (crest is { } c) { item.SetMetadata(c.Column, c.AssetId(id)); item.SetIcon(c.Column, Crest(c.AssetId(id))); }
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
        if (primary >= 0)
        {
            tree.SetColumnExpandRatio(primary, 3);
            tree.SetColumnCustomMinimumWidth(primary, (int)(ui.Tokens.ActionMinimumWidth * 1.15f) + (crest?.Column == primary ? crestSize.X + ui.Theme.GetConstant("h_separation", "Tree") : 0));
        }
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
            ("Contracts", ["Contract", "Renewal"]), ("Squad", ["Development", "Conduct", "Talent"]), ("Matches & season", ["Match", "Season", "Meta", "League"]) };
        messages.AddChild(Segments("InboxFilterTrack", "Filter_", groups.Select(g => (g.Name,
            view.Inbox.Count(r => g.Kinds.Length == 0 || g.Kinds.Contains(r.Kind)).ToString(System.Globalization.CultureInfo.InvariantCulture))), inboxFilter,
            name => { inboxFilter = name; ShowSection(); }));
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

    // ---------- Squad (S5; approved reference 03_squad_mockup) ----------
    private Control SquadScreen()
    {
        var missing = RoleNames.All.Where(r => !view.People.Any(p => p.Role == r || p.SecondaryRole == r)).ToArray();
        var page = Page(out var body, "Squad", missing.Length == 0 ? $"{view.People.Length} players · every role is covered"
            : $"{PresentationText.WarningMarker} No player for {string.Join(", ", missing.Select(RoleNames.Name))}: matches will be forfeited");
        Columns(body, out var left, out var right, 2.2f);
        right.CustomMinimumSize = new Vector2(ui.Tokens.Size(SizeRole.InspectorWidth) * (GetViewportRect().Size.X >= 1500 ? 1f : 0.82f), 0);
        // The profile holds the screen's decisions (renew, release, sign): the premium region.
        var profile = ui.Stack(); profile.Name = "Profile"; Card(right, "Profile", "", "squad", FrameTier.Premium).AddChild(profile);
        squadSelection ??= view.People.FirstOrDefault()?.Id;
        void Show(string id) { squadSelection = id; Profile(profile, id); }
        var players = Card(left, "Players", "Who plays, how good are they, what do they cost?", "squad");
        players.AddChild(Table("SquadTable", ["Player", "Role", "Rating", "Age", "Readiness", "Wage/day", "Contract ends"],
            view.People.Select(p => (p.Id, new[] { p.Name, RoleNames.Name(p.Role), p.Rating.ToString(Invariant), p.Age.ToString(Invariant), p.Readiness.ToString(Invariant),
                Amount(p.Salary), (p.CanRenew ? "▲ " : "") + "day " + p.ContractEnd })), Show, squadSelection, numeric: [2, 3, 4, 5], fit: [1, 6]));
        var market = Card(left, "Free agents / Candidates", "Who could we sign, and who is under contract elsewhere?", "transfer");
        var rivals = RivalPlayers();
        market.AddChild(Segments("MarketTrack", "Market_", new[] { (FreeAgentsView, Figures.Count(view.Candidates.Length)), (UnderContractView, Figures.Count(rivals.Length)) },
            squadMarket, name => { squadMarket = name; ShowSection(); }));
        if (squadMarket == UnderContractView)
            market.AddChild(Table("UnderContract", ["Player", "Role", "Age", "Rating (est.)", "Club"],
                rivals.Select(x => (x.Player.Id, new[] { x.Player.Name, RoleNames.Name(x.Player.Role), x.Player.Age.ToString(Invariant), Band(x.Player.RatingEstimate), x.Club })),
                Show, squadSelection, numeric: [2], fit: [1, 3, 4]));
        // The potential estimate is a column where the screen is roomy; on a compact screen it stays in the profile.
        else if (ShellLayout.Roomy(GetViewportRect().Size))
            market.AddChild(Table("FreeAgents", ["Player", "Role", "Age", "Rating (est.)", "Potential (est.)", "Fee", "Wage/day"],
                view.Candidates.Select(c => (c.Id, new[] { c.Name, RoleNames.Name(c.Role), c.Age.ToString(Invariant), Band(c.RatingEstimate), Band(c.PotentialEstimate),
                    Amount(c.Fee), Amount(c.Salary) })), Show, squadSelection, numeric: [5, 6], fit: [1, 2, 3, 4]));
        else
            market.AddChild(Table("FreeAgents", ["Player", "Role", "Age", "Rating (est.)", "Fee", "Wage/day"],
                view.Candidates.Select(c => (c.Id, new[] { c.Name, RoleNames.Name(c.Role), c.Age.ToString(Invariant), Band(c.RatingEstimate),
                    Amount(c.Fee), Amount(c.Salary) })), Show, squadSelection, numeric: [4, 5], fit: [1, 2, 3]));
        if (squadSelection is not null) Profile(profile, squadSelection);
        return page;
    }
    private static readonly System.Globalization.CultureInfo Invariant = System.Globalization.CultureInfo.InvariantCulture;
    /// <summary>One attribute as label, a ten-segment bar and the value; the bar is relative to the 1–99 scale.</summary>
    private void Attribute(VBoxContainer parent, string label, int value, string name)
    {
        var row = new HBoxContainer { Name = name }; row.AddThemeConstantOverride("separation", 10);
        var text = SemanticText.Create(ui, label, TypographyRole.Annotation, ColorRole.TextSecondary);
        text.AutowrapMode = TextServer.AutowrapMode.Off; text.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(text);
        var bar = SegmentBar.Create(ui, value, 99, 10, value >= 75 ? ColorRole.StatePositive : value >= 55 ? ColorRole.TextSecondary : ColorRole.StateWarning);
        row.AddChild(bar);
        var number = SemanticText.Create(ui, value.ToString(Invariant), TypographyRole.Data, ColorRole.TextPrimary);
        number.AddThemeFontSizeOverride("font_size", ui.Tokens.FontSize(TypographyRole.Annotation));
        number.CustomMinimumSize = new Vector2(28, 0); number.HorizontalAlignment = HorizontalAlignment.Right;
        row.AddChild(number);
        parent.AddChild(row);
    }
    private static string FormText(int form) => form switch { >= 4 => "Hot", >= 1 => "Good", 0 => "Steady", >= -3 => "Off", _ => "Cold" };
    private static string MoraleText(int morale) => morale switch { >= 75 => "High", >= 50 => "Good", >= 30 => "Low", _ => "Poor" };
    private void Profile(VBoxContainer profile, string id)
    {
        foreach (var child in profile.GetChildren()) { profile.RemoveChild(child); child.QueueFree(); }
        if (view.People.FirstOrDefault(p => p.Id == id) is { } person)
        {
            var secondary = person.SecondaryRole.Length > 0 ? $" · also {RoleNames.Name(person.SecondaryRole)} ({person.SecondaryRating})" : "";
            profile.AddChild(SectionHeader.Create(ui, person.Name, $"{RoleNames.Name(person.Role)}{secondary} · age {person.Age} · {person.Nationality} · {person.FullName}"));
            var rating = SemanticText.Create(ui, person.Rating.ToString(Invariant), TypographyRole.Display, ColorRole.StatePositive); rating.Name = "ProfileRating";
            profile.AddChild(rating);
            profile.AddChild(ConfidenceIndicator.Create(ui, InformationState.Known, "Rating in role, observed in training and competition."));
            if (person.PotentialEstimate.Length > 0)
                profile.AddChild(ConfidenceIndicator.Create(ui, InformationState.Estimated, $"Potential {person.PotentialEstimate} · {person.Trend.ToLowerInvariant()} at {person.Age}"));
            if (person.Skills is { } s)
            {
                profile.AddChild(Caps("Core attributes"));
                var attributes = ui.Stack(); attributes.Name = "ProfileAttributes"; attributes.AddThemeConstantOverride("separation", 2); profile.AddChild(attributes);
                foreach (var (label, value) in new[] { ("Mechanics", s.Mechanics), ("Early game", s.EarlyGame), ("Teamfight", s.Teamfight), ("Macro", s.Macro),
                    ("Champion pool", s.ChampionPool), ("Consistency", s.Consistency), ("Composure", s.Composure), ("Communication", s.Communication) })
                    Attribute(attributes, label, value, "Attribute_" + label.Replace(" ", ""));
            }
            profile.AddChild(Caps("Status"));
            Line(profile, $"Readiness {person.Readiness} · form {FormText(person.Form)} · morale {MoraleText(person.Morale)} · popularity {person.Popularity}");
            profile.AddChild(Caps("Contract"));
            Line(profile, $"Wage {Currency.PerDay(person.Salary)} · contract ends day {person.ContractEnd} · market value {Currency.Format(person.MarketValue)}");
            if (person.CanRenew)
            {
                var seasons = Seasons("RenewSeasons");
                Line(profile, $"{PresentationText.WarningMarker} Asks {Currency.PerDay(person.RenewalSalary)} to renew (now {Currency.PerDay(person.Salary)}).", tone: ColorRole.StateWarning);
                profile.AddChild(seasons);
                var renew = ui.PrimaryButton("Renew contract", () => Confirm("Renew contract", $"Renew {person.Name} for {seasons.Selected + 1} more season(s) at {Currency.PerDay(person.RenewalSalary)}?",
                    "Renew", () => Send(new RenewalDecision(person.Id, seasons.Selected + 1))));
                renew.Name = "Renew"; profile.AddChild(renew);
            }
            else Line(profile, person.RenewalNote.Length > 0 ? $"{PresentationText.WarningMarker} {person.RenewalNote}" : "Renewal talks open near the end of the contract.",
                tone: person.RenewalNote.Length > 0 ? ColorRole.StateWarning : ColorRole.TextSecondary);
            var release = ui.Button("Release player", () => Confirm("Release player", $"Release {person.Name}? Pay {Currency.Format(person.ExitCost)} now; wages stop. Arrears remain payable.",
                "Release", () => Send(new ReleaseDecision(person.Id))));
            release.Name = "Release"; release.Disabled = !person.CanRelease; profile.AddChild(release);
            if (!person.CanRelease) Line(profile, $"Only {RoleNames.Name(person.Role)} on the roster; sign a replacement before releasing.", tone: ColorRole.TextSecondary);
            return;
        }
        if (view.Candidates.FirstOrDefault(c => c.Id == id) is { } candidate)
        {
            var secondary = candidate.SecondaryRole.Length > 0 ? $" · also {RoleNames.Name(candidate.SecondaryRole)}" : "";
            profile.AddChild(SectionHeader.Create(ui, candidate.Name, $"{RoleNames.Name(candidate.Role)}{secondary} · age {candidate.Age} · {candidate.Nationality} · {candidate.FullName}"));
            profile.AddChild(ConfidenceIndicator.Create(ui, InformationState.Estimated, "Rating " + candidate.RatingEstimate));
            profile.AddChild(ConfidenceIndicator.Create(ui, InformationState.Estimated, "Potential " + candidate.PotentialEstimate));
            Line(profile, $"Popularity {candidate.Popularity} · free agent", tone: ColorRole.TextSecondary);
            Line(profile, $"Signing fee {Currency.Format(candidate.Fee)} · wage {Currency.PerDay(candidate.Salary)}");
            var seasons = Seasons("SignSeasons"); profile.AddChild(seasons);
            var sign = ui.PrimaryButton("Sign player", () => Confirm("Sign player", $"Sign {candidate.Name} for {seasons.Selected + 1} season(s)? Pay {Currency.Format(candidate.Fee)} now and {Currency.PerDay(candidate.Salary)}. New signings start with reduced readiness.",
                "Sign", () => Send(new SigningDecision(candidate.Id, seasons.Selected + 1))));
            sign.Name = "Sign"; profile.AddChild(sign);
            return;
        }
        if (RivalPlayers().FirstOrDefault(x => x.Player.Id == id) is { Player: not null } rival)
        {
            var p = rival.Player;
            profile.AddChild(SectionHeader.Create(ui, p.Name, $"{RoleNames.Name(p.Role)} · age {p.Age} · {rival.Club}"));
            profile.AddChild(ConfidenceIndicator.Create(ui, InformationState.Estimated, "Rating " + p.RatingEstimate));
            var status = Line(profile, $"Popularity {p.Popularity} · under contract with {rival.Club}", tone: ColorRole.TextSecondary); status.Name = "ContractStatus";
            Line(profile, "Not available to sign. A player reaches the free-agent market when their club releases them or their contract ends.", tone: ColorRole.TextSecondary);
        }
    }

    // ---------- Competition (approved references 04_competition and 05_league) ----------
    private string competitionView = "Match";
    private string? leagueSelection;
    /// <summary>The company's place in the league table, once a match is played ("" before).</summary>
    private string LeaguePlace() => view.Standings.IsDefaultOrEmpty || view.Standings.All(s => s.Played == 0) ? ""
        : $"{Figures.Ordinal(view.Standings.Single(s => s.Company).Place)} of {Figures.Count(view.Standings.Length)}";
    private Control CompetitionScreen()
    {
        var place = LeaguePlace();
        var page = Page(out var body, "Competition", $"Season {view.Season}{(place.Length > 0 ? " · " + place : "")} · public meta: {view.Meta.ToLowerInvariant()} · coach control: {view.Delegation.ToString().ToLowerInvariant()}");
        // Two views of one section (no extra rail item): the match workspace and the league around it.
        body.AddChild(Segments("CompetitionViewTrack", "View_", new[] { ("Match", ""), ("League", "") }, competitionView, name => { competitionView = name; ShowSection(); }));
        if (competitionView == "League") LeagueView(body); else MatchView(body);
        return page;
    }
    private void MatchView(VBoxContainer body)
    {
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
            // One choice per role in lineup order: the role's players first, then those who can cover it as a secondary role.
            foreach (var role in RoleNames.All)
            {
                var choice = new OptionButton { Name = "Role_" + role };
                foreach (var p in view.People.Where(p => p.Role == role)) { choice.AddItem($"{p.Name} · {p.Rating}/{p.Readiness}"); choice.SetItemMetadata(choice.ItemCount - 1, p.Id); }
                foreach (var p in view.People.Where(p => p.SecondaryRole == role)) { choice.AddItem($"{p.Name} · {p.SecondaryRating}/{p.Readiness} (secondary)"); choice.SetItemMetadata(choice.ItemCount - 1, p.Id); }
                lineup[role] = choice; roles.AddChild(Field(RoleNames.Name(role), choice));
            }
            void Apply(PlanView source)
            {
                execution.Value = source.Execution; opponent.Value = source.Opponent; meta.Value = source.Meta; posture.Selected = (int)source.Posture;
                for (var r = 0; r < RoleNames.All.Length && r < source.Lineup.Length; r++)
                {
                    var choice = lineup[RoleNames.All[r]];
                    for (var i = 0; i < choice.ItemCount; i++) if (choice.GetItemMetadata(i).AsString() == source.Lineup[r]) choice.Select(i);
                }
            }
            if ((view.CommittedPlan ?? view.Recommendation) is { } draft) Apply(draft);
            Line(plan, view.RecommendationReason, tone: ColorRole.TextSecondary);
            var buttons = ui.Flow(); plan.AddChild(buttons);
            var recommend = ui.Button("Use coach recommendation", () => { if (view.Recommendation is { } r) Apply(r); }); recommend.Name = "UseRecommendation"; buttons.AddChild(recommend);
            var commit = ui.PrimaryButton(view.CommittedPlan is null ? "Commit plan" : "Update plan", () =>
            {
                if (lineup.Values.Any(c => c.ItemCount == 0)) { Notify("Every role needs a player before a plan can be committed.", true); return; }
                Send(new PreparationDecision(view.NextFixtureId, (int)execution.Value, (int)opponent.Value, (int)meta.Value, (Risk)posture.Selected,
                    RoleNames.All.Select(r => lineup[r].GetSelectedMetadata().AsString()).ToImmutableArray()));
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
        fixtures.AddChild(Table("Fixtures", ["Day", "Opponent", "Stage", "Result"],
            view.Fixtures.Select(f => (f.Id, new[] { f.Day.ToString(Invariant), f.Opponent, Stage(f.Stage, f.Importance),
                f.Result.Length == 0 ? "—" : $"{ResultMarker(f.Result)} {f.Result} {f.Score}".TrimEnd() })), null, primary: 1, fit: [0, 2, 3]));
        var rivals = Card(right, "Head-to-head", "Which rivals have our number?", "record");
        foreach (var rival in view.Rivals) Line(rivals, $"{rival.Name}  {rival.Wins}–{rival.Losses}{(rival.Remaining > 0 ? $"  ({rival.Remaining} left)" : "")}");
    }
    /// <summary>A fixture's stage as the public names it; the second half of the regular season carries double stakes.</summary>
    private static string Stage(string stage, int importance) => stage switch
    { "semifinal" => "Semifinal", "final" => "Final", _ => importance >= 2 ? "Double" : "Regular" };

    /// <summary>The league around the company (reference 05_league): the derived table, this week's and next week's matches,
    /// the playoffs once they exist, and the league's format.</summary>
    private void LeagueView(VBoxContainer body)
    {
        var info = view.League;
        // The standings need less width than the match names beside them.
        Columns(body, out var left, out var right, 0.85f);
        var table = Card(left, "League standings", "Where do we stand, and who is above the playoff line?", "record", FrameTier.Premium);
        var standings = view.Standings.IsDefault ? [] : view.Standings;
        var company = standings.FirstOrDefault(s => s.Company);
        // Selecting a rival's row opens its profile below; the company's own row stays selected until then.
        table.AddChild(Table("Standings", ["#", "Team", "W", "L", "Map diff", "Pts"],
            standings.Select(s => (s.TeamId, new[] { s.Place.ToString(Invariant), s.Name, s.Won.ToString(Invariant), s.Lost.ToString(Invariant),
                (s.GamesWon - s.GamesLost).ToString("+#;−#;0", Invariant), s.Points.ToString(Invariant) })),
            id => { if (id == (leagueSelection ?? company?.TeamId)) return; leagueSelection = id; ShowSection(); }, leagueSelection ?? company?.TeamId,
            primary: 1, numeric: [2, 3, 4, 5], fit: [0], crest: (1, AssetIds.Crest)));
        if (info is not null)
            Line(table, $"Top {info.PlayoffTeams} reach the playoffs. Level teams are separated by map difference, then head-to-head, then maps won.", TypographyRole.Annotation, ColorRole.TextSecondary);

        // The rival profile: the selected rival, otherwise the next opponent, otherwise the leader.
        var profiles = view.RivalProfiles.IsDefault ? [] : view.RivalProfiles;
        if ((profiles.FirstOrDefault(p => p.Id == leagueSelection) ?? profiles.FirstOrDefault(p => p.Id == view.OpponentId)
            ?? profiles.FirstOrDefault(p => standings.Any(s => !s.Company && s.TeamId == p.Id && s.Place == standings.Where(x => !x.Company).Min(x => x.Place)))) is { } rival)
        {
            var team = Card(left, rival.Name, "Who plays for them, and how do they play?", "staff");
            Line(team, $"Head coach {rival.Coach} · plays {rival.Posture.ToLowerInvariant()} (estimate)", tone: ColorRole.TextSecondary);
            team.AddChild(ConfidenceIndicator.Create(ui, InformationState.Estimated, "Strength " + rival.LevelEstimate));
            team.AddChild(Table("RivalSquad", ["Player", "Role", "Age", "Rating (est.)"],
                rival.Players.Select(p => (p.Id, new[] { p.Name, RoleNames.Name(p.Role), p.Age.ToString(Invariant), Band(p.RatingEstimate) })), null, primary: 0, numeric: [2], fit: [1, 3]));
        }
        var matches = view.LeagueMatches.IsDefault ? [] : view.LeagueMatches;
        var playoffs = matches.Where(m => m.Stage != "regular").ToArray();
        if (playoffs.Length > 0)
        {
            var bracket = Card(right, "Playoffs", "Who is still in, and who plays whom?", "competition");
            bracket.AddChild(Table("Playoffs", ["Day", "Stage", "Match", "Score"],
                playoffs.Select(m => (m.Id, new[] { m.Day.ToString(Invariant), Stage(m.Stage, 3), MatchName(m), m.Score.Length == 0 ? "—" : m.Score })),
                null, matches.FirstOrDefault(m => m.Company && m.Stage != "regular")?.Id, primary: 2, fit: [0, 3]));
        }
        var week = Card(right, "Matches", "Who plays whom this week and next?", "calendar");
        var seasonStart = view.Day - view.DayOfSeason + 1;
        var thisWeek = (view.DayOfSeason - 1) / 7;
        foreach (var (title, index) in new[] { ("This week", thisWeek), ("Next week", thisWeek + 1) })
        {
            var first = seasonStart + index * 7; var last = Math.Min(first + 6, seasonStart + view.SeasonLength - 1);
            var rows = matches.Where(m => m.Day >= first && m.Day <= last && m.Stage == "regular").ToArray();
            week.AddChild(Caps(first > last ? title : $"{title} · days {first}–{last}"));
            if (rows.Length == 0) { Line(week, first > last ? "The season is over." : "No league matches.", TypographyRole.Annotation, ColorRole.TextSecondary); continue; }
            week.AddChild(Table(index == thisWeek ? "MatchesThisWeek" : "MatchesNextWeek", ["Day", "Match", "Score"],
                rows.Select(m => (m.Id, new[] { m.Day.ToString(Invariant), MatchName(m), m.Score.Length == 0 ? "—" : m.Score })),
                null, rows.FirstOrDefault(m => m.Company)?.Id, primary: 1, fit: [0, 2]));
        }

        if (info is null) return;
        var facts = Card(body, "League information", "How does the season work?", "plan");
        var flow = ui.Flow(); facts.AddChild(flow);
        void Fact(string label, string value)
        {
            var cell = ui.Stack(); cell.AddThemeConstantOverride("separation", 2); cell.CustomMinimumSize = new Vector2(ui.Tokens.ActionMinimumWidth * 1.6f, 0);
            cell.AddChild(Caps(label)); Line(cell, value); flow.AddChild(cell);
        }
        Fact("Regular season", $"{Figures.Count(info.Teams)} teams, double round robin, best of {Figures.Count(info.RegularGames)}");
        Fact("Matches per team", Figures.Count(info.MatchesPerTeam));
        Fact("Playoffs", $"Top {Figures.Count(info.PlayoffTeams)}, single elimination, best of {Figures.Count(info.PlayoffGames)}");
        Fact("Key days", $"Regular season ends day {Figures.Count(info.LastRegularDay)} · semifinals day {Figures.Count(info.SemifinalDay)} · final day {Figures.Count(info.FinalDay)}");
        var prizes = info.PlacePrizes.Select((amount, i) => (amount, place: i + 1)).Where(p => p.amount > 0).ToArray();
        if (prizes.Length > 0) Fact("Place prizes", string.Join(" · ", prizes.Select(p => $"{Figures.Ordinal(p.place)} {Currency.Format(p.amount)}")));
    }
    private string MatchName(LeagueMatchRow m) => $"{m.Home} vs {m.Away}";
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
        // Availability (reference 09_staff): the coaches the company may hire, and the rival head coaches, shown under
        // contract with their club and never approachable. A rival coach's skill is not scouted, so none is shown.
        var rivals = RivalCoaches();
        market.AddChild(Segments("CoachMarketTrack", "Coaches_", new[] { (AvailableView, Figures.Count(view.CoachCandidates.Length)), (UnderContractView, Figures.Count(rivals.Length)) },
            coachMarket, name => { coachMarket = name; ShowSection(); }));
        if (coachMarket == UnderContractView)
        {
            market.AddChild(Table("CoachesUnderContract", ["Name", "Club"], rivals.Select(r => (r.CoachId, new[] { r.Coach, r.Name })), null, coachSelection, fit: [1]));
            Line(market, "A head coach under contract with a rival cannot be approached.", TypographyRole.Annotation, ColorRole.TextSecondary);
            return page;
        }
        if (!view.CoachCandidates.Any(c => c.Id == coachSelection)) coachSelection = view.CoachCandidates.FirstOrDefault()?.Id;
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
