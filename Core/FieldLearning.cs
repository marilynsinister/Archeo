namespace Archeo.Core;

public sealed class LearningProgress
{
    public bool Initialized { get; set; }
    public HashSet<string> Events { get; set; } = new();
    public HashSet<string> Announced { get; set; } = new();
}

/// <summary>Lessons follow actual discoveries; reading never advances or rewards gameplay.</summary>
public static class FieldLearning
{
    public static readonly string[] Lessons = { "welcome", "search", "signal", "dig", "fragments", "keystones", "restore", "relics", "chapters", "weather", "nature", "workshop", "residents", "shipwreck", "gallery", "museum", "mastery", "frontiers", "expeditions", "homecoming", "routes" };
    private static readonly string[] Events = { "survey", "marked", "fragments", "keystone", "site" };

    public static void Observe(ArchaeologyState state)
    {
        var events = state.Learning.Events;
        if (state.Finds > 0 || state.Cultures.Values.Any(p => p.Fragments > 0 || p.Solves > 0)) events.Add("fragments");
        if (state.Sites.Any(p => p.Points.Any(n => n.Revealed || n.Dug))) { events.Add("survey"); events.Add("marked"); }
        if (state.Sites.Any(p => p.Points.Count > 0 && p.Points.All(n => n.Dug))) events.Add("site");
        if (state.Cultures.Values.Any(p => p.Keystones > 0) || state.Donated.Count > 0) events.Add("keystone");
    }

    public static void Initialize(ArchaeologyState state, IReadOnlyList<ArtifactDefinition> catalog)
    {
        Observe(state);
        if (state.Learning.Initialized) return;
        // Existing players can reread everything without receiving a backlog of introductory messages.
        state.Learning.Announced.UnionWith(Available(state, catalog));
        state.Learning.Initialized = true;
    }

    public static bool Ready(ArchaeologyState state, IReadOnlyList<ArtifactDefinition> catalog)
        => catalog.Any(a => a.Source == "research" && Journey.IsOpen(state, a.Culture)
            && state.Cultures.TryGetValue(a.Culture, out var p) && p.Project == a.Id
            && p.Fragments >= Research.Required(a, p, p.Keystones > 0, state));

    public static string Current(ArchaeologyState state, IReadOnlyList<ArtifactDefinition> catalog)
    {
        if (!state.Journey.Started) return "welcome";
        if (state.Opening.Stage == OpeningStage.Restored) return "homecoming";
        if (RouteStories.Pending(state) != null) return "routes";
        if (!state.Learning.Events.Contains("fragments"))
            return state.Learning.Events.Contains("marked") ? "dig" : state.Learning.Events.Contains("survey") ? "signal" : "search";
        if (state.Restored.Count == 0) return Ready(state, catalog) ? "restore" : "fragments";
        return state.Restored.Values.Sum(p => (long)p) == 1 && state.Donated.Count == 0 ? "relics" : "chapters";
    }

    public static List<string> Available(ArchaeologyState state, IReadOnlyList<ArtifactDefinition> catalog)
    {
        var result = new List<string> { "welcome" };
        if (!state.Journey.Started) return result;
        result.Add("search");
        if (state.Learning.Events.Contains("survey")) result.Add("signal");
        if (state.Learning.Events.Contains("marked")) result.Add("dig");
        if (state.Learning.Events.Contains("fragments")) result.Add("fragments");
        if (state.Learning.Events.Contains("keystone")) result.Add("keystones");
        if (Ready(state, catalog) || state.Restored.Count > 0 || state.Learning.Announced.Contains("restore")) result.Add("restore");
        if (state.Opening.Stage is OpeningStage.Restored or OpeningStage.Complete) result.Add("homecoming");
        if (state.Restored.Count > 0 && !OpeningStory.Active(state)) { result.Add("relics"); result.Add("chapters"); }
        foreach (string chapter in Lessons.Skip(9))
            if (Journey.IsOpen(state, chapter)) result.Add(chapter);
        return result;
    }

    public static bool IsValid(LearningProgress? progress)
        => progress != null && progress.Events != null && progress.Announced != null
            && progress.Events.All(Events.Contains) && progress.Announced.All(Lessons.Contains);

    public static string? TakeNotice(ArchaeologyState state, IReadOnlyList<ArtifactDefinition> catalog)
    {
        var available = Available(state, catalog);
        string current = Current(state, catalog);
        // Skip old instructions if the player has already performed the next action.
        state.Learning.Announced.UnionWith(available.Where(p => p != current
            && (p is "search" or "signal" or "dig" or "fragments" || p == "restore" && state.Restored.Count > 0)));
        string? lesson = !state.Learning.Announced.Contains(current) ? current
            : available.FirstOrDefault(p => !state.Learning.Announced.Contains(p));
        if (lesson != null) state.Learning.Announced.Add(lesson);
        return lesson;
    }
}
