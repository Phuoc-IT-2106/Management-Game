using ManagementGame.Application;
using ManagementGame.Domain;
using ManagementGame.Infrastructure;

static class FixtureIdentity
{
    public static void Verify(LoadedContent current, Action<bool, string> check)
    {
        // Reconstruct the exact published fixture bytes, not a second maintained fixture.
        var oldText = File.ReadAllText("content/fixture.json").Replace("\"CompanyName\": \"DEV_ORG_001\"", "\"CompanyName\": \"Northstar Esports\"", StringComparison.Ordinal);
        var oldHash = Canonical.Digest(oldText);
        check(current.Definition.CompanyName == "DEV_ORG_001" && oldHash == "605c6673574379e0d18d97e969e8db58eb84c3f004057d22d618cc9ced9e3f89",
            "fixture differs from published bytes only in company identity");
        var old = new LoadedContent(current.Definition with { CompanyName = "Northstar Esports" }, oldHash);
        Campaign Create(LoadedContent c) => CampaignFactory.Create(c.Definition, c.Hash, 20261004, "identity:comparison");
        Campaign Normalize(Campaign c) => c with { Company = c.Company with { Name = "identity:normalized" }, Execution = c.Execution with { ContentHash = "identity:normalized" } };
        bool Equivalent(Campaign a, Campaign b) => Canonical.Json(Normalize(a)) == Canonical.Json(Normalize(b));
        var previous = new Session(old.Definition, Create(old));
        var next = new Session(current.Definition, Create(current));
        check(Canonical.Hash(previous.Capture()) != Canonical.Hash(next.Capture()) && Equivalent(previous.Capture(), next.Capture()), "canonical identity changes without initial gameplay change");
        Decision[] decisions = [new SponsorDecision("offer:atlas"), new CoachDecision(Delegation.Autonomous, Risk.Balanced), new AdvanceDecision(), new AdvanceDecision()];
        foreach (var decision in decisions)
        {
            var request = new Request("identity:" + previous.Observe().Revision, previous.Observe().Revision, decision);
            var a = previous.Submit(request); var b = next.Submit(request);
            check(a.Accepted && a == b && Equivalent(previous.Capture(), next.Capture()), "identity-only behavior equivalence: " + decision.GetType().Name);
        }
        var aPhase = Create(old); var bPhase = Create(current);
        var recommendation = Observations.Build(aPhase, old.Definition).Recommendation!;
        var plan = new CommitPlan(new Plan(recommendation.FixtureId, recommendation.Execution, recommendation.Opponent, recommendation.Meta,
            (Posture)recommendation.Posture, recommendation.Lineup, Posture.Balanced, "identity:plan"));
        aPhase = Simulation.Apply(Simulation.Apply(aPhase, new AcceptSponsor("offer:atlas"), old.Definition, "identity:sponsor"), plan, old.Definition, "identity:plan");
        bPhase = Simulation.Apply(Simulation.Apply(bPhase, new AcceptSponsor("offer:atlas"), current.Definition, "identity:sponsor"), plan, current.Definition, "identity:plan");
        var boundaries = 0;
        while (aPhase.World.Results.Length == 0 && boundaries < 400)
        {
            aPhase = Simulation.Step(aPhase, old.Definition); bPhase = Simulation.Step(bPhase, current.Definition); boundaries++;
            if (!Equivalent(aPhase, bPhase)) throw new Exception("Fixture behavior changed at phase " + boundaries);
        }
        check(boundaries == 63 && aPhase.World.Results.Length == 1, "identity-only equivalence at all 63 first-match phase boundaries");
        var root = Path.GetFullPath("artifacts/sponsor-workspace/identity-save-" + Guid.NewGuid().ToString("N"));
        new SnapshotStore(root, old).Save("old", previous.Capture());
        var service = new Session(current.Definition, next.Capture(), new SnapshotStore(root, current));
        var before = Canonical.Hash(service.Capture()); var response = service.Load("old");
        check(!response.Accepted && response.Message.Contains("content identity") && Canonical.Hash(service.Capture()) == before, "old exact-content save rejected without mutation");
        check(service.Save("new").Accepted && service.Load("new").Accepted && Canonical.Hash(service.Capture()) == before, "new fixture schema-1 save round trip");
        Console.WriteLine($"FIXTURE_IDENTITY_PASS old={old.Hash} new={current.Hash} phaseBoundaries={boundaries}");
    }
}
