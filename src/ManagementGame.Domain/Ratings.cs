namespace ManagementGame.Domain;

/// <summary>Derived player numbers (rules v4): role ratings from role-weighted skills, age, and the market's view of a
/// player's value. Nothing here is stored; every figure is recomputed from the person and the content.</summary>
public static class Ratings
{
    /// <summary>Proficiency (percent) in a secondary role; a player cannot play any other role.</summary>
    public const int SecondaryProficiency = 80;

    public static int Age(Person p, int season) => season - p.BornSeason;
    public static int Skill(Skills s, int index) => index switch
    {
        0 => s.Mechanics, 1 => s.EarlyGame, 2 => s.Teamfight, 3 => s.Macro, _ => s.ChampionPool
    };
    public static int Proficiency(Person p, string role) => role == p.Role ? 100 : role == p.SecondaryRole && role.Length > 0 ? SecondaryProficiency : 0;

    /// <summary>Rating in a role, 0–99: the role-weighted skills (weights in basis points) times proficiency. 0 when the
    /// person cannot play the role.</summary>
    public static int InRole(Person p, string role, Content content)
    {
        var weights = content.RoleWeights.Single(w => w.Role == role).Weights;
        long sum = 0;
        for (var i = 0; i < weights.Length; i++) sum += (long)weights[i] * Skill(p.Skills, i);
        return (int)Numbers.Divide(sum * Proficiency(p, role), 10_000 * 100);
    }
    /// <summary>Rating in the primary role: the number the squad, the market and wages read.</summary>
    public static int Rating(Person p, Content content) => InRole(p, p.Role, content);
}

/// <summary>Development salary curve; values are minor currency units per day.</summary>
public static class Pay
{
    /// <summary>What a player asks per day on the market: rating squared over five (the rules v3 curve on Execution), then
    /// fan following (75–125%), ambition (90–110%) and, up to age 21, the room left to grow (+1% per point of headroom).</summary>
    public static long Player(Person p, int season, Content content)
    {
        var rating = Ratings.Rating(p, content);
        var wage = (long)rating * rating / 5;
        wage = wage * (100 + (p.Popularity - 50) / 2) / 100;
        wage = wage * (100 + (p.Mind.Ambition - 50) / 5) / 100;
        if (Ratings.Age(p, season) <= 21) wage = wage * (100 + Math.Max(0, p.Potential - rating)) / 100;
        return Math.Max(100, wage);
    }
    /// <summary>A player's transfer fee: the asking wage for FeeDaysMin days up to the pivot rating, FeeDaysPerPoint more days
    /// for every point above it, at most FeeDaysMax. Good players are dear, so a strong squad is built over seasons.</summary>
    public static long Fee(Person p, int season, Content content)
    {
        var b = content.Balance; var rating = Ratings.Rating(p, content);
        return Player(p, season, content) * Math.Clamp(b.FeeDaysMin + Math.Max(0, rating - b.FeePivot) * b.FeeDaysPerPoint, b.FeeDaysMin, b.FeeDaysMax);
    }
    public static long Coach(Coach coach) => (coach.Preparation + coach.Analysis + coach.Adaptability + coach.Development) * 4L + coach.Capacity * 2L;
    /// <summary>A renewing person asks for at least their current pay, scaled by the company's standing; loyalty lowers the
    /// premium (−10..+10 percentage points around neutral 50).</summary>
    public static long Demand(long current, long market, int reputation, int loyalty = 50) =>
        Math.Max(current, Numbers.Divide(market * (100 + reputation / 5 - (loyalty - 50) / 5), 100));
}
