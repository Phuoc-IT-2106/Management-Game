using ManagementGame.Domain;

namespace WorldExperiment;

/// <summary>One season's development environment for a person.</summary>
public sealed record DevEnvironment(int CoachDevelopment, Intensity Intensity, double PlayingShare, int Matches, int Morale);

public enum DevState { Normal, Breakthrough, Plateau, Setback }

public sealed record DevOutcome(int OverallBefore, int OverallAfter, int PotentialBefore, int PotentialAfter, DevState State, double Environment);

/// <summary>Experimental development model dev2 (EXPERIMENT; coefficients in config). Per season and per skill:
/// <list type="bullet">
/// <item>expected growth = age curve (shifted by the person's peak age) x headroom factor G/(G + half) x environment
/// (coach, intensity, readiness, playing time, morale, professionalism, trainability);</item>
/// <item>a keyed season state: breakthrough, plateau, setback or normal; plus keyed noise scaled by volatility;</item>
/// <item>growth is spent as skill points by the training focus (role-aligned, specialist or weakness), the player's affinity
/// and the room left below each skill's own ceiling; decline after the peak falls mostly on mechanics and early game;
/// match experience adds to macro and champion pool at any age;</item>
/// <item>the Overall never exceeds the potential; the potential is min(ceiling, Overall + optimistic remaining growth),
/// so it shrinks when the years pass without growth, and it is never below the Overall.</item>
/// </list></summary>
public static class DevelopmentV2
{
    public static double EffectiveAge(DevelopmentConfig d, WPerson p, int age) => age - (p.Dev.PeakAge - d.ReferencePeak);

    /// <summary>The optimistic growth still possible from an effective age: the age curve summed to its end, times the
    /// optimistic multiplier.</summary>
    public static double RemainingCapacity(DevelopmentConfig d, double effectiveAge)
    {
        double sum = 0;
        var end = d.GrowthByAge[^1][0];
        for (var a = effectiveAge; a < end; a += 1) sum += Curve.At(d.GrowthByAge, a);
        return sum * d.OptimisticMultiplier;
    }

    public static int Potential(DevelopmentConfig d, WPerson p, int overall, int age) =>
        Math.Clamp(Math.Min(p.Ceiling, overall + (int)Math.Round(RemainingCapacity(d, EffectiveAge(d, p, age)), MidpointRounding.ToEven)), overall, 99);

    public static double Environment(DevelopmentConfig d, WPerson p, DevEnvironment env)
    {
        var coach = d.CoachBase + d.CoachPerPoint * env.CoachDevelopment;
        var intensity = d.Intensity[env.Intensity.ToString()];
        var readinessAvg = d.IntensityReadiness[env.Intensity.ToString()] - d.ReadinessPerMatch * env.Matches;
        var readiness = Math.Clamp(0.5 + 0.5 * (readinessAvg - 40) / 50, 0.5, 1.0);
        var playing = d.PlayingBase + d.PlayingPerShare * Math.Clamp(env.PlayingShare, 0, 1);
        var morale = d.MoraleBase + d.MoralePerPoint * env.Morale;
        var professionalism = d.ProfessionalismBase + d.ProfessionalismPerPoint * p.Core.Mind.Professionalism;
        return Math.Clamp(coach * intensity * readiness * playing * morale * professionalism * p.Dev.Trainability,
            d.EnvironmentRange[0], d.EnvironmentRange[1]);
    }

    /// <summary>Applies one season of development to <paramref name="p"/> (skills, progress, potential) and returns the outcome.
    /// <paramref name="age"/> is the age during the season; the potential afterwards is read at age + 1.</summary>
    public static DevOutcome Season(DevelopmentConfig d, Content content, ulong seed, WPerson p, int age, int season, DevEnvironment env)
    {
        var core = p.Core;
        var overall = Ratings.Rating(core, content);
        var potential = core.Potential;
        var gap = Math.Max(0, potential - overall);
        var eff = EffectiveAge(d, p, age);
        var key = $"{season}:{p.Id}";
        var baseGrowth = Curve.At(d.GrowthByAge, eff);
        var headroom = gap / (gap + d.HeadroomHalf);
        var environment = Environment(d, p, env);

        var pBreak = eff < 25 && gap >= d.BreakthroughMinGap
            ? Math.Clamp(d.BreakthroughBase + d.BreakthroughAmbition * (core.Mind.Ambition - 50) / 50.0 + d.BreakthroughEnvironment * (environment - 1),
                d.BreakthroughRange[0], d.BreakthroughRange[1])
            : d.BreakthroughRange[0];
        var pPlateau = baseGrowth > 0 ? Math.Clamp(d.PlateauBase - d.PlateauEnvironment * (environment - 1), d.PlateauRange[0], d.PlateauRange[1]) : 0;
        var u = Draw.Unit(seed, "dev2", key, "state");
        var state = u < pBreak ? DevState.Breakthrough : u < pBreak + pPlateau ? DevState.Plateau
            : u < pBreak + pPlateau + d.SetbackChance ? DevState.Setback : DevState.Normal;

        var growth = baseGrowth * headroom * environment;
        growth = state switch
        {
            DevState.Breakthrough => growth * d.BreakthroughMultiplier + Draw.Between(seed, "dev2", key, "jump", d.BreakthroughJump[0], d.BreakthroughJump[1]) * headroom,
            DevState.Plateau => growth * d.PlateauMultiplier,
            DevState.Setback => -Draw.Between(seed, "dev2", key, "setback", d.SetbackPoints[0], d.SetbackPoints[1]),
            _ => growth
        };
        growth += d.Noise * p.Dev.Volatility * Draw.Normal(seed, "dev2", key, "noise") * (state == DevState.Setback ? 0 : headroom);
        growth = Math.Min(growth, gap);
        var professionalism = core.Mind.Professionalism;
        var decline = eff > d.DeclineStart
            ? (d.DeclineBase + d.DeclinePerYear * (eff - d.DeclineStart)) * (1.3 - d.DeclineProfessionalism * professionalism / 100.0)
              * (env.PlayingShare >= 0.5 ? d.DeclineRegularRelief : 1.0)
            : 0;
        var experience = d.ExperiencePerSeason * Math.Min(1.0, env.Matches / (double)d.ExperienceMatches);

        var skills = Skills(core.Skills);
        var weights = content.RoleWeights.Single(w => w.Role == core.Role).Weights.Select(x => x / 10_000.0).ToArray();
        var delta = new double[5];
        if (growth > 0)
        {
            var w = new double[5];
            var order = Enumerable.Range(0, 5).OrderByDescending(i => p.Dev.Profile[i]).ThenBy(i => i).ToArray();
            var weak = Enumerable.Range(0, 5).OrderBy(i => skills[i]).ThenBy(i => i).ToArray();
            for (var i = 0; i < 5; i++)
            {
                var skillCeiling = Math.Clamp(p.Ceiling + p.Dev.Profile[i] * d.SkillCeilingProfile, 1, 99);
                var room = Math.Max(d.RoomFloor, (skillCeiling - skills[i]) / d.RoomScale);
                var focus = p.Dev.Focus switch
                {
                    Focus.Specialist => order.Take(2).Contains(i) ? d.SpecialistBoost : d.OtherWhenFocused,
                    Focus.Weakness => weak.Take(2).Contains(i) ? d.SpecialistBoost : d.OtherWhenFocused,
                    _ => weights[i] * 5
                };
                w[i] = focus * p.Dev.Affinity[i] * room;
            }
            var total = w.Sum();
            for (var i = 0; i < 5; i++) delta[i] = 5 * growth * w[i] / total;
        }
        else
            for (var i = 0; i < 5; i++) delta[i] = growth;
        for (var i = 0; i < 5; i++)
            delta[i] += -5 * decline * d.DeclineSkillShares[i] + 5 * experience * d.ExperienceSkillShares[i];

        var next = new int[5];
        var progress = p.Progress;
        for (var i = 0; i < 5; i++)
        {
            var value = skills[i] + progress[i] + delta[i];
            var whole = Math.Floor(value);
            next[i] = (int)Math.Clamp(whole, 1, 99);
            progress[i] = next[i] == whole ? value - whole : 0;
        }
        core = core with { Skills = new Skills(next[0], next[1], next[2], next[3], next[4]) };
        // The Overall never exceeds the potential: take back the largest gains first.
        var guard = 0;
        while (Ratings.Rating(core, content) > potential && guard++ < 500)
        {
            var i = Enumerable.Range(0, 5).Where(k => next[k] > skills[k]).OrderByDescending(k => next[k] - skills[k]).ThenBy(k => k).FirstOrDefault(-1);
            if (i < 0) i = Enumerable.Range(0, 5).OrderByDescending(k => weights[k]).ThenBy(k => k).First(k => next[k] > 1);
            next[i]--; progress[i] = 0;
            core = core with { Skills = new Skills(next[0], next[1], next[2], next[3], next[4]) };
        }
        var after = Ratings.Rating(core, content);
        p.Core = core;
        var newPotential = Potential(d, p, after, age + 1);
        p.Core = core with { Potential = newPotential };
        p.MaxOverall = Math.Max(p.MaxOverall, after);
        if (state == DevState.Breakthrough) p.Breakthroughs++;
        if (state == DevState.Plateau) p.Plateaus++;
        if (state == DevState.Setback) p.Setbacks++;
        return new DevOutcome(overall, after, potential, newPotential, state, environment);
    }

    public static int[] Skills(Skills s) => [s.Mechanics, s.EarlyGame, s.Teamfight, s.Macro, s.ChampionPool];
}

/// <summary>The production model (rules v4: <see cref="Careers.Change"/> and <see cref="Careers.Develop"/>), driven the same
/// way, as the baseline the experiment is compared with.</summary>
public static class ProductionDevelopment
{
    public static Person Season(Content content, ulong seed, Person p, int season, int coachDevelopment, int playedPercent) =>
        Careers.Develop(p, Careers.Change(seed, p, season, content.Lifecycle, coachDevelopment, playedPercent), content);
}
