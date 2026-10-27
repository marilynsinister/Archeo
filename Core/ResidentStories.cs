namespace Archeo.Core;

public enum ResidentStage { Locked, Clue, Searching, Recovered, Reward, Complete }

public sealed class ResidentProgress
{
    public Dictionary<string, ResidentStage> Stages { get; set; } = new();
}

public sealed record ResidentStory(string Id, string Npc, string Trigger, string Artifact, string Culture, string Location)
{
    public string SiteKind => "resident_" + Id;
    public string SiteKey => "resident:" + Id + ":cache";
}

/// <summary>Personal stories use discovery credit, persistent caches and one-time conversation rewards.</summary>
public static class ResidentStories
{
    public const int Friendship = 100;
    public const int Experience = 100;
    public const int Fragments = 12;
    public static readonly ResidentStory[] All =
    {
        new("penny", "Penny", "rune_tablet", "reader_seal", "dwarf", "Town"),
        new("evelyn", "Evelyn", "copper_brooch", "garden_locket", "valley", "Forest"),
        new("willy", "Willy", "clay_whistle", "harbor_whistle", "sea", "Beach")
    };
    public static readonly string[] Sources = All.Select(p => p.Id).ToArray();
    public static readonly string[] Kinds = All.Select(p => p.SiteKind).ToArray();

    public static ResidentStage Stage(ResidentProgress state, string id) => state.Stages.GetValueOrDefault(id);

    public static List<string> Discover(ResidentProgress state, IReadOnlyDictionary<string, int> restored)
    {
        var discovered = new List<string>();
        foreach (var story in All)
        {
            if (Stage(state,story.Id) != ResidentStage.Locked || !restored.TryGetValue(story.Trigger,out int count) || count < 1) continue;
            state.Stages[story.Id] = ResidentStage.Clue;
            discovered.Add(story.Id);
        }
        return discovered;
    }

    public static bool Start(ResidentProgress state, string id, bool nearNpc)
    {
        if (!nearNpc || !Sources.Contains(id) || Stage(state,id) != ResidentStage.Clue) return false;
        state.Stages[id] = ResidentStage.Searching;
        return true;
    }

    public static bool ValidSite(DigSite site)
    {
        var story = All.FirstOrDefault(p => p.SiteKind == site.EventKind);
        return story != null && site.EventId == story.SiteKey && site.Culture == story.Culture && site.Location == story.Location
            && site.Expires == int.MaxValue && site.Points.Count == 2 && site.Points.All(p => p.X >= 0 && p.Y >= 0)
            && site.Points.Select(p => (p.X,p.Y)).Distinct().Count() == 2;
    }

    public static bool CompleteSite(ResidentProgress state, DigSite site)
    {
        var story = All.FirstOrDefault(p => p.SiteKind == site.EventKind);
        if (story == null || Stage(state,story.Id) != ResidentStage.Searching || !ValidSite(site) || site.Points.Any(p => !p.Dug)) return false;
        state.Stages[story.Id] = ResidentStage.Recovered;
        return true;
    }

    public static bool Share(ResidentProgress state, string id, bool nearNpc)
    {
        if (!nearNpc || !Sources.Contains(id) || Stage(state,id) != ResidentStage.Recovered) return false;
        state.Stages[id] = ResidentStage.Reward;
        return true;
    }

    public static bool PendingArtifact(ResidentProgress state, string artifact)
        => All.Any(p => p.Artifact == artifact && Stage(state,p.Id) == ResidentStage.Reward);

    public static bool RecordClaim(ArchaeologyState state, string id, bool inventorySpace)
    {
        var story = All.FirstOrDefault(p => p.Id == id);
        if (!inventorySpace || story == null || Stage(state.Residents,id) != ResidentStage.Reward) return false;
        state.Residents.Stages[id] = ResidentStage.Complete;
        state.Restored[story.Artifact] = state.Restored.GetValueOrDefault(story.Artifact) + 1;
        return true;
    }

    public static bool IsValid(ResidentProgress? state)
        => state?.Stages != null && state.Stages.All(p => Sources.Contains(p.Key) && Enum.IsDefined(typeof(ResidentStage),p.Value));
}
