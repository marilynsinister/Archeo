namespace Archeo.Core;

public sealed class RouteProgress
{
    public bool Initialized { get; set; }
    public bool LegacyFrontierIndex { get; set; }
    public Dictionary<string,string> Clues { get; set; } = new();
    public HashSet<string> Explained { get; set; } = new();
}

public sealed record RouteStory(string Id, string Trigger, string Npc);

/// <summary>Field routes follow recorded finds and conversations; existing routes never close.</summary>
public static class RouteStories
{
    public static readonly RouteStory[] All =
    {
        new("sea", "brass_gear", "Willy"),
        new("calico", "clay_whistle", "Gunther"),
        new("island", "sailor_buckle", "Willy")
    };

    public static void Initialize(ArchaeologyState state)
    {
        if (state.Routes.Initialized) return;
        foreach (var story in All.Where(p => state.Journey.Chapters.Contains(p.Id))) state.Routes.Explained.Add(story.Id);
        state.Routes.LegacyFrontierIndex = state.Journey.Chapters.Contains("frontiers");
        state.Routes.Initialized = true;
    }

    public static bool AllowsField(ArchaeologyState state, string culture)
        => !state.Routes.Initialized || !All.Any(p => p.Id == culture) || state.Routes.Explained.Contains(culture);

    private static bool Eligible(ArchaeologyState state, string id) => state.Journey.Started && !OpeningStory.Active(state) && (id switch
    {
        "sea" => Journey.IsOpen(state,"dwarf"),
        "calico" => Journey.IsOpen(state,"sea") && state.Frontiers.DesertAccess,
        "island" => Journey.IsOpen(state,"sea") && state.Frontiers.IslandAccess,
        _ => false
    });

    public static void Discover(ArchaeologyState state)
    {
        if (!state.Routes.Initialized) return;
        foreach (var story in All)
            if (!state.Routes.Explained.Contains(story.Id) && Eligible(state,story.Id) && state.Restored.GetValueOrDefault(story.Trigger) > 0)
                state.Routes.Clues.TryAdd(story.Id,story.Trigger);
    }

    public static RouteStory? Pending(ArchaeologyState state)
        => All.FirstOrDefault(p => state.Routes.Clues.ContainsKey(p.Id) && !state.Routes.Explained.Contains(p.Id));

    public static List<RouteStory> Visible(ArchaeologyState state)
    {
        string? current = Pending(state)?.Id;
        return All.Where(p => state.Routes.Clues.ContainsKey(p.Id) && (state.Routes.Explained.Contains(p.Id) || p.Id == current)).ToList();
    }

    public static bool Explain(ArchaeologyState state, string id, bool nearPartner)
    {
        var story = Pending(state);
        if (!nearPartner || story?.Id != id || !Eligible(state,id) || state.Restored.GetValueOrDefault(story.Trigger) < 1) return false;
        state.Routes.Explained.Add(id);
        state.Journey.Chapters.Add(id);
        return true;
    }

    public static bool IsValid(RouteProgress? progress)
        => progress != null && progress.Clues != null && progress.Explained != null
            && progress.Clues.All(p => All.Any(s => s.Id == p.Key && s.Trigger == p.Value))
            && progress.Explained.All(id => All.Any(p => p.Id == id))
            && (progress.Initialized || progress.Clues.Count == 0 && progress.Explained.Count == 0 && !progress.LegacyFrontierIndex);
}
