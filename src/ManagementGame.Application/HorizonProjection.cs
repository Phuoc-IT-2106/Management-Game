using System.Collections.Immutable;
using ManagementGame.Formatting;

namespace ManagementGame.Application;

public enum HorizonKind { Match, OfferDeadline, ProposalAnswer, ContractEnd, Receipt, SeasonEnd }
/// <summary>One operating-horizon event. Kind is the typed category; Subject names the opponent, brand or person
/// (several names when Count &gt; 1 events of one kind share a day); Amount is set for receipts (minor units). Text is the
/// full sentence for tooltips; Note carries secondary detail (e.g. "double stakes"). A grouped event routes to its
/// section overview (empty TargetId). Presentation never parses Text.</summary>
public sealed record HorizonEvent(int Day, HorizonKind Kind, string Text, bool Material, string Section, string TargetId, string Note = "",
    string Subject = "", long Amount = 0, int Count = 1);
/// <summary>One week of the operating horizon. ScheduleDrawn is false for weeks of a season whose fixtures do not exist yet.</summary>
public sealed record HorizonWeek(int Season, int Week, int FirstDayOfSeason, int LastDayOfSeason, int FirstDay, int LastDay, bool Current,
    bool ScheduleDrawn, ImmutableArray<HorizonEvent> Events);

/// <summary>The operating horizon: the next weeks of the season with every dated commitment the company can observe
/// (fixtures, offer deadlines, proposal answers, contract ends, scheduled receipts, season end). No longer part of the
/// Portal; kept as a read model for calendar-type presentation. It ranks and groups; it decides nothing.</summary>
public static class HorizonProjection
{
    public const int Weeks = 4;
    private const int WeekLength = PortalProjection.WeekLength;

    /// <summary>Four season weeks starting with the current one. Only events with an authoritative record appear.</summary>
    public static ImmutableArray<HorizonWeek> Build(Situation v)
    {
        var seasonStart = v.Day - v.DayOfSeason + 1;
        var weeksPerSeason = (v.SeasonLength + WeekLength - 1) / WeekLength;
        var current = (v.DayOfSeason - 1) / WeekLength;
        var events = Events(v);
        var weeks = ImmutableArray.CreateBuilder<HorizonWeek>(Weeks);
        for (var k = 0; k < Weeks; k++)
        {
            var w = current + k;
            var season = v.Season + w / weeksPerSeason;
            var week = w % weeksPerSeason;
            var first = week * WeekLength + 1;
            var last = Math.Min(first + WeekLength - 1, v.SeasonLength);
            var start = seasonStart + (season - v.Season) * v.SeasonLength;
            var firstDay = start + first - 1; var lastDay = start + last - 1;
            weeks.Add(new HorizonWeek(season, week + 1, first, last, firstDay, lastDay, k == 0, season == v.Season,
                [.. events.Where(e => e.Day >= Math.Max(firstDay, v.Day) && e.Day <= lastDay).OrderBy(e => e.Day).ThenBy(e => e.Kind)]));
        }
        return weeks.MoveToImmutable();
    }

    private static List<HorizonEvent> Events(Situation v)
    {
        var list = new List<HorizonEvent>();
        foreach (var f in v.Fixtures.Where(f => f.Result.Length == 0))
            list.Add(new(f.Day, HorizonKind.Match, "vs " + f.Opponent, true, ShellSections.Competition, f.Id, f.Importance > 1 ? "double stakes" : "", f.Opponent));
        foreach (var o in v.Offers.Where(o => o.Availability == "Available"))
            list.Add(new(o.Deadline, HorizonKind.OfferDeadline, $"{o.Name} offer ends", true, ShellSections.Commercial, o.Id, Subject: o.Name));
        foreach (var n in v.Negotiations)
            list.Add(new(n.ResponseDay, HorizonKind.ProposalAnswer, $"{n.Brand} answers", false, ShellSections.Commercial, n.Id, Subject: n.Brand));
        foreach (var p in v.People)
            list.Add(new(p.ContractEnd, HorizonKind.ContractEnd, $"{p.Name} contract ends", true, ShellSections.Squad, p.Id, Subject: p.Name));
        list.Add(new(v.CoachContract.ContractEnd, HorizonKind.ContractEnd, $"{v.CoachContract.Name} contract ends", true, ShellSections.Staff, v.CoachContract.Id,
            Subject: v.CoachContract.Name));
        foreach (var day in v.Bills.Where(x => x.Incoming && x.DueDay >= v.Day).GroupBy(x => x.DueDay))
        {
            var amount = day.Sum(x => x.Remaining);
            list.Add(new(day.Key, HorizonKind.Receipt, "Receipt " + Currency.Format(amount),
                false, ShellSections.Finance, "day:" + day.Key, Amount: amount));
        }
        var seasonEnd = v.Day - v.DayOfSeason + v.SeasonLength;
        list.Add(new(seasonEnd, HorizonKind.SeasonEnd, $"Season {v.Season} ends", true, ShellSections.Competition, "season:" + v.Season, Subject: $"Season {v.Season}"));
        // Several deadlines of one kind and section on one day read as one event; it routes to the section overview.
        return [.. list.GroupBy(e => (e.Day, e.Kind, e.Section)).Select(g => g.Count() == 1 ? g.First() : g.First() with
        {
            Text = string.Join("; ", g.Select(e => e.Text)), Subject = string.Join(", ", g.Select(e => e.Subject)), TargetId = "",
            Material = g.Any(e => e.Material), Count = g.Count()
        })];
    }
}
