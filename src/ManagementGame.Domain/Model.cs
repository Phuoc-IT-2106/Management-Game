using System.Collections.Immutable;

namespace ManagementGame.Domain;

public enum Posture { Conservative, Balanced, Aggressive }
public enum ControlMode { Manual, Recommend, Autonomous }
public enum RecoveryStage { Stable, Warning, Distress, Restructuring, Stabilized }
public enum Phase { Finance = 1, Contracts, Preparation, Organization, Rivals, Meta, Checkpoint, Competition, Consequences, Recovery, Review }

public sealed record Person(string Id, string DefinitionId, string Name, string Role, int Execution, int Adaptability, int Consistency, int Readiness);
public sealed record Coach(string Id, string Name, int Preparation, int Analysis, int Adaptability, int Capacity);
public sealed record Employment(string Id, string PersonId, long Salary, long ReleaseCost, int EndDay);
public sealed record SponsorContract(string Id, string Name, int EndDay, int Load, long WinBonus);
public sealed record FinancialItem(string Id, string CauseId, int DueDay, long Amount, bool Incoming, long Remaining, int? MissedDay);
public sealed record Settlement(string Id, string CauseId, int Day, long Delta);
public sealed record Authority(ControlMode Mode, Posture RiskCeiling);
public sealed record Plan(string FixtureId, int Execution, int Opponent, int Meta, Posture Posture, ImmutableArray<string> Lineup, Posture ExpectedOpponent, string CauseId);
public sealed record Work(int Execution, int Opponent, int Meta);
public sealed record CommercialEffect(string Id, string CauseId, int DueDay, int Reputation, int Audience);
public sealed record ReviewEntry(string Id, int Day, string CauseId, string Text);

public sealed record Company(
    string Id, string Name, ImmutableArray<Person> People, Coach Coach,
    ImmutableArray<Employment> Employment, ImmutableArray<SponsorContract> Sponsors,
    long Cash, ImmutableArray<FinancialItem> FinancialItems, ImmutableArray<Settlement> Settlements,
    int Reputation, int Audience, int Information, Authority Authority, Plan? Plan, Work Work,
    RecoveryStage Recovery, ImmutableArray<CommercialEffect> PendingEffects,
    ImmutableArray<string> FinanceReceipts, ImmutableArray<string> CommercialReceipts,
    ImmutableArray<ReviewEntry> Reviews);
public sealed record Rival(string Id, string Name, int Strength, int Analysis, int Adaptability, Posture Posture, int Budget, int Need);
public sealed record Fixture(string Id, int Day, string RivalId, int Importance, long Prize);
public sealed record Offer(string Id, string Name, int Deadline, int EndDay, long Payment, long WinBonus, int Load, int MinimumReputation, string? ClaimedBy);
public sealed record Candidate(Person Person, long Fee, long Salary, long ReleaseCost, int Deadline);
public sealed record CompetitiveOutcome(string Id, string FixtureId, int Day, string RivalId, bool Won, Posture Posture,
    int Probability, int Base, int Preparation, int Matchup, int Adaptation, int Variance, int Importance, string PlanCause);
public sealed record Calendar(int Day, Phase Phase, long Sequence);
public sealed record World(Calendar Calendar, ImmutableArray<Rival> Rivals, ImmutableArray<Fixture> Fixtures,
    ImmutableArray<CompetitiveOutcome> Results, ImmutableArray<Offer> Offers, ImmutableArray<Candidate> Candidates,
    Posture Meta, bool Finished);
public sealed record CommandReceipt(string Id, string Digest, long Revision, string Message);
public sealed record Execution(string CampaignId, ulong Seed, long Revision, string RulesVersion, string ContentHash,
    ImmutableArray<CommandReceipt> Receipts);
public sealed record Campaign(Company Company, World World, Execution Execution);

// All numerical values below are development content, never invisible production defaults.
public sealed record Balance(int Horizon, long StartingCash, int Reputation, int Audience, int Information,
    long OperatingCost, int BaseLoad, int PreparationScale, int MatchScale, int Variance,
    int MetaDay, int RivalCadence, int ReputationGain, int AudienceGain);
public sealed record PersonDefinition(Person Person, long Salary, long ReleaseCost);
public sealed record Content(string Format, int Schema, string Label, string RulesVersion, string CompanyName,
    Balance Balance, Coach Coach, ImmutableArray<PersonDefinition> Players, ImmutableArray<Rival> Rivals,
    ImmutableArray<Fixture> Fixtures, ImmutableArray<Offer> Offers, ImmutableArray<Candidate> Candidates,
    Offer InitialSponsor);

public abstract record Intent;
public sealed record CommitPlan(Plan Plan) : Intent;
public sealed record SetAuthority(Authority Authority) : Intent;
public sealed record SignPlayer(string PersonId) : Intent;
public sealed record ReleasePlayer(string PersonId) : Intent;
public sealed record AcceptSponsor(string OfferId) : Intent;
public sealed record Advance : Intent;
public sealed class RuleViolation(string reason) : Exception(reason);
