using System.Text.Json;

namespace WorldExperiment;

/// <summary>The experiment's configuration: every coefficient is read from a versioned JSON file
/// (config/world-experiment.v1.json), never from production content. Nothing here is approved balance.</summary>
public sealed record ExperimentConfig(string Version, string Status, ulong Seed, int SeasonDays, PopulationConfig Population,
    WorldConfig World, DevelopmentConfig Development, MarketConfig Market, EconomyConfig Economy)
{
    public static ExperimentConfig Load(string path) =>
        JsonSerializer.Deserialize<ExperimentConfig>(File.ReadAllBytes(path)) ?? throw new InvalidDataException("Empty config.");
}

public sealed record VW(int Value, int Weight);
public sealed record TierWeight(string Tier, int Weight);
public sealed record IdWeight(string Id, int Weight);
public sealed record ProfileConfig(string Id, int Weight, TierWeight[] Tiers, string AgeTier, int AgeMin, int AgeMax);

public sealed record PopulationConfig(int NpcCount, int InitialOverallCap, int OverallFloor, int FreeAgentTargetPercent,
    int FreeAgentHardLimitPercent, ProfileConfig[] Profiles, VW[] ProspectAges, VW[] YoungShift, double[][] HeadroomByAge,
    double[][] TalentQuantiles, double CeilingSoftStart, double CeilingSoftRoom, int LateBloomerPercent, int[] LateBloomerAges, int[] LateBloomerExtra, double[] LateBloomerPeak,
    double[] PeakAge, double[] Volatility, double[] Trainability, IdWeight[] Focus, string[] HandleBlocklist,
    double[]? CeilingTail = null);

public sealed record TeamNameConfig(string[] Adjectives, string[] Nouns, string[] Suffixes);
public sealed record WorldConfig(int MaxSquad, int FictionalProLeagues, int FictionalProTeamsPerLeague, int[] FictionalProSquad,
    int[] AcademySquad, int AcademySeniorRegistrationPercent, int AcademySeniorRegistrationMax, int SemiProLeagues,
    int SemiProTeamsPerLeague, int[] SemiProSquad, int[] AmateurSquad, int AmateurCircuitSize, int RealTeamNpcTargetSquad,
    int RealTeamNpcMax, int RealTeamNpcChancePercent, bool RealPlayersOnFictionalTeams, string RealStarterPolicy, double StarterYouthWeight,
    Dictionary<string, int[]> Strategies,
    Dictionary<string, int[]> CoachDevelopment, TeamNameConfig TeamNames);

public sealed record DevelopmentConfig(string Version, double[][] GrowthByAge, double ReferencePeak, double HeadroomHalf,
    double OptimisticMultiplier, double CoachBase, double CoachPerPoint, int FreeAgentCoach, Dictionary<string, double> Intensity,
    Dictionary<string, double> IntensityReadiness, double ReadinessPerMatch, double PlayingBase, double PlayingPerShare,
    double MoraleBase, double MoralePerPoint, double ProfessionalismBase, double ProfessionalismPerPoint, double[] EnvironmentRange,
    double BreakthroughBase, double BreakthroughAmbition, double BreakthroughEnvironment, double[] BreakthroughRange,
    double BreakthroughMultiplier, double[] BreakthroughJump, int BreakthroughMinGap, double PlateauBase, double PlateauEnvironment,
    double[] PlateauRange, double PlateauMultiplier, double SetbackChance, double[] SetbackPoints, double Noise, double DeclineStart,
    double DeclineBase, double DeclinePerYear, double DeclineProfessionalism, double DeclineRegularRelief, double[] DeclineSkillShares,
    double ExperiencePerSeason, int ExperienceMatches, double[] ExperienceSkillShares, double SpecialistBoost, double OtherWhenFocused,
    double RoomScale, double RoomFloor, double SkillCeilingProfile);

public sealed record MarketConfig(Dictionary<string, int[]> ContractSeasons, double[][] RetirementByAge,
    Dictionary<string, double> RetirementLayer, double RetirementPerFreeAgentSeason, int RetirementRatingPivot,
    double RetirementRatingPercent, double[] RetirementRatingRange, string IntakeMode, int Intake, int[] IntakeRange, VW[] IntakeAges, TierWeight[] IntakeTiers,
    Dictionary<string, double> ScoutingNoise, int UpgradeMargin, int MaxUpgrades, Dictionary<string, int> Rotation,
    int AcademyGraduationAge, int AcademyPromotionMinimum, int DismissalProfessionalism, int DismissalBpPerPoint,
    int AmbitionDeparture, Dictionary<string, int> LayerCeiling, int BenchDepartureAmbition, double BenchShare, int AspirationSemi,
    int AspirationPro, int AspirationPatienceSeasons, double ReservationShare, double ReservationDecayPerSeason,
    Dictionary<string, int> DissolutionBp, int DebtDissolutionBp,
    double UnemploymentWarningPercent, int ReleaseOverTarget);

public sealed record EconomyConfig(Dictionary<string, long> Revenue, Dictionary<string, double> WageFactor,
    Dictionary<string, double> FeeFactor, double WageShare, double StartingFunds, double ChampionBonus, double ReleaseShare);

/// <summary>Piecewise-linear interpolation over [x, y] pairs sorted by x; flat beyond both ends.</summary>
public static class Curve
{
    public static double At(double[][] points, double x)
    {
        if (x <= points[0][0]) return points[0][1];
        for (var i = 1; i < points.Length; i++)
            if (x <= points[i][0])
            {
                var (x0, y0, x1, y1) = (points[i - 1][0], points[i - 1][1], points[i][0], points[i][1]);
                return y0 + (x - x0) * (y1 - y0) / (x1 - x0);
            }
        return points[^1][1];
    }
    /// <summary>A step table [[from, value], ...]: the value of the last row whose first element is at most x.</summary>
    public static double Step(double[][] rows, double x)
    {
        var value = rows[0][1];
        foreach (var row in rows) if (x >= row[0]) value = row[1];
        return value;
    }
}
