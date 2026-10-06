using System.Collections.Immutable;

namespace ManagementGame.Domain;

public static class CampaignFactory
{
    public static Campaign Create(Content content, string contentHash, ulong seed, string campaignId)
    {
        var b = content.Balance;
        var contracts = content.Players.OrderBy(p => p.Person.Id, StringComparer.Ordinal)
            .Select(p => new Employment("employment:" + p.Person.Id, p.Person.Id, p.Salary, p.ReleaseCost, b.Horizon)).ToImmutableArray()
            .Add(new Employment("employment:" + content.Coach.Id, content.Coach.Id, b.OperatingCost, 0, b.Horizon));
        var items = ImmutableArray.CreateBuilder<FinancialItem>();
        foreach (var contract in contracts)
            for (var day = 1; day <= b.Horizon; day++)
                items.Add(new FinancialItem($"{contract.Id}:day:{day}", contract.Id, day, contract.Salary, false, contract.Salary, null));
        var company = new Company("company:player", content.CompanyName,
            content.Players.Select(p => p.Person).OrderBy(p => p.Id, StringComparer.Ordinal).ToImmutableArray(), content.Coach,
            contracts, [], b.StartingCash, items.ToImmutable(), [], b.Reputation, b.Audience, b.Information,
            new Authority(ControlMode.Manual, Posture.Aggressive), null, new Work(0, 0, 0), RecoveryStage.Stable, [], [], [], []);
        company = Finance.AddSponsor(company, content.InitialSponsor, 1);
        var world = new World(new Calendar(1, Phase.Finance, 0), content.Rivals.OrderBy(r => r.Id, StringComparer.Ordinal).ToImmutableArray(),
            content.Fixtures.OrderBy(f => f.Day).ThenBy(f => f.Id, StringComparer.Ordinal).ToImmutableArray(), [],
            content.Offers.OrderBy(o => o.Id, StringComparer.Ordinal).ToImmutableArray(),
            content.Candidates.OrderBy(c => c.Person.Id, StringComparer.Ordinal).ToImmutableArray(), Posture.Balanced, false);
        return new Campaign(company, world, new Execution(campaignId, seed, 0, content.RulesVersion, contentHash, []));
    }
}
