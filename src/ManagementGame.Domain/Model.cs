using System.Collections.Immutable;

namespace ManagementGame.Domain;

public enum Posture { Conservative, Balanced, Aggressive }
public enum ControlMode { Manual, Recommend, Autonomous }
public enum RecoveryStage { Stable, Warning, Distress, Restructuring, Stabilized }
public enum Phase { Finance = 1, Contracts, Preparation, Organization, Rivals, Meta, Checkpoint, Competition, Consequences, Recovery, Review }
/// <summary>How an offer reached the company: the market approached it, or the player negotiated terms.</summary>
public enum OfferOrigin { Inbound, Negotiated, Counter }

/// <summary>The five roles of the discipline in lineup order (rules, not content). Display names are presentation.</summary>
public static class Roles
{
    public static readonly ImmutableArray<string> All = ["top", "jungle", "mid", "bot", "support"];
    public static bool Valid(string role) => All.Contains(role);
    /// <summary>The role's name in notices and records ("Top", "Jungle", ...).</summary>
    public static string Name(string role) => role switch
    {
        "top" => "Top", "jungle" => "Jungle", "mid" => "Mid", "bot" => "Bot", "support" => "Support", _ => role
    };
}
/// <summary>Role-weighted competitive skills, 1–99: mechanics, early game (laning, early jungle pressure, lane support),
/// teamfight (damage, engage, peel), macro (vision, objectives, rotations) and champion pool (meta flexibility).</summary>
public sealed record Skills(int Mechanics, int EarlyGame, int Teamfight, int Macro, int ChampionPool);
/// <summary>Mental attributes, 1–99. Consistency, composure and communication act in matches; professionalism, ambition
/// and loyalty are personality (hidden from every actor) and act on development, wages, renewals and conduct.</summary>
public sealed record Mind(int Consistency, int Composure, int Communication, int Professionalism, int Ambition, int Loyalty);
/// <summary>A player. Handle is the in-game name shown in play; Name is the legal name. Age is the current season minus
/// <paramref name="BornSeason"/> (one season is one competitive year). Potential is the hidden ceiling of the primary-role
/// rating. Form (−10..10), Morale (0–100) and Readiness (1–100) are condition; Popularity (0–100) is fan following.
/// SecondaryRole is empty or another role the player can cover at reduced proficiency.</summary>
public sealed record Person(string Id, string DefinitionId, string Name, string Handle, string Nationality, int BornSeason,
    string Role, string SecondaryRole, Skills Skills, Mind Mind, int Potential, int Readiness, int Form, int Morale, int Popularity);
/// <summary>A head coach. Analysis sharpens estimates (scouting), Adaptability reads the meta, Development grows players.</summary>
public sealed record Coach(string Id, string Name, int Preparation, int Analysis, int Adaptability, int Capacity, int Development);
/// <summary>A contract. Familiarity (0–100) is how integrated the person is with the team: it starts at zero for a new
/// signing and grows with every day and every match played together. Played counts the person's matches this season
/// (playing time, which feeds development); it restarts at each season.</summary>
public sealed record Employment(string Id, string PersonId, long Salary, long ReleaseCost, int EndDay, int Familiarity, int Played);
/// <summary>A signed agreement. <paramref name="ForfeitLimit"/> is its termination clause: that many forfeits in one season
/// let the brand end it (0: no clause).</summary>
public sealed record SponsorContract(string Id, string BrandId, string Name, int StartDay, int EndDay, int Load, long WinBonus, long Payment, int ForfeitLimit);
public sealed record FinancialItem(string Id, string CauseId, int DueDay, long Amount, bool Incoming, long Remaining, int? MissedDay);
public sealed record Settlement(string Id, string CauseId, int Day, long Delta);
/// <summary>The head coach's authority envelope (DEC-014): the control mode, the risk ceiling and, for an autonomous coach,
/// the recruitment limits the owner sets: the largest signing fee and the largest daily wage of one signing (minor units).
/// Zero limits mean the coach does not recruit.</summary>
public sealed record Authority(ControlMode Mode, Posture RiskCeiling, long RecruitFee = 0, long RecruitWage = 0);
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
/// <summary>A rival organization's strategy for a season (rules v4 phase 5): Contend spends to win now, Balanced keeps the
/// squad competitive, Rebuild lets experience go and plays youth, Conserve protects its funds.</summary>
public enum Strategy { Contend, Balanced, Rebuild, Conserve }
/// <summary>A rival's contract with one of its players: the daily wage and the last day.</summary>
public sealed record RivalContract(string PersonId, long Salary, int EndDay);
/// <summary>A rival organization (rules v4 phase 5): its players and their contracts, a head coach, the season's transfer
/// funds (minor units), reputation, the match posture it tends to play (what the company's scouting reads), its strategy,
/// its tendencies (Aggression: how readily it buys; YouthFocus: how much it favours young players) and its run of poor
/// seasons. Its strength is derived from its players and never stored (<see cref="Rivals.Strength"/>); its analysis and
/// adaptability are its head coach's.</summary>
public sealed record Rival(string Id, string Name, ImmutableArray<Person> People, ImmutableArray<RivalContract> Contracts, Coach Coach,
    long Funds, int Reputation, Posture Posture, Strategy Strategy, int Aggression, int YouthFocus, int PoorSeasons);
/// <summary>A match of the company against a rival: regular season, semifinal or final, played as a best-of-<paramref name="Games"/>.</summary>
public sealed record Fixture(string Id, int Day, string RivalId, int Importance, long Prize, int Games, string Stage);
/// <summary>A league match between two rivals (the company is not in it).</summary>
public sealed record LeagueFixture(string Id, int Day, string HomeId, string AwayId, int Importance, int Games, string Stage);
/// <summary>The result of a league match between rivals: games won by each side.</summary>
public sealed record LeagueResult(string Id, string FixtureId, int Day, string HomeId, string AwayId, int HomeGames, int AwayGames);
/// <summary>A finished season's final order of teams, champion first.</summary>
public sealed record SeasonFinish(int Season, ImmutableArray<string> Order);
public sealed record Offer(string Id, string BrandId, string Name, OfferOrigin Origin, int Deadline, int DurationDays,
    long Payment, long WinBonus, int Load, int MinimumReputation, string? ClaimedBy);
public sealed record Negotiation(string Id, string BrandId, long Payment, int DurationDays, int SubmittedDay, int ResponseDay);
/// <summary>A brand is unavailable to offers and approaches until the given day (after a rejection, expiry or rival deal).</summary>
public sealed record BrandStatus(string BrandId, int AvailableFromDay);
/// <summary>A free agent on the open market: asking fee and terms. Free agents stay on the market until someone signs them
/// or they retire (rules v4 lifecycle); their terms follow them as they develop and age.</summary>
public sealed record Candidate(Person Person, long Fee, long Salary, long ReleaseCost);
public sealed record CoachCandidate(Coach Coach, long Fee, long Salary, int Deadline);
/// <summary>The company's match result. Probability is the series win probability before match-day variance (millionths);
/// the other figures are the terms of the first game's strength difference (developer truth, never observed).</summary>
public sealed record CompetitiveOutcome(string Id, string FixtureId, int Day, string RivalId, bool Won, Posture Posture,
    int Probability, int Base, int Preparation, int Matchup, int Adaptation, int Variance, int Importance, string PlanCause,
    int Familiarity, int Style, int Composure, int Leadership, int GamesWon, int GamesLost);
public enum Personnel { Player, HeadCoach }
/// <summary>Why a person moved: the company signed a player or appointed a head coach; an interim head coach took over
/// when the head coach's contract ended; the company released a player; a contract ended without renewal; or a head coach
/// was replaced by a new appointment; a player retired, when a contract ended or from the open market; or a player moved
/// directly from one organization to another for a fee (rival organizations, rules v4 phase 5).</summary>
public enum MoveReason { Signed, Appointed, Interim, Released, ContractEnded, Replaced, Retired, Transferred }
/// <summary>A completed personnel move, recorded when it happens and never rewritten (transfer history). Origin and
/// Destination are the organizations the person left and joined; null is the open market, where a person is a free
/// agent. Name and Role are the person as they were then (Role is empty for a head coach), so the move can still be told
/// after the person has left every current record. CauseId is the command or contract that caused it.</summary>
public sealed record Move(string Id, int Day, string PersonId, string Name, Personnel Kind, string Role, MoveReason Reason,
    string? Origin, string? Destination, string CauseId);
public sealed record Calendar(int Day, Phase Phase, long Sequence);
/// <summary>Moves is transfer history in the order it happened, bounded to the current and the previous season.</summary>
public sealed record World(Calendar Calendar, ImmutableArray<Rival> Rivals, ImmutableArray<Fixture> Fixtures,
    ImmutableArray<CompetitiveOutcome> Results, ImmutableArray<Offer> Offers, ImmutableArray<Negotiation> Negotiations,
    ImmutableArray<BrandStatus> Brands, ImmutableArray<Candidate> Candidates, ImmutableArray<CoachCandidate> CoachCandidates,
    Posture Meta, long NextId, ImmutableArray<Move> Moves, ImmutableArray<LeagueFixture> LeagueFixtures, ImmutableArray<LeagueResult> LeagueResults,
    ImmutableArray<SeasonFinish> Finishes);
public sealed record CommandReceipt(string Id, string Digest, long Revision, string Message);
public sealed record Execution(string CampaignId, ulong Seed, long Revision, string RulesVersion, string ContentHash,
    ImmutableArray<CommandReceipt> Receipts);
public sealed record Campaign(Company Company, World World, Execution Execution);

// All numerical values below are development content, never invisible production defaults.
public sealed record Balance(int SeasonLength, int FirstMatchDay, int MatchInterval, int RivalCount,
    long StartingCash, int Reputation, int Audience, int Information,
    int BaseLoad, int PreparationScale, int LogitScale, int Variance, int MetaDayOfSeason, int RivalCadence,
    int ReputationGain, int AudienceGain, long PrizeBase,
    int SponsorSlots, int MaxOpenOffers, int MarketInterval, int OfferWindow, int NegotiationDelay, int RejectCooldown,
    int RosterSize, int MaxRoster, int CandidateCount, int CoachCandidateCount, int TalentRefreshInterval,
    int RenewalWindow, int StartingContractSeasons,
    // Rules v3 wave 1 (incentives): a fixed daily running cost; a registered roster minimum whose shortfall withholds each
    // match's prize and owes ShortfallFinePercent of it as a fine; a sponsor termination clause after repeated forfeits; and
    // a season-end pull of reputation toward the level the season's record earns (ReputationRegression percent of the gap,
    // the target running from ReputationTargetLow at no wins to ReputationTargetHigh at all wins; rules v4 weighs the final
    // league place equally with the record).
    long OperatingCost, int MinimumRoster, int ShortfallFinePercent, int SponsorForfeitLimit, int ReputationRegression,
    int ReputationTargetLow, int ReputationTargetHigh,
    // Rules v4: every win also earns ReputationWinBase × its importance, whatever was expected (the user's decision,
    // 2026-10-13), besides the surprise-weighted change.
    int ReputationWinBase,
    // How many of the company's latest results a rival reads for a repeated posture. Rules v2 counted every result since
    // the campaign began, so the penalty grew without bound across continuous seasons; v1's four-match season never saw
    // more than three.
    int PredictabilityWindow,
    // Rules v4 league: the company and RivalCount rivals play a double round robin of best-of-RegularGames; the top
    // PlayoffTeams play semifinals and a final of best-of-PlayoffGames, PlayoffInterval days apart. PlacePrizes pays the
    // final order (champion first). LogitScale is rating points per logit unit in hundredths. Familiarity grows by
    // FamiliarityPerDay each day and FamiliarityPerMatch for each match played; the starting roster begins at
    // StartingFamiliarity.
    int RegularGames, int PlayoffGames, int PlayoffTeams, int PlayoffInterval, ImmutableArray<long> PlacePrizes,
    int FamiliarityPerDay, int FamiliarityPerMatch, int StartingFamiliarity,
    // Readiness a player loses for each match played (recovered at one point a day).
    int MatchFatigue,
    // Rules v4 transfer fees, in days of the asking wage: FeeDaysMin up to the FeePivot rating, FeeDaysPerPoint more for
    // every point above it, at most FeeDaysMax, so a strong squad takes seasons to buy (the user's decision, 2026-10-13).
    int FeeDaysMin, int FeeDaysMax, int FeePivot, int FeeDaysPerPoint);
public sealed record AttributeRange(int Min, int Max);
/// <summary>A value with a draw weight (basis points or any positive integers).</summary>
public sealed record WeightedValue(int Value, int Weight);
/// <summary>A player archetype of the roles it lists: skill offsets in Skills order and a communication offset.</summary>
public sealed record Archetype(string Id, ImmutableArray<string> Roles, int Weight, ImmutableArray<int> Offsets, int Communication);
/// <summary>For ages up to <paramref name="MaxAge"/>: the shift applied to the ability band and the range of potential
/// headroom above the generated rating.</summary>
public sealed record AgeProfile(int MaxAge, int Shift, int HeadroomMin, int HeadroomMax);
/// <summary>Player generation (rules v4). Abilities come from one shared band distribution for the starting roster, the
/// market and rivals, shifted by age; an archetype of the role shapes the skills. The company's opening squad is shifted by
/// <paramref name="StartingShift"/> more: a new organization starts as an underdog and has to build (the user's decision,
/// 2026-10-13).</summary>
public sealed record Generation(ImmutableArray<WeightedValue> AbilityBands, int SkillJitter, ImmutableArray<Archetype> Archetypes,
    ImmutableArray<WeightedValue> Ages, ImmutableArray<AgeProfile> AgeProfiles, AttributeRange Mind, AttributeRange Personality,
    int SecondaryRoleChance, AttributeRange CoachSkill, AttributeRange CoachCapacity, AttributeRange Rival, int StartingShift);
/// <summary>The weight of each skill (Skills order, basis points summing to 10,000) in a role's rating.</summary>
public sealed record RoleWeight(string Role, ImmutableArray<int> Weights);
/// <summary>A nationality of generated people: its draw weight and name pools. FamilyFirst writes the family name first.</summary>
public sealed record Nationality(string Id, int Weight, bool FamilyFirst, ImmutableArray<string> Given, ImmutableArray<string> Family);
/// <summary>Parts that compose generated handles (prefix + suffix); never copied from real players.</summary>
public sealed record HandleParts(ImmutableArray<string> Prefixes, ImmutableArray<string> Suffixes);
/// <summary>Where the generation tables were calibrated from: an aggregate report and its hash (no person data).</summary>
public sealed record Calibration(string Source, string ReportSha256, string ObservedFrom, string ObservedTo);
/// <summary>The per-season change of the primary-role rating for ages up to <paramref name="MaxAge"/>: a keyed draw in
/// [<paramref name="Min"/>, <paramref name="Max"/>] before professionalism, the coach and playing time scale it.</summary>
public sealed record DevelopmentBand(int MaxAge, int Min, int Max);
/// <summary>The player lifecycle (rules v4 phase 4). Development by age; retirement by age (basis points per season,
/// calibrated) scaled by rating; youth intake by age (weights, calibrated); a free-agent pool that persists between
/// <see cref="PoolSize"/> at the start and <see cref="PoolCap"/>, with at least <see cref="PoolFloor"/> free agents in every role
/// once a season's recruitment is over; form, morale and popularity from matches; conduct from professionalism.</summary>
public sealed record Lifecycle(
    ImmutableArray<DevelopmentBand> Development, ImmutableArray<WeightedValue> RetirementByAge, ImmutableArray<WeightedValue> IntakeAges,
    // Retirement chance moves RetirementRatingPercent percent per rating point below (more) or above (less) RetirementPivot.
    int RetirementPivot, int RetirementRatingPercent,
    int YouthIntake, int PoolSize, int PoolCap,
    // Form moves by FormStep plus up to FormSurprise more for an unexpected result, and back toward 0 every DecayDays.
    int FormStep, int FormSurprise, int DecayDays,
    // Morale: a win, a loss, a match on the bench; every DecayDays it settles MoraleSettle toward MoraleRest. Below MoraleLow
    // readiness recovers at half speed, and a player of at least RefusalAmbition will not renew.
    int MoraleWin, int MoraleLoss, int MoraleBench, int MoraleRest, int MoraleSettle, int MoraleLow, int RefusalAmbition,
    // Popularity: a win (times its importance), a match on the bench; stars (StarPopularity and above) add StarBonusPercent
    // to sponsors' fair payment, for at most StarBonusCap of them.
    int PopularityWin, int PopularityBench, int StarPopularity, int StarBonusPercent, int StarBonusCap,
    // Conduct, every ConductInterval days: ConductRate basis points per point of professionalism below ConductThreshold.
    int ConductInterval, int ConductThreshold, int ConductRate, int ConductReputation, int ConductPopularity, int ConductMorale,
    int PoolFloor = 0);
/// <summary>Rival organizations (rules v4 phase 5). Edge is what a rival adds to its lineup's rating in a match (its own
/// preparation and adaptation, not modelled in detail). Squads are 6–8 players by <see cref="RosterSizes"/>. Wages stay
/// under WageCapBase + WageCapPerReputation × reputation a day; funds for fees are FundsBase + FundsPerPlace for every
/// place above last, each season. At most MaxMoves recruitment moves a season besides filling the squad; BuyPercent of
/// upgrades are sought from another organization (the calibrated share of newcomers who come from another team); an
/// upgrade must be UpgradeMargin points better. A rival PoorPlace or lower for CoachPatience seasons replaces its head
/// coach; a rebuilding rival lets players of SellAge or older go.</summary>
public sealed record RivalRules(int Edge, ImmutableArray<WeightedValue> RosterSizes, long WageCapBase, long WageCapPerReputation,
    long FundsBase, long FundsPerPlace, int MaxMoves, int BuyPercent, int UpgradeMargin, int PoorPlace, int CoachPatience, int SellAge);
public sealed record BrandDefinition(string Id, string Name, string Sector, int MinimumReputation, int MinimumAudience,
    long BasePayment, long WinBonus, int Load, ImmutableArray<int> DurationDays);
public sealed record Content(string Format, int Schema, string Label, string RulesVersion, string DefaultCompanyName,
    Balance Balance, Generation Generation, ImmutableArray<RoleWeight> RoleWeights, ImmutableArray<Nationality> Nationalities,
    HandleParts Handles, Calibration Calibration, ImmutableArray<string> OrganizationNames, ImmutableArray<BrandDefinition> Brands, Lifecycle Lifecycle, RivalRules Rivals);

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
