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
        var c = s.Company; var w = s.World; var e = s.Execution; var b = content.Definition.Balance; var day = w.Calendar.Day;
        Require(e.ContentHash == content.Hash && e.RulesVersion == content.Definition.RulesVersion && e.CampaignId.Length is > 0 and <= 128, "Campaign identity mismatch.");
        Require(e.Revision >= 0 && e.Revision == e.Receipts.Length, "Revision/receipt mismatch.");
        Unique(e.Receipts.Select(x => x.Id), "command receipts");
        Require(e.Receipts.Select((r,i) => r.Revision == i+1 && r.Digest.Length == 64).All(x => x), "Receipt sequence invalid.");
        Require(day >= 1 && day <= b.Horizon + 1 && Enum.IsDefined(w.Calendar.Phase) && w.Calendar.Sequence == (day - 1L) * 11 + (int)w.Calendar.Phase - 1, "Invalid daily cursor.");
        Require(w.Finished == (day == b.Horizon + 1), "Invalid fixture completion boundary.");
        Require(c.Id == "company:player" && c.People.Length is >= 5 and <= 7 && c.People.Select(p => p.Role).Distinct().Count() == 5, "Roster bounds/role coverage invalid.");
        Unique(c.People.Select(p => p.Id).Concat(w.Candidates.Select(p => p.Person.Id)).Append(c.Coach.Id), "current person owners");
        Require(c.People.All(p => p.Readiness is > 0 and <= 100 && p.Execution is >= 0 and <= 100 && p.Adaptability is >= 0 and <= 100 && p.Consistency is >= 0 and <= 100 && p.Role.Length == 1 && "ABCDE".Contains(p.Role, StringComparison.Ordinal)), "Invalid person attributes.");
        Unique(c.Employment.Select(x => x.Id), "employment IDs");
        Unique(c.Employment.Select(x => x.PersonId), "employment person references");
        Require(c.Employment.Length == c.People.Length + 1 && c.Employment.All(x => (x.PersonId == c.Coach.Id || c.People.Any(p => p.Id == x.PersonId)) && x.Salary >= 0 && x.ReleaseCost >= 0 && x.EndDay <= b.Horizon), "Invalid employment references/terms.");
        Require(c.Cash >= 0 && c.Cash == checked(b.StartingCash + c.Settlements.Sum(x => x.Delta)), "Cash ledger does not reconcile.");
        Require(c.Reputation is >= 0 and <= 100 && c.Audience >= 0 && c.Information is >= 0 and <= 100 && Enum.IsDefined(c.Recovery), "Resource bounds invalid.");
        Require(Enum.IsDefined(c.Authority.Mode) && Enum.IsDefined(c.Authority.RiskCeiling), "Invalid authority.");
        Require(c.Work.Execution >= 0 && c.Work.Opponent >= 0 && c.Work.Meta >= 0, "Invalid completed work.");
        Unique(c.FinancialItems.Select(x => x.Id), "financial item IDs");
        Unique(c.Settlements.Select(x => x.Id), "settlement IDs");
        foreach (var item in c.FinancialItems)
        {
            Require(item.DueDay >= 1 && item.DueDay <= b.Horizon && item.Amount >= 0 && item.Remaining >= 0 && item.Remaining <= item.Amount &&
                (item.MissedDay is null || !item.Incoming && item.MissedDay >= item.DueDay && item.MissedDay <= day), "Invalid financial item.");
            var paid = c.Settlements.Where(x => x.CauseId == item.Id).Sum(x => item.Incoming ? x.Delta : -x.Delta);
            Require(paid == item.Amount - item.Remaining, "Financial item settlement mismatch.");
        }
        Require(c.Settlements.All(x => x.Day >= 1 && x.Day <= day), "Invalid settlement date.");
        Unique(w.Rivals.Select(x => x.Id), "rival IDs"); Unique(w.Fixtures.Select(x => x.Id), "fixture IDs");
        Require(w.Fixtures.SequenceEqual(content.Definition.Fixtures.OrderBy(f => f.Day).ThenBy(f => f.Id, StringComparer.Ordinal)), "Schedule changed without a supported rule.");
        Require(w.Rivals.Length == content.Definition.Rivals.Length && w.Rivals.All(r => content.Definition.Rivals.Any(x => x.Id == r.Id) && r.Strength is > 0 and <= 100 && r.Budget >= 0 && Enum.IsDefined(r.Posture)), "Invalid rival state.");
        Unique(w.Results.Select(x => x.Id), "result IDs"); Unique(w.Results.Select(x => x.FixtureId), "resolved fixtures");
        foreach (var result in w.Results)
            Require(w.Fixtures.Any(f => f.Id == result.FixtureId && f.Day == result.Day && f.RivalId == result.RivalId) && result.Day <= day &&
                (result.Day != day || w.Calendar.Phase > Phase.Competition) && result.Probability is >= 50000 and <= 950000, "Invalid result/cursor relationship.");
        Require(c.FinanceReceipts.Order().SequenceEqual(w.Results.Select(r => r.Id).Order()) && c.CommercialReceipts.Order().SequenceEqual(w.Results.Select(r => r.Id).Order()), "Immediate outcome consumers incomplete/duplicated.");
        Unique(c.PendingEffects.Select(x => x.Id), "pending effects");
        Require(c.PendingEffects.All(x => w.Results.Any(r => r.Id == x.CauseId && x.DueDay == r.Day + 1)), "Dangling delayed consequence.");
        Unique(w.Offers.Select(x => x.Id), "offer IDs"); Unique(c.Sponsors.Select(x => x.Id), "sponsor IDs");
        Require(w.Offers.All(o => content.Definition.Offers.Any(x => x.Id == o.Id) && (o.ClaimedBy is null || o.ClaimedBy == c.Id || w.Rivals.Any(r => r.Id == o.ClaimedBy))), "Invalid offer claim.");
        Require(c.Sponsors.All(x => x.EndDay <= b.Horizon && x.Load >= 0 && x.WinBonus >= 0 && (x.Id == "agreement:" + content.Definition.InitialSponsor.Id || w.Offers.Any(o => x.Id == "agreement:" + o.Id && o.ClaimedBy == c.Id))), "Invalid sponsor contract.");
        if (c.Plan is { } p)
        {
            Require(Simulation.NextFixture(s)?.Id == p.FixtureId && p.Execution >= 0 && p.Opponent >= 0 && p.Meta >= 0 && (long)p.Execution + p.Opponent + p.Meta == 100 && Enum.IsDefined(p.Posture) && Enum.IsDefined(p.ExpectedOpponent), "Invalid committed plan.");
            Require(p.Lineup.Length == 5 && p.Lineup.Distinct().Count() == 5 && p.Lineup.All(id => c.People.Any(x => x.Id == id)), "Invalid lineup references.");
            Require(p.Lineup.Select(id => c.People.Single(x => x.Id == id).Role).Distinct().Count() == 5, "Invalid lineup roles.");
        }
    }
}
