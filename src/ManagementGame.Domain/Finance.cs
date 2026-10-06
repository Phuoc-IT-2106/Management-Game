namespace ManagementGame.Domain;

public static class Finance
{
    public static Company AddSponsor(Company c, Offer offer, int day)
    {
        var contract = new SponsorContract("agreement:" + offer.Id, offer.Name, offer.EndDay, offer.Load, offer.WinBonus);
        var items = c.FinancialItems;
        for (var due = day + 3; due <= offer.EndDay; due += 7)
            items = items.Add(new FinancialItem($"{contract.Id}:day:{due}", contract.Id, due, offer.Payment, true, offer.Payment, null));
        return c with { Sponsors = c.Sponsors.Add(contract), FinancialItems = items };
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

    public static long Forecast(Company c, int day, int horizon = 7) => checked(c.Cash + c.FinancialItems
        .Where(x => x.DueDay <= day + horizon).Sum(x => x.Incoming ? x.Remaining : -x.Remaining));
    public static Company PayNow(Company c, long amount, string cause, int day)
    {
        if (amount < 0 || c.Cash < amount) throw new RuleViolation("Insufficient cash for this commitment.");
        return c with { Cash = checked(c.Cash - amount), Settlements = c.Settlements.Add(new Settlement("immediate:" + cause, cause, day, -amount)) };
    }
}
