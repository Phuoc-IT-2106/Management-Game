// Explicit schema-1 DTOs. Changes require deliberate save-schema review.
using System.Collections.Immutable;
using ManagementGame.Domain;
namespace ManagementGame.Infrastructure;

public sealed record PersonDto(string Id, string DefinitionId, string Name, string Role, int Execution, int Adaptability, int Consistency, int Readiness)
{
    public static PersonDto From(Person x) => new(x.Id, x.DefinitionId, x.Name, x.Role, x.Execution, x.Adaptability, x.Consistency, x.Readiness);
    public Person ToDomain() => new(Id, DefinitionId, Name, Role, Execution, Adaptability, Consistency, Readiness);
}

public sealed record CoachDto(string Id, string Name, int Preparation, int Analysis, int Adaptability, int Capacity)
{
    public static CoachDto From(Coach x) => new(x.Id, x.Name, x.Preparation, x.Analysis, x.Adaptability, x.Capacity);
    public Coach ToDomain() => new(Id, Name, Preparation, Analysis, Adaptability, Capacity);
}

public sealed record EmploymentDto(string Id, string PersonId, long Salary, long ReleaseCost, int EndDay)
{
    public static EmploymentDto From(Employment x) => new(x.Id, x.PersonId, x.Salary, x.ReleaseCost, x.EndDay);
    public Employment ToDomain() => new(Id, PersonId, Salary, ReleaseCost, EndDay);
}

public sealed record SponsorContractDto(string Id, string Name, int EndDay, int Load, long WinBonus)
{
    public static SponsorContractDto From(SponsorContract x) => new(x.Id, x.Name, x.EndDay, x.Load, x.WinBonus);
    public SponsorContract ToDomain() => new(Id, Name, EndDay, Load, WinBonus);
}

public sealed record FinancialItemDto(string Id, string CauseId, int DueDay, long Amount, bool Incoming, long Remaining, int? MissedDay)
{
    public static FinancialItemDto From(FinancialItem x) => new(x.Id, x.CauseId, x.DueDay, x.Amount, x.Incoming, x.Remaining, x.MissedDay);
    public FinancialItem ToDomain() => new(Id, CauseId, DueDay, Amount, Incoming, Remaining, MissedDay);
}

public sealed record SettlementDto(string Id, string CauseId, int Day, long Delta)
{
    public static SettlementDto From(Settlement x) => new(x.Id, x.CauseId, x.Day, x.Delta);
    public Settlement ToDomain() => new(Id, CauseId, Day, Delta);
}

public sealed record AuthorityDto(ControlMode Mode, Posture RiskCeiling)
{
    public static AuthorityDto From(Authority x) => new(x.Mode, x.RiskCeiling);
    public Authority ToDomain() => new(Mode, RiskCeiling);
}

public sealed record PlanDto(string FixtureId, int Execution, int Opponent, int Meta, Posture Posture, ImmutableArray<string> Lineup, Posture ExpectedOpponent, string CauseId)
{
    public static PlanDto From(Plan x) => new(x.FixtureId, x.Execution, x.Opponent, x.Meta, x.Posture, x.Lineup, x.ExpectedOpponent, x.CauseId);
    public Plan ToDomain() => new(FixtureId, Execution, Opponent, Meta, Posture, Lineup, ExpectedOpponent, CauseId);
}

public sealed record WorkDto(int Execution, int Opponent, int Meta)
{
    public static WorkDto From(Work x) => new(x.Execution, x.Opponent, x.Meta);
    public Work ToDomain() => new(Execution, Opponent, Meta);
}

public sealed record CommercialEffectDto(string Id, string CauseId, int DueDay, int Reputation, int Audience)
{
    public static CommercialEffectDto From(CommercialEffect x) => new(x.Id, x.CauseId, x.DueDay, x.Reputation, x.Audience);
    public CommercialEffect ToDomain() => new(Id, CauseId, DueDay, Reputation, Audience);
}

public sealed record ReviewEntryDto(string Id, int Day, string CauseId, string Text)
{
    public static ReviewEntryDto From(ReviewEntry x) => new(x.Id, x.Day, x.CauseId, x.Text);
    public ReviewEntry ToDomain() => new(Id, Day, CauseId, Text);
}

public sealed record CompanyDto(string Id, string Name, ImmutableArray<PersonDto> People, CoachDto Coach, ImmutableArray<EmploymentDto> Employment, ImmutableArray<SponsorContractDto> Sponsors, long Cash, ImmutableArray<FinancialItemDto> FinancialItems, ImmutableArray<SettlementDto> Settlements, int Reputation, int Audience, int Information, AuthorityDto Authority, PlanDto? Plan, WorkDto Work, RecoveryStage Recovery, ImmutableArray<CommercialEffectDto> PendingEffects, ImmutableArray<string> FinanceReceipts, ImmutableArray<string> CommercialReceipts, ImmutableArray<ReviewEntryDto> Reviews)
{
    public static CompanyDto From(Company x) => new(x.Id, x.Name, x.People.Select(PersonDto.From).ToImmutableArray(), CoachDto.From(x.Coach), x.Employment.Select(EmploymentDto.From).ToImmutableArray(), x.Sponsors.Select(SponsorContractDto.From).ToImmutableArray(), x.Cash, x.FinancialItems.Select(FinancialItemDto.From).ToImmutableArray(), x.Settlements.Select(SettlementDto.From).ToImmutableArray(), x.Reputation, x.Audience, x.Information, AuthorityDto.From(x.Authority), x.Plan is null ? null : PlanDto.From(x.Plan), WorkDto.From(x.Work), x.Recovery, x.PendingEffects.Select(CommercialEffectDto.From).ToImmutableArray(), x.FinanceReceipts, x.CommercialReceipts, x.Reviews.Select(ReviewEntryDto.From).ToImmutableArray());
    public Company ToDomain() => new(Id, Name, People.Select(v => v.ToDomain()).ToImmutableArray(), Coach.ToDomain(), Employment.Select(v => v.ToDomain()).ToImmutableArray(), Sponsors.Select(v => v.ToDomain()).ToImmutableArray(), Cash, FinancialItems.Select(v => v.ToDomain()).ToImmutableArray(), Settlements.Select(v => v.ToDomain()).ToImmutableArray(), Reputation, Audience, Information, Authority.ToDomain(), Plan?.ToDomain(), Work.ToDomain(), Recovery, PendingEffects.Select(v => v.ToDomain()).ToImmutableArray(), FinanceReceipts, CommercialReceipts, Reviews.Select(v => v.ToDomain()).ToImmutableArray());
}

public sealed record RivalDto(string Id, string Name, int Strength, int Analysis, int Adaptability, Posture Posture, int Budget, int Need)
{
    public static RivalDto From(Rival x) => new(x.Id, x.Name, x.Strength, x.Analysis, x.Adaptability, x.Posture, x.Budget, x.Need);
    public Rival ToDomain() => new(Id, Name, Strength, Analysis, Adaptability, Posture, Budget, Need);
}

public sealed record FixtureDto(string Id, int Day, string RivalId, int Importance, long Prize)
{
    public static FixtureDto From(Fixture x) => new(x.Id, x.Day, x.RivalId, x.Importance, x.Prize);
    public Fixture ToDomain() => new(Id, Day, RivalId, Importance, Prize);
}

public sealed record OfferDto(string Id, string Name, int Deadline, int EndDay, long Payment, long WinBonus, int Load, int MinimumReputation, string? ClaimedBy)
{
    public static OfferDto From(Offer x) => new(x.Id, x.Name, x.Deadline, x.EndDay, x.Payment, x.WinBonus, x.Load, x.MinimumReputation, x.ClaimedBy);
    public Offer ToDomain() => new(Id, Name, Deadline, EndDay, Payment, WinBonus, Load, MinimumReputation, ClaimedBy);
}

public sealed record CandidateDto(PersonDto Person, long Fee, long Salary, long ReleaseCost, int Deadline)
{
    public static CandidateDto From(Candidate x) => new(PersonDto.From(x.Person), x.Fee, x.Salary, x.ReleaseCost, x.Deadline);
    public Candidate ToDomain() => new(Person.ToDomain(), Fee, Salary, ReleaseCost, Deadline);
}

public sealed record CompetitiveOutcomeDto(string Id, string FixtureId, int Day, string RivalId, bool Won, Posture Posture, int Probability, int Base, int Preparation, int Matchup, int Adaptation, int Variance, int Importance, string PlanCause)
{
    public static CompetitiveOutcomeDto From(CompetitiveOutcome x) => new(x.Id, x.FixtureId, x.Day, x.RivalId, x.Won, x.Posture, x.Probability, x.Base, x.Preparation, x.Matchup, x.Adaptation, x.Variance, x.Importance, x.PlanCause);
    public CompetitiveOutcome ToDomain() => new(Id, FixtureId, Day, RivalId, Won, Posture, Probability, Base, Preparation, Matchup, Adaptation, Variance, Importance, PlanCause);
}

public sealed record CalendarDto(int Day, Phase Phase, long Sequence)
{
    public static CalendarDto From(Calendar x) => new(x.Day, x.Phase, x.Sequence);
    public Calendar ToDomain() => new(Day, Phase, Sequence);
}

public sealed record WorldDto(CalendarDto Calendar, ImmutableArray<RivalDto> Rivals, ImmutableArray<FixtureDto> Fixtures, ImmutableArray<CompetitiveOutcomeDto> Results, ImmutableArray<OfferDto> Offers, ImmutableArray<CandidateDto> Candidates, Posture Meta, bool Finished)
{
    public static WorldDto From(World x) => new(CalendarDto.From(x.Calendar), x.Rivals.Select(RivalDto.From).ToImmutableArray(), x.Fixtures.Select(FixtureDto.From).ToImmutableArray(), x.Results.Select(CompetitiveOutcomeDto.From).ToImmutableArray(), x.Offers.Select(OfferDto.From).ToImmutableArray(), x.Candidates.Select(CandidateDto.From).ToImmutableArray(), x.Meta, x.Finished);
    public World ToDomain() => new(Calendar.ToDomain(), Rivals.Select(v => v.ToDomain()).ToImmutableArray(), Fixtures.Select(v => v.ToDomain()).ToImmutableArray(), Results.Select(v => v.ToDomain()).ToImmutableArray(), Offers.Select(v => v.ToDomain()).ToImmutableArray(), Candidates.Select(v => v.ToDomain()).ToImmutableArray(), Meta, Finished);
}

public sealed record CommandReceiptDto(string Id, string Digest, long Revision, string Message)
{
    public static CommandReceiptDto From(CommandReceipt x) => new(x.Id, x.Digest, x.Revision, x.Message);
    public CommandReceipt ToDomain() => new(Id, Digest, Revision, Message);
}

public sealed record ExecutionDto(string CampaignId, ulong Seed, long Revision, string RulesVersion, string ContentHash, ImmutableArray<CommandReceiptDto> Receipts)
{
    public static ExecutionDto From(Execution x) => new(x.CampaignId, x.Seed, x.Revision, x.RulesVersion, x.ContentHash, x.Receipts.Select(CommandReceiptDto.From).ToImmutableArray());
    public Execution ToDomain() => new(CampaignId, Seed, Revision, RulesVersion, ContentHash, Receipts.Select(v => v.ToDomain()).ToImmutableArray());
}

public sealed record CampaignDto(CompanyDto Company, WorldDto World, ExecutionDto Execution)
{
    public static CampaignDto From(Campaign x) => new(CompanyDto.From(x.Company), WorldDto.From(x.World), ExecutionDto.From(x.Execution));
    public Campaign ToDomain() => new(Company.ToDomain(), World.ToDomain(), Execution.ToDomain());
}
