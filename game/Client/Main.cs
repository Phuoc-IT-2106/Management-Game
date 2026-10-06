using Godot;
using ManagementGame.Application;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

public partial class Main : Control
{
    private IGameSession session = null!;
    private Situation view = null!;
    private long requestId;
    private string contentPath = "", saveDirectory = "", activeSlot = "campaign";
    private readonly Label heading = new(), status = new(), message = new(), dashboard = new(), finance = new(), commercial = new(), review = new(), opponent = new(), recommendation = new(), rosterCount = new();
    private readonly TabContainer tabs = new();
    private readonly Tree roster = new();
    private TreeItem root = null!;
    private readonly List<TreeItem> rows = [];
    private readonly LineEdit search = new() { PlaceholderText = "Search roster by name or ID", CustomMinimumSize = new Vector2(240, 0) };
    private readonly OptionButton roleFilter = new(), sort = new(), posture = new(), authority = new(), ceiling = new(), candidates = new(), offers = new(),
        contractSeasons = new(), coachCandidates = new(), brands = new(), proposalDuration = new();
    private readonly SpinBox proposalPayment = new() { MinValue = 5, MaxValue = 3000, Step = 5, Value = 300, Suffix = "CU/week", CustomMinimumSize = new Vector2(170, 0) };
    private readonly LineEdit companyInput = new() { PlaceholderText = "Company name", CustomMinimumSize = new Vector2(200, 0) };
    private readonly SpinBox execution = new() { MinValue = 0, MaxValue = 100, Value = 50 }, opponentWork = new() { MinValue = 0, MaxValue = 100, Value = 30 }, meta = new() { MinValue = 0, MaxValue = 100, Value = 20 };
    private readonly Dictionary<string, OptionButton> lineup = [];
    private readonly ConfirmationDialog confirmation = new();
    private Action? confirmed;
    private string? selectedId;
    private int page;
    private Button commit = null!, advance = null!, sign = null!, sponsor = null!, release = null!, coach = null!, renew = null!, renewCoach = null!, hire = null!, propose = null!;
    private readonly List<double> binds = [], acknowledgements = [], frames = [];
    private bool benchmark, refreshing;
    private int benchmarkFrames, controlsBefore, captureFrames;
    private ImmutableArray<PersonRow> stressRows;
    private long lastFrame, memoryBefore;
    private readonly LineEdit seedInput = new() { Text = "20261004", CustomMinimumSize = new Vector2(130, 0) };
    private static string Money(long value) => (value / 100m).ToString("N2", CultureInfo.InvariantCulture) + " CU";
    private static string? Arg(string key) => OS.GetCmdlineUserArgs().FirstOrDefault(x => x.StartsWith(key + "=", StringComparison.Ordinal))?[(key.Length + 1)..];

    public override void _Ready()
    {
        try
        {
            contentPath = Arg("--content") ?? System.IO.Path.GetFullPath(System.IO.Path.Combine(ProjectSettings.GlobalizePath("res://"), "../../content/fixture.json"));
            saveDirectory = Arg("--saves") ?? ProjectSettings.GlobalizePath("user://saves");
            session = Composition.Create(contentPath, saveDirectory, 20261004);
            Build(); Refresh();
            benchmark = OS.GetCmdlineUserArgs().Contains("--benchmark");
            if (OS.GetCmdlineUserArgs().Contains("--smoke")) CallDeferred(MethodName.RunSmoke);
            if (benchmark) { tabs.CurrentTab = 1; stressRows = Enumerable.Range(0,1000).Select(i => new PersonRow($"stress:{i:D6}","Synthetic person " + i,"ABCDE"[i%5].ToString(),70,90,i,0,false)).ToImmutableArray(); memoryBefore = GC.GetTotalMemory(true); controlsBefore = CountControls(this); lastFrame = Stopwatch.GetTimestamp(); }
            if (int.TryParse(Arg("--tab"), out var tab)) tabs.CurrentTab = Math.Clamp(tab,0,5);
        }
        catch (Exception e) { GD.PrintErr(e.ToString()); GetTree().Quit(1); }
    }
    private void Build()
    {
        var theme = new Theme { DefaultFontSize = 17 }; theme.SetColor("font_color", "Label", new Color("dce6ee")); Theme = theme;
        RenderingServer.SetDefaultClearColor(new Color("101923"));
        var margin = new MarginContainer(); margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        foreach (var edge in new[] { "left", "right", "top", "bottom" }) margin.AddThemeConstantOverride("margin_" + edge, 20);
        AddChild(margin); var shell = new VBoxContainer(); shell.AddThemeConstantOverride("separation", 12); margin.AddChild(shell);
        var title = new HBoxContainer(); shell.AddChild(title); heading.AddThemeFontSizeOverride("font_size", 28); heading.SizeFlagsHorizontal = SizeFlags.ExpandFill; title.AddChild(heading);
        title.AddChild(companyInput); title.AddChild(new Label { Text = "Campaign seed" }); title.AddChild(seedInput);
        title.AddChild(Button("New campaign", () => Ask("Start a fresh campaign? Unsaved progress will be replaced.", () =>
        {
            if (!ulong.TryParse(seedInput.Text, out var seed)) { message.Text = "Enter an unsigned integer seed."; return; }
            session = Composition.Create(contentPath, saveDirectory, seed, companyInput.Text); requestId = 0; selectedId = null; activeSlot = "campaign"; Refresh();
        })));
        var toolbar = new HBoxContainer(); shell.AddChild(toolbar); status.SizeFlagsHorizontal = SizeFlags.ExpandFill; toolbar.AddChild(status);
        toolbar.AddChild(Button("Save", () => Report(session.Save(activeSlot))));
        toolbar.AddChild(Button("Load", () => Ask("Replace the active campaign with the saved campaign?", () => { Report(session.Load(activeSlot)); requestId = Composition.LastUiCommand(session); })));
        toolbar.AddChild(Button("Recover backup", () => Ask("Load the newest valid backup? Some progress may be lost. Future saves use a separate recovered slot, preserving the original.", () =>
        { var result = session.Load(activeSlot, true); if (result.Accepted) activeSlot = "recovered"; Report(result); requestId = Composition.LastUiCommand(session); })));
        advance = Button("Advance to next checkpoint →", () => Send(new AdvanceDecision())); toolbar.AddChild(advance);
        var fixture = new Label { Text = "DEVELOPMENT FIXTURE · Fictional 5v5 Role Arena · Noncanonical content & balance · CU = fixture currency", AutowrapMode = TextServer.AutowrapMode.WordSmart };
        fixture.AddThemeColorOverride("font_color", new Color("d4b781")); shell.AddChild(fixture);
        message.AutowrapMode = TextServer.AutowrapMode.WordSmart; message.CustomMinimumSize = new Vector2(0, 42); shell.AddChild(message);
        tabs.SizeFlagsVertical = SizeFlags.ExpandFill; shell.AddChild(tabs);
        AddText(Tab("Dashboard"), dashboard);
        var companyEntry = Button("Company context · live sponsor workspace", () =>
        {
            margin.Visible = false;
            var companyHost = new SponsorCompanyHost();
            companyHost.Configure(session, () => { RemoveChild(companyHost); companyHost.QueueFree(); margin.Visible = true; Refresh(); });
            AddChild(companyHost); companyHost.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        });
        shell.MoveChild(tabs, shell.GetChildCount() - 1);
        shell.AddChild(companyEntry);
        var team = Tab("Team & people"); var filters = new HBoxContainer(); team.AddChild(filters); filters.AddChild(search);
        roleFilter.AddItem("All roles"); foreach (var role in new[] { "A", "B", "C", "D", "E" }) roleFilter.AddItem(role);
        foreach (var key in new[] { "Name", "Salary", "Readiness" }) sort.AddItem(key);
        filters.AddChild(roleFilter); filters.AddChild(sort); filters.AddChild(Button("Previous", () => { page--; BindRoster(); })); filters.AddChild(Button("Next", () => { page++; BindRoster(); })); team.AddChild(rosterCount);
        roster.Columns = 6; roster.ColumnTitlesVisible = true; roster.HideRoot = true; roster.SizeFlagsVertical = SizeFlags.ExpandFill; roster.CustomMinimumSize = new Vector2(0, 230); roster.SelectMode = Tree.SelectModeEnum.Row;
        var columns = new[] { "Player", "Role", "Execution · fact", "Readiness · fact", "Daily salary", "Contract ends" };
        for (var i = 0; i < columns.Length; i++) { roster.SetColumnTitle(i, columns[i]); roster.SetColumnExpand(i, true); }
        team.AddChild(roster); root = roster.CreateItem();
        roster.ItemSelected += () => { if (!refreshing) { selectedId = roster.GetSelected()?.GetMetadata(0).AsString(); UpdateRelease(); } };
        search.TextChanged += _ => { page = 0; BindRoster(); }; roleFilter.ItemSelected += _ => { page = 0; BindRoster(); }; sort.ItemSelected += _ => BindRoster();
        var personnel = new HBoxContainer(); team.AddChild(personnel); personnel.AddChild(candidates);
        foreach (var length in new[] { 1, 2, 3 }) contractSeasons.AddItem(length + (length == 1 ? " season" : " seasons")); personnel.AddChild(contractSeasons);
        sign = Button("Review signing", () =>
        {
            var id = candidates.GetSelectedMetadata().AsString(); var candidate = view.Candidates.FirstOrDefault(x => x.Id == id); if (candidate is null) return;
            var seasons = contractSeasons.Selected + 1;
            Ask($"Sign {candidate.Name} for {seasons} season(s)?\nFee {Money(candidate.Fee)} now; salary {Money(candidate.Salary)}/day until day {view.Day + seasons * view.SeasonLength - 1}.\nInitial integration reduces readiness. Future payroll reduces financial flexibility.", () => Send(new SigningDecision(id, seasons)));
        }); personnel.AddChild(sign);
        renew = Button("Review renewal", () =>
        {
            var person = view.People.FirstOrDefault(x => x.Id == selectedId); if (person is null) return;
            var seasons = contractSeasons.Selected + 1;
            Ask($"Renew {person.Name} for {seasons} more season(s)?\nNew wage {Money(person.RenewalSalary)}/day (now {Money(person.Salary)}); contract would end day {person.ContractEnd + seasons * view.SeasonLength}.", () => Send(new RenewalDecision(person.Id, seasons)));
        }); personnel.AddChild(renew);
        release = Button("Review release", () =>
        {
            var person = view.People.FirstOrDefault(x => x.Id == selectedId); if (person is null) return;
            Ask($"Release {person.Name}?\nPay {Money(person.ExitCost)} now; future wages stop tomorrow. Due wages and arrears remain.\nRoster depth and competitive options are lost.", () => Send(new ReleaseDecision(person.Id)));
        }); personnel.AddChild(release);
        var staff = new HBoxContainer(); team.AddChild(staff); staff.AddChild(coachCandidates);
        hire = Button("Review coach hire", () =>
        {
            var id = coachCandidates.GetSelectedMetadata().AsString(); var candidate = view.CoachCandidates.FirstOrDefault(x => x.Id == id); if (candidate is null) return;
            Ask($"Hire {candidate.Name} as head coach for two seasons?\nFee {Money(candidate.Fee)} plus the current coach's exit cost now; salary {Money(candidate.Salary)}/day.", () => Send(new HireCoachDecision(id)));
        }); staff.AddChild(hire);
        renewCoach = Button("Review coach renewal", () =>
        {
            var current = view.CoachContract; var seasons = contractSeasons.Selected + 1;
            Ask($"Renew head coach {current.Name} for {seasons} more season(s)?\nNew wage {Money(current.RenewalSalary)}/day (now {Money(current.Salary)}).", () => Send(new RenewalDecision(current.Id, seasons)));
        }); staff.AddChild(renewCoach);
        var preparation = Tab("Competition & coach"); AddText(preparation, opponent);
        var allocations = new HBoxContainer(); preparation.AddChild(allocations);
        AddField(allocations, "Team execution %", execution); AddField(allocations, "Opponent prep %", opponentWork); AddField(allocations, "Meta adaptation %", meta);
        foreach (var name in Enum.GetNames<Risk>()) { posture.AddItem(name); ceiling.AddItem(name); }
        foreach (var name in Enum.GetNames<Delegation>()) authority.AddItem(name);
        posture.Selected = 1; ceiling.Selected = 2; AddField(allocations, "Posture", posture);
        var selection = new HBoxContainer(); preparation.AddChild(selection);
        foreach (var role in new[] { "A", "B", "C", "D", "E" }) { var choice = new OptionButton(); lineup.Add(role, choice); AddField(selection, "Role " + role, choice); }
        var actions = new HBoxContainer(); preparation.AddChild(actions);
        actions.AddChild(Button("Use coach recommendation", () => { if (view.Recommendation is { } plan) SetDraft(plan); }));
        commit = Button("Commit preparation & lineup", () => Send(new PreparationDecision(view.NextFixtureId, (int)execution.Value, (int)opponentWork.Value, (int)meta.Value,
            (Risk)posture.Selected, lineup.Values.Select(x => x.GetSelectedMetadata().AsString()).ToImmutableArray()))); actions.AddChild(commit);
        var coachRow = new HBoxContainer(); preparation.AddChild(coachRow); AddField(coachRow, "Coach control", authority); AddField(coachRow, "Autonomous risk ceiling", ceiling);
        coach = Button("Commit coach authority", () => Send(new CoachDecision((Delegation)authority.Selected, (Risk)ceiling.Selected))); coachRow.AddChild(coach); AddText(preparation, recommendation);
        AddText(Tab("Finance"), finance);
        var sponsors = Tab("Sponsors"); AddText(sponsors, commercial); sponsors.AddChild(offers);
        sponsor = Button("Review sponsor commitment", () =>
        {
            var id = offers.GetSelectedMetadata().AsString(); var offer = view.Offers.FirstOrDefault(x => x.Id == id); if (offer is null) return;
            Ask($"Sign {offer.Name}?\n{Money(offer.Payment)} in three days, then weekly until day {view.Day + offer.DurationDays - 1}.\nWin bonus {Money(offer.WinBonus)}; delivery load +{offer.Load}. Sponsor slots: {view.ActiveSponsors}/{view.SponsorSlots}.\nOverload reduces preparation conversion.", () => Send(new SponsorDecision(id)));
        }); sponsors.AddChild(sponsor);
        var approach = new HBoxContainer(); sponsors.AddChild(new Label { Text = "APPROACH A BRAND · propose your own terms; the brand answers in a few days" }); sponsors.AddChild(approach);
        approach.AddChild(brands); approach.AddChild(proposalPayment); approach.AddChild(proposalDuration);
        brands.ItemSelected += _ => BindProposal();
        propose = Button("Review proposal", () =>
        {
            var id = brands.GetSelectedMetadata().AsString(); var brand = view.Brands.FirstOrDefault(x => x.Id == id); if (brand is null || proposalDuration.ItemCount == 0) return;
            var payment = (long)Math.Round(proposalPayment.Value * 100); var days = (int)proposalDuration.GetSelectedMetadata().AsInt32();
            Ask($"Propose {Money(payment)} per week for {days} days to {brand.Name}?\nMarket guide {Money(brand.GuideLow)}–{Money(brand.GuideHigh)} (estimate). Asking more risks a counter-offer or a refusal with a cooling-off period.", () => Send(new ProposalDecision(id, payment, days)));
        }); approach.AddChild(propose); AddText(Tab("Review & results"), review);
        confirmation.Title = "Review commitment"; confirmation.Confirmed += () => { var action = confirmed; confirmed = null; action?.Invoke(); }; confirmation.Canceled += () => confirmed = null; AddChild(confirmation);
    }
    private VBoxContainer Tab(string name)
    {
        var scroll = new ScrollContainer { Name = name, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }; tabs.AddChild(scroll);
        var panel = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill }; panel.AddThemeConstantOverride("separation", 16); scroll.AddChild(panel); return panel;
    }
    private static Button Button(string text, Action action) { var b = new Button { Text = text, CustomMinimumSize = new Vector2(0, 38) }; b.Pressed += action; return b; }
    private static void AddText(VBoxContainer panel, Label label) { label.AutowrapMode = TextServer.AutowrapMode.WordSmart; label.SizeFlagsHorizontal = SizeFlags.ExpandFill; panel.AddChild(label); }
    private static void AddField(HBoxContainer row, string title, Control control) { var box = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; box.AddChild(new Label { Text = title }); box.AddChild(control); row.AddChild(box); }
    private void Ask(string text, Action action) { confirmed = action; confirmation.DialogText = text; confirmation.PopupCentered(new Vector2I(650, 230)); }
    private void Send(Decision decision)
    {
        var start = Stopwatch.GetTimestamp(); var response = session.Submit(new Request("ui:" + ++requestId, view.Revision, decision)); Report(response); acknowledgements.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
        if (response.Accepted && decision is AdvanceDecision) tabs.CurrentTab = 5;
    }
    private void Report(Response response) { message.Text = response.Message; message.AddThemeColorOverride("font_color", new Color(response.Accepted ? "86cfb7" : "ffb1a6")); Refresh(); }
    private void Refresh()
    {
        view = session.Observe(); refreshing = true; heading.Text = view.Company; status.Text = $"Season {view.Season} · day {view.DayOfSeason}/{view.SeasonLength}  ·  {view.Status}  ·  Cash {Money(view.Cash)}";
        dashboard.Text = $"YOUR NEXT DECISION\n\n{(view.NextFixtureId.Length == 0 ? "Off-season: no match until next season. Use the time for contracts, sponsors and recruitment." : $"Prepare for {view.Opponent} on day {view.NextMatchDay}. Inspect terms before spending, then advance to the next meaningful checkpoint.")}\n\n" +
            $"7-day committed cash forecast: {Money(view.Forecast)}\nReputation: {view.Reputation}/100  ·  Audience: {view.Audience:N0}\nOrganizational load: {view.Load}/{view.Capacity} — {(view.Load > view.Capacity ? "overload reduces preparation throughput" : "within capacity")}\nCoach: {view.Coach} · {view.Delegation}\n\n" +
            $"{(view.CommittedPlan is null ? "No plan committed. Open Competition & coach or authorize autonomous preparation." : "Preparation plan committed. You may change future work before advancing.")}\n\nFinances settle before matches. Prizes and sponsor bonuses become receivable tomorrow.\n\nPending consequences: {(view.PendingConsequences.Length == 0 ? "None" : view.PendingConsequences)}";
        opponent.Text = $"NEXT COMPETITION · DAY {view.NextMatchDay}\n{view.Opponent}\nStrength: {view.OpponentEstimate}; likely posture: {view.OpponentTendency} ({view.Confidence}).\nPublic meta: {view.Meta}. Hidden opponent preparation and match-day variance: unknown.\n\nPreparation must total 100%. Changing the plan affects future work; completed work is retained.";
        recommendation.Text = $"HEAD COACH {view.CoachContract.Name} · {Money(view.CoachContract.Salary)}/day · contract ends day {view.CoachContract.ContractEnd}{(view.CoachContract.CanRenew ? $" · renewal asks {Money(view.CoachContract.RenewalSalary)}/day" : "")}\n\n{view.RecommendationReason}\n\nAutonomous mode commits a legal recommendation when a new plan is needed. Low confidence escalates. Manual overrides carry no hidden bonus or penalty.\n\n" +
            (view.CommittedPlan is { } plan ? $"COMMITTED: {plan.Execution}/{plan.Opponent}/{plan.Meta}% · {plan.Posture}" : "No committed plan.");
        authority.Selected = (int)view.Delegation; ceiling.Selected = (int)view.Ceiling;
        foreach (var (role, choice) in lineup)
        {
            var old = choice.ItemCount > 0 ? choice.GetSelectedMetadata().AsString() : null; choice.Clear();
            foreach (var p in view.People.Where(p => p.Role == role)) { choice.AddItem(p.Name); choice.SetItemMetadata(choice.ItemCount - 1, p.Id); }
            SelectId(choice, old ?? view.Recommendation?.Lineup.FirstOrDefault(id => view.People.Any(p => p.Id == id && p.Role == role)));
        }
        candidates.Clear(); foreach (var c in view.Candidates) { candidates.AddItem($"{c.Name} · role {c.Role} · ability {c.AbilityEstimate}"); candidates.SetItemMetadata(candidates.ItemCount - 1, c.Id); } sign.Disabled = candidates.ItemCount == 0;
        offers.Clear(); foreach (var o in view.Offers) { offers.AddItem($"{o.Name} · {o.Availability}"); offers.SetItemMetadata(offers.ItemCount - 1, o.Id); } sponsor.Disabled = offers.ItemCount == 0;
        coachCandidates.Clear(); foreach (var c in view.CoachCandidates) { coachCandidates.AddItem($"{c.Name} · skill {c.SkillEstimate} · {Money(c.Salary)}/day · until day {c.Deadline}"); coachCandidates.SetItemMetadata(coachCandidates.ItemCount - 1, c.Id); }
        hire.Disabled = coachCandidates.ItemCount == 0; renewCoach.Disabled = !view.CoachContract.CanRenew;
        var chosenBrand = brands.ItemCount > 0 ? brands.GetSelectedMetadata().AsString() : null; brands.Clear();
        foreach (var b in view.Brands) { brands.AddItem($"{b.Name} · {b.Sector} · {b.Availability}"); brands.SetItemMetadata(brands.ItemCount - 1, b.Id); }
        SelectId(brands, chosenBrand ?? view.Brands.FirstOrDefault(b => b.Availability == "Open to proposals")?.Id); BindProposal(chosenBrand is null);
        commercial.Text = $"ACTIVE AGREEMENTS ({view.ActiveSponsors}/{view.SponsorSlots} slots)\n" + string.Join("\n", view.Sponsors) + "\n\nOFFERS\n" + string.Join("\n\n", view.Offers.Select(o =>
            $"{o.Name} · {o.Origin} · {o.Availability}\n{Money(o.Payment)} weekly for {o.DurationDays} days; win bonus {Money(o.WinBonus)}; load {o.Load}; minimum reputation {o.MinimumReputation}; answer by day {o.Deadline}.")) +
            "\n\nPROPOSALS UNDER REVIEW\n" + (view.Negotiations.IsEmpty ? "None" : string.Join("\n", view.Negotiations.Select(n => $"{n.Brand}: {Money(n.Payment)} weekly for {n.DurationDays} days · answer on day {n.ResponseDay}"))) + "\n\nCommercial value creates opportunities. Cash comes only from signed terms and due settlements.";
        finance.Text = $"Cash: {Money(view.Cash)}\n7-day committed forecast: {Money(view.Forecast)}\nCondition: {view.Status}\n\nUPCOMING ITEMS (grouped by due day)\n" +
            string.Join("\n", view.Bills.GroupBy(x => x.DueDay).Take(14).Select(g => $"Day {g.Key}: receipts {Money(g.Where(x => x.Incoming).Sum(x => x.Remaining))} / obligations {Money(g.Where(x => !x.Incoming).Sum(x => x.Remaining))}")) +
            "\n\nArrears: " + Money(view.Bills.Where(x => !x.Incoming && x.MissedDay is not null).Sum(x => x.Remaining)) + "\nReceipts settle before obligations. Unpaid balances and first missed dates persist. Releasing a duplicate-role player sacrifices depth and future wages; existing arrears remain. No unapproved terminal threshold is applied.";
        review.Text = "COMPETITION RECORD\n" + string.Join("\n", view.Results.Select(x => $"Day {x.Day} · {x.Result} vs {x.Opponent} · {x.Posture}")) + "\n\nDECISIONS & CONSEQUENCES\n" + string.Join("\n\n", view.Review) + "\n\nPending: " + view.PendingConsequences;
        advance.Disabled = false; commit.Disabled = view.NextFixtureId.Length == 0; coach.Disabled = false; refreshing = false; BindRoster(); UpdateRelease();
    }
    private static void SelectId(OptionButton choice, string? id) { for (var i = 0; i < choice.ItemCount; i++) if (choice.GetItemMetadata(i).AsString() == id) { choice.Select(i); return; } }
    private void SetDraft(PlanView plan) { execution.Value = plan.Execution; opponentWork.Value = plan.Opponent; meta.Value = plan.Meta; posture.Selected = (int)plan.Posture; foreach (var choice in lineup.Values) foreach (var id in plan.Lineup) SelectId(choice, id); }
    private void UpdateRelease()
    {
        release.Disabled = !view.People.Any(x => x.Id == selectedId && x.CanRelease);
        renew.Disabled = !view.People.Any(x => x.Id == selectedId && x.CanRenew);
    }
    private void BindProposal(bool resetPayment = true)
    {
        proposalDuration.Clear(); propose.Disabled = true;
        if (brands.ItemCount == 0 || view.Brands.FirstOrDefault(x => x.Id == brands.GetSelectedMetadata().AsString()) is not { } brand) return;
        foreach (var days in brand.DurationDays) { proposalDuration.AddItem(days + " days"); proposalDuration.SetItemMetadata(proposalDuration.ItemCount - 1, days); }
        if (resetPayment) proposalPayment.Value = (brand.GuideLow + brand.GuideHigh) / 200.0;
        propose.Disabled = !brand.Availability.StartsWith("Open", StringComparison.Ordinal);
    }
    private void BindRoster()
    {
        if (view is null || root is null) return; var start = Stopwatch.GetTimestamp(); refreshing = true;
        var result = RosterTable.Query(benchmark && benchmarkFrames >= 300 ? stressRows : view.People, new RosterQuery(search.Text, roleFilter.Selected <= 0 ? "" : roleFilter.GetItemText(roleFilter.Selected), sort.GetItemText(Math.Max(0, sort.Selected)), false, page), selectedId);
        page = result.Page; selectedId = result.SelectedId;
        while (rows.Count < result.Rows.Length) rows.Add(roster.CreateItem(root));
        while (rows.Count > result.Rows.Length) { rows[^1].Free(); rows.RemoveAt(rows.Count - 1); }
        for (var i = 0; i < rows.Count; i++)
        {
            var p = result.Rows[i]; var item = rows[i]; item.SetMetadata(0, p.Id); var values = new[] { p.Name, p.Role, p.Execution.ToString(), p.Readiness.ToString(), Money(p.Salary), p.ContractEnd == 0 ? "—" : "day " + p.ContractEnd };
            for (var j = 0; j < values.Length; j++) if (item.GetText(j) != values[j]) item.SetText(j, values[j]); item.Deselect(0); if (p.Id == selectedId) item.Select(0);
        }
        rosterCount.Text = $"{result.Total} people · page {page + 1} · {rows.Count} bound rows · actions use stable person IDs"; refreshing = false; binds.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
    }
    private static int CountControls(Node node) => (node is Control ? 1 : 0) + node.GetChildren().Sum(CountControls);
    public override void _Process(double delta)
    {
        if (Arg("--capture") is { } capture && ++captureFrames == 30) { GetViewport().GetTexture().GetImage().SavePng(capture); if (!benchmark) GetTree().Quit(0); }
        if (!benchmark) return; var now = Stopwatch.GetTimestamp(); frames.Add(Stopwatch.GetElapsedTime(lastFrame, now).TotalMilliseconds); lastFrame = now; benchmarkFrames++;
        if (benchmarkFrames % 20 == 0) { sort.Selected = (sort.Selected + 1) % 3; BindRoster(); }
        if (benchmarkFrames % 40 == 0) { search.Text = search.Text.Length == 0 ? "a" : ""; roleFilter.Selected = roleFilter.Selected == 0 ? 1 : 0; BindRoster(); }
        if (benchmarkFrames % 60 == 0) { page++; BindRoster(); if (rows.Count > 0) roster.ScrollToItem(rows[^1]); tabs.CurrentTab = tabs.CurrentTab == 1 ? 0 : 1; }
        if (benchmarkFrames < 600) return; benchmark = false;
        var report = new { Kind = "production-roster-rendered-workflow", Samples = frames.Count, FrameWallP95Ms = P95(frames.Skip(60)), MaxFrameMs = frames.Max(), BindP95Ms = P95(binds.Skip(5)),
            AckP95Ms = P95(acknowledgements), Rows = rows.Count, ControlsBefore = controlsBefore, ControlsAfter = CountControls(this), ManagedBefore = memoryBefore, ManagedAfter = GC.GetTotalMemory(true), PhysicalInput = false };
        if (Arg("--report") is { } path) System.IO.File.WriteAllText(path, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true })); GD.Print(JsonSerializer.Serialize(report)); GetTree().Quit(0);
    }
    private static double P95(IEnumerable<double> samples) { var values = samples.Order().ToArray(); return values.Length == 0 ? 0 : values[(int)Math.Ceiling(values.Length * .95) - 1]; }
    private void RunSmoke()
    {
        try
        {
            // Same decision sequence as tools/Headless; identities are read from the observation, never hard-coded.
            SelectId(offers, view.Offers.First(o => o.Availability == "Available").Id); sponsor.EmitSignal(BaseButton.SignalName.Pressed); confirmation.EmitSignal(ConfirmationDialog.SignalName.Confirmed); confirmation.Hide();
            contractSeasons.Selected = 0; SelectId(candidates, view.Candidates[0].Id); sign.EmitSignal(BaseButton.SignalName.Pressed); confirmation.EmitSignal(ConfirmationDialog.SignalName.Confirmed); confirmation.Hide();
            authority.Selected = (int)Delegation.Recommend; ceiling.Selected = (int)Risk.Balanced; coach.EmitSignal(BaseButton.SignalName.Pressed);
            SetDraft(view.Recommendation!); execution.Value = 70; opponentWork.Value = 20; meta.Value = 10; posture.Selected = (int)Risk.Balanced; commit.EmitSignal(BaseButton.SignalName.Pressed);
            for (var i = 0; i < 20 && view.Results.Length < 1; i++) advance.EmitSignal(BaseButton.SignalName.Pressed);
            if (view.Results.Length != 1) throw new Exception("First UI cycle failed: " + message.Text);
            authority.Selected = (int)Delegation.Autonomous; ceiling.Selected = (int)Risk.Aggressive; coach.EmitSignal(BaseButton.SignalName.Pressed);
            for (var i = 0; i < 20 && view.Results.Length < 2; i++) advance.EmitSignal(BaseButton.SignalName.Pressed);
            if (view.Results.Length != 2) throw new Exception("Second UI cycle failed: " + message.Text);
            var hash = Composition.Hash(session);
            if (!session.Save("smoke").Accepted || !session.Load("smoke").Accepted || Composition.Hash(session) != hash) throw new Exception("UI save/load failed.");
            Refresh(); GD.Print("SLICE_UI_SMOKE_PASS hash=" + hash + " results=" + view.Results.Length + " boundRows=" + rows.Count);
            if (Arg("--hash") is { } output) System.IO.File.WriteAllText(output, hash); if (!benchmark && Arg("--capture") is null) GetTree().Quit(0);
        }
        catch (Exception e) { GD.PrintErr(e.ToString()); GetTree().Quit(1); }
    }
}
