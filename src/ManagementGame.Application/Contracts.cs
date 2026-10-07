using System.Collections.Immutable;

namespace ManagementGame.Application;

public enum Risk { Conservative, Balanced, Aggressive }
public enum Delegation { Manual, Recommend, Autonomous }
public abstract record Decision;
public sealed record PreparationDecision(string FixtureId, int Execution, int Opponent, int Meta, Risk Posture, ImmutableArray<string> Lineup) : Decision;
public sealed record CoachDecision(Delegation Mode, Risk Ceiling) : Decision;
public sealed record SigningDecision(string PersonId, int Seasons = 1) : Decision;
public sealed record ReleaseDecision(string PersonId) : Decision;
public sealed record SponsorDecision(string OfferId) : Decision;
/// <summary>Player-authored sponsorship terms; the brand answers after a delay.</summary>
public sealed record ProposalDecision(string BrandId, long Payment, int DurationDays) : Decision;
public sealed record RenewalDecision(string PersonId, int Seasons) : Decision;
public sealed record HireCoachDecision(string CoachId) : Decision;
public sealed record AdvanceDecision : Decision;
public sealed record Request(string Id, long Revision, Decision Decision);
public sealed record Response(bool Accepted, long Revision, string Message);

public sealed record PersonRow(string Id, string Name, string Role, int Execution, int Readiness, long Salary, long ExitCost, bool CanRelease,
    int ContractEnd = 0, long RenewalSalary = 0, bool CanRenew = false);
public sealed record CandidateRow(string Id, string Name, string Role, string AbilityEstimate, long Fee, long Salary, int Deadline);
public sealed record SponsorRow(string Id, string Name, long Payment, long WinBonus, int Load, int Deadline, int MinimumReputation, string Availability,
    string Origin = "", int DurationDays = 0);
public sealed record BillRow(string Id, string Cause, int DueDay, long Remaining, bool Incoming, int? MissedDay);
public sealed record ResultRow(string Id, int Day, string Opponent, string Result, string Posture);
public sealed record PlanView(string FixtureId, int Execution, int Opponent, int Meta, Risk Posture, ImmutableArray<string> Lineup);
public sealed record StaffRow(string Id, string Name, long Salary, int ContractEnd, long RenewalSalary, bool CanRenew);
public sealed record CoachCandidateRow(string Id, string Name, string SkillEstimate, long Fee, long Salary, int Deadline);
/// <summary>A brand the company may approach; the guide is a market estimate, not the brand's private valuation.</summary>
public sealed record BrandRow(string Id, string Name, string Sector, int MinimumReputation, int MinimumAudience, string Availability,
    long GuideLow, long GuideHigh, ImmutableArray<int> DurationDays);
public sealed record NegotiationRow(string Id, string Brand, long Payment, int DurationDays, int ResponseDay);
/// <summary>A message the world sent the company; Kind is a stable category, CauseId the affected record and
/// SubjectId the stable ID of the organization, brand or person it concerns (empty when none is known).</summary>
public sealed record InboxRow(string Id, int Day, string Kind, string Text, string CauseId, string SubjectId = "");
public sealed record FixtureRow(string Id, int Day, string Opponent, int Importance, string Result, string RivalId = "");
/// <summary>Current-season head-to-head record against one rival organization.</summary>
public sealed record RivalRow(string Id, string Name, int Wins, int Losses, int Remaining);
public sealed record Situation(long Revision, int Day, int Season, int DayOfSeason, int SeasonLength, string Company, string Status,
    long Cash, long Forecast, int Reputation, int Audience, int Load, int Capacity,
    string Coach, Delegation Delegation, Risk Ceiling, string NextFixtureId, int NextMatchDay, string Opponent,
    string OpponentEstimate, string OpponentTendency, string Confidence, string Meta, PlanView? CommittedPlan,
    PlanView? Recommendation, string RecommendationReason, ImmutableArray<PersonRow> People,
    ImmutableArray<CandidateRow> Candidates, ImmutableArray<SponsorRow> Offers, ImmutableArray<string> Sponsors,
    ImmutableArray<BillRow> Bills, ImmutableArray<ResultRow> Results, ImmutableArray<string> Review,
    string PendingConsequences, string FixtureLabel, StaffRow CoachContract, ImmutableArray<CoachCandidateRow> CoachCandidates,
    ImmutableArray<BrandRow> Brands, ImmutableArray<NegotiationRow> Negotiations, int SponsorSlots, int ActiveSponsors,
    ImmutableArray<InboxRow> Inbox, ImmutableArray<FixtureRow> Fixtures, ImmutableArray<RivalRow> Rivals,
    string CompanyId = "", string OpponentId = "");

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
