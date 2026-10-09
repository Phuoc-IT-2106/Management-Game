using System.Collections.Immutable;
using System.Globalization;
using ManagementGame.Formatting;

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
/// <summary>How sure the public can be of a transfer story; the label every story carries. Confirmed: a completed move in
/// the records. The rules have no public reporting of interest or negotiations, so nothing can honestly be called a
/// rumour or a report, and those labels do not exist yet (docs/10-vertical-slice/genre-shell/TRANSFER_NEWS.md).</summary>
public enum StoryStatus { Confirmed }
/// <summary>Where a story comes from. Simulation: an event of this campaign, about generated people and organizations.
/// Imported real-world facts need an approved, licensed dataset (player-data PD-D03) and are not supported.</summary>
public enum StorySource { Simulation }
/// <summary>How much a move matters to the competition, in order: a head-coach move (one per organization, it directs all
/// match preparation) outranks a player move. No player or organization prominence is modelled, so nothing finer exists,
/// and significance orders the feed without enlarging any story.</summary>
public enum StorySignificance { Player, HeadCoach }
/// <summary>Where a person stood on one side of a move: with an organization, on the open market (a free agent), or with an
/// organization the records name but the company cannot (unknown).</summary>
public enum Standing { Organization, FreeAgent, Unknown }
/// <summary>One side of a transfer journey, as it was when the move happened. An organization has its stable ID, display
/// name and crest. Free agent is a status, not an organization: it has no ID, name or crest. Unknown keeps the ID the
/// records hold and is shown as unknown, never guessed or replaced by another organization.</summary>
public sealed record JourneySide(Standing Standing, string OrganizationId, string Name, string CrestAssetId)
{
    public static readonly JourneySide FreeAgent = new(Standing.FreeAgent, "", "", "");
}

public sealed record PortalCompany(string Id, string Name, string CrestAssetId, int Reputation, int Audience);
public sealed record PortalTime(int Season, int Day, int DayOfSeason, int SeasonLength, SeasonPhase Phase);
/// <summary>Cash and arrears are known; the forecast and the payments ahead include projected wages and are estimates.</summary>
public sealed record PortalFinance(long Cash, string Condition, long Forecast, int ForecastDays, long ReceiptsAhead, long PaymentsAhead, int AheadDays, long Arrears);
/// <summary>Kind is what the decision is (art and wording follow it); SubjectAssetId is the identity mark of the brand or
/// person concerned, by stable ID (empty when the decision has no single subject).</summary>
public sealed record PortalDecision(string Id, DecisionCategory Category, string Subject, string Why, TaskUrgency Urgency, int? DueDay, string Section, string TargetId,
    DecisionKind Kind = DecisionKind.MatchPreparation, string SubjectAssetId = "");
public sealed record PortalNews(string Id, string Category, string Headline, string Detail, int Day, string SubjectId, string ImageAssetId, string Section);
/// <summary>One Transfer News story: the editorial projection of one recorded move (<see cref="MoveRow"/>), or a roundup
/// of several players making the same move on one day. It says who moved (the person, or the count of a roundup), from
/// where to where (<see cref="From"/> → <see cref="To"/>, as they were when the move happened), what kind of move it was
/// and when. Records are the moves it reports; Id derives from them, so a story exists once and survives save and load
/// unchanged. Headline and Summary are presentation text from typed fields, written as the public could know it: no fee,
/// wage, ability or contract term (private to the parties) and no market availability. Person fields are empty for a
/// roundup.</summary>
public sealed record TransferStory(string Id, MoveKind Kind, StoryStatus Status, StorySource Source, StorySignificance Significance, int Day,
    string Headline, string Summary, ImmutableArray<string> Records, string PersonId, string PersonName, string PortraitAssetId, bool Coach,
    JourneySide From, JourneySide To, string Section, string TargetId)
{
    /// <summary>Records compare by content, so a story read again (or after a reload) equals the one told before.</summary>
    public bool Equals(TransferStory? other) => other is not null && Id == other.Id && Kind == other.Kind && Status == other.Status && Source == other.Source
        && Significance == other.Significance && Day == other.Day && Headline == other.Headline && Summary == other.Summary && Records.SequenceEqual(other.Records)
        && PersonId == other.PersonId && PersonName == other.PersonName && PortraitAssetId == other.PortraitAssetId && Coach == other.Coach
        && From == other.From && To == other.To && Section == other.Section && TargetId == other.TargetId;
    public override int GetHashCode() => HashCode.Combine(Id, Day, Headline, Records.Length);
}
public sealed record PortalTeam(string Id, string Name, string CrestAssetId);
/// <summary>Only facts the simulation owns: no venue or series format exists, so none is offered.</summary>
public sealed record PortalMatch(string FixtureId, PortalTeam Us, PortalTeam Opponent, int Day, int DayOfSeason, int MatchNumber, int SeasonMatches,
    int Importance, string StrengthEstimate, string Tendency, string Confidence, PlanState Plan, int HeadToHeadWon, int HeadToHeadLost);
public sealed record SeasonResult(string FixtureId, int Day, string Opponent, string Outcome);
public sealed record PortalSeason(int Season, int Won, int Lost, int ToPlay, ImmutableArray<SeasonResult> Recent, ImmutableArray<RivalRow> HeadToHead);
/// <summary>News is world news (results, market, league and commercial notices); Transfers is transfer news (completed
/// personnel moves). The two never share a story; the player market itself lives in Squad and Staff.</summary>
public sealed record PortalView(PortalCompany Company, PortalTime Time, PortalFinance Finance, ImmutableArray<PortalDecision> Decisions,
    ImmutableArray<PortalNews> News, ImmutableArray<TransferStory> Transfers, PortalMatch? NextMatch, PortalSeason Season);

/// <summary>Bounded Portal read model built only from the player's observation. It ranks and groups; it decides nothing.</summary>
public static class PortalProjection
{
    public const int WeekLength = 7, MoneyAheadDays = 14, NewsLimit = 8, RecentResults = 5;
    /// <summary>Transfer news tells a few stories, not every move: at most this many (all of them on a roomy Portal).</summary>
    public const int TransferStoryLimit = 4;
    /// <summary>Inbox kinds that concern the company's own people. They are not world news: a contract-end departure is a
    /// transfer story, and a renewal window is a key decision.</summary>
    public static bool Personnel(string kind) => kind is "Contract" or "Renewal";
    /// <summary>A transfer story stays news for half a season; older moves stay in the records only.</summary>
    public static int StoryDays(int seasonLength) => seasonLength / 2;

    public static PortalView Build(Situation v)
    {
        var played = v.Fixtures.Where(f => f.Result.Length > 0).ToArray();
        var next = v.Fixtures.FirstOrDefault(f => f.Id == v.NextFixtureId);
        var phase = next is null ? SeasonPhase.Complete : next.Importance > 1 ? SeasonPhase.SecondHalf : SeasonPhase.FirstHalf;
        var company = new PortalCompany(v.CompanyId, v.Company, AssetIds.Crest(v.CompanyId), v.Reputation, v.Audience);
        return new PortalView(company, new PortalTime(v.Season, v.Day, v.DayOfSeason, v.SeasonLength, phase), Money(v),
            [.. PortalTasks.Build(v).Select(t => new PortalDecision(t.Id, t.Category, t.Subject.Length > 0 ? t.Subject : t.Text, t.Why, t.Urgency, t.DueDay, t.Section, t.TargetId,
                t.Kind, t.SubjectId.Length > 0 ? AssetIds.ForSubject(t.SubjectId, "") : ""))],
            [.. v.Inbox.Where(row => !Personnel(row.Kind)).Take(NewsLimit).Select(row => News(v, row))], [.. TransferStories(v).Take(TransferStoryLimit)], Match(v, company),
            new PortalSeason(v.Season, played.Count(f => f.Result == "Victory"), played.Count(f => f.Result != "Victory"), v.Fixtures.Length - played.Length,
                [.. played.OrderByDescending(f => f.Day).Take(RecentResults).Select(f => new SeasonResult(f.Id, f.Day, f.Opponent, f.Result))], v.Rivals));
    }

    /// <summary>Transfer news: recorded move → eligibility → editorial selection → story.
    /// <list type="bullet">
    /// <item>Eligible: a move in transfer history (<see cref="Situation.Moves"/>) within <see cref="StoryDays"/>, of a named
    /// person, with at least one organization the company can name. Each side is resolved as it was recorded: an
    /// organization, the open market (free agent) or unknown. A free agent merely being on the market is not news.</item>
    /// <item>Editing: players making the same move on one day are one roundup story, not a run of near-identical rows. A
    /// head-coach move, and a player returning to an organization they left, is always its own story.</item>
    /// <item>Selection: a head-coach story before a player story, then newest first, then the newer record first. The Portal
    /// tells at most <see cref="TransferStoryLimit"/> of them.</item>
    /// </list>
    /// Every move is in exactly one story, and a story is fixed by its own moves and the history before them: a later move
    /// adds a story and never rewrites an earlier one (a player who leaves and returns has a departure story and a return
    /// story). The same history always tells the same stories, so a reload or re-opening the Portal repeats nothing.</summary>
    public static ImmutableArray<TransferStory> TransferStories(Situation v)
    {
        if (v.Moves.IsDefaultOrEmpty) return [];
        var company = new PortalCompany(v.CompanyId, v.Company, AssetIds.Crest(v.CompanyId), v.Reputation, v.Audience);
        var since = v.Day - StoryDays(v.SeasonLength);
        JourneySide Side(string id) => id.Length == 0 ? JourneySide.FreeAgent
            : id == company.Id ? new(Standing.Organization, id, company.Name, company.CrestAssetId)
            : v.Rivals.FirstOrDefault(r => r.Id == id) is { } rival ? new(Standing.Organization, id, rival.Name, AssetIds.Crest(id))
            : new(Standing.Unknown, id, "", "");
        // Moves are newest first in recorded order, so a higher index is an earlier move.
        var moves = v.Moves.Select((m, order) => new Told(m, order, Side(m.From), Side(m.To))).ToArray();
        // The person's previous move, when it took them away from the organization they now join from the open market.
        MoveRow? Left(Told t) => t.From.Standing == Standing.FreeAgent && t.To.Standing == Standing.Organization
            && moves.Skip(t.Order + 1).FirstOrDefault(p => p.Move.PersonId == t.Move.PersonId) is { } prior
            && prior.Move.From == t.Move.To && prior.To.Standing == Standing.FreeAgent ? prior.Move : null;
        var eligible = moves.Where(t => t.Move.Day > since && t.Move.Day <= v.Day && t.Move.Name.Length > 0
            && (t.From.Standing == Standing.Organization || t.To.Standing == Standing.Organization)).ToArray();
        return [.. eligible
            .GroupBy(t => t.Move.Coach || Left(t) is not null ? t.Move.Id : $"{t.Move.Kind}:{t.Move.From}:{t.Move.To}:{t.Move.Day}")
            .Select(g => g.OrderBy(t => t.Order).ToArray())
            .Select(g => (Story: g.Length > 1 ? Roundup(g) : Story(g[0], Left(g[0]), moves), g[0].Order))
            .OrderByDescending(x => x.Story.Significance).ThenByDescending(x => x.Story.Day).ThenBy(x => x.Order)
            .Select(x => x.Story)];
    }
    private sealed record Told(MoveRow Move, int Order, JourneySide From, JourneySide To);

    /// <summary>Several players who made the same move on the same day: one story with the shared journey. The summary
    /// names two of them whole and counts the rest; each is still in the Inbox or on the market.</summary>
    private static TransferStory Roundup(Told[] moves)
    {
        var (first, from, to) = (moves[0].Move, moves[0].From, moves[0].To);
        var names = moves.Length <= 3 ? string.Join(", ", moves[..^1].Select(t => t.Move.Name)) + " and " + moves[^1].Move.Name
            : $"{moves[0].Move.Name}, {moves[1].Move.Name} and {Figures.Count(moves.Length - 2)} others";
        var players = $"{Figures.Count(moves.Length)} players";
        var (headline, summary) = (from.Standing, to.Standing, first.Kind) switch
        {
            (Standing.Organization, Standing.Organization, _) => ($"{players} join {to.Name}", $"{names} moved directly from {from.Name}."),
            (_, Standing.Organization, _) => ($"{players} sign with {to.Name}", $"{names} signed from the free-agent market."),
            (_, _, MoveKind.Release) => ($"{players} released by {from.Name}", $"{names}; released before their contracts ended."),
            _ => ($"{players} leave {from.Name}", $"{names}; contracts ended without renewals.")
        };
        return new TransferStory($"roundup:{first.Kind.ToString().ToLowerInvariant()}:{first.From}:{first.To}:{first.Day}", first.Kind, StoryStatus.Confirmed,
            StorySource.Simulation, StorySignificance.Player, first.Day, headline, summary, [.. moves.Select(t => t.Move.Id)], "", "", "", false, from, to, ShellSections.Squad, "");
    }

    /// <summary>The story of one move. The headline leads with the person's whole name, so a bounded headline only ever
    /// shortens an organization's name; the summary says what kind of move it was, never its terms. A return
    /// (<paramref name="left"/>: the person's previous move took them away from this organization) keeps its free-agent
    /// origin and names the earlier departure as context only. <paramref name="history"/> supplies the moves that happened
    /// together with this one (the appointment that replaced a coach, the interim who covered).</summary>
    private static TransferStory Story(Told t, MoveRow? left, Told[] history)
    {
        var (m, from, to) = (t.Move, t.From, t.To);
        var who = m.Coach ? "Head coach" : $"Role {m.Role} player";
        string? Partner(MoveKind kind) => history.FirstOrDefault(p => p.Move.CauseId == m.CauseId && p.Move.Kind == kind && p.Move.Id != m.Id)?.Move.Name;
        var gap = left is null ? "" : (m.Day - left.Day) switch { <= 0 => "the same day", 1 => "a day", var n => $"{Figures.Count(n)} days" };
        var how = left?.Kind switch { MoveKind.Release => "being released", MoveKind.Replacement => "being replaced", _ => "the contract ended" };
        var (headline, summary) = (from.Standing, to.Standing) switch
        {
            (Standing.Organization, Standing.Organization) => ($"{m.Name} joins {to.Name}", $"{who}; moved directly from {from.Name}."),
            (_, Standing.Organization) => m.Kind switch
            {
                MoveKind.Interim => ($"{m.Name} takes interim charge of {to.Name}", "Covers match preparation until a permanent head coach is appointed."),
                MoveKind.Appointment when left is not null => ($"{m.Name} returns as head coach of {to.Name}", $"Back {gap} after {how}."),
                MoveKind.Appointment => ($"{m.Name} named head coach of {to.Name}", "Appointed from the coach market; takes over match preparation."),
                _ when left is not null => ($"{m.Name} re-signs with {to.Name}", $"{who}; back {gap} after {how}."),
                _ when from.Standing == Standing.Unknown => ($"{m.Name} joins {to.Name}", $"{who}; where they came from is not on record."),
                _ => ($"{m.Name} signs with {to.Name}", $"{who}, signed from the free-agent market.")
            },
            _ => m.Kind switch
            {
                _ when to.Standing == Standing.Unknown => ($"{m.Name} leaves {from.Name}", $"{who}; where they went is not on record."),
                MoveKind.Release => ($"{m.Name} released by {from.Name}", $"{who}; released before the contract ended."),
                MoveKind.Replacement => ($"{m.Name} leaves {from.Name}",
                    Partner(MoveKind.Appointment) is { } successor ? $"{who}; replaced by {successor}." : $"{who}; replaced by a new appointment."),
                _ when m.Coach => ($"{m.Name} leaves {from.Name}", Partner(MoveKind.Interim) is { } interim
                    ? $"{who}; the contract ended without a renewal, and {interim} covers as interim." : $"{who}; the contract ended without a renewal."),
                _ => ($"{m.Name} leaves {from.Name}", $"{who}; the contract ended without a renewal.")
            }
        };
        return new TransferStory(m.Id, m.Kind, StoryStatus.Confirmed, StorySource.Simulation, m.Coach ? StorySignificance.HeadCoach : StorySignificance.Player, m.Day,
            headline, summary, [m.Id], m.PersonId, m.Name, AssetIds.Portrait(m.PersonId), m.Coach, from, to, m.Coach ? ShellSections.Staff : ShellSections.Squad, m.PersonId);
    }

    private static PortalFinance Money(Situation v)
    {
        var last = v.Day + MoneyAheadDays - 1;
        var wages = v.People.Select(p => (p.Salary, p.ContractEnd)).Append((v.CoachContract.Salary, v.CoachContract.ContractEnd)).ToArray();
        var receipts = v.Bills.Where(x => x.Incoming && x.DueDay <= last).Sum(x => x.Remaining);
        var bills = v.Bills.Where(x => !x.Incoming && x.DueDay <= last).Sum(x => x.Remaining);
        long projected = 0;
        for (var d = v.Day + 1; d <= last; d++) projected += wages.Where(w => w.ContractEnd >= d).Sum(w => w.Salary) + v.OperatingCost;
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
            detail = $"{Currency.Format(offer.Payment)} a week for {Figures.Count(offer.DurationDays / WeekLength)} weeks, +{Figures.Count(offer.Load)} delivery load.";
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
}
