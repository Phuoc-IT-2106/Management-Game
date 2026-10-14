// Explicit schema-5 DTOs (continuous seasons; rules v3 sponsor termination clause; schema 4 added transfer history,
// World.Moves; schema 5 is rules v4: role-weighted skills, mind, career, condition and popularity on Person, Coach development).
// Changes require deliberate save-schema review.
using System.Collections.Immutable;
using ManagementGame.Domain;
namespace ManagementGame.Infrastructure;

public sealed record SkillsDto(int Mechanics, int EarlyGame, int Teamfight, int Macro, int ChampionPool)
{
    public static SkillsDto From(Skills x) => new(x.Mechanics, x.EarlyGame, x.Teamfight, x.Macro, x.ChampionPool);
    public Skills ToDomain() => new(Mechanics, EarlyGame, Teamfight, Macro, ChampionPool);
}

public sealed record MindDto(int Consistency, int Composure, int Communication, int Professionalism, int Ambition, int Loyalty)
{
    public static MindDto From(Mind x) => new(x.Consistency, x.Composure, x.Communication, x.Professionalism, x.Ambition, x.Loyalty);
    public Mind ToDomain() => new(Consistency, Composure, Communication, Professionalism, Ambition, Loyalty);
}

public sealed record PersonDto(string Id, string DefinitionId, string Name, string Handle, string Nationality, int BornSeason, string Role, string SecondaryRole,
    SkillsDto Skills, MindDto Mind, int Potential, int Readiness, int Form, int Morale, int Popularity)
{
    public static PersonDto From(Person x) => new(x.Id, x.DefinitionId, x.Name, x.Handle, x.Nationality, x.BornSeason, x.Role, x.SecondaryRole,
        SkillsDto.From(x.Skills), MindDto.From(x.Mind), x.Potential, x.Readiness, x.Form, x.Morale, x.Popularity);
    public Person ToDomain() => new(Id, DefinitionId, Name, Handle, Nationality, BornSeason, Role, SecondaryRole, Skills.ToDomain(), Mind.ToDomain(),
        Potential, Readiness, Form, Morale, Popularity);
}

public sealed record CoachDto(string Id, string Name, int Preparation, int Analysis, int Adaptability, int Capacity, int Development)
{
    public static CoachDto From(Coach x) => new(x.Id, x.Name, x.Preparation, x.Analysis, x.Adaptability, x.Capacity, x.Development);
    public Coach ToDomain() => new(Id, Name, Preparation, Analysis, Adaptability, Capacity, Development);
}

public sealed record EmploymentDto(string Id, string PersonId, long Salary, long ReleaseCost, int EndDay, int Familiarity, int Played)
{
    public static EmploymentDto From(Employment x) => new(x.Id, x.PersonId, x.Salary, x.ReleaseCost, x.EndDay, x.Familiarity, x.Played);
    public Employment ToDomain() => new(Id, PersonId, Salary, ReleaseCost, EndDay, Familiarity, Played);
}

public sealed record SponsorContractDto(string Id, string BrandId, string Name, int StartDay, int EndDay, int Load, long WinBonus, long Payment, int ForfeitLimit)
{
    public static SponsorContractDto From(SponsorContract x) => new(x.Id, x.BrandId, x.Name, x.StartDay, x.EndDay, x.Load, x.WinBonus, x.Payment, x.ForfeitLimit);
    public SponsorContract ToDomain() => new(Id, BrandId, Name, StartDay, EndDay, Load, WinBonus, Payment, ForfeitLimit);
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

public sealed record AuthorityDto(ControlMode Mode, Posture RiskCeiling, long RecruitFee, long RecruitWage)
{
    public static AuthorityDto From(Authority x) => new(x.Mode, x.RiskCeiling, x.RecruitFee, x.RecruitWage);
    public Authority ToDomain() => new(Mode, RiskCeiling, RecruitFee, RecruitWage);
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

public sealed record CompanyDto(string Id, string Name, ImmutableArray<PersonDto> People, CoachDto Coach, ImmutableArray<EmploymentDto> Employment, ImmutableArray<SponsorContractDto> Sponsors, long Cash, long LedgerBase, ImmutableArray<FinancialItemDto> FinancialItems, ImmutableArray<SettlementDto> Settlements, int Reputation, int Audience, int Information, AuthorityDto Authority, PlanDto? Plan, WorkDto Work, RecoveryStage Recovery, ImmutableArray<CommercialEffectDto> PendingEffects, ImmutableArray<string> FinanceReceipts, ImmutableArray<string> CommercialReceipts, ImmutableArray<ReviewEntryDto> Reviews)
{
    public static CompanyDto From(Company x) => new(x.Id, x.Name, x.People.Select(PersonDto.From).ToImmutableArray(), CoachDto.From(x.Coach), x.Employment.Select(EmploymentDto.From).ToImmutableArray(), x.Sponsors.Select(SponsorContractDto.From).ToImmutableArray(), x.Cash, x.LedgerBase, x.FinancialItems.Select(FinancialItemDto.From).ToImmutableArray(), x.Settlements.Select(SettlementDto.From).ToImmutableArray(), x.Reputation, x.Audience, x.Information, AuthorityDto.From(x.Authority), x.Plan is null ? null : PlanDto.From(x.Plan), WorkDto.From(x.Work), x.Recovery, x.PendingEffects.Select(CommercialEffectDto.From).ToImmutableArray(), x.FinanceReceipts, x.CommercialReceipts, x.Reviews.Select(ReviewEntryDto.From).ToImmutableArray());
    public Company ToDomain() => new(Id, Name, People.Select(v => v.ToDomain()).ToImmutableArray(), Coach.ToDomain(), Employment.Select(v => v.ToDomain()).ToImmutableArray(), Sponsors.Select(v => v.ToDomain()).ToImmutableArray(), Cash, LedgerBase, FinancialItems.Select(v => v.ToDomain()).ToImmutableArray(), Settlements.Select(v => v.ToDomain()).ToImmutableArray(), Reputation, Audience, Information, Authority.ToDomain(), Plan?.ToDomain(), Work.ToDomain(), Recovery, PendingEffects.Select(v => v.ToDomain()).ToImmutableArray(), FinanceReceipts, CommercialReceipts, Reviews.Select(v => v.ToDomain()).ToImmutableArray());
}

public sealed record RivalContractDto(string PersonId, long Salary, int EndDay)
{
    public static RivalContractDto From(RivalContract x) => new(x.PersonId, x.Salary, x.EndDay);
    public RivalContract ToDomain() => new(PersonId, Salary, EndDay);
}
public sealed record RivalDto(string Id, string Name, ImmutableArray<PersonDto> People, ImmutableArray<RivalContractDto> Contracts, CoachDto Coach,
    long Funds, int Reputation, Posture Posture, Strategy Strategy, int Aggression, int YouthFocus, int PoorSeasons)
{
    public static RivalDto From(Rival x) => new(x.Id, x.Name, x.People.Select(PersonDto.From).ToImmutableArray(), x.Contracts.Select(RivalContractDto.From).ToImmutableArray(),
        CoachDto.From(x.Coach), x.Funds, x.Reputation, x.Posture, x.Strategy, x.Aggression, x.YouthFocus, x.PoorSeasons);
    public Rival ToDomain() => new(Id, Name, People.Select(p => p.ToDomain()).ToImmutableArray(), Contracts.Select(c => c.ToDomain()).ToImmutableArray(),
        Coach.ToDomain(), Funds, Reputation, Posture, Strategy, Aggression, YouthFocus, PoorSeasons);
}

public sealed record FixtureDto(string Id, int Day, string RivalId, int Importance, long Prize, int Games, string Stage)
{
    public static FixtureDto From(Fixture x) => new(x.Id, x.Day, x.RivalId, x.Importance, x.Prize, x.Games, x.Stage);
    public Fixture ToDomain() => new(Id, Day, RivalId, Importance, Prize, Games, Stage);
}

public sealed record LeagueFixtureDto(string Id, int Day, string HomeId, string AwayId, int Importance, int Games, string Stage)
{
    public static LeagueFixtureDto From(LeagueFixture x) => new(x.Id, x.Day, x.HomeId, x.AwayId, x.Importance, x.Games, x.Stage);
    public LeagueFixture ToDomain() => new(Id, Day, HomeId, AwayId, Importance, Games, Stage);
}

public sealed record LeagueResultDto(string Id, string FixtureId, int Day, string HomeId, string AwayId, int HomeGames, int AwayGames)
{
    public static LeagueResultDto From(LeagueResult x) => new(x.Id, x.FixtureId, x.Day, x.HomeId, x.AwayId, x.HomeGames, x.AwayGames);
    public LeagueResult ToDomain() => new(Id, FixtureId, Day, HomeId, AwayId, HomeGames, AwayGames);
}

public sealed record SeasonFinishDto(int Season, ImmutableArray<string> Order)
{
    public static SeasonFinishDto From(SeasonFinish x) => new(x.Season, x.Order);
    public SeasonFinish ToDomain() => new(Season, Order);
}

public sealed record OfferDto(string Id, string BrandId, string Name, OfferOrigin Origin, int Deadline, int DurationDays, long Payment, long WinBonus, int Load, int MinimumReputation, string? ClaimedBy)
{
    public static OfferDto From(Offer x) => new(x.Id, x.BrandId, x.Name, x.Origin, x.Deadline, x.DurationDays, x.Payment, x.WinBonus, x.Load, x.MinimumReputation, x.ClaimedBy);
    public Offer ToDomain() => new(Id, BrandId, Name, Origin, Deadline, DurationDays, Payment, WinBonus, Load, MinimumReputation, ClaimedBy);
}

public sealed record NegotiationDto(string Id, string BrandId, long Payment, int DurationDays, int SubmittedDay, int ResponseDay)
{
    public static NegotiationDto From(Negotiation x) => new(x.Id, x.BrandId, x.Payment, x.DurationDays, x.SubmittedDay, x.ResponseDay);
    public Negotiation ToDomain() => new(Id, BrandId, Payment, DurationDays, SubmittedDay, ResponseDay);
}

public sealed record BrandStatusDto(string BrandId, int AvailableFromDay)
{
    public static BrandStatusDto From(BrandStatus x) => new(x.BrandId, x.AvailableFromDay);
    public BrandStatus ToDomain() => new(BrandId, AvailableFromDay);
}

public sealed record CoachCandidateDto(CoachDto Coach, long Fee, long Salary, int Deadline)
{
    public static CoachCandidateDto From(CoachCandidate x) => new(CoachDto.From(x.Coach), x.Fee, x.Salary, x.Deadline);
    public CoachCandidate ToDomain() => new(Coach.ToDomain(), Fee, Salary, Deadline);
}

public sealed record CandidateDto(PersonDto Person, long Fee, long Salary, long ReleaseCost)
{
    public static CandidateDto From(Candidate x) => new(PersonDto.From(x.Person), x.Fee, x.Salary, x.ReleaseCost);
    public Candidate ToDomain() => new(Person.ToDomain(), Fee, Salary, ReleaseCost);
}

public sealed record CompetitiveOutcomeDto(string Id, string FixtureId, int Day, string RivalId, bool Won, Posture Posture, int Probability, int Base, int Preparation, int Matchup, int Adaptation, int Variance, int Importance, string PlanCause,
    int Familiarity, int Style, int Composure, int Leadership, int GamesWon, int GamesLost)
{
    public static CompetitiveOutcomeDto From(CompetitiveOutcome x) => new(x.Id, x.FixtureId, x.Day, x.RivalId, x.Won, x.Posture, x.Probability, x.Base, x.Preparation, x.Matchup, x.Adaptation, x.Variance, x.Importance, x.PlanCause,
        x.Familiarity, x.Style, x.Composure, x.Leadership, x.GamesWon, x.GamesLost);
    public CompetitiveOutcome ToDomain() => new(Id, FixtureId, Day, RivalId, Won, Posture, Probability, Base, Preparation, Matchup, Adaptation, Variance, Importance, PlanCause,
        Familiarity, Style, Composure, Leadership, GamesWon, GamesLost);
}

public sealed record CalendarDto(int Day, Phase Phase, long Sequence)
{
    public static CalendarDto From(Calendar x) => new(x.Day, x.Phase, x.Sequence);
    public Calendar ToDomain() => new(Day, Phase, Sequence);
}

public sealed record MoveDto(string Id, int Day, string PersonId, string Name, Personnel Kind, string Role, MoveReason Reason, string? Origin, string? Destination, string CauseId)
{
    public static MoveDto From(Move x) => new(x.Id, x.Day, x.PersonId, x.Name, x.Kind, x.Role, x.Reason, x.Origin, x.Destination, x.CauseId);
    public Move ToDomain() => new(Id, Day, PersonId, Name, Kind, Role, Reason, Origin, Destination, CauseId);
}

public sealed record WorldDto(CalendarDto Calendar, ImmutableArray<RivalDto> Rivals, ImmutableArray<FixtureDto> Fixtures, ImmutableArray<CompetitiveOutcomeDto> Results, ImmutableArray<OfferDto> Offers, ImmutableArray<NegotiationDto> Negotiations, ImmutableArray<BrandStatusDto> Brands, ImmutableArray<CandidateDto> Candidates, ImmutableArray<CoachCandidateDto> CoachCandidates, Posture Meta, long NextId, ImmutableArray<MoveDto> Moves,
    ImmutableArray<LeagueFixtureDto> LeagueFixtures, ImmutableArray<LeagueResultDto> LeagueResults, ImmutableArray<SeasonFinishDto> Finishes)
{
    public static WorldDto From(World x) => new(CalendarDto.From(x.Calendar), x.Rivals.Select(RivalDto.From).ToImmutableArray(), x.Fixtures.Select(FixtureDto.From).ToImmutableArray(), x.Results.Select(CompetitiveOutcomeDto.From).ToImmutableArray(), x.Offers.Select(OfferDto.From).ToImmutableArray(), x.Negotiations.Select(NegotiationDto.From).ToImmutableArray(), x.Brands.Select(BrandStatusDto.From).ToImmutableArray(), x.Candidates.Select(CandidateDto.From).ToImmutableArray(), x.CoachCandidates.Select(CoachCandidateDto.From).ToImmutableArray(), x.Meta, x.NextId, x.Moves.Select(MoveDto.From).ToImmutableArray(),
        x.LeagueFixtures.Select(LeagueFixtureDto.From).ToImmutableArray(), x.LeagueResults.Select(LeagueResultDto.From).ToImmutableArray(), x.Finishes.Select(SeasonFinishDto.From).ToImmutableArray());
    public World ToDomain() => new(Calendar.ToDomain(), Rivals.Select(v => v.ToDomain()).ToImmutableArray(), Fixtures.Select(v => v.ToDomain()).ToImmutableArray(), Results.Select(v => v.ToDomain()).ToImmutableArray(), Offers.Select(v => v.ToDomain()).ToImmutableArray(), Negotiations.Select(v => v.ToDomain()).ToImmutableArray(), Brands.Select(v => v.ToDomain()).ToImmutableArray(), Candidates.Select(v => v.ToDomain()).ToImmutableArray(), CoachCandidates.Select(v => v.ToDomain()).ToImmutableArray(), Meta, NextId, Moves.Select(v => v.ToDomain()).ToImmutableArray(),
        LeagueFixtures.Select(v => v.ToDomain()).ToImmutableArray(), LeagueResults.Select(v => v.ToDomain()).ToImmutableArray(), Finishes.Select(v => v.ToDomain()).ToImmutableArray());
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
