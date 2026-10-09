using System.Collections.Immutable;

namespace ManagementGame.Domain;

/// <summary>Absolute days form consecutive seasons; nothing ends the campaign by date.</summary>
public static class Seasons
{
    public static int Of(int day, Balance b) => (day - 1) / b.SeasonLength + 1;
    public static int Start(int season, Balance b) => (season - 1) * b.SeasonLength + 1;
    public static int End(int season, Balance b) => season * b.SeasonLength;
    public static int DayOf(int day, Balance b) => (day - 1) % b.SeasonLength + 1;
    public static string FixturePrefix(int season) => $"fixture:s{season}:";

    /// <summary>Each rival is met twice per season in a seeded order; the second half carries higher stakes.</summary>
    public static ImmutableArray<Fixture> Schedule(ulong seed, int season, ImmutableArray<Rival> rivals, Balance b)
    {
        var ids = rivals.Select(r => r.Id).Order(StringComparer.Ordinal).ToArray();
        var first = Generator.Shuffle(seed, $"schedule:{season}:first", ids);
        var second = Generator.Shuffle(seed, $"schedule:{season}:second", ids);
        var order = first.Concat(second).ToArray();
        var start = Start(season, b) + b.FirstMatchDay - 1;
        return order.Select((rival, k) =>
        {
            var importance = k >= order.Length / 2 ? 2 : 1;
            return new Fixture($"{FixturePrefix(season)}{k + 1:D2}", start + k * b.MatchInterval, rival, importance, b.PrizeBase * importance);
        }).ToImmutableArray();
    }
}
