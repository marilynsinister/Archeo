namespace Archeo.Core;

public sealed class JourneyProgress
{
    public bool Initialized { get; set; }
    public bool Started { get; set; }
    public int InstrumentLevel { get; set; } = 1;
    public HashSet<string> Chapters { get; set; } = new();
}

/// <summary>Discovery unlocks are monotonic: old discoveries remain available without revealing future stories.</summary>
public static class Journey
{
    public static readonly string[] Chapters = { "valley", "opening", "routes", "dwarf", "sea", "calico", "island", "frontiers", "catalog", "workshop", "weather", "nature", "gallery", "museum", "shipwreck", "residents", "expeditions", "mastery" };
    public static void Initialize(ArchaeologyState state, IReadOnlyList<ArtifactDefinition> catalog, bool ownsEquipment = false)
    {
        if (!state.Journey.Initialized)
        {
            state.Journey.Started |= ownsEquipment || state.Finds > 0 || state.Experience > 0 || state.Restored.Count > 0
                || state.Cultures.Values.Any(p => p.Fragments > 0 || p.Solves > 0 || p.Keystones > 0)
                || state.Workshop.Stage != WorkshopStage.Undiscovered || state.Shipwreck.Stage != ShipwreckStage.Undiscovered
                || state.Residents.Stages.Count > 0 || state.CollectionRewards.Count > 0 || state.Weather.Lead != null || state.Weather.Completed.Count > 0
                || state.Weather.PendingRewards.Count > 0 || state.Nature.Leads.Count > 0 || state.Nature.Completed.Count > 0 || state.Nature.PendingRewards.Count > 0
                || state.Sites.Any(p => p.EventKind != "");
            state.Journey.Initialized = true;
        }
        OpeningStory.Initialize(state, state.Journey.Started);
        if (!state.Journey.Started) RouteStories.Initialize(state);
        Refresh(state, catalog);
    }

    public static void Refresh(ArchaeologyState state, IReadOnlyList<ArtifactDefinition> catalog)
    {
        if (!state.Journey.Started) return;
        OpeningStory.Begin(state);
        var open = state.Journey.Chapters;
        open.Add("valley");
        if (OpeningStory.Introduced(state)) open.Add("opening");
        if (OpeningStory.Active(state)) return;
        foreach (var artifact in catalog.Where(p => state.Restored.ContainsKey(p.Id) && RouteStories.AllowsField(state,p.Culture))) open.Add(artifact.Culture);
        foreach (var site in state.Sites.Where(p => p.EventKind != "" && RouteStories.AllowsField(state,p.Culture))) open.Add(site.Culture);
        foreach (var artifact in catalog.Where(p => RouteStories.AllowsField(state,p.Culture) && (state.Weather.PendingRewards.Contains(p.Id) || state.Nature.PendingRewards.Contains(p.Id) || ResidentStories.PendingArtifact(state.Residents,p.Id)))) open.Add(artifact.Culture);
        foreach (var culture in state.Cultures.Where(p => RouteStories.AllowsField(state,p.Key) && (p.Value.Fragments > 0 || p.Value.Solves > 0 || p.Value.Keystones > 0))) open.Add(culture.Key);
        if (state.Restored.Count > 0 || state.Opening.Stage == OpeningStage.Complete) { open.Add("dwarf"); open.Add("catalog"); }
        if (state.Cultures.Values.Sum(p => (long)p.Solves) >= 2 && RouteStories.AllowsField(state,"sea")) open.Add("sea");
        RouteStories.Discover(state);
        if (RouteStories.Visible(state).Count > 0) open.Add("routes");
        if (state.Workshop.Stage != WorkshopStage.Undiscovered) open.Add("workshop");
        if (state.Shipwreck.Stage != ShipwreckStage.Undiscovered) open.Add("shipwreck");
        if (state.Residents.Stages.Any(p => p.Value != ResidentStage.Locked)) open.Add("residents");
        if(state.Expeditions.Stories.Values.Any(p=>p.Stage!=ExpeditionStage.Locked))open.Add("expeditions");
        if (state.Weather.Lead != null || state.Weather.Completed.Count > 0 || state.Weather.PendingRewards.Count > 0
            || state.Sites.Any(p => WeatherExpeditions.Kinds.Contains(p.EventKind))) open.Add("weather");
        if (state.Nature.Leads.Count > 0 || state.Nature.Completed.Count > 0 || state.Nature.PendingRewards.Count > 0 || state.Sites.Any(p => NaturalEvents.Kinds.Contains(p.EventKind))) open.Add("nature");
        if (state.Restored.Count >= 3 || state.CollectionRewards.Count > 0) open.Add("gallery");
        if(LivingMuseum.Unlocked(state))open.Add("museum");
        if (Research.Level(state.Experience) >= 4 || state.Mastery.Level5 != "") open.Add("mastery");
        FrontierResearch.Unlock(state);
    }

    public static bool IsOpen(ArchaeologyState state, string chapter) => state.Journey.Started && state.Journey.Chapters.Contains(chapter);
    public static bool VisibleArtifact(ArchaeologyState state, ArtifactDefinition artifact)
        => state.Journey.Started && JournalCatalog.IsKnown(state, artifact) && (IsOpen(state, artifact.Culture)
            || state.Restored.ContainsKey(artifact.Id) || state.Weather.PendingRewards.Contains(artifact.Id)
            || state.Nature.PendingRewards.Contains(artifact.Id) || ResidentStories.PendingArtifact(state.Residents,artifact.Id));
    public static bool VisibleSite(ArchaeologyState state, DigSite site)
        => state.Journey.Started && (IsOpen(state, site.Culture) || site.EventKind != "");
    public static bool IsValid(JourneyProgress? progress)
        => progress != null && progress.InstrumentLevel is >= 1 and <= 3 && progress.Chapters != null
            && progress.Chapters.All(Chapters.Contains) && (progress.Started || progress.InstrumentLevel == 1 && progress.Chapters.Count == 0);
    public static (int Level, string Material, int Count, int Gold) UpgradeCost(int tier)
        => tier switch { 1 => (3, "334", 5, 250), 2 => (5, "335", 5, 1000), _ => (10, "", 0, 0) };
    public static bool CanUpgrade(ArchaeologyState state, bool inMuseum, int materials, int gold)
    {
        var cost = UpgradeCost(state.Journey.InstrumentLevel);
        return state.Journey.Started && state.Journey.InstrumentLevel < 3 && inMuseum && Research.Level(state.Experience) >= cost.Level && materials >= cost.Count && gold >= cost.Gold;
    }
    public static float RevealRadius(ArchaeologyState state) => Mastery.RevealRadius(state) + (state.Journey.InstrumentLevel - 1) * .5f;
}
