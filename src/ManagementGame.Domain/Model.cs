using System.Collections.Immutable;

namespace ManagementGame.Domain;

public enum Posture { Conservative, Balanced, Aggressive }
public enum ControlMode { Manual, Recommend, Autonomous }
public enum RecoveryStage { Stable, Warning, Distress, Restructuring, Stabilized }
public enum Phase { Finance = 1, Contracts, Preparation, Organization, Rivals, Meta, Checkpoint, Competition, Consequences, Recovery, Review }
/// <summary>How an offer reached the company: the market approached it, or the player negotiated terms.</summary>
public enum OfferOrigin { Inbound, Negotiated, Counter }

public sealed record Person(string Id, string DefinitionId, string Name, string Role, int Execution, int Adaptability, int Consistency, int Readiness);
public sealed record Coach(string Id, string Name, int Preparation, int Analysis, int Adaptability, int Capacity);
public sealed record Employment(string Id, string PersonId, long Salary, long ReleaseCost, int EndDay);
public sealed record SponsorContract(string Id, string BrandId, string Name, int StartDay, int EndDay, int Load, long WinBonus, long Payment);
public sealed record FinancialItem(string Id, string CauseId, int DueDay, long Amount, bool Incoming, long Remaining, int? MissedDay);
public sealed record Settlement(string Id, string CauseId, int Day, long Delta);
public sealed record Authority(ControlMode Mode, Posture RiskCeiling);
public sealed record Plan(string FixtureId, int Execution, int Opponent, int Meta, Posture Posture, ImmutableArray<string> Lineup, Posture ExpectedOpponent, string CauseId);
public sealed record Work(int Execution, int Opponent, int Meta);
public sealed record CommercialEffect(string Id, string CauseId, int DueDay, int Reputation, int Audience);
public sealed record ReviewEntry(string Id, int Day, string CauseId, string Text);

// Cash always reconciles as LedgerBase + retained settlements; season rollover folds settled history into LedgerBase.
public sealed record Company(
    string Id, string Name, ImmutableArray<Person> People, Coach Coach,
    ImmutableArray<Employment> Employment, ImmutableArray<SponsorContract> Sponsors,
    long Cash, long LedgerBase, ImmutableArray<FinancialItem> FinancialItems, ImmutableArray<Settlement> Settlements,
    int Reputation, int Audience, int Information, Authority Authority, Plan? Plan, Work Work,
    RecoveryStage Recovery, ImmutableArray<CommercialEffect> PendingEffects,
    ImmutableArray<string> FinanceReceipts, ImmutableArray<string> CommercialReceipts,
    ImmutableArray<ReviewEntry> Reviews);
public sealed record Rival(string Id, string Name, int Strength, int Analysis, int Adaptability, Posture Posture, int Budget, int Need);
public sealed record Fixture(string Id, int Day, string RivalId, int Importance, long Prize);
public sealed record Offer(string Id, string BrandId, string Name, OfferOrigin Origin, int Deadline, int DurationDays,
    long Payment, long WinBonus, int Load, int MinimumReputation, string? ClaimedBy);
public sealed record Negotiation(string Id, string BrandId, long Payment, int DurationDays, int SubmittedDay, int ResponseDay);
/// <summary>A brand is unavailable to offers and approaches until the given day (after a rejection, expiry or rival deal).</summary>
public sealed record BrandStatus(string BrandId, int AvailableFromDay);
public sealed record Candidate(Person Person, long Fee, long Salary, long ReleaseCost, int Deadline);
public sealed record CoachCandidate(Coach Coach, long Fee, long Salary, int Deadline);
public sealed record CompetitiveOutcome(string Id, string FixtureId, int Day, string RivalId, bool Won, Posture Posture,
    int Probability, int Base, int Preparation, int Matchup, int Adaptation, int Variance, int Importance, string PlanCause);
public sealed record Calendar(int Day, Phase Phase, long Sequence);
public sealed record World(Calendar Calendar, ImmutableArray<Rival> Rivals, ImmutableArray<Fixture> Fixtures,
    ImmutableArray<CompetitiveOutcome> Results, ImmutableArray<Offer> Offers, ImmutableArray<Negotiation> Negotiations,
    ImmutableArray<BrandStatus> Brands, ImmutableArray<Candidate> Candidates, ImmutableArray<CoachCandidate> CoachCandidates,
    Posture Meta, long NextId);
public sealed record CommandReceipt(string Id, string Digest, long Revision, string Message);
public sealed record Execution(string CampaignId, ulong Seed, long Revision, string RulesVersion, string ContentHash,
    ImmutableArray<CommandReceipt> Receipts);
public sealed record Campaign(Company Company, World World, Execution Execution);

// All numerical values below are development content, never invisible production defaults.
public sealed record Balance(int SeasonLength, int FirstMatchDay, int MatchInterval, int RivalCount,
    long StartingCash, int Reputation, int Audience, int Information,
    int BaseLoad, int PreparationScale, int MatchScale, int Variance, int MetaDayOfSeason, int RivalCadence,
    int ReputationGain, int AudienceGain, long PrizeBase,
    int SponsorSlots, int MaxOpenOffers, int MarketInterval, int OfferWindow, int NegotiationDelay, int RejectCooldown,
    int RosterSize, int MaxRoster, int CandidateCount, int CoachCandidateCount, int TalentRefreshInterval,
    int RenewalWindow, int StartingContractSeasons);
public sealed record AttributeRange(int Min, int Max);
public sealed record Generation(AttributeRange Talent, AttributeRange Prospect, AttributeRange CoachSkill,
    AttributeRange CoachCapacity, AttributeRange Rival);
public sealed record NamePool(ImmutableArray<string> Given, ImmutableArray<string> Family);
public sealed record BrandDefinition(string Id, string Name, string Sector, int MinimumReputation, int MinimumAudience,
    long BasePayment, long WinBonus, int Load, ImmutableArray<int> DurationDays);
public sealed record Content(string Format, int Schema, string Label, string RulesVersion, string DefaultCompanyName,
    Balance Balance, Generation Generation, NamePool Names, ImmutableArray<string> OrganizationNames,
    ImmutableArray<BrandDefinition> Brands);

public abstract record Intent;
public sealed record CommitPlan(Plan Plan) : Intent;
public sealed record SetAuthority(Authority Authority) : Intent;
public sealed record SignPlayer(string PersonId, int Seasons) : Intent;
public sealed record ReleasePlayer(string PersonId) : Intent;
public sealed record AcceptSponsor(string OfferId) : Intent;
public sealed record ProposeSponsorship(string BrandId, long Payment, int DurationDays) : Intent;
public sealed record RenewContract(string PersonId, int Seasons) : Intent;
public sealed record HireCoach(string CoachId) : Intent;
public sealed record Advance : Intent;
public sealed class RuleViolation(string reason) : Exception(reason);
