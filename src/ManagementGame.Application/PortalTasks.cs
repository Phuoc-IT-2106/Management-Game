using System.Collections.Immutable;
using ManagementGame.Domain;
using ManagementGame.Formatting;

namespace ManagementGame.Application;

public enum TaskUrgency { Blocking, Due, Open }
public enum DecisionCategory { Competition, Squad, Contract, Staff, Commercial, Finance }
/// <summary>What the decision is, as a type: presentation chooses art and wording from this, never from text or IDs.</summary>
public enum DecisionKind { Distress, MissingRole, MatchPreparation, SponsorOffer, PlayerRenewal, CoachRenewal, ProposalPending, FreeSponsorSlot, RosterShortfall }
/// <summary>A decision the Portal surfaces; Section names the shell area that resolves it and TargetId the stable record
/// it concerns; SubjectId is the stable ID of the brand or person it is about (empty when none). Subject and Why are
/// presentation text; navigation never reads them.</summary>
public sealed record PortalTask(string Id, string Section, TaskUrgency Urgency, string Text, int? DueDay,
    DecisionCategory Category = DecisionCategory.Competition, string Subject = "", string Why = "", string TargetId = "",
    DecisionKind Kind = DecisionKind.MatchPreparation, string SubjectId = "");

public static class ShellSections
{
    public const string Portal = "portal", Inbox = "inbox", Squad = "squad", Competition = "competition",
        Commercial = "commercial", Finance = "finance", Staff = "staff", Company = "company";
    /// <summary>Inbox categories resolve in the section that owns the affected record.</summary>
    public static string ForInbox(string kind) => kind switch
    {
        "Offer" or "Negotiation" or "Market" or "Commercial" => Commercial,
        "Contract" or "Renewal" or "League" => Squad,
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
            tasks.Add(new("distress", ShellSections.Finance, TaskUrgency.Blocking, "Financial distress: obligations are overdue", v.Day,
                DecisionCategory.Finance, "Overdue obligations", "Financial distress until the arrears are paid", Kind: DecisionKind.Distress));
        // A role is covered by a player whose primary or secondary role it is.
        foreach (var role in Roles.All.Where(r => !v.People.Any(p => p.Role == r || p.SecondaryRole == r)))
            tasks.Add(new("role:" + role, ShellSections.Squad, TaskUrgency.Blocking, $"No player for {Roles.Name(role)}: matches will be forfeited", v.NextMatchDay == 0 ? null : v.NextMatchDay,
                DecisionCategory.Squad, Roles.Name(role), "No player: matches will be forfeited", "role:" + role, DecisionKind.MissingRole));
        // The league's registered-roster minimum: below it every match owes its prize as a fine (rules v3).
        if (v.MinimumRoster > 0 && v.People.Length < v.MinimumRoster)
            tasks.Add(new("roster", ShellSections.Squad, TaskUrgency.Blocking, $"Registered roster {v.People.Length} of {v.MinimumRoster}: each match costs its prize as a fine",
                v.NextMatchDay == 0 ? null : v.NextMatchDay, DecisionCategory.Squad, "League roster minimum",
                $"{v.People.Length} of {v.MinimumRoster} players registered: each match owes its prize as a fine", "roster", DecisionKind.RosterShortfall));
        if (v.NextFixtureId.Length > 0 && v.CommittedPlan is null && v.Delegation != Delegation.Autonomous)
            tasks.Add(new("prepare:" + v.NextFixtureId, ShellSections.Competition, v.NextMatchDay == v.Day ? TaskUrgency.Blocking : TaskUrgency.Due,
                $"Prepare for {v.Opponent}", v.NextMatchDay, DecisionCategory.Competition, v.Opponent, "No match plan committed", v.NextFixtureId, DecisionKind.MatchPreparation, v.OpponentId));
        foreach (var offer in v.Offers.Where(o => o.Availability == "Available").OrderBy(o => o.Deadline).ThenBy(o => o.Id, StringComparer.Ordinal))
            tasks.Add(new("offer:" + offer.Id, ShellSections.Commercial, TaskUrgency.Due, $"Answer {offer.Name}'s offer", offer.Deadline,
                DecisionCategory.Commercial, offer.Name, $"Offers {Currency.Format(offer.Payment)} a week for {Figures.Count(offer.DurationDays / 7)} weeks", offer.Id, DecisionKind.SponsorOffer, offer.BrandId));
        foreach (var person in v.People.Where(p => p.CanRenew).OrderBy(p => p.ContractEnd))
            tasks.Add(new("renew:" + person.Id, ShellSections.Squad, TaskUrgency.Due, $"Renew or let {person.Name} go", person.ContractEnd,
                DecisionCategory.Contract, person.Name, "Contract ends: renew or let them go", person.Id, DecisionKind.PlayerRenewal, person.Id));
        if (v.CoachContract.CanRenew)
            tasks.Add(new("renew:" + v.CoachContract.Id, ShellSections.Staff, TaskUrgency.Due, $"Renew or replace head coach {v.CoachContract.Name}", v.CoachContract.ContractEnd,
                DecisionCategory.Staff, v.CoachContract.Name, "Head coach contract ends: renew or replace", v.CoachContract.Id, DecisionKind.CoachRenewal, v.CoachContract.Id));
        foreach (var n in v.Negotiations)
            tasks.Add(new("negotiation:" + n.Id, ShellSections.Commercial, TaskUrgency.Open, $"Awaiting {n.Brand}'s answer", n.ResponseDay,
                DecisionCategory.Commercial, n.Brand, "Your proposal is under review", n.Id, DecisionKind.ProposalPending, n.BrandId));
        if (v.ActiveSponsors < v.SponsorSlots && !v.Offers.Any(o => o.Availability == "Available") && v.Negotiations.IsEmpty
            && v.Brands.Any(b => b.Availability.StartsWith("Open", StringComparison.Ordinal)))
            tasks.Add(new("slot", ShellSections.Commercial, TaskUrgency.Open, "A sponsor slot is free: approach a brand", null,
                DecisionCategory.Commercial, "Free sponsor slot", "Approach a brand to fill it", "slot", DecisionKind.FreeSponsorSlot));
        return [.. tasks.OrderBy(t => t.Urgency).ThenBy(t => t.DueDay ?? int.MaxValue).ThenBy(t => t.Id, StringComparer.Ordinal)];
    }
    /// <summary>The Continue control is redirected, not hidden, when a blocking task must be resolved first.</summary>
    public static PortalTask? Blocker(Situation v) => Build(v).FirstOrDefault(t => t.Urgency == TaskUrgency.Blocking && t.Section == ShellSections.Competition);
}
