using System.Collections.Immutable;
using Godot;
using ManagementGame.Application;
using ManagementGame.SponsorWorkspace;
using ManagementGame.UiKit;

namespace ManagementGame.Shell;

/// <summary>Section screens (S3–S6). Each screen composes kit components from the current observation.</summary>
public partial class GameShell
{
    private string inboxFilter = "All";
    private string? squadSelection;
    private string? coachSelection;

    // ---------- Shared composition ----------
    private ScrollContainer Page(out VBoxContainer body, string title, string subtitle)
    {
        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, FollowFocus = true };
        var margin = new MarginContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        margin.AddThemeConstantOverride("margin_left", ui.Tokens.Space(SpaceRole.SpaceGroup));
        margin.AddThemeConstantOverride("margin_right", ui.Tokens.Space(SpaceRole.SpaceGroup));
        scroll.AddChild(margin);
        body = ui.Stack(); margin.AddChild(body);
        body.AddChild(SemanticText.Create(ui, title, TypographyRole.WorkspaceTitle));
        body.AddChild(SemanticText.Create(ui, subtitle, TypographyRole.Label, ColorRole.TextSecondary));
        return scroll;
    }
    private BoxContainer Columns(VBoxContainer parent, out VBoxContainer left, out VBoxContainer right, float leftRatio = 1.4f)
    {
        BoxContainer box = GetViewportRect().Size.X >= 1200 ? new HBoxContainer() : new VBoxContainer();
        box.SizeFlagsHorizontal = SizeFlags.ExpandFill; box.AddThemeConstantOverride("separation", ui.Tokens.Space(SpaceRole.SpaceGroup));
        left = ui.Stack(); right = ui.Stack(); left.SizeFlagsStretchRatio = leftRatio;
        box.AddChild(left); box.AddChild(right); parent.AddChild(box); return box;
    }
    /// <summary>A bounded region answering one question (S6); never decorative.</summary>
    private VBoxContainer Card(VBoxContainer parent, string title, string question = "")
    {
        var stack = ui.Stack(); stack.AddChild(SectionHeader.Create(ui, title, question));
        parent.AddChild(ui.Panel(stack, ColorRole.SurfaceRaised)); return stack;
    }
    private Label Line(VBoxContainer parent, string text, TypographyRole role = TypographyRole.Label, ColorRole tone = ColorRole.TextPrimary)
    { var label = SemanticText.Create(ui, text, role, tone); parent.AddChild(label); return label; }
    private Tree Table(string name, string[] columns, IEnumerable<(string Id, string[] Cells)> rows, Action<string>? select, string? selected = null)
    {
        var tree = new Tree { Name = name, Columns = columns.Length, ColumnTitlesVisible = true, HideRoot = true, SelectMode = Tree.SelectModeEnum.Row,
            SizeFlagsHorizontal = SizeFlags.ExpandFill, FocusMode = FocusModeEnum.All };
        for (var i = 0; i < columns.Length; i++) { tree.SetColumnTitle(i, columns[i]); tree.SetColumnExpand(i, true); tree.SetColumnTitleAlignment(i, HorizontalAlignment.Left); }
        var root = tree.CreateItem(); var count = 0; TreeItem? chosen = null;
        foreach (var (id, cells) in rows)
        {
            var item = tree.CreateItem(root); item.SetMetadata(0, id); count++;
            for (var i = 0; i < cells.Length; i++) item.SetText(i, cells[i]);
            if (id == selected) chosen = item;
        }
        // Tables show every row (S5): size to content so no squad fact hides behind an inner scroll.
        tree.ScrollVerticalEnabled = false; tree.ScrollHorizontalEnabled = false;
        tree.CustomMinimumSize = new Vector2(0, (count + 1) * (ui.Tokens.ControlHeight + ui.Tokens.Space(SpaceRole.SpaceRelated)) + ui.Tokens.Space(SpaceRole.SpaceGroup));
        // The first column names the row; it gets the most width.
        tree.SetColumnExpandRatio(0, 3); tree.SetColumnCustomMinimumWidth(0, (int)(ui.Tokens.ActionMinimumWidth * 1.15f));
        if (select is not null) tree.ItemSelected += () => { if (tree.GetSelected() is { } item) select(item.GetMetadata(0).AsString()); };
        chosen?.Select(0);
        return tree;
    }
    private OptionButton Seasons(string name)
    {
        var choice = new OptionButton { Name = name };
        foreach (var length in new[] { 1, 2, 3 }) choice.AddItem(length == 1 ? "1 season" : length + " seasons");
        return choice;
    }
    // Table cells carry the number only; units and estimate status live once in the column title.
    private static string Amount(long minor) => (minor / 100m).ToString("N2", System.Globalization.CultureInfo.InvariantCulture);
    private static string Band(string estimate) => estimate.Replace(" (estimate)", "", StringComparison.Ordinal);
    private static string Marker(TaskUrgency urgency) => urgency switch
    { TaskUrgency.Blocking => PresentationText.WarningMarker, TaskUrgency.Due => PresentationText.NeutralMarker, _ => PresentationText.InformationMarker(InformationState.Unknown) };
    private string Due(int? day) => day is null ? "" : day == view.Day ? " · today" : $" · day {day}";
    private static string ResultMarker(string result) => result switch { "Victory" => PresentationText.PositiveMarker, "" => "", _ => PresentationText.CriticalMarker };

    // ---------- Portal (S4) ----------
    private Control PortalScreen()
    {
        var page = Page(out var body, "Portal", $"Season {view.Season} · day {view.DayOfSeason} of {view.SeasonLength} · reputation {view.Reputation} · audience {view.Audience:N0}");
        var tasks = PortalTasks.Build(view);
        body.AddChild(SectionHeader.Create(ui, "Decisions", tasks.IsEmpty ? "Nothing needs a decision. Continue when ready." : "Blocking items must be resolved before time moves on."));
        var strip = ui.Flow(); strip.Name = "Tasks"; body.AddChild(strip);
        for (var i = 0; i < tasks.Length; i++)
        {
            var task = tasks[i];
            var button = ui.Button($"{Marker(task.Urgency)} {task.Text}{Due(task.DueDay)}", () => Navigate(task.Section));
            button.Name = "Task_" + i; button.AutowrapMode = TextServer.AutowrapMode.Off;
            if (task.Urgency == TaskUrgency.Blocking) Tone(button, ColorRole.StateWarning);
            strip.AddChild(button);
        }
        Columns(body, out var left, out var right);
        var inbox = Card(left, "Inbox", "What has the world told the company?");
        foreach (var row in view.Inbox.Take(6))
        {
            var unread = !seenInbox.Contains(row.Id);
            var entry = new Button { ThemeTypeVariation = "Entity", Alignment = HorizontalAlignment.Left, AutowrapMode = TextServer.AutowrapMode.WordSmart,
                Text = $"{(unread ? "● " : "")}Day {row.Day} · {row.Kind} — {row.Text}", FocusMode = FocusModeEnum.All };
            entry.Pressed += () => { seenInbox.Add(row.Id); Navigate(ShellSections.ForInbox(row.Kind)); };
            inbox.AddChild(entry);
        }
        if (view.Inbox.IsEmpty) Line(inbox, "No messages yet.", tone: ColorRole.TextSecondary);
        inbox.AddChild(ui.Button("Open inbox", () => Navigate(ShellSections.Inbox)));
        // P4: the four-week instrument is drawn, with its full text equivalent.
        var horizon = ui.Stack(); left.AddChild(ui.Panel(horizon, ColorRole.SurfaceRaised));
        horizon.AddChild(DayTrack.Create(ui, SponsorPresentation.Track(((ISponsorSession)session!).ObserveSponsors(), null)));

        var match = Card(right, "Next match", "Who do we face, and are we ready?");
        if (view.NextFixtureId.Length == 0) Line(match, "No match until next season.", tone: ColorRole.TextSecondary);
        else
        {
            Line(match, view.Opponent, TypographyRole.SectionTitle);
            Line(match, view.NextMatchDay == view.Day ? "Today" : $"Day {view.NextMatchDay} · in {view.NextMatchDay - view.Day} days");
            match.AddChild(ConfidenceIndicator.Create(ui, InformationState.Estimated, $"Strength {view.OpponentEstimate}; likely {view.OpponentTendency.ToLowerInvariant()} · {view.Confidence.ToLowerInvariant()}"));
            Line(match, view.CommittedPlan is null ? (view.Delegation == Delegation.Autonomous ? "The coach will plan this match." : $"{PresentationText.WarningMarker} No preparation plan yet.")
                : $"Plan committed: {view.CommittedPlan.Execution}/{view.CommittedPlan.Opponent}/{view.CommittedPlan.Meta} · {view.CommittedPlan.Posture}",
                tone: view.CommittedPlan is null && view.Delegation != Delegation.Autonomous ? ColorRole.StateWarning : ColorRole.TextSecondary);
            match.AddChild(ui.Button("Prepare match", () => Navigate(ShellSections.Competition)));
        }
        var season = Card(right, "Season record", "How are we doing against each rival?");
        var played = view.Fixtures.Where(f => f.Result.Length > 0).ToArray();
        Line(season, $"{played.Count(f => f.Result == "Victory")} wins · {played.Count(f => f.Result != "Victory")} losses · {view.Fixtures.Length - played.Length} to play", TypographyRole.Data);
        foreach (var rival in view.Rivals) Line(season, $"{rival.Name}  {rival.Wins}–{rival.Losses}{(rival.Remaining > 0 ? $"  ({rival.Remaining} left)" : "")}", tone: ColorRole.TextSecondary);
        var money = Card(right, "Money", "Can we afford our commitments?");
        money.AddChild(ResourceValue.Create(ui, new("Cash", view.Cash / 100m, "CU", InformationState.Known, "Settled today.")));
        money.AddChild(ResourceValue.Create(ui, new("7-day forecast", view.Forecast / 100m, "CU", InformationState.Estimated, "Scheduled items and running wages; unearned wins excluded.")));
        Line(money, $"Sponsor slots {view.ActiveSponsors}/{view.SponsorSlots} · load {view.Load}/{view.Capacity}", tone: view.Load > view.Capacity ? ColorRole.StateWarning : ColorRole.TextSecondary);
        return page;
    }

    // ---------- Inbox ----------
    private Control InboxScreen()
    {
        var page = Page(out var body, "Inbox", $"{view.Inbox.Length} recent messages · newest first");
        var filters = ui.Flow(); body.AddChild(filters);
        var groups = new (string Name, string[] Kinds)[] { ("All", []), ("Offers & deals", ["Offer", "Negotiation", "Market", "Commercial"]),
            ("Contracts", ["Contract", "Renewal"]), ("Matches & season", ["Match", "Season", "Meta"]) };
        foreach (var (name, _) in groups)
        {
            var filter = new Button { Text = name, ThemeTypeVariation = name == inboxFilter ? "SelectedEntity" : "", FocusMode = FocusModeEnum.All, Name = "Filter_" + name.Split(' ')[0] };
            filter.Pressed += () => { inboxFilter = name; ShowSection(); }; filters.AddChild(filter);
        }
        var kinds = groups.Single(g => g.Name == inboxFilter).Kinds;
        var rows = view.Inbox.Where(r => kinds.Length == 0 || kinds.Contains(r.Kind)).ToArray();
        foreach (var row in rows)
        {
            var unread = !seenInbox.Contains(row.Id);
            var entry = new Button { ThemeTypeVariation = "Entity", Alignment = HorizontalAlignment.Left, AutowrapMode = TextServer.AutowrapMode.WordSmart, FocusMode = FocusModeEnum.All,
                Text = $"{(unread ? "● " : "")}Day {row.Day} · {row.Kind}\n{row.Text}" };
            entry.Pressed += () => Navigate(ShellSections.ForInbox(row.Kind));
            body.AddChild(entry);
        }
        if (rows.Length == 0) Line(body, "No messages in this view.", tone: ColorRole.TextSecondary);
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
        var profile = ui.Stack(); profile.Name = "Profile"; right.AddChild(ui.Panel(profile, ColorRole.SurfaceRaised));
        squadSelection ??= view.People.FirstOrDefault()?.Id;
        void Show(string id) { squadSelection = id; Profile(profile, id); }
        var players = Card(left, "Players", "Who plays, what do they cost, and when do contracts end?");
        players.AddChild(Table("SquadTable", ["Player", "Role", "Execution", "Readiness", "Wage/day (CU)", "Contract ends"],
            view.People.Select(p => (p.Id, new[] { p.Name, p.Role, p.Execution.ToString(), p.Readiness.ToString(), Amount(p.Salary), (p.CanRenew ? "▲ " : "") + "day " + p.ContractEnd })), Show, squadSelection));
        var market = Card(left, "Free agents", "Who could we sign, and what would it cost?");
        market.AddChild(Table("FreeAgents", ["Name", "Role", "Ability (est.)", "Fee (CU)", "Wage/day (CU)", "Available until"],
            view.Candidates.Select(c => (c.Id, new[] { c.Name, c.Role, Band(c.AbilityEstimate), Amount(c.Fee), Amount(c.Salary), "day " + c.Deadline })), Show, squadSelection));
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
            Line(profile, $"Readiness {person.Readiness} · wage {Money(person.Salary)}/day · contract ends day {person.ContractEnd}");
            if (person.CanRenew)
            {
                var seasons = Seasons("RenewSeasons");
                Line(profile, $"{PresentationText.WarningMarker} Asks {Money(person.RenewalSalary)}/day to renew (now {Money(person.Salary)}).", tone: ColorRole.StateWarning);
                profile.AddChild(seasons);
                var renew = ui.PrimaryButton("Renew contract", () => Confirm("Renew contract", $"Renew {person.Name} for {seasons.Selected + 1} more season(s) at {Money(person.RenewalSalary)}/day?",
                    "Renew", () => Send(new RenewalDecision(person.Id, seasons.Selected + 1))));
                renew.Name = "Renew"; profile.AddChild(renew);
            }
            else Line(profile, "Renewal talks open near the end of the contract.", tone: ColorRole.TextSecondary);
            var release = ui.Button("Release player", () => Confirm("Release player", $"Release {person.Name}? Pay {Money(person.ExitCost)} now; wages stop. Arrears remain payable.",
                "Release", () => Send(new ReleaseDecision(person.Id))));
            release.Name = "Release"; release.Disabled = !person.CanRelease; profile.AddChild(release);
            if (!person.CanRelease) Line(profile, $"Only player in role {person.Role}; sign a replacement before releasing.", tone: ColorRole.TextSecondary);
            return;
        }
        if (view.Candidates.FirstOrDefault(c => c.Id == id) is { } candidate)
        {
            profile.AddChild(SectionHeader.Create(ui, candidate.Name, $"Role {candidate.Role} · free agent until day {candidate.Deadline}"));
            profile.AddChild(ConfidenceIndicator.Create(ui, InformationState.Estimated, "Ability " + candidate.AbilityEstimate));
            Line(profile, $"Signing fee {Money(candidate.Fee)} · wage {Money(candidate.Salary)}/day");
            var seasons = Seasons("SignSeasons"); profile.AddChild(seasons);
            var sign = ui.PrimaryButton("Sign player", () => Confirm("Sign player", $"Sign {candidate.Name} for {seasons.Selected + 1} season(s)? Pay {Money(candidate.Fee)} now and {Money(candidate.Salary)}/day. New signings start with reduced readiness.",
                "Sign", () => Send(new SigningDecision(candidate.Id, seasons.Selected + 1))));
            sign.Name = "Sign"; profile.AddChild(sign);
        }
    }

    // ---------- Competition ----------
    private Control CompetitionScreen()
    {
        var page = Page(out var body, "Competition", $"Public meta: {view.Meta.ToLowerInvariant()} · coach control: {view.Delegation.ToString().ToLowerInvariant()}");
        Columns(body, out var left, out var right);
        var match = Card(left, "Next match", "Who do we face, and what do we know?");
        if (view.NextFixtureId.Length == 0) Line(match, "No match until next season. Use the time for contracts and sponsors.", tone: ColorRole.TextSecondary);
        else
        {
            Line(match, $"{view.Opponent} · {(view.NextMatchDay == view.Day ? "today" : "day " + view.NextMatchDay)}", TypographyRole.SectionTitle);
            match.AddChild(ConfidenceIndicator.Create(ui, InformationState.Estimated, $"Strength {view.OpponentEstimate}; likely {view.OpponentTendency.ToLowerInvariant()} · {view.Confidence.ToLowerInvariant()}"));
            match.AddChild(ConfidenceIndicator.Create(ui, InformationState.Unknown, "Hidden opponent preparation and match-day variance."));
            var plan = Card(left, "Preparation plan", "Where should this week's work go?");
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
        var authority = Card(left, "Coach authority", "How much does the coach decide alone?");
        var mode = new OptionButton { Name = "Delegation" }; foreach (var name in Enum.GetNames<Delegation>()) mode.AddItem(name); mode.Selected = (int)view.Delegation;
        var ceiling = new OptionButton { Name = "Ceiling" }; foreach (var name in Enum.GetNames<Risk>()) ceiling.AddItem(name); ceiling.Selected = (int)view.Ceiling;
        var row = ui.Flow(); authority.AddChild(row); row.AddChild(Field("Coach control", mode)); row.AddChild(Field("Risk ceiling", ceiling));
        authority.AddChild(ui.Button("Apply authority", () => Send(new CoachDecision((Delegation)mode.Selected, (Risk)ceiling.Selected))));

        var fixtures = Card(right, "Season fixtures", "What is left this season, and what did we win?");
        fixtures.AddChild(Table("Fixtures", ["Day", "Opponent", "Stakes", "Result"],
            view.Fixtures.Select(f => (f.Id, new[] { f.Day.ToString(), f.Opponent, f.Importance == 2 ? "Double" : "Regular", f.Result.Length == 0 ? "—" : $"{ResultMarker(f.Result)} {f.Result}" })), null));
        var rivals = Card(right, "Head-to-head", "Which rivals have our number?");
        foreach (var rival in view.Rivals) Line(rivals, $"{rival.Name}  {rival.Wins}–{rival.Losses}{(rival.Remaining > 0 ? $"  ({rival.Remaining} left)" : "")}");
        return page;
    }
    private SpinBox Spin(string name, int value) => new() { Name = name, MinValue = 0, MaxValue = 100, Step = 5, Value = value, CustomMinimumSize = new Vector2(ui.Tokens.ActionMinimumWidth * 0.8f, 0) };

    // ---------- Commercial ----------
    private Control CommercialScreen()
    {
        var page = Page(out var body, "Commercial", $"Sponsor slots in use {view.ActiveSponsors}/{view.SponsorSlots} · delivery load {view.Load}/{view.Capacity}");
        Columns(body, out var left, out var right);
        var active = Card(left, "Active agreements", "Who pays us, for what load, until when?");
        foreach (var agreement in view.Sponsors) Line(active, agreement);
        if (view.Sponsors.IsEmpty) Line(active, "No active sponsors.", tone: ColorRole.TextSecondary);
        var offers = Card(left, "Offers", "Which terms are on the table, and until when?");
        foreach (var offer in view.Offers)
        {
            var entry = ui.Stack(); offers.AddChild(ui.Panel(entry, ColorRole.SurfaceInset));
            Line(entry, $"{offer.Name} · {offer.Origin.ToLowerInvariant()}", TypographyRole.SectionTitle);
            Line(entry, $"{Money(offer.Payment)} weekly for {offer.DurationDays} days · win bonus {Money(offer.WinBonus)} · +{offer.Load} load");
            Line(entry, $"{offer.Availability} · answer by day {offer.Deadline}", tone: offer.Availability == "Available" ? ColorRole.TextSecondary : ColorRole.StateWarning);
            var review = ui.Button("Review offer", () => OpenOffer(offer.Id)); review.Name = "Review_" + offer.Id.Replace(':', '_'); review.Disabled = offer.Availability != "Available";
            entry.AddChild(review);
        }
        if (view.Offers.IsEmpty) Line(offers, "No offers. Brands approach weekly when you meet their tier, or approach one yourself.", tone: ColorRole.TextSecondary);

        var approach = Card(right, "Approach a brand", "What terms should we propose?");
        var brands = new OptionButton { Name = "Brand" };
        foreach (var b in view.Brands) { brands.AddItem($"{b.Name} · {b.Sector}"); brands.SetItemMetadata(brands.ItemCount - 1, b.Id); }
        approach.AddChild(brands);
        var status = Line(approach, ""); var guide = Line(approach, "", tone: ColorRole.InformationEstimated);
        var payment = new SpinBox { Name = "Payment", MinValue = 5, MaxValue = 3000, Step = 5, Suffix = "CU/week", CustomMinimumSize = new Vector2(ui.Tokens.ActionMinimumWidth * 1.2f, 0) };
        var duration = new OptionButton { Name = "Duration" };
        var terms = ui.Flow(); approach.AddChild(terms); terms.AddChild(Field("Weekly payment", payment)); terms.AddChild(Field("Duration", duration));
        var send = ui.PrimaryButton("Send proposal", () =>
        {
            if (view.Brands.FirstOrDefault(x => x.Id == brands.GetSelectedMetadata().AsString()) is not { } brand || duration.ItemCount == 0) return;
            var minor = (long)Math.Round(payment.Value * 100); var days = duration.GetSelectedMetadata().AsInt32();
            Confirm("Send proposal", $"Propose {Money(minor)} per week for {days} days to {brand.Name}? Market guide {Money(brand.GuideLow)}–{Money(brand.GuideHigh)} (estimate). Asking more risks a counter-offer or a refusal with a cooling-off period.",
                "Send", () => Send(new ProposalDecision(brand.Id, minor, days)));
        });
        send.Name = "SendProposal"; approach.AddChild(send);
        void Bind()
        {
            if (brands.ItemCount == 0 || view.Brands.FirstOrDefault(x => x.Id == brands.GetSelectedMetadata().AsString()) is not { } brand) return;
            var open = brand.Availability.StartsWith("Open", StringComparison.Ordinal);
            status.Text = (open ? PresentationText.NeutralMarker : PresentationText.WarningMarker) + " " + brand.Availability;
            ui.Tone(status, open ? ColorRole.TextSecondary : ColorRole.StateWarning);
            guide.Text = $"{PresentationText.InformationMarker(InformationState.Estimated)} Market guide {Money(brand.GuideLow)}–{Money(brand.GuideHigh)} per week (estimate)";
            duration.Clear(); foreach (var days in brand.DurationDays) { duration.AddItem(days + " days"); duration.SetItemMetadata(duration.ItemCount - 1, days); }
            payment.Value = (brand.GuideLow + brand.GuideHigh) / 200.0; send.Disabled = !open || view.ActiveSponsors >= view.SponsorSlots;
        }
        brands.ItemSelected += _ => Bind();
        var firstOpen = view.Brands.Select((b, i) => (b, i)).FirstOrDefault(x => x.b.Availability == "Open to proposals");
        if (brands.ItemCount > 0) { brands.Select(firstOpen.b is null ? 0 : firstOpen.i); Bind(); }
        var pending = Card(right, "Proposals under review", "Who still owes us an answer?");
        foreach (var n in view.Negotiations) Line(pending, $"{n.Brand}: {Money(n.Payment)} weekly for {n.DurationDays} days · answer on day {n.ResponseDay}");
        if (view.Negotiations.IsEmpty) Line(pending, "None.", tone: ColorRole.TextSecondary);
        return page;
    }

    // ---------- Finance ----------
    private Control FinanceScreen()
    {
        var page = Page(out var body, "Finance", "Each day receipts settle first, then obligations; unpaid amounts become arrears.");
        Columns(body, out var left, out var right);
        var position = Card(left, "Position", "Where does our money stand?");
        position.AddChild(ResourceValue.Create(ui, new("Cash", view.Cash / 100m, "CU", InformationState.Known, "Settled today.")));
        position.AddChild(ResourceValue.Create(ui, new("7-day forecast", view.Forecast / 100m, "CU", InformationState.Estimated, "Scheduled items and running wages; unearned wins excluded.")));
        var arrears = view.Bills.Where(x => !x.Incoming && x.MissedDay is not null).Sum(x => x.Remaining);
        Line(position, arrears == 0 ? "No arrears." : $"{PresentationText.WarningMarker} Arrears {Money(arrears)}", tone: arrears == 0 ? ColorRole.TextSecondary : ColorRole.StateWarning);
        var wages = view.People.Select(p => (p.Salary, p.ContractEnd)).Append((view.CoachContract.Salary, view.CoachContract.ContractEnd)).ToArray();
        var outlook = Card(left, "Next 14 days", "When does money arrive and leave?");
        outlook.AddChild(Table("Outlook", ["Day", "Receipts (CU)", "Obligations (CU)", "Wages, projected (CU)", "Net (CU)"],
            Enumerable.Range(view.Day, 14).Select(d =>
            {
                var receipts = view.Bills.Where(x => x.Incoming && x.DueDay == d).Sum(x => x.Remaining);
                var obligations = view.Bills.Where(x => !x.Incoming && (x.DueDay == d || d == view.Day && x.DueDay < d)).Sum(x => x.Remaining);
                var projected = d > view.Day ? wages.Where(w => w.ContractEnd >= d).Sum(w => w.Salary) : 0;
                return ("day:" + d, new[] { d == view.Day ? "Today" : "Day " + d, Amount(receipts), Amount(obligations), Amount(projected), Amount(receipts - obligations - projected) });
            }), null));
        var income = Card(right, "Sponsor income", "Which agreements fund us?");
        foreach (var agreement in view.Sponsors) Line(income, agreement);
        if (view.Sponsors.IsEmpty) Line(income, "No sponsor income.", tone: ColorRole.StateWarning);
        var payroll = Card(right, "Payroll", "What do people cost per day?");
        Line(payroll, $"Players {Money(view.People.Sum(p => p.Salary))}/day · head coach {Money(view.CoachContract.Salary)}/day", TypographyRole.Data);
        return page;
    }

    // ---------- Staff ----------
    private Control StaffScreen()
    {
        var page = Page(out var body, "Staff", "The head coach drives preparation throughput and load capacity.");
        Columns(body, out var left, out var right, 0.8f);
        var coach = Card(left, "Head coach", "Is our coach under contract, and at what price?");
        var current = view.CoachContract;
        Line(coach, current.Name, TypographyRole.SectionTitle);
        Line(coach, $"Wage {Money(current.Salary)}/day · contract ends day {current.ContractEnd} · capacity {view.Capacity}");
        if (current.CanRenew)
        {
            Line(coach, $"{PresentationText.WarningMarker} Asks {Money(current.RenewalSalary)}/day to renew.", tone: ColorRole.StateWarning);
            var seasons = Seasons("CoachSeasons"); coach.AddChild(seasons);
            var renew = ui.PrimaryButton("Renew head coach", () => Confirm("Renew head coach", $"Renew {current.Name} for {seasons.Selected + 1} more season(s) at {Money(current.RenewalSalary)}/day?",
                "Renew", () => Send(new RenewalDecision(current.Id, seasons.Selected + 1))));
            renew.Name = "RenewCoach"; coach.AddChild(renew);
        }
        else Line(coach, "Renewal talks open near the end of the contract.", tone: ColorRole.TextSecondary);
        var market = Card(right, "Coach market", "Who could lead preparation instead?");
        coachSelection ??= view.CoachCandidates.FirstOrDefault()?.Id;
        market.AddChild(Table("CoachMarket", ["Name", "Skill (est.)", "Fee (CU)", "Wage/day (CU)", "Until"],
            view.CoachCandidates.Select(c => (c.Id, new[] { c.Name, Band(c.SkillEstimate), Amount(c.Fee), Amount(c.Salary), "day " + c.Deadline })), id => coachSelection = id, coachSelection));
        var hire = ui.Button("Hire selected coach", () =>
        {
            if (view.CoachCandidates.FirstOrDefault(c => c.Id == coachSelection) is not { } candidate) return;
            Confirm("Hire head coach", $"Hire {candidate.Name} for two seasons? Pay {Money(candidate.Fee)} plus {current.Name}'s exit cost now; {Money(candidate.Salary)}/day.",
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
