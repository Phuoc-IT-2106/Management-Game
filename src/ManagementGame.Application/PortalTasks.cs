using System.Collections.Immutable;

namespace ManagementGame.Application;

public enum TaskUrgency { Blocking, Due, Open }
/// <summary>A decision the Portal surfaces; Section names the shell area that resolves it.</summary>
public sealed record PortalTask(string Id, string Section, TaskUrgency Urgency, string Text, int? DueDay);

public static class ShellSections
{
    public const string Portal = "portal", Inbox = "inbox", Squad = "squad", Competition = "competition",
        Commercial = "commercial", Finance = "finance", Staff = "staff", Company = "company";
    /// <summary>Inbox categories resolve in the section that owns the affected record.</summary>
    public static string ForInbox(string kind) => kind switch
    {
        "Offer" or "Negotiation" or "Market" or "Commercial" => Commercial,
        "Contract" or "Renewal" => Squad,
        "Match" or "Season" or "Meta" => Competition,
        _ => Portal
    };
}

/// <summary>Derives the Portal task strip from the same observation the player sees; it never decides anything.</summary>
public static class PortalTasks
{
    public static ImmutableArray<PortalTask> Build(Situation v)
    {
        var tasks = new List<PortalTask>();
        if (v.Status == "Distress")
            tasks.Add(new("distress", ShellSections.Finance, TaskUrgency.Blocking, "Financial distress: obligations are overdue", v.Day));
        foreach (var role in "ABCDE".Select(r => r.ToString()).Where(r => !v.People.Any(p => p.Role == r)))
            tasks.Add(new("role:" + role, ShellSections.Squad, TaskUrgency.Blocking, $"No player for role {role}: matches will be forfeited", v.NextMatchDay == 0 ? null : v.NextMatchDay));
        if (v.NextFixtureId.Length > 0 && v.CommittedPlan is null && v.Delegation != Delegation.Autonomous)
            tasks.Add(new("prepare:" + v.NextFixtureId, ShellSections.Competition, v.NextMatchDay == v.Day ? TaskUrgency.Blocking : TaskUrgency.Due,
                $"Prepare for {v.Opponent}", v.NextMatchDay));
        foreach (var offer in v.Offers.Where(o => o.Availability == "Available").OrderBy(o => o.Deadline).ThenBy(o => o.Id, StringComparer.Ordinal))
            tasks.Add(new("offer:" + offer.Id, ShellSections.Commercial, TaskUrgency.Due, $"Answer {offer.Name}'s offer", offer.Deadline));
        foreach (var person in v.People.Where(p => p.CanRenew).OrderBy(p => p.ContractEnd))
            tasks.Add(new("renew:" + person.Id, ShellSections.Squad, TaskUrgency.Due, $"Renew or let {person.Name} go", person.ContractEnd));
        if (v.CoachContract.CanRenew)
            tasks.Add(new("renew:" + v.CoachContract.Id, ShellSections.Staff, TaskUrgency.Due, $"Renew or replace head coach {v.CoachContract.Name}", v.CoachContract.ContractEnd));
        foreach (var n in v.Negotiations)
            tasks.Add(new("negotiation:" + n.Id, ShellSections.Commercial, TaskUrgency.Open, $"Awaiting {n.Brand}'s answer", n.ResponseDay));
        if (v.ActiveSponsors < v.SponsorSlots && !v.Offers.Any(o => o.Availability == "Available") && v.Negotiations.IsEmpty
            && v.Brands.Any(b => b.Availability.StartsWith("Open", StringComparison.Ordinal)))
            tasks.Add(new("slot", ShellSections.Commercial, TaskUrgency.Open, "A sponsor slot is free: approach a brand", null));
        return [.. tasks.OrderBy(t => t.Urgency).ThenBy(t => t.DueDay ?? int.MaxValue).ThenBy(t => t.Id, StringComparer.Ordinal)];
    }
    /// <summary>The Continue control is redirected, not hidden, when a blocking task must be resolved first.</summary>
    public static PortalTask? Blocker(Situation v) => Build(v).FirstOrDefault(t => t.Urgency == TaskUrgency.Blocking && t.Section == ShellSections.Competition);
}
