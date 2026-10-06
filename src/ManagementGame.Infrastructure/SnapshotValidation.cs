using ManagementGame.Domain;

namespace ManagementGame.Infrastructure;

public static class SnapshotValidation
{
    public static void Validate(Campaign s, LoadedContent content)
    {
        void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
        void Unique(IEnumerable<string> ids, string label)
        {
            var array = ids.ToArray();
            Require(array.Length == array.Distinct(StringComparer.Ordinal).Count() && array.All(x => !string.IsNullOrWhiteSpace(x) && x.Length <= 256), "Duplicate/invalid " + label);
        }
        bool Person(Person p) => p.Readiness is > 0 and <= 100 && p.Execution is >= 0 and <= 100 && p.Adaptability is >= 0 and <= 100
            && p.Consistency is >= 0 and <= 100 && p.Role.Length == 1 && Generator.Roles.Contains(p.Role, StringComparison.Ordinal) && p.Name.Length is > 0 and <= 120;
        bool Coach(Coach x) => x.Preparation is > 0 and <= 100 && x.Analysis is >= 0 and <= 100 && x.Adaptability is >= 0 and <= 100 && x.Capacity is > 0 and <= 1000 && x.Name.Length is > 0 and <= 120;
        var c = s.Company; var w = s.World; var e = s.Execution; var definition = content.Definition; var b = definition.Balance; var day = w.Calendar.Day;
        var season = Seasons.Of(day, b);
        Require(e.ContentHash == content.Hash && e.RulesVersion == definition.RulesVersion && e.CampaignId.Length is > 0 and <= 128, "Campaign identity mismatch.");
        Require(e.Revision >= 0 && e.Revision == e.Receipts.Length, "Revision/receipt mismatch.");
        Unique(e.Receipts.Select(x => x.Id), "command receipts");
        Require(e.Receipts.Select((r, i) => r.Revision == i + 1 && r.Digest.Length == 64).All(x => x), "Receipt sequence invalid.");
        Require(day >= 1 && Enum.IsDefined(w.Calendar.Phase) && w.Calendar.Sequence == (day - 1L) * 11 + (int)w.Calendar.Phase - 1, "Invalid daily cursor.");
        Require(c.Id == "company:player" && c.Name.Trim().Length is > 0 and <= 60 && c.People.Length <= b.MaxRoster && c.People.All(Person) && Coach(c.Coach), "Roster bounds or attributes invalid.");
        Unique(c.People.Select(p => p.Id).Concat(w.Candidates.Select(p => p.Person.Id)).Append(c.Coach.Id).Concat(w.CoachCandidates.Select(x => x.Coach.Id)), "current person owners");
        Require(w.Candidates.All(x => Person(x.Person) && x.Fee >= 0 && x.Salary >= 0 && x.ReleaseCost >= 0) && w.CoachCandidates.All(x => Coach(x.Coach) && x.Fee >= 0 && x.Salary >= 0), "Invalid market entries.");
        Unique(c.Employment.Select(x => x.Id), "employment IDs");
        Unique(c.Employment.Select(x => x.PersonId), "employment person references");
        // Expiry is processed in the Contracts phase, so a contract may still be present on the day after its end.
        Require(c.Employment.Length == c.People.Length + 1 && c.Employment.All(x => (x.PersonId == c.Coach.Id || c.People.Any(p => p.Id == x.PersonId))
            && x.Salary >= 0 && x.ReleaseCost >= 0 && x.EndDay >= day - 1), "Invalid employment references/terms.");
        Require(c.Cash >= 0 && c.Cash == checked(c.LedgerBase + c.Settlements.Sum(x => x.Delta)), "Cash ledger does not reconcile.");
        Require(c.Reputation is >= 0 and <= 100 && c.Audience >= 0 && c.Information is >= 0 and <= 100 && Enum.IsDefined(c.Recovery), "Resource bounds invalid.");
        Require(Enum.IsDefined(c.Authority.Mode) && Enum.IsDefined(c.Authority.RiskCeiling), "Invalid authority.");
        Require(c.Work.Execution >= 0 && c.Work.Opponent >= 0 && c.Work.Meta >= 0, "Invalid completed work.");
        Unique(c.FinancialItems.Select(x => x.Id), "financial item IDs");
        Unique(c.Settlements.Select(x => x.Id), "settlement IDs");
        foreach (var item in c.FinancialItems)
        {
            Require(item.DueDay >= 1 && item.Amount >= 0 && item.Remaining >= 0 && item.Remaining <= item.Amount &&
                (item.MissedDay is null || !item.Incoming && item.MissedDay >= item.DueDay && item.MissedDay <= day), "Invalid financial item.");
            var paid = c.Settlements.Where(x => x.CauseId == item.Id).Sum(x => item.Incoming ? x.Delta : -x.Delta);
            Require(paid == item.Amount - item.Remaining, "Financial item settlement mismatch.");
        }
        Require(c.Settlements.All(x => x.Day >= 1 && x.Day <= day), "Invalid settlement date.");
        Unique(w.Rivals.Select(x => x.Id), "rival IDs"); Unique(w.Fixtures.Select(x => x.Id), "fixture IDs");
        Require(w.Rivals.Length == b.RivalCount && w.Rivals.All(r => r.Strength is > 0 and <= 100 && r.Budget >= 0 && Enum.IsDefined(r.Posture) && r.Name.Length is > 0 and <= 60), "Invalid rival state.");
        Require(w.Fixtures.SequenceEqual(Seasons.Schedule(e.Seed, season, w.Rivals, b)), "Schedule changed without a supported rule.");
        Unique(w.Results.Select(x => x.Id), "result IDs"); Unique(w.Results.Select(x => x.FixtureId), "resolved fixtures");
        foreach (var result in w.Results)
        {
            var current = result.FixtureId.StartsWith(Seasons.FixturePrefix(season), StringComparison.Ordinal);
            Require(w.Rivals.Any(r => r.Id == result.RivalId) && result.Day <= day && (result.Day != day || w.Calendar.Phase > Phase.Competition)
                && result.Probability is >= 50000 and <= 950000 && (current
                    ? w.Fixtures.Any(f => f.Id == result.FixtureId && f.Day == result.Day && f.RivalId == result.RivalId)
                    : result.Day < Seasons.Start(season, b)), "Invalid result/cursor relationship.");
        }
        Require(c.FinanceReceipts.Order().SequenceEqual(w.Results.Select(r => r.Id).Order()) && c.CommercialReceipts.Order().SequenceEqual(w.Results.Select(r => r.Id).Order()), "Immediate outcome consumers incomplete/duplicated.");
        Unique(c.PendingEffects.Select(x => x.Id), "pending effects");
        Require(c.PendingEffects.All(x => w.Results.Any(r => r.Id == x.CauseId && x.DueDay == r.Day + 1)), "Dangling delayed consequence.");
        var brands = definition.Brands.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        Unique(w.Offers.Select(x => x.Id), "offer IDs"); Unique(c.Sponsors.Select(x => x.Id), "sponsor IDs");
        Require(w.Offers.All(o => brands.Contains(o.BrandId) && Enum.IsDefined(o.Origin) && o.Payment > 0 && o.DurationDays > 0 && o.WinBonus >= 0 && o.Load >= 0
            && (o.ClaimedBy is null || o.ClaimedBy == c.Id || w.Rivals.Any(r => r.Id == o.ClaimedBy))), "Invalid offer.");
        Require(c.Sponsors.All(x => brands.Contains(x.BrandId) && x.EndDay >= x.StartDay && x.Load >= 0 && x.WinBonus >= 0 && x.Payment > 0), "Invalid sponsor contract.");
        Unique(w.Negotiations.Select(x => x.Id), "negotiation IDs");
        Require(w.Negotiations.All(n => brands.Contains(n.BrandId) && n.Payment > 0 && n.ResponseDay >= n.SubmittedDay && n.SubmittedDay <= day), "Invalid negotiation.");
        Unique(w.Brands.Select(x => x.BrandId), "brand status");
        Require(w.Brands.All(x => brands.Contains(x.BrandId)) && w.NextId >= 0, "Invalid market state.");
        if (c.Plan is { } p)
        {
            Require(Simulation.NextFixture(s)?.Id == p.FixtureId && p.Execution >= 0 && p.Opponent >= 0 && p.Meta >= 0 && (long)p.Execution + p.Opponent + p.Meta == 100 && Enum.IsDefined(p.Posture) && Enum.IsDefined(p.ExpectedOpponent), "Invalid committed plan.");
            Require(p.Lineup.Length == 5 && p.Lineup.Distinct().Count() == 5 && p.Lineup.All(id => c.People.Any(x => x.Id == id)), "Invalid lineup references.");
            Require(p.Lineup.Select(id => c.People.Single(x => x.Id == id).Role).Distinct().Count() == 5, "Invalid lineup roles.");
        }
    }
}
