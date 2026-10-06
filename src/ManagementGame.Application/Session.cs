using System.Collections.Immutable;
using ManagementGame.Domain;

namespace ManagementGame.Application;

public sealed class Session(Content content, Campaign initial, ISnapshotStore? store = null) : IGameSession
{
    private Campaign state = initial;
    private readonly object gate = new();
    public Campaign Capture() { lock (gate) return state; }
    public Situation Observe() { lock (gate) return Observations.Build(state, content); }
    public Response Submit(Request request)
    {
        lock (gate)
        {
            if (string.IsNullOrWhiteSpace(request.Id) || request.Id.Length > 128) return Reject("Invalid command ID.");
            var digest = Canonical.Digest(request.Revision + ":" + request.Decision.GetType().Name + ":" + Canonical.Json((object)request.Decision));
            var receipt = state.Execution.Receipts.FirstOrDefault(x => x.Id == request.Id);
            if (receipt is not null) return receipt.Digest == digest ? new Response(true, receipt.Revision, receipt.Message) : Reject("Command ID already used for different intent.");
            if (request.Revision != state.Execution.Revision) return Reject("Stale view: refresh and review the decision before retrying.");
            try
            {
                var staged = state;
                if (request.Decision is AdvanceDecision && state.Company.Plan is null && Simulation.NextFixture(state) is not null && state.Company.Authority.Mode == ControlMode.Autonomous)
                {
                    var observation = Observations.Build(staged, content);
                    var plan = observation.Recommendation ?? throw new RuleViolation("Coach escalates: no legal lineup is available.");
                    if (observation.Confidence == "Low confidence") throw new RuleViolation("Coach escalates uncertain opponent information. Review and commit a plan manually.");
                    // Same typed decision mapping and domain executor as a manual commitment.
                    staged = ApplyDecision(staged, new PreparationDecision(plan.FixtureId, plan.Execution, plan.Opponent, plan.Meta, plan.Posture, plan.Lineup), "coach:" + request.Id);
                }
                staged = ApplyDecision(staged, request.Decision, request.Id);
                var revision = checked(state.Execution.Revision + 1);
                var message = request.Decision is AdvanceDecision ? $"Advanced to day {staged.World.Calendar.Day}; {staged.World.Results.Length} competition results recorded." : "Decision committed.";
                state = staged with { Execution = staged.Execution with { Revision = revision,
                    Receipts = staged.Execution.Receipts.Add(new CommandReceipt(request.Id, digest, revision, message)) } };
                return new Response(true, revision, message);
            }
            catch (Exception e) when (e is RuleViolation or OverflowException or ArgumentException)
            { return Reject(e.Message); }
        }
    }
    private Campaign ApplyDecision(Campaign s, Decision decision, string cause)
    {
        Intent intent = decision switch
        {
            PreparationDecision p => new CommitPlan(new Plan(p.FixtureId, p.Execution, p.Opponent, p.Meta, (Posture)p.Posture, p.Lineup,
                Enum.Parse<Posture>(Observations.Build(s, content).OpponentTendency), cause)),
            CoachDecision a => new SetAuthority(new Authority((ControlMode)a.Mode, (Posture)a.Ceiling)),
            SigningDecision p => new SignPlayer(p.PersonId), ReleaseDecision p => new ReleasePlayer(p.PersonId),
            SponsorDecision o => new AcceptSponsor(o.OfferId), AdvanceDecision => new Advance(),
            _ => throw new RuleViolation("Unsupported decision.")
        };
        return Simulation.Apply(s, intent, content, cause);
    }
    private Response Reject(string reason) => new(false, state.Execution.Revision, reason);
    public Response Save(string slot)
    {
        lock (gate)
        {
            try { (store ?? throw new InvalidOperationException("No save store configured.")).Save(slot, state); return new(true, state.Execution.Revision, "Saved committed campaign."); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException or ArgumentException)
            { return Reject("Save failed: " + e.Message); }
        }
    }
    public Response Load(string slot, bool backup = false)
    {
        lock (gate)
        {
            try
            {
                var candidate = (store ?? throw new InvalidOperationException("No save store configured.")).Load(slot, backup);
                // Adapter validates a separate campaign before this single replacement.
                state = candidate;
                return new(true, state.Execution.Revision, backup ? "Loaded explicitly selected backup; progress may be older." : "Loaded campaign.");
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException or ArgumentException)
            { return Reject("Load failed; current campaign retained: " + e.Message); }
        }
    }
}
