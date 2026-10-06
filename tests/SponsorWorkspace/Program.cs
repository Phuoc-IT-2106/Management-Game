using System.Collections.Immutable;
using ManagementGame.Application;
using ManagementGame.Domain;
using ManagementGame.Infrastructure;
using ManagementGame.SponsorWorkspace;
using ManagementGame.OperatingMap;
using ManagementGame.UiKit;
using System.Text.Json;

var content = ContentLoader.Load(Path.GetFullPath("content/fixture.json"));
Session New(Campaign? state = null) => new(content.Definition, state ?? CampaignFactory.Create(content.Definition, content.Hash, 20261004, "sponsor:test"));
var count = 0;
void Check(bool value, string name) { if (!value) throw new Exception(name); count++; Console.WriteLine("PASS " + name); }
var session = New(); var before = Canonical.Hash(session.Capture()); var snapshot = session.ObserveSponsors();
var offer = snapshot.Offers.First();
MapReturn Origin(bool list = true) => new(snapshot.CompanyId, MapFixtures.Business, "matter:" + offer.OfferId, list, "list:matter:" + offer.OfferId, 37);
SponsorPresenter Presenter(ISponsorSession? service = null) => new(service ?? session, snapshot, offer.OfferId, Origin());
Check(snapshot.CompanyName == session.Observe().Company && snapshot.CompanyId == session.Capture().Company.Id, "live campaign identity");
Check(snapshot.Offers.All(o => o.CanAccept) && offer.ScheduledPayments.Select(p => p.DueDay).SequenceEqual(new[] { 4, 11, 18, 25 }), "authoritative eligibility and schedule");
Check(Canonical.Hash(session.Capture()) == before, "projection preview cannot mutate state or consume RNG");
var accepted = Simulation.Apply(session.Capture(), new AcceptSponsor(offer.OfferId), content.Definition, "test:preview").Company;
Check(offer.LoadIfAccepted == Simulation.Load(accepted, content.Definition.Balance, snapshot.Day) && offer.LoadIfAccepted == snapshot.Load + offer.Load &&
    offer.ForecastIfAccepted == Finance.Forecast(accepted, snapshot.Day), "if-accepted load and forecast match authoritative transition");
var days = Enumerable.Range(snapshot.Day, snapshot.LastDay - snapshot.Day + 1).ToArray();
Check(snapshot.LastDay == content.Definition.Balance.Horizon && snapshot.LoadByDay.SequenceEqual(days.Select(d => Simulation.Load(session.Capture().Company, content.Definition.Balance, d))) &&
    offer.LoadByDayIfAccepted.SequenceEqual(days.Select(d => Simulation.Load(accepted, content.Definition.Balance, d))), "daily load series use the authoritative load rule");
Check(snapshot.CompetitionDays.SequenceEqual(session.Capture().World.Fixtures.Select(f => f.Day).Where(d => d >= snapshot.Day).Order()), "competition days are the scheduled fixtures");
var sponsorTrack = SponsorPresentation.Track(snapshot, offer); var sponsorLines = PresentationText.DayTrackSummary(sponsorTrack);
Check(sponsorLines[0].StartsWith("If accepted: over capacity on day") && sponsorLines.Any(l => l.StartsWith("Receipts · this offer: day 4, 11, 18, 25")) &&
    sponsorLines.Any(l => l.StartsWith("Competition: day")), "sponsor track states overload, offer receipts and matches in text");
var hidden = session.Capture() with { World = session.Capture().World with { Rivals = session.Capture().World.Rivals.Select(r => r with { Strength = 1, Budget = 999, Need = 88 }).ToImmutableArray() } };
Check(JsonSerializer.Serialize(SponsorProjection.Build(hidden, content.Definition)) == JsonSerializer.Serialize(snapshot), "private rival changes are invisible to sponsor read model");
Check(!JsonSerializer.Serialize(snapshot).Contains("Probability") && !JsonSerializer.Serialize(snapshot).Contains("Variance"), "no hidden resolver fields");
Check(SponsorPresentation.Document(snapshot, offer).Sections.Any(s => s.Value.Contains("day 25")), "document renders Application dates");
Check(SponsorPresentation.Forecast(snapshot).Information == InformationState.Estimated, "forecast explicitly estimated");
Check(PresentationText.Resource(new("Future bonuses", 999, "CU", InformationState.Unknown, "future wins unknown")).Contains("Unknown") &&
    !PresentationText.Resource(new("Future bonuses", 999, "CU", InformationState.Unknown, "future wins unknown")).Contains("999"), "unknown never zero or hidden numeric backing");
var probe = new Probe(session); var presenter = Presenter(probe);
Check(presenter.Handle(new("review", offer.OfferId, snapshot.Revision)), "review local draft");
Check(Canonical.Hash(session.Capture()) == before && probe.Calls == 0, "review submits nothing");
probe.DuringSubmit = () => { presenter.Refresh(); Check(!presenter.Handle(new("commit", offer.OfferId, snapshot.Revision)), "pending rebind cannot submit again"); };
Check(presenter.Handle(new("commit", offer.OfferId, snapshot.Revision)), "real commitment command");
Check(probe.Last!.Revision == snapshot.Revision && probe.Last.Decision == new SponsorDecision(offer.OfferId), "exact offer ID and viewed revision");
Check(presenter.Phase == SponsorPhase.Accepted && presenter.Snapshot.Revision == snapshot.Revision + 1, "success refreshes observation");
Check(presenter.Snapshot.Cash == snapshot.Cash && presenter.Snapshot.Load == snapshot.Load + offer.Load, "cash unchanged, delivery load immediate");
for (var i = 0; i < 20; i++) presenter.Handle(new("commit", offer.OfferId, presenter.Snapshot.Revision));
Check(probe.Calls == 1 && session.Capture().Company.Sponsors.Length == 2, "double click Enter and spam suppressed");
var committed = Canonical.Hash(session.Capture());
Check(session.Submit(probe.Last).Accepted && Canonical.Hash(session.Capture()) == committed, "existing receipt retry remains sole idempotency authority");
Check(presenter.Snapshot.Offers.All(o => !o.CanAccept), "slot consumed; signed and competing offer disabled");
var staleSession = New(); var staleProbe = new Probe(staleSession); var stale = Presenter(staleProbe);
stale.Handle(new("review", offer.OfferId, snapshot.Revision));
staleSession.Submit(new("external", snapshot.Revision, new CoachDecision(Delegation.Manual, Risk.Balanced)));
var staleBefore = Canonical.Hash(staleSession.Capture());
stale.Handle(new("commit", offer.OfferId, snapshot.Revision));
Check(stale.Phase == SponsorPhase.Rejected && stale.Feedback.Contains("Stale view") && stale.Snapshot.Revision == 1, "stale message and actor-safe refresh");
Check(Canonical.Hash(staleSession.Capture()) == staleBefore && staleProbe.Calls == 1, "stale rejects without mutation or retry");
Check(!stale.Handle(new("commit", offer.OfferId, 1)) && stale.Handle(new("review", offer.OfferId, 1)), "fresh explicit review required");
var cancelSession = New(); var cancel = Presenter(cancelSession);
cancel.Handle(new("review", offer.OfferId, 0)); cancel.Handle(new("back", offer.OfferId, 0));
Check(cancel.Phase == SponsorPhase.Review, "Escape confirmation returns review");
cancel.Handle(new("back", offer.OfferId, 0));
Check(cancel.Phase == SponsorPhase.Closed && Canonical.Hash(cancelSession.Capture()) == before && cancel.Origin == Origin(), "cancel and exact return preserve state");
var reordered = snapshot with { Offers = snapshot.Offers.Reverse().ToImmutableArray() };
Check(new SponsorPresenter(New(), reordered, offer.OfferId, Origin()).Offer == offer, "sorting cannot retarget stable ID");
var removedState = New().Capture(); removedState = removedState with { World = removedState.World with { Offers = [] } };
var removed = New(removedState); var removedPresenter = Presenter(removed);
removedPresenter.Handle(new("review", offer.OfferId, 0)); removedPresenter.Handle(new("commit", offer.OfferId, 0));
Check(removedPresenter.Phase == SponsorPhase.Rejected && removedPresenter.Offer is null && !removedPresenter.CanReview, "removed offer rejected and disabled");
var expiredState = New().Capture(); expiredState = expiredState with { World = expiredState.World with { Offers = expiredState.World.Offers.Select(o => o with { Deadline = 0 }).ToImmutableArray() } };
var expired = New(expiredState).ObserveSponsors();
Check(expired.Offers.All(o => !o.CanAccept && o.LoadIfAccepted is null && o.ForecastIfAccepted is null && o.LoadByDayIfAccepted.IsEmpty), "expired offer not actionable and has no consequence preview");
Check(SponsorPresentation.Track(expired, expired.Offers[0]).Proposed.IsEmpty && SponsorPresentation.Track(expired, expired.Offers[0]).Rows.All(r => r.Label != "Receipts · this offer"), "unavailable offer draws no proposed series");
foreach (var variant in new[] { "end", "reputation", "claimed", "finished" })
{
    var state = New().Capture();
    state = variant switch {
        "end" => state with { World = state.World with { Offers = state.World.Offers.Select(o => o with { EndDay = 2 }).ToImmutableArray() } },
        "reputation" => state with { Company = state.Company with { Reputation = 0 } },
        "claimed" => state with { World = state.World with { Offers = state.World.Offers.Select(o => o with { ClaimedBy = state.World.Rivals[0].Id }).ToImmutableArray() } },
        _ => state with { World = state.World with { Finished = true } } };
    Check(New(state).ObserveSponsors().Offers.All(o => !o.CanAccept), "authoritative unavailable: " + variant);
}
var map = SponsorPresentation.Company(snapshot, "live"); map.Validate(); var nav = new MapNavigation(map) { ListMode = true };
nav.Select("matter:" + offer.OfferId, 0); nav.Enter(offer.OfferId, 0, Origin().FocusKey, 37);
Check(nav.Back() == Origin(), "list return tuple");
nav.ListMode = false; nav.Enter(offer.OfferId, 0, "map:offer", 0);
Check(nav.Return is { ListMode: false } && nav.Situation!.TargetId == offer.OfferId, "optional map shares workspace target");
nav.Back(); nav.Replace(SponsorPresentation.Company(removed.ObserveSponsors(), "live"));
Check(nav.ScopeId == MapFixtures.Business && nav.SituationId is null && nav.Notice.Contains("no longer exists"), "removed matter returns nearest business context");
var updatedMap = SponsorPresentation.Company(presenter.Snapshot, "live");
Check(updatedMap.Situations.Any(m => m.Category == "Receipt" && m.DueDay == 4), "bounded Affairs receipt from actual scheduled item");
var saveRoot = Path.GetFullPath("artifacts/sponsor-workspace/save-tests-" + Guid.NewGuid().ToString("N"));
var store = new SnapshotStore(saveRoot, content); store.Save("signed", session.Capture()); var loaded = store.Load("signed");
Check(Canonical.Hash(loaded) == committed && JsonSerializer.Serialize(New(loaded).ObserveSponsors()) == JsonSerializer.Serialize(presenter.Snapshot), "schema 1 save/load signed terms and schedule");
Check(JsonDocument.Parse(File.ReadAllText(store.PrimaryPath("signed"))).RootElement.GetProperty("Schema").GetInt32() == 1, "save schema unchanged");
var a = New(); var b = New(); var request = new Request("same", 0, new SponsorDecision(offer.OfferId));
for(var i=0;i<10;i++) b.ObserveSponsors(); a.Submit(request); b.Submit(request);
Check(Canonical.Hash(a.Capture()) == Canonical.Hash(b.Capture()), "same decisions deterministic despite extra queries");
var rules = New().Capture(); var signed = Simulation.Apply(rules, new AcceptSponsor(offer.OfferId), content.Definition, "load-test");
var plan = Observations.Build(rules, content.Definition).Recommendation!;
Campaign Prepare(Campaign state) => state with { Company = state.Company with { Plan = new Plan(plan.FixtureId, 100, 0, 0, Posture.Balanced, plan.Lineup, Posture.Balanced, "prep") }, World = state.World with { Calendar = new(1, Phase.Preparation, 2) } };
Check(Simulation.Step(Prepare(signed), content.Definition).Company.Work.Execution < Simulation.Step(Prepare(rules), content.Definition).Company.Work.Execution, "actual sponsor load reduces future preparation conversion");
foreach(var file in Directory.GetFiles("game/Client/UI/SponsorWorkspace", "*.cs"))
    Check(!File.ReadAllText(file).Contains("using ManagementGame.Domain") && !File.ReadAllText(file).Contains(".Capture()"), "UI authority boundary " + Path.GetFileName(file));
FixtureIdentity.Verify(content, Check);
Console.WriteLine("SPONSOR_WORKSPACE_PASS " + count);

sealed class Probe(ISponsorSession session) : ISponsorSession
{
    public int Calls; public Request? Last; public Action? DuringSubmit;
    public SponsorSnapshot ObserveSponsors() => session.ObserveSponsors();
    public Response Submit(Request request) { Calls++; Last=request; DuringSubmit?.Invoke(); return session.Submit(request); }
}

