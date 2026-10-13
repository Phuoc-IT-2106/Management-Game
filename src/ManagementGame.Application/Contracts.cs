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

/// <summary>The five roles in lineup order and their display names: the Domain's rule list, read for presentation.</summary>
public static class RoleNames
{
    public static ImmutableArray<string> All => ManagementGame.Domain.Roles.All;
    public static string Name(string role) => ManagementGame.Domain.Roles.Name(role);
}
/// <summary>The skills and match-relevant mind of a player the company employs (known exactly to its own staff).</summary>
public sealed record PlayerSkills(int Mechanics, int EarlyGame, int Teamfight, int Macro, int ChampionPool, int Consistency, int Composure, int Communication);
/// <summary>A player on the company's roster. Name is the in-game handle shown in play; FullName is the legal name. Rating
/// is the primary-role rating; SecondaryRating the rating in the secondary role (0 without one). Personality is never
/// observed; potential only as the head coach's estimate. Trend is the direction age suggests ("Improving", "At peak",
/// "Declining"). RenewalNote says why a contract in its renewal window cannot be renewed (empty otherwise).</summary>
public sealed record PersonRow(string Id, string Name, string Role, int Rating, int Readiness, long Salary, long ExitCost, bool CanRelease,
    int ContractEnd = 0, long RenewalSalary = 0, bool CanRenew = false, string FullName = "", string Nationality = "", int Age = 0,
    string SecondaryRole = "", int SecondaryRating = 0, PlayerSkills? Skills = null, int Form = 0, int Morale = 0, int Popularity = 0, long MarketValue = 0,
    int Familiarity = 0, string PotentialEstimate = "", string Trend = "", string RenewalNote = "");
/// <summary>A free agent as the company can estimate them: rating and potential as bands whose width follows the company's
/// information and the head coach's analysis. Age, nationality, role and fan following are public.</summary>
public sealed record CandidateRow(string Id, string Name, string Role, string RatingEstimate, long Fee, long Salary,
    string FullName = "", string Nationality = "", int Age = 0, string PotentialEstimate = "", int Popularity = 0, string SecondaryRole = "");
public sealed record SponsorRow(string Id, string Name, long Payment, long WinBonus, int Load, int Deadline, int MinimumReputation, string Availability,
    string Origin = "", int DurationDays = 0, string BrandId = "");
public sealed record BillRow(string Id, string Cause, int DueDay, long Remaining, bool Incoming, int? MissedDay);
public sealed record ResultRow(string Id, int Day, string Opponent, string Result, string Posture);
public sealed record PlanView(string FixtureId, int Execution, int Opponent, int Meta, Risk Posture, ImmutableArray<string> Lineup);
public sealed record StaffRow(string Id, string Name, long Salary, int ContractEnd, long RenewalSalary, bool CanRenew);
public sealed record CoachCandidateRow(string Id, string Name, string SkillEstimate, long Fee, long Salary, int Deadline);
/// <summary>A brand the company may approach; the guide is a market estimate, not the brand's private valuation.</summary>
public sealed record BrandRow(string Id, string Name, string Sector, int MinimumReputation, int MinimumAudience, string Availability,
    long GuideLow, long GuideHigh, ImmutableArray<int> DurationDays);
public sealed record NegotiationRow(string Id, string Brand, long Payment, int DurationDays, int ResponseDay, string BrandId = "");
/// <summary>A message the world sent the company; Kind is a stable category, CauseId the affected record and
/// SubjectId the stable ID of the organization, brand or person it concerns (empty when none is known).</summary>
public sealed record InboxRow(string Id, int Day, string Kind, string Text, string CauseId, string SubjectId = "");
/// <summary>A company match: stage (regular, semifinal, final), best-of format and, once played, the result and score ("2–1").</summary>
public sealed record FixtureRow(string Id, int Day, string Opponent, int Importance, string Result, string RivalId = "", string Stage = "regular", int Games = 1, string Score = "");
/// <summary>One line of the league table, in table order: place, team, series won and lost, games won and lost, points.</summary>
public sealed record StandingRow(int Place, string TeamId, string Name, int Played, int Won, int Lost, int GamesWon, int GamesLost, int Points, bool Company);
/// <summary>A league match of any two teams (the company's included): stage, format and, once played, the score and winner.</summary>
public sealed record LeagueMatchRow(string Id, int Day, string Stage, int Games, string HomeId, string Home, string AwayId, string Away, string Score, string WinnerId, bool Company);
/// <summary>A league story the public can read from the results, derived and never stored: a new leader at the top of the
/// table ("Leader"), or an upset between two rivals ("Upset": the winner sat at least <c>Observations.UpsetGap</c> places
/// below the loser before the match). SubjectId is the organization the story is about.</summary>
public sealed record LeagueNewsRow(string Id, int Day, string Kind, string Headline, string Detail, string SubjectId);
/// <summary>A rival's player as the company can scout them: handle, role, age, a rating band and fan following.</summary>
public sealed record RivalPlayerRow(string Id, string Name, string Role, int Age, string RatingEstimate, int Popularity);
/// <summary>A rival's public profile (rules v4 phase 5): its squad as scouted, its head coach, the posture it is believed to
/// play and a band for its strength. Estimates narrow with the company's information and its head coach's analysis.</summary>
public sealed record RivalProfile(string Id, string Name, string Coach, string Posture, string LevelEstimate, ImmutableArray<RivalPlayerRow> Players);
/// <summary>The league's format this season: teams, series per team in the regular season, best-of formats, playoff places,
/// the key days (absolute) and the place prizes (champion first, minor units).</summary>
public sealed record LeagueInfo(int Teams, int MatchesPerTeam, int RegularGames, int PlayoffTeams, int PlayoffGames, int FirstDay, int LastRegularDay,
    int SemifinalDay, int FinalDay, ImmutableArray<long> PlacePrizes);
/// <summary>Current-season head-to-head record against one rival organization.</summary>
public sealed record RivalRow(string Id, string Name, int Wins, int Losses, int Remaining);
/// <summary>How a completed personnel move came about (the domain's <c>MoveReason</c>, in the same order): an
/// organization signed a player or appointed a head coach, an interim head coach took over, a player was released, a
/// contract ended without renewal, a head coach was replaced by a new appointment, a player retired, or a player moved
/// directly between organizations for a fee.</summary>
public enum MoveKind { Signing, Appointment, Interim, Release, ContractEnd, Replacement, Retirement, Transfer }
/// <summary>A completed personnel move exactly as the simulation recorded it when it happened (transfer history). From
/// and To are the stable IDs of the organizations the person left and joined; empty is the open market, where the person
/// is a free agent. Name and Role are the person as they were then; Role is empty for a head coach. CauseId is the command
/// or contract behind it, shared by moves that happened together (a replaced coach and the appointment that replaced them).</summary>
public sealed record MoveRow(string Id, MoveKind Kind, int Day, string PersonId, string Name, bool Coach, string Role, string From, string To, string CauseId);
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
    string CompanyId = "", string OpponentId = "", long OperatingCost = 0, int MinimumRoster = 0, ImmutableArray<MoveRow> Moves = default,
    ImmutableArray<StandingRow> Standings = default, ImmutableArray<LeagueMatchRow> LeagueMatches = default, int PlayoffTeams = 0, int LastSeasonPlace = 0,
    ImmutableArray<LeagueNewsRow> LeagueNews = default, LeagueInfo? League = null, ImmutableArray<RivalProfile> RivalProfiles = default);

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
