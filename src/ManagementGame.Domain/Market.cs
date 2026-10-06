using System.Collections.Immutable;

namespace ManagementGame.Domain;

/// <summary>Commercial and talent markets. Offers arrive only when the company qualifies; terms come from its standing.</summary>
public static class Market
{
    public const string NoticePrefix = "notice:";

    public static BrandDefinition Brand(Content content, string brandId) =>
        content.Brands.FirstOrDefault(b => b.Id == brandId) ?? throw new RuleViolation("Unknown sponsor brand.");

    /// <summary>What a brand considers fair per weekly payment, from the company's current reputation and audience.</summary>
    public static long FairPayment(BrandDefinition brand, int reputation, int audience)
    {
        var standing = 100 + (reputation - brand.MinimumReputation) + (audience - brand.MinimumAudience) / 1000;
        if (reputation < brand.MinimumReputation) standing -= (brand.MinimumReputation - reputation) * 2;
        return Round(Numbers.Divide(brand.BasePayment * Math.Max(40, standing), 100));
    }
    private static long Round(long value) => Math.Max(500, Numbers.Divide(value, 500) * 500);

    public static bool Qualifies(BrandDefinition brand, Company c) => c.Reputation >= brand.MinimumReputation && c.Audience >= brand.MinimumAudience;
    /// <summary>A brand may be approached slightly above the company's tier, at a weaker valuation.</summary>
    public static bool Approachable(BrandDefinition brand, Company c) =>
        c.Reputation >= brand.MinimumReputation - 10 && c.Audience * 10L >= brand.MinimumAudience * 8L;

    /// <summary>Why a brand cannot currently receive an approach or send an offer; null when it can.</summary>
    public static string? Blocked(BrandDefinition brand, Company c, World w, int day)
    {
        if (c.Sponsors.Any(s => s.BrandId == brand.Id && s.EndDay >= day)) return "Already a partner";
        if (w.Offers.Any(o => o.BrandId == brand.Id && o.ClaimedBy is null && o.Deadline >= day)) return "Offer awaiting your answer";
        if (w.Negotiations.Any(n => n.BrandId == brand.Id)) return "Proposal under review";
        if (w.Brands.FirstOrDefault(x => x.BrandId == brand.Id) is { } status && status.AvailableFromDay > day) return $"Unavailable until day {status.AvailableFromDay}";
        return null;
    }

    public static int ActiveSponsors(Company c, int day) => c.Sponsors.Count(s => s.EndDay >= day);
    private static int OpenOffers(World w, int day) => w.Offers.Count(o => o.ClaimedBy is null && o.Deadline >= day);

    private static World Cooldown(World w, string brandId, int until)
    {
        var existing = w.Brands.FirstOrDefault(x => x.BrandId == brandId);
        if (existing is not null && existing.AvailableFromDay >= until) return w;
        var brands = existing is null ? w.Brands : w.Brands.Remove(existing);
        return w with { Brands = brands.Add(new BrandStatus(brandId, until)).OrderBy(x => x.BrandId, StringComparer.Ordinal).ToImmutableArray() };
    }

    private static Company Notice(Company c, string key, int day, string cause, string text) =>
        c with { Reviews = c.Reviews.Add(new ReviewEntry($"{NoticePrefix}{key}:{day}", day, cause, text)) };

    /// <summary>Weekly market: qualifying brands may approach; the opening market guarantees a first choice.</summary>
    public static (Company, World) Approach(Company c, World w, ulong seed, Content content, int day, int guaranteed = 0)
    {
        var b = content.Balance;
        // Offers may outnumber free slots (that is the choice); brands stop approaching only when every slot is taken.
        var room = b.MaxOpenOffers - OpenOffers(w, day);
        if (room <= 0 || ActiveSponsors(c, day) >= b.SponsorSlots) return (c, w);
        var eligible = content.Brands.Where(x => Qualifies(x, c) && Blocked(x, c, w, day) is null).OrderBy(x => x.Id, StringComparer.Ordinal)
            .Select(x => (Brand: x, Roll: KeyedRandom.Range(seed, "market", $"{x.Id}:{day}", "interest", 100))).ToArray();
        var interested = eligible.Where(x => x.Roll < Math.Clamp(25 + (c.Reputation - x.Brand.MinimumReputation) * 2, 10, 85))
            .OrderBy(x => x.Roll).ThenBy(x => x.Brand.Id, StringComparer.Ordinal).Take(Math.Min(2, room)).ToList();
        foreach (var extra in eligible.OrderBy(x => x.Roll).ThenBy(x => x.Brand.Id, StringComparer.Ordinal))
            if (interested.Count < Math.Min(guaranteed, room) && !interested.Contains(extra)) interested.Add(extra);
        foreach (var (brand, _) in interested)
        {
            var durations = brand.DurationDays.Order().ToArray();
            var duration = durations[KeyedRandom.Range(seed, "market", $"{brand.Id}:{day}", "duration", durations.Length)];
            var offer = new Offer($"offer:{brand.Id}:{day}", brand.Id, brand.Name, OfferOrigin.Inbound, day + b.OfferWindow - 1, duration,
                FairPayment(brand, c.Reputation, c.Audience), brand.WinBonus, brand.Load, brand.MinimumReputation, null);
            w = w with { Offers = w.Offers.Add(offer) };
            c = Notice(c, "offer:" + offer.Id, day, offer.Id,
                $"{brand.Name} ({brand.Sector}) offers {Pay.Money(offer.Payment)} per week for {duration} days, +{brand.Load} delivery load. Answer by day {offer.Deadline}.");
        }
        return (c, w);
    }

    public static (Company, World) Resolve(Company c, World w, ulong seed, Content content, int day)
    {
        var b = content.Balance;
        foreach (var n in w.Negotiations.Where(x => x.ResponseDay <= day).OrderBy(x => x.Id, StringComparer.Ordinal))
        {
            var brand = Brand(content, n.BrandId);
            var fair = FairPayment(brand, c.Reputation, c.Audience);
            var ratio = Numbers.Divide(n.Payment * 100, fair);
            var roll = KeyedRandom.Range(seed, "negotiation", n.Id, "response", 100);
            var belowTier = !Qualifies(brand, c);
            // Asking at or under fair value is accepted; stretching the ask trades certainty for money.
            var response = ratio <= 100 ? "accept"
                : ratio <= 130 ? (!belowTier && roll < 100 - (ratio - 100) * 3 ? "accept" : "counter")
                : ratio <= 170 ? (roll < 50 ? "counter" : "reject") : "reject";
            w = w with { Negotiations = w.Negotiations.Remove(n) };
            if (response == "reject")
            {
                w = Cooldown(w, brand.Id, day + b.RejectCooldown);
                c = Notice(c, "negotiation:" + n.Id, day, n.Id, $"{brand.Name} declined your proposal of {Pay.Money(n.Payment)} per week. They will not reconsider before day {day + b.RejectCooldown}.");
                continue;
            }
            var payment = response == "accept" ? n.Payment : ratio > 130 ? Numbers.Divide(fair * 95, 100) : fair;
            var offer = new Offer($"offer:{brand.Id}:{day}:n", brand.Id, brand.Name, response == "accept" ? OfferOrigin.Negotiated : OfferOrigin.Counter,
                day + b.OfferWindow - 1, n.DurationDays, Round(payment), brand.WinBonus, brand.Load, 0, null);
            w = w with { Offers = w.Offers.Add(offer) };
            c = Notice(c, "negotiation:" + n.Id, day, n.Id, response == "accept"
                ? $"{brand.Name} accepted your terms: {Pay.Money(offer.Payment)} per week for {offer.DurationDays} days. Sign by day {offer.Deadline}."
                : $"{brand.Name} countered with {Pay.Money(offer.Payment)} per week for {offer.DurationDays} days. Sign by day {offer.Deadline} or let it lapse.");
        }
        return (c, w);
    }

    /// <summary>Unanswered offers lapse; a rival with budget may take an inbound one. Settled offers are pruned.</summary>
    public static (Company, World) Expire(Company c, World w, Content content, int day)
    {
        var b = content.Balance;
        foreach (var offer in w.Offers.Where(o => o.ClaimedBy is null && o.Deadline < day).OrderBy(o => o.Id, StringComparer.Ordinal))
        {
            var rival = offer.Origin == OfferOrigin.Inbound
                ? w.Rivals.Where(r => r.Need > 0 && r.Budget >= 10).OrderBy(r => r.Id, StringComparer.Ordinal).FirstOrDefault() : null;
            if (rival is not null)
            {
                w = w with { Offers = w.Offers.Replace(offer, offer with { ClaimedBy = rival.Id }),
                    Rivals = w.Rivals.Replace(rival, rival with { Budget = rival.Budget - 10, Need = 0 }) };
                w = Cooldown(w, offer.BrandId, day + b.SeasonLength / 2);
                c = c with { Reviews = c.Reviews.Add(new ReviewEntry($"claim:{offer.Id}", day, offer.Id, $"{rival.Name} signed with {offer.Name} after your offer lapsed.")) };
            }
            else
            {
                w = w with { Offers = w.Offers.Remove(offer) };
                w = Cooldown(w, offer.BrandId, day + (offer.Origin == OfferOrigin.Inbound ? b.MarketInterval * 2 : b.RejectCooldown));
                c = c with { Reviews = c.Reviews.Add(new ReviewEntry($"lapse:{offer.Id}", day, offer.Id, $"The offer from {offer.Name} lapsed unanswered.")) };
            }
        }
        var keep = w.Offers.Where(o => o.ClaimedBy is null
            || o.ClaimedBy == c.Id && c.Sponsors.Any(s => s.Id == "agreement:" + o.Id && s.EndDay >= day)
            || o.ClaimedBy != c.Id && o.Deadline >= day - 7).ToImmutableArray();
        return (c, w with { Offers = keep, Brands = w.Brands.Where(x => x.AvailableFromDay > day).ToImmutableArray() });
    }

    /// <summary>Keeps the free-agent and coach markets stocked, always covering any role the roster lacks.</summary>
    public static World Refresh(Company c, World w, ulong seed, Content content, int day)
    {
        var b = content.Balance; var next = w.NextId;
        var players = w.Candidates.Where(x => x.Deadline >= day).ToList();
        var missing = Generator.Roles.Where(r => !c.People.Any(p => p.Role == r.ToString()) && !players.Any(x => x.Person.Role == r.ToString())).ToList();
        while (players.Count < b.CandidateCount || missing.Count > 0)
        {
            var id = $"person:g{++next:D5}";
            var role = missing.Count > 0 ? missing[0] : Generator.Roles[KeyedRandom.Range(seed, "talent", id, "role", 5)];
            if (missing.Count > 0) missing.RemoveAt(0);
            var person = Generator.Player(seed, content, id, role.ToString(), content.Generation.Prospect);
            var salary = Pay.Player(person.Execution);
            players.Add(new Candidate(person, salary * 20, salary, salary * 3, day + b.TalentRefreshInterval - 1));
        }
        var coaches = w.CoachCandidates.Where(x => x.Deadline >= day).ToList();
        while (coaches.Count < b.CoachCandidateCount)
        {
            var coach = Generator.Coach(seed, content, $"coach:g{++next:D5}");
            var salary = Pay.Coach(coach);
            coaches.Add(new CoachCandidate(coach, salary * 15, salary, day + b.TalentRefreshInterval - 1));
        }
        return w with { Candidates = [.. players], CoachCandidates = [.. coaches], NextId = next };
    }

    /// <summary>Contracts end on their date: players become free agents, a departing coach is covered by an interim.</summary>
    public static (Company, World) Contracts(Company c, World w, ulong seed, Content content, int day)
    {
        var b = content.Balance;
        foreach (var contract in c.Employment.Where(e => e.EndDay < day).OrderBy(e => e.Id, StringComparer.Ordinal))
        {
            if (contract.PersonId == c.Coach.Id)
            {
                var next = w.NextId + 1;
                var interim = Generator.Coach(seed, content, $"coach:g{next:D5}", -12);
                var salary = Pay.Coach(interim);
                var departed = c.Coach;
                c = c with { Coach = interim, Employment = c.Employment.Remove(contract)
                    .Add(new Employment("employment:" + interim.Id, interim.Id, salary, salary * 5, day + b.SeasonLength - 1)) };
                w = w with { NextId = next, CoachCandidates = w.CoachCandidates.Add(new CoachCandidate(departed, contract.Salary * 15, contract.Salary, day + b.TalentRefreshInterval - 1)) };
                c = Notice(c, "contract:" + contract.Id, day, contract.Id, $"Head coach {departed.Name} left at contract end. Interim coach {interim.Name} is covering; hire a permanent coach from the market.");
                continue;
            }
            var person = c.People.Single(p => p.Id == contract.PersonId);
            c = c with { People = c.People.Remove(person), Employment = c.Employment.Remove(contract),
                Plan = c.Plan is { } plan && plan.Lineup.Contains(person.Id) ? null : c.Plan };
            w = w with { Candidates = w.Candidates.Add(new Candidate(person, contract.Salary * 5, contract.Salary, contract.ReleaseCost, day + b.TalentRefreshInterval - 1)) };
            c = Notice(c, "contract:" + contract.Id, day, contract.Id, $"{person.Name} (role {person.Role}) left as a free agent when the contract ended.");
        }
        foreach (var contract in c.Employment.Where(e => e.EndDay - day == b.RenewalWindow))
        {
            var name = contract.PersonId == c.Coach.Id ? "Head coach " + c.Coach.Name : c.People.Single(p => p.Id == contract.PersonId).Name;
            c = Notice(c, "renewal:" + contract.Id, day, contract.Id, $"{name}'s contract ends on day {contract.EndDay}. Renew it or let them leave.");
        }
        return (c, w);
    }
}
