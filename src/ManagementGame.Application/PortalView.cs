using System.Collections.Immutable;
using System.Globalization;

namespace ManagementGame.Application;

/// <summary>Presentation asset identifiers derived from stable entity IDs. The client resolves them through its asset
/// catalog and falls back deterministically; display names are never asset keys.</summary>
public static class AssetIds
{
    public static string Crest(string organizationId) => "crest:" + organizationId;
    public static string Portrait(string personId) => "portrait:" + personId;
    public static string BrandLogo(string brandId) => "logo:" + brandId;
    public static string NewsFallback(string category) => "fallback:news:" + category.ToLowerInvariant();
    /// <summary>The identity mark of a message's subject, chosen by the kind encoded in its stable ID.</summary>
    public static string ForSubject(string subjectId, string category) =>
        subjectId.StartsWith("rival:", StringComparison.Ordinal) || subjectId.StartsWith("company:", StringComparison.Ordinal) ? Crest(subjectId)
        : subjectId.StartsWith("brand:", StringComparison.Ordinal) ? BrandLogo(subjectId)
        : subjectId.StartsWith("person:", StringComparison.Ordinal) || subjectId.StartsWith("coach:", StringComparison.Ordinal) ? Portrait(subjectId)
        : NewsFallback(category);
}

public enum SeasonPhase { FirstHalf, SecondHalf, Complete }
public enum PlanState { Committed, CoachWillPlan, Missing }
public enum HorizonKind { Match, OfferDeadline, ProposalAnswer, ContractEnd, Receipt, SeasonEnd }

public sealed record PortalCompany(string Id, string Name, string CrestAssetId, int Reputation, int Audience);
public sealed record PortalTime(int Season, int Day, int DayOfSeason, int SeasonLength, SeasonPhase Phase);
/// <summary>Cash and arrears are known; the forecast and the payments ahead include projected wages and are estimates.</summary>
public sealed record PortalFinance(long Cash, string Condition, long Forecast, int ForecastDays, long ReceiptsAhead, long PaymentsAhead, int AheadDays, long Arrears);
public sealed record PortalDecision(string Id, DecisionCategory Category, string Subject, string Why, TaskUrgency Urgency, int? DueDay, string Section, string TargetId);
public sealed record PortalNews(string Id, string Category, string Headline, string Detail, int Day, string SubjectId, string ImageAssetId, string Section);
public sealed record PortalTeam(string Id, string Name, string CrestAssetId);
/// <summary>Only facts the simulation owns: no venue or series format exists, so none is offered.</summary>
public sealed record PortalMatch(string FixtureId, PortalTeam Us, PortalTeam Opponent, int Day, int DayOfSeason, int MatchNumber, int SeasonMatches,
    int Importance, string StrengthEstimate, string Tendency, string Confidence, PlanState Plan, int HeadToHeadWon, int HeadToHeadLost);
/// <summary>One operating-horizon event. Kind is the typed category; Subject names the opponent, brand or person
/// (several names when Count &gt; 1 events of one kind share a day); Amount is set for receipts (minor units). Text is the
/// full sentence for tooltips; Note carries secondary detail (e.g. "double stakes"). A grouped event routes to its
/// section overview (empty TargetId). Presentation never parses Text.</summary>
public sealed record HorizonEvent(int Day, HorizonKind Kind, string Text, bool Material, string Section, string TargetId, string Note = "",
    string Subject = "", long Amount = 0, int Count = 1);
/// <summary>One week of the operating horizon. ScheduleDrawn is false for weeks of a season whose fixtures do not exist yet.</summary>
public sealed record HorizonWeek(int Season, int Week, int FirstDayOfSeason, int LastDayOfSeason, int FirstDay, int LastDay, bool Current,
    bool ScheduleDrawn, ImmutableArray<HorizonEvent> Events);
public sealed record SeasonResult(string FixtureId, int Day, string Opponent, string Outcome);
public sealed record PortalSeason(int Season, int Won, int Lost, int ToPlay, ImmutableArray<SeasonResult> Recent, ImmutableArray<RivalRow> HeadToHead);
public sealed record PortalView(PortalCompany Company, PortalTime Time, PortalFinance Finance, ImmutableArray<PortalDecision> Decisions,
    ImmutableArray<PortalNews> News, PortalMatch? NextMatch, ImmutableArray<HorizonWeek> Horizon, PortalSeason Season);

/// <summary>Bounded Portal read model built only from the player's observation. It ranks and groups; it decides nothing.</summary>
public static class PortalProjection
{
    public const int HorizonWeeks = 4, WeekLength = 7, MoneyAheadDays = 14, NewsLimit = 6, RecentResults = 5;

    public static PortalView Build(Situation v)
    {
        var played = v.Fixtures.Where(f => f.Result.Length > 0).ToArray();
        var next = v.Fixtures.FirstOrDefault(f => f.Id == v.NextFixtureId);
        var phase = next is null ? SeasonPhase.Complete : next.Importance > 1 ? SeasonPhase.SecondHalf : SeasonPhase.FirstHalf;
        var company = new PortalCompany(v.CompanyId, v.Company, AssetIds.Crest(v.CompanyId), v.Reputation, v.Audience);
        return new PortalView(company, new PortalTime(v.Season, v.Day, v.DayOfSeason, v.SeasonLength, phase), Money(v),
            [.. PortalTasks.Build(v).Select(t => new PortalDecision(t.Id, t.Category, t.Subject.Length > 0 ? t.Subject : t.Text, t.Why, t.Urgency, t.DueDay, t.Section, t.TargetId))],
            [.. v.Inbox.Take(NewsLimit).Select(row => News(v, row))], Match(v, company), Horizon(v),
            new PortalSeason(v.Season, played.Count(f => f.Result == "Victory"), played.Count(f => f.Result != "Victory"), v.Fixtures.Length - played.Length,
                [.. played.OrderByDescending(f => f.Day).Take(RecentResults).Select(f => new SeasonResult(f.Id, f.Day, f.Opponent, f.Result))], v.Rivals));
    }

    private static PortalFinance Money(Situation v)
    {
        var last = v.Day + MoneyAheadDays - 1;
        var wages = v.People.Select(p => (p.Salary, p.ContractEnd)).Append((v.CoachContract.Salary, v.CoachContract.ContractEnd)).ToArray();
        var receipts = v.Bills.Where(x => x.Incoming && x.DueDay <= last).Sum(x => x.Remaining);
        var bills = v.Bills.Where(x => !x.Incoming && x.DueDay <= last).Sum(x => x.Remaining);
        long projected = 0;
        for (var d = v.Day + 1; d <= last; d++) projected += wages.Where(w => w.ContractEnd >= d).Sum(w => w.Salary);
        var arrears = v.Bills.Where(x => !x.Incoming && x.MissedDay is not null).Sum(x => x.Remaining);
        return new PortalFinance(v.Cash, v.Status, v.Forecast, 7, receipts, bills + projected, MoneyAheadDays, arrears);
    }

    /// <summary>A sponsorship offer still on the table reads as a headline built from its typed record; every other
    /// message leads with its first sentence and keeps the rest as detail. Presentation only, never used for routing.</summary>
    private static PortalNews News(Situation v, InboxRow row)
    {
        string headline, detail;
        if (row.Kind == "Offer" && v.Offers.FirstOrDefault(o => o.Id == row.CauseId) is { } offer)
        {
            headline = $"{offer.Name} makes a sponsorship offer";
            detail = string.Create(CultureInfo.InvariantCulture,
                $"{offer.Payment / 100m:N0} CU a week for {offer.DurationDays / WeekLength} weeks, +{offer.Load} delivery load.");
        }
        else
        {
            var cut = row.Text.IndexOf(". ", StringComparison.Ordinal);
            headline = cut < 0 ? row.Text.TrimEnd('.') : row.Text[..cut];
            detail = cut < 0 ? "" : row.Text[(cut + 2)..];
        }
        return new PortalNews(row.Id, row.Kind, headline, detail, row.Day, row.SubjectId, AssetIds.ForSubject(row.SubjectId, row.Kind), ShellSections.ForInbox(row.Kind));
    }

    private static PortalMatch? Match(Situation v, PortalCompany company)
    {
        if (v.NextFixtureId.Length == 0 || v.OpponentId.Length == 0) return null;
        var index = v.Fixtures.Select((f, i) => (f, i)).FirstOrDefault(x => x.f.Id == v.NextFixtureId);
        if (index.f is null) return null;
        var rival = v.Rivals.FirstOrDefault(r => r.Id == v.OpponentId);
        var plan = v.CommittedPlan?.FixtureId == v.NextFixtureId ? PlanState.Committed
            : v.Delegation == Delegation.Autonomous ? PlanState.CoachWillPlan : PlanState.Missing;
        return new PortalMatch(v.NextFixtureId, new PortalTeam(company.Id, company.Name, company.CrestAssetId),
            new PortalTeam(v.OpponentId, v.Opponent, AssetIds.Crest(v.OpponentId)), v.NextMatchDay, v.NextMatchDay - (v.Day - v.DayOfSeason),
            index.i + 1, v.Fixtures.Length, index.f.Importance, v.OpponentEstimate.Replace(" (estimate)", "", StringComparison.Ordinal),
            v.OpponentTendency, v.Confidence, plan, rival?.Wins ?? 0, rival?.Losses ?? 0);
    }

    /// <summary>Four season weeks starting with the current one. Only events with an authoritative record appear.</summary>
    private static ImmutableArray<HorizonWeek> Horizon(Situation v)
    {
        var seasonStart = v.Day - v.DayOfSeason + 1;
        var weeksPerSeason = (v.SeasonLength + WeekLength - 1) / WeekLength;
        var current = (v.DayOfSeason - 1) / WeekLength;
        var events = Events(v);
        var weeks = ImmutableArray.CreateBuilder<HorizonWeek>(HorizonWeeks);
        for (var k = 0; k < HorizonWeeks; k++)
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
            list.Add(new(day.Key, HorizonKind.Receipt, "Receipt " + (amount / 100m).ToString("N0", CultureInfo.InvariantCulture) + " CU",
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
