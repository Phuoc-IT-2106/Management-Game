using ManagementGame.Application;
using ManagementGame.Domain;
using ManagementGame.Infrastructure;
public static class Composition
{
    public static IGameSession Create(string contentPath, string saveDirectory, ulong seed, string? companyName = null)
    {
        var content = ContentLoader.Load(contentPath);
        return new Session(content.Definition, CampaignFactory.Create(content.Definition, content.Hash, seed, "campaign:development", companyName), new SnapshotStore(saveDirectory, content));
    }
    public static string Hash(IGameSession session) => Canonical.Hash(((Session)session).Capture());
    public static long LastUiCommand(IGameSession session) => ((Session)session).Capture().Execution.Receipts
        .Where(r => r.Id.StartsWith("ui:", StringComparison.Ordinal)).Select(r => long.TryParse(r.Id[3..], out var value) ? value : 0).DefaultIfEmpty().Max();
}
