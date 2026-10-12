using System.Collections.Immutable;

namespace ManagementGame.Domain;

/// <summary>Absolute days form consecutive seasons; nothing ends the campaign by date. Each season is a league (rules v4):
/// the company and its rivals play a double round robin, the top teams play semifinals and a final, and the rest of the
/// season is the off-season.</summary>
public static class Seasons
{
    public const string Regular = "regular", Semifinal = "semifinal", Final = "final";
    public static int Of(int day, Balance b) => (day - 1) / b.SeasonLength + 1;
    public static int Start(int season, Balance b) => (season - 1) * b.SeasonLength + 1;
    public static int End(int season, Balance b) => season * b.SeasonLength;
    public static int DayOf(int day, Balance b) => (day - 1) % b.SeasonLength + 1;
    public static string FixturePrefix(int season) => $"fixture:s{season}:";
    public static string LeaguePrefix(int season) => $"league:s{season}:";
    public static int Rounds(Balance b) => 2 * b.RivalCount;
    public static int RoundDay(int season, int round, Balance b) => Start(season, b) + b.FirstMatchDay - 1 + round * b.MatchInterval;
    public static int LastRegularDay(int season, Balance b) => RoundDay(season, Rounds(b) - 1, b);
    public static int SemifinalDay(int season, Balance b) => LastRegularDay(season, b) + b.PlayoffInterval;
    public static int FinalDay(int season, Balance b) => SemifinalDay(season, b) + b.PlayoffInterval;
    /// <summary>The stage of a fixture from its ID: playoff fixtures end in their stage name.</summary>
    public static string StageOf(string fixtureId) =>
        fixtureId.EndsWith(":" + Final, StringComparison.Ordinal) ? Final : fixtureId.Contains(":" + Semifinal, StringComparison.Ordinal) ? Semifinal : Regular;

    /// <summary>The regular season: a seeded circle-method double round robin of the company and the rivals. Every round
    /// the company plays one rival and the other rivals play each other; the return leg swaps sides. The second half
    /// carries higher stakes (importance 2).</summary>
    public static (ImmutableArray<Fixture> Company, ImmutableArray<LeagueFixture> League) Schedule(ulong seed, int season, string companyId,
        ImmutableArray<Rival> rivals, Balance b)
    {
        var teams = Generator.Shuffle(seed, $"league:{season}", [companyId, .. rivals.Select(r => r.Id).Order(StringComparer.Ordinal)]).ToList();
        var n = teams.Count;
        var firstLeg = new List<(string Home, string Away)[]>();
        for (var r = 0; r < n - 1; r++)
        {
            firstLeg.Add([.. Enumerable.Range(0, n / 2).Select(i => (i + r) % 2 == 0 ? (teams[i], teams[n - 1 - i]) : (teams[n - 1 - i], teams[i]))]);
            var last = teams[^1]; teams.RemoveAt(n - 1); teams.Insert(1, last);
        }
        var rounds = firstLeg.Concat(firstLeg.Select(round => round.Select(p => (p.Away, p.Home)).ToArray())).ToArray();
        var company = ImmutableArray.CreateBuilder<Fixture>(); var league = ImmutableArray.CreateBuilder<LeagueFixture>();
        for (var k = 0; k < rounds.Length; k++)
        {
            var day = RoundDay(season, k, b); var importance = k >= rounds.Length / 2 ? 2 : 1;
            var index = 0;
            foreach (var (home, away) in rounds[k])
            {
                if (home == companyId || away == companyId)
                    company.Add(new Fixture($"{FixturePrefix(season)}{k + 1:D2}", day, home == companyId ? away : home, importance, b.PrizeBase * importance, b.RegularGames, Regular));
                else league.Add(new LeagueFixture($"{LeaguePrefix(season)}{k + 1:D2}:{++index}", day, home, away, importance, b.RegularGames, Regular));
            }
        }
        return (company.ToImmutable(), league.ToImmutable());
    }

    /// <summary>A team's regular-season line.</summary>
    public sealed record Standing(string TeamId, int Played, int Won, int Lost, int GamesWon, int GamesLost)
    {
        public int Points => Won;
    }

    /// <summary>The regular-season table of one season, derived from results (never stored): points (one per series won),
    /// then game difference, then head-to-head (series won in the matches among the teams still level), then games won,
    /// then a seeded draw that stands in for a tiebreaker match.</summary>
    public static ImmutableArray<Standing> Table(World w, string companyId, int season, ulong seed)
    {
        var rows = new[] { companyId }.Concat(w.Rivals.Select(r => r.Id)).ToDictionary(id => id, id => new Standing(id, 0, 0, 0, 0, 0));
        var series = new List<(string Winner, string Loser)>();
        void Add(string winner, string loser, int winnerGames, int loserGames)
        {
            rows[winner] = rows[winner] with { Played = rows[winner].Played + 1, Won = rows[winner].Won + 1, GamesWon = rows[winner].GamesWon + winnerGames, GamesLost = rows[winner].GamesLost + loserGames };
            rows[loser] = rows[loser] with { Played = rows[loser].Played + 1, Lost = rows[loser].Lost + 1, GamesWon = rows[loser].GamesWon + loserGames, GamesLost = rows[loser].GamesLost + winnerGames };
            series.Add((winner, loser));
        }
        foreach (var r in w.Results.Where(r => r.FixtureId.StartsWith(FixturePrefix(season), StringComparison.Ordinal) && StageOf(r.FixtureId) == Regular))
        {
            if (!rows.ContainsKey(r.RivalId)) continue;
            if (r.Won) Add(companyId, r.RivalId, r.GamesWon, r.GamesLost); else Add(r.RivalId, companyId, r.GamesLost, r.GamesWon);
        }
        foreach (var r in w.LeagueResults.Where(r => r.FixtureId.StartsWith(LeaguePrefix(season), StringComparison.Ordinal) && StageOf(r.FixtureId) == Regular))
        {
            if (!rows.ContainsKey(r.HomeId) || !rows.ContainsKey(r.AwayId)) continue;
            if (r.HomeGames > r.AwayGames) Add(r.HomeId, r.AwayId, r.HomeGames, r.AwayGames); else Add(r.AwayId, r.HomeId, r.AwayGames, r.HomeGames);
        }
        // Teams level on points and game difference are separated by the series they won against each other.
        var level = rows.Values.GroupBy(s => (s.Points, s.GamesWon - s.GamesLost)).ToDictionary(g => g.Key, g => g.Select(s => s.TeamId).ToHashSet(StringComparer.Ordinal));
        int HeadToHead(Standing s)
        {
            var group = level[(s.Points, s.GamesWon - s.GamesLost)];
            return group.Count < 2 ? 0 : series.Count(x => x.Winner == s.TeamId && group.Contains(x.Loser));
        }
        return [.. rows.Values.OrderByDescending(s => s.Points).ThenByDescending(s => s.GamesWon - s.GamesLost).ThenByDescending(HeadToHead).ThenByDescending(s => s.GamesWon)
            .ThenBy(s => KeyedRandom.Draw(seed, "standings", $"{season}:{s.TeamId}", "tiebreak")).ThenBy(s => s.TeamId, StringComparer.Ordinal)];
    }

    /// <summary>The winner of a played playoff match, from the company's result or the league result; null when unplayed.</summary>
    private static string? Winner(World w, string companyId, string companyFixtureId, string leagueFixtureId)
    {
        if (w.Results.FirstOrDefault(r => r.FixtureId == companyFixtureId) is { } own) return own.Won ? companyId : own.RivalId;
        if (w.LeagueResults.FirstOrDefault(r => r.FixtureId == leagueFixtureId) is { } other) return other.HomeGames > other.AwayGames ? other.HomeId : other.AwayId;
        return null;
    }
    private static string? Loser(World w, string companyId, string companyFixtureId, string leagueFixtureId, string first, string second) =>
        Winner(w, companyId, companyFixtureId, leagueFixtureId) is { } winner ? winner == first ? second : first : null;

    /// <summary>The playoff fixtures that exist at this point of the season, derived from results and the calendar: the
    /// semifinals (1 v 4, 2 v 3 of the table) once the regular season's last day has passed, the final once the
    /// semifinal day has passed. The simulation creates exactly these and validation checks them.</summary>
    public static (ImmutableArray<Fixture> Company, ImmutableArray<LeagueFixture> League) Playoffs(World w, string companyId, int season, ulong seed, Balance b)
    {
        var day = w.Calendar.Day;
        if (day <= LastRegularDay(season, b) || Of(day, b) != season || b.PlayoffTeams < 2) return ([], []);
        var seeds = Table(w, companyId, season, seed).Take(b.PlayoffTeams).Select(s => s.TeamId).ToArray();
        var semis = new[] { (seeds[0], seeds[3]), (seeds[1], seeds[2]) };
        var company = ImmutableArray.CreateBuilder<Fixture>(); var league = ImmutableArray.CreateBuilder<LeagueFixture>();
        void Match(string companyFixtureId, string leagueFixtureId, int matchDay, string stage, string home, string away)
        {
            if (home == companyId || away == companyId)
                company.Add(new Fixture(companyFixtureId, matchDay, home == companyId ? away : home, 3, b.PrizeBase * 3, b.PlayoffGames, stage));
            else league.Add(new LeagueFixture(leagueFixtureId, matchDay, home, away, 3, b.PlayoffGames, stage));
        }
        // Semifinal i (1 v 4, then 2 v 3) has one ID per side of the books: the company's fixture or the league's.
        string Ids(int i, bool own) => own ? $"{FixturePrefix(season)}{Semifinal}:{i + 1}" : $"{LeaguePrefix(season)}{Semifinal}:{i + 1}";
        for (var i = 0; i < semis.Length; i++) Match(Ids(i, true), Ids(i, false), SemifinalDay(season, b), Semifinal, semis[i].Item1, semis[i].Item2);
        if (day > SemifinalDay(season, b))
        {
            var winners = Enumerable.Range(0, semis.Length).Select(i => Winner(w, companyId, Ids(i, true), Ids(i, false))).ToArray();
            if (winners.All(x => x is not null))
                Match($"{FixturePrefix(season)}{Final}", $"{LeaguePrefix(season)}{Final}", FinalDay(season, b), Final, winners[0]!, winners[1]!);
        }
        return (company.ToImmutable(), league.ToImmutable());
    }

    /// <summary>The final order once the final is played: champion, runner-up, the semifinal losers by table order, then
    /// everyone else by table order. Null before the final has a result.</summary>
    public static ImmutableArray<string>? FinalOrder(World w, string companyId, int season, ulong seed, Balance b)
    {
        var table = Table(w, companyId, season, seed).Select(s => s.TeamId).ToArray();
        var final = w.Fixtures.FirstOrDefault(f => f.Id == $"{FixturePrefix(season)}{Final}") is { } own ? (companyId, own.RivalId)
            : w.LeagueFixtures.FirstOrDefault(f => f.Id == $"{LeaguePrefix(season)}{Final}") is { } other ? (other.HomeId, other.AwayId) : ((string, string)?)null;
        if (final is not { } pair || Winner(w, companyId, $"{FixturePrefix(season)}{Final}", $"{LeaguePrefix(season)}{Final}") is not { } champion) return null;
        var runnerUp = champion == pair.Item1 ? pair.Item2 : pair.Item1;
        var top = new[] { champion, runnerUp };
        var semifinalists = table.Take(b.PlayoffTeams).Where(t => !top.Contains(t));
        return [.. top, .. semifinalists, .. table.Where(t => !top.Contains(t) && !semifinalists.Contains(t))];
    }
}
