using System.Collections.Immutable;

namespace ManagementGame.Application;

public enum Risk { Conservative, Balanced, Aggressive }
public enum Delegation { Manual, Recommend, Autonomous }
public abstract record Decision;
public sealed record PreparationDecision(string FixtureId, int Execution, int Opponent, int Meta, Risk Posture, ImmutableArray<string> Lineup) : Decision;
public sealed record CoachDecision(Delegation Mode, Risk Ceiling) : Decision;
public sealed record SigningDecision(string PersonId) : Decision;
public sealed record ReleaseDecision(string PersonId) : Decision;
public sealed record SponsorDecision(string OfferId) : Decision;
public sealed record AdvanceDecision : Decision;
public sealed record Request(string Id, long Revision, Decision Decision);
public sealed record Response(bool Accepted, long Revision, string Message);

public sealed record PersonRow(string Id, string Name, string Role, int Execution, int Readiness, long Salary, long ExitCost, bool CanRelease);
public sealed record CandidateRow(string Id, string Name, string Role, string AbilityEstimate, long Fee, long Salary, int Deadline);
public sealed record SponsorRow(string Id, string Name, long Payment, long WinBonus, int Load, int Deadline, int MinimumReputation, string Availability);
public sealed record BillRow(string Id, string Cause, int DueDay, long Remaining, bool Incoming, int? MissedDay);
public sealed record ResultRow(string Id, int Day, string Opponent, string Result, string Posture);
public sealed record PlanView(string FixtureId, int Execution, int Opponent, int Meta, Risk Posture, ImmutableArray<string> Lineup);
public sealed record Situation(long Revision, int Day, string Company, string Status, bool Finished,
    long Cash, long Forecast, int Reputation, int Audience, int Load, int Capacity,
    string Coach, Delegation Delegation, Risk Ceiling, string NextFixtureId, int NextMatchDay, string Opponent,
    string OpponentEstimate, string OpponentTendency, string Confidence, string Meta, PlanView? CommittedPlan,
    PlanView? Recommendation, string RecommendationReason, ImmutableArray<PersonRow> People,
    ImmutableArray<CandidateRow> Candidates, ImmutableArray<SponsorRow> Offers, ImmutableArray<string> Sponsors,
    ImmutableArray<BillRow> Bills, ImmutableArray<ResultRow> Results, ImmutableArray<string> Review,
    string PendingConsequences, string FixtureLabel);

public interface IGameSession
{
    Situation Observe();
    Response Submit(Request request);
    Response Save(string slot);
    Response Load(string slot, bool backup = false);
}
public interface ISnapshotStore
{
    void Save(string slot, ManagementGame.Domain.Campaign campaign);
    ManagementGame.Domain.Campaign Load(string slot, bool backup = false);
}
