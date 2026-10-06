using System.Collections.Immutable;

namespace ManagementGame.Domain;

public static class Finance
{
    public static Company AddSponsor(Company c, Offer offer, int day, string contractId)
    {
        var contract = new SponsorContract(contractId, offer.BrandId, offer.Name, day, day + offer.DurationDays - 1, offer.Load, offer.WinBonus, offer.Payment);
        var items = c.FinancialItems;
        for (var due = day + 3; due <= contract.EndDay; due += 7)
            items = items.Add(new FinancialItem($"{contract.Id}:day:{due}", contract.Id, due, offer.Payment, true, offer.Payment, null));
        return c with { Sponsors = c.Sponsors.Add(contract), FinancialItems = items };
    }

    /// <summary>Wages fall due one day at a time for every contract still running; nothing is scheduled beyond today.</summary>
    public static Company Payroll(Company c, int day)
    {
        var items = c.FinancialItems;
        foreach (var contract in c.Employment.Where(e => e.EndDay >= day).OrderBy(e => e.Id, StringComparer.Ordinal))
        {
            var id = $"{contract.Id}:day:{day}";
            if (!items.Any(x => x.Id == id)) items = items.Add(new FinancialItem(id, contract.Id, day, contract.Salary, false, contract.Salary, null));
        }
        return c with { FinancialItems = items };
    }

    public static Company Settle(Company c, int day)
    {
        var cash = c.Cash;
        var items = c.FinancialItems;
        var ledger = c.Settlements;
        foreach (var item in items.Where(x => x.DueDay <= day && x.Remaining > 0)
            .OrderByDescending(x => x.Incoming).ThenBy(x => x.DueDay).ThenBy(x => x.Id, StringComparer.Ordinal))
        {
            var amount = item.Incoming ? item.Remaining : Math.Min(cash, item.Remaining);
            var delta = item.Incoming ? amount : -amount;
            cash = checked(cash + delta);
            var remaining = item.Remaining - amount;
            var missed = item.MissedDay ?? (!item.Incoming && remaining > 0 ? day : (int?)null);
            items = items.Replace(item, item with { Remaining = remaining, MissedDay = missed });
            if (amount > 0) ledger = ledger.Add(new Settlement($"settle:{item.Id}:{day}", item.Id, day, delta));
        }
        return c with { Cash = cash, FinancialItems = items, Settlements = ledger };
    }

    public static Company Consume(Company c, CompetitiveOutcome outcome, Fixture fixture)
    {
        if (c.FinanceReceipts.Contains(outcome.Id)) return c;
        var items = c.FinancialItems;
        if (outcome.Won && fixture.Prize > 0)
            items = items.Add(new FinancialItem("prize:" + outcome.Id, outcome.Id, outcome.Day + 1, fixture.Prize, true, fixture.Prize, null));
        foreach (var sponsor in c.Sponsors.Where(s => s.EndDay >= outcome.Day && outcome.Won && s.WinBonus > 0))
            items = items.Add(new FinancialItem($"bonus:{sponsor.Id}:{outcome.Id}", outcome.Id, outcome.Day + 1,
                sponsor.WinBonus, true, sponsor.WinBonus, null));
        return c with { FinancialItems = items, FinanceReceipts = c.FinanceReceipts.Add(outcome.Id) };
    }

    /// <summary>Cash plus scheduled items, plus wages the running contracts will incur inside the window.</summary>
    public static long Forecast(Company c, int day, int horizon = 7)
    {
        var scheduled = c.FinancialItems.Where(x => x.DueDay <= day + horizon).Sum(x => x.Incoming ? x.Remaining : -x.Remaining);
        long payroll = 0;
        for (var d = day; d <= day + horizon; d++)
            foreach (var contract in c.Employment.Where(e => e.EndDay >= d))
                if (!c.FinancialItems.Any(x => x.Id == $"{contract.Id}:day:{d}")) payroll += contract.Salary;
        return checked(c.Cash + scheduled - payroll);
    }

    public static Company PayNow(Company c, long amount, string cause, int day)
    {
        if (amount < 0 || c.Cash < amount) throw new RuleViolation("Insufficient cash for this commitment.");
        return c with { Cash = checked(c.Cash - amount), Settlements = c.Settlements.Add(new Settlement("immediate:" + cause, cause, day, -amount)) };
    }

    /// <summary>Folds fully settled history before <paramref name="cutoff"/> into the ledger base; cash is unchanged.</summary>
    public static Company Archive(Company c, int cutoff)
    {
        var archived = c.FinancialItems.Where(x => x.Remaining == 0 && x.DueDay < cutoff).Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        var folded = c.Settlements.Where(x => archived.Contains(x.CauseId) || x.Id.StartsWith("immediate:", StringComparison.Ordinal) && x.Day < cutoff).ToArray();
        if (archived.Count == 0 && folded.Length == 0) return c;
        var removed = folded.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        return c with
        {
            LedgerBase = checked(c.LedgerBase + folded.Sum(x => x.Delta)),
            FinancialItems = c.FinancialItems.Where(x => !archived.Contains(x.Id)).ToImmutableArray(),
            Settlements = c.Settlements.Where(x => !removed.Contains(x.Id)).ToImmutableArray()
        };
    }
}
