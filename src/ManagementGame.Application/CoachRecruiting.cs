using System.Globalization;
using ManagementGame.Domain;

namespace ManagementGame.Application;

/// <summary>The coach's recruitment for one day: a signing and, for an upgrade, the player it displaces (released first).</summary>
public sealed record CoachMove(SigningDecision Signing, ReleaseDecision? Release);

/// <summary>Bounded delegation of recruitment (DEC-014). An autonomous head coach may sign free agents inside the envelope
/// the owner set: a signing fee and a daily wage no larger than the limits, and a fee the company can pay now. The coach
/// works from the same observation the owner sees: candidates by their rating estimate (the band's midpoint), never their
/// true rating. In priority order the coach:
/// <list type="number">
/// <item>covers a role no player of the squad has as their own;</item>
/// <item>keeps the registered roster at the league minimum;</item>
/// <item>upgrades at most once every <see cref="UpgradeInterval"/> days: a free agent whose estimate clears the best
/// player of the squad's weakest role by <see cref="UpgradeMargin"/>, paid from no more than half the cash. The upgrade
/// displaces the weakest player of that role who can be released (on a full roster, the squad's weakest such player), when
/// the exit cost also fits the fee limit; on a full roster without such a player there is no upgrade.</item>
/// </list>
/// The coach never renews, never recruits while the company is in distress or restructuring, and signs once a day at most.</summary>
public static class CoachRecruiting
{
    public const int UpgradeMargin = 4, UpgradeInterval = 28;
    public const string CausePrefix = "coach-recruit:";

    /// <summary>The coach's next move, or null. <paramref name="lastSigning"/> is the day of the coach's latest signing.</summary>
    public static CoachMove? Next(Situation v, int lastSigning)
    {
        if (v.Delegation != Delegation.Autonomous || v.RecruitFee <= 0 || v.RecruitWage <= 0) return null;
        if (v.Status is nameof(RecoveryStage.Distress) or nameof(RecoveryStage.Restructuring)) return null;
        var full = v.MaxRoster > 0 && v.People.Length >= v.MaxRoster;
        var allowed = v.Candidates.Where(c => c.Fee <= v.RecruitFee && c.Salary <= v.RecruitWage && c.Fee <= v.Cash).ToArray();
        CandidateRow? Best(IEnumerable<CandidateRow> pool) => pool.OrderByDescending(c => Midpoint(c.RatingEstimate)).ThenBy(c => c.Id, StringComparer.Ordinal).FirstOrDefault();
        if (!full)
        {
            // 1. A role nobody in the squad plays as their own.
            foreach (var role in Roles.All.Where(r => !v.People.Any(p => p.Role == r)))
                if (Best(allowed.Where(c => c.Role == role)) is { } fill) return new(new SigningDecision(fill.Id), null);
            // 2. The league's registered-roster minimum: the best affordable player for the thinnest role.
            if (v.People.Length < v.MinimumRoster
                && allowed.OrderBy(c => v.People.Count(p => p.Role == c.Role)).ThenByDescending(c => Midpoint(c.RatingEstimate)).ThenBy(c => c.Id, StringComparer.Ordinal).FirstOrDefault() is { } depth)
                return new(new SigningDecision(depth.Id), null);
        }
        // 3. One upgrade per interval, for the weakest role, from no more than half the cash.
        if (v.Day - lastSigning < UpgradeInterval || v.People.Length == 0) return null;
        var weakest = Roles.All.Where(r => v.People.Any(p => p.Role == r)).Select(r => (Role: r, Rating: v.People.Where(p => p.Role == r).Max(p => p.Rating)))
            .OrderBy(x => x.Rating).ThenBy(x => Roles.All.IndexOf(x.Role)).FirstOrDefault();
        if (weakest.Role is null) return null;
        var upgrade = Best(allowed.Where(c => c.Role == weakest.Role && c.Fee * 2 <= v.Cash && Midpoint(c.RatingEstimate) >= weakest.Rating + UpgradeMargin));
        if (upgrade is null) return null;
        static IOrderedEnumerable<PersonRow> Weakest(IEnumerable<PersonRow> people) => people.OrderBy(p => p.Rating).ThenBy(p => p.Id, StringComparer.Ordinal);
        var displaced = Weakest(v.People.Where(p => p.Role == weakest.Role && p.CanRelease)).FirstOrDefault()
            ?? (full ? Weakest(v.People.Where(p => p.CanRelease)).FirstOrDefault() : null);
        var release = displaced is not null && displaced.ExitCost <= v.RecruitFee && (upgrade.Fee + displaced.ExitCost) * 2 <= v.Cash ? displaced : null;
        if (full && release is null) return null;
        return new(new SigningDecision(upgrade.Id), release is null ? null : new ReleaseDecision(release.Id));
    }

    /// <summary>The midpoint of an estimate band such as "61–77 (estimate)"; 0 when the text holds no band.</summary>
    public static int Midpoint(string band)
    {
        var parts = band.Split(['–', ' '], StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var low)
            && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var high) ? (low + high) / 2 : 0;
    }
}
