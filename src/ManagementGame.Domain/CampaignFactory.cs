using System.Collections.Immutable;

namespace ManagementGame.Domain;

public static class CampaignFactory
{
    /// <summary>Generates a seeded world from content pools; nothing about people, rivals or brands is fixed in code.</summary>
    public static Campaign Create(Content content, string contentHash, ulong seed, string campaignId, string? companyName = null)
    {
        var b = content.Balance; var g = content.Generation;
        var name = string.IsNullOrWhiteSpace(companyName) ? content.DefaultCompanyName : companyName.Trim();
        if (name.Length > 60) throw new RuleViolation("Company name is limited to 60 characters.");
        long next = 0;
        var roles = Generator.Roles.Select(r => r.ToString()).ToList();
        while (roles.Count < b.RosterSize) roles.Add(Generator.Roles[KeyedRandom.Range(seed, "roster", "extra:" + roles.Count, "role", 5)].ToString());
        var people = roles.Select(role => Generator.Player(seed, content, $"person:g{++next:D5}", role, g.Talent)).ToImmutableArray();
        var coach = Generator.Coach(seed, content, $"coach:g{++next:D5}");
        // Staggered starting contracts so renewals arrive over several seasons.
        var contracts = people.Select(p =>
        {
            var salary = Pay.Player(p.Execution);
            var seasons = 1 + KeyedRandom.Range(seed, "roster", p.Id, "contract", b.StartingContractSeasons);
            return new Employment("employment:" + p.Id, p.Id, salary, salary * 3, Seasons.End(seasons, b));
        }).ToImmutableArray();
        var coachSalary = Pay.Coach(coach);
        contracts = contracts.Add(new Employment("employment:" + coach.Id, coach.Id, coachSalary, coachSalary * 5, Seasons.End(2, b)));
        var company = new Company("company:player", name, people, coach, contracts, [], b.StartingCash, b.StartingCash, [], [],
            b.Reputation, b.Audience, b.Information, new Authority(ControlMode.Manual, Posture.Aggressive), null, new Work(0, 0, 0),
            RecoveryStage.Stable, [], [], [], []);
        // A modest first-season partner from the lowest qualifying tier keeps the opening economy playable.
        var starters = content.Brands.Where(x => Market.Qualifies(x, company)).OrderBy(x => x.MinimumReputation).ThenBy(x => x.Id, StringComparer.Ordinal).ToArray();
        if (starters.Length > 0)
        {
            var lowest = starters.Where(x => x.MinimumReputation <= starters[0].MinimumReputation + 20).ToArray();
            var brand = lowest[KeyedRandom.Range(seed, "market", "starter", "brand", lowest.Length)];
            var starter = new Offer($"offer:{brand.Id}:starter", brand.Id, brand.Name, OfferOrigin.Inbound, 1, b.SeasonLength, brand.BasePayment, brand.WinBonus, brand.Load, 0, company.Id);
            company = Finance.AddSponsor(company, starter, 1, "agreement:" + starter.Id);
        }
        var rivals = Generator.Rivals(seed, content);
        var world = new World(new Calendar(1, Phase.Finance, 0), rivals, Seasons.Schedule(seed, 1, rivals, b), [], [], [], [], [], [], Posture.Balanced, next);
        world = Market.Refresh(company, world, seed, content, 1);
        (company, world) = Market.Approach(company, world, seed, content, 1, guaranteed: 2);
        return new Campaign(company, world, new Execution(campaignId, seed, 0, content.RulesVersion, contentHash, []));
    }
}
