namespace Archeo.Core;

/// <summary>Deterministic progression, shared by the mod and the offline tests.</summary>
public sealed class Research
{
    public static readonly string[] Cultures = { "valley", "dwarf", "sea", "calico", "island" };
    public IReadOnlyList<ArtifactDefinition> Artifacts { get; }

    public Research(IEnumerable<ArtifactDefinition> artifacts)
    {
        Artifacts = artifacts.ToList();
        if (Artifacts.Count == 0 || Artifacts.Select(p => p.Id).Distinct().Count() != Artifacts.Count)
            throw new ArgumentException("Artifact IDs must be unique, and the catalog must not be empty.");
        if (Artifacts.Any(p => !Cultures.Contains(p.Culture) || p.Cost < 1 || p.Rarity is < 0 or > 4 || !(p.Source == "research" || WeatherExpeditions.Kinds.Contains(p.Source) || NaturalEvents.Kinds.Contains(p.Source) || ResidentStories.Sources.Contains(p.Source))))
            throw new ArgumentException("Invalid culture, rarity, or research cost.");
        if (Cultures.Any(c => !Artifacts.Any(p => p.Culture == c && p.Rarity == 0 && p.Source == "research")))
            throw new ArgumentException("Every culture needs a common project.");
    }

    public static void ValidateState(ArchaeologyState state)
    {
        if (state.Version != 1 || state.Cultures == null || state.Restored == null || state.Donated == null
            || state.LastUse == null || state.Sites == null || state.VanillaSpots == null || !Workshop.IsValid(state.Workshop) || !WeatherExpeditions.IsValid(state.Weather)
            || state.CollectionRewards == null || state.CollectionRewards.Any(id => !MuseumCollections.All.Any(p => p.Id == id)) || !Shipwreck.IsValid(state.Shipwreck)
            || !UnusualRelics.IsValid(state.Stars) || !NaturalEvents.IsValid(state.Nature) || !ResidentStories.IsValid(state.Residents)
            || state.TrackedObjective == null || state.TrackedObjective.Length > 2048 || !Mastery.IsValid(state.Mastery, state.Experience) || !Journey.IsValid(state.Journey) || state.Frontiers == null || !LivingMuseum.IsValid(state.Museum))
            throw new InvalidDataException("Unsupported or incomplete archaeology save; preserve original data.");
        if (state.Experience < 0 || state.Finds < 0 || state.Restored.Any(p => p.Value < 1)
            || state.Cultures.Any(p => p.Value == null || p.Value.Fragments < 0 || p.Value.Keystones < 0 || p.Value.Solves < 0 || p.Value.SinceRare < 0)
            || state.Sites.Any(s => s == null || s.Points == null || !Cultures.Contains(s.Culture) || s.Points.Any(p => p == null)
                || s.EventKind == null || s.EventId == null || s.EventKind != "" && (!(WeatherExpeditions.Kinds.Contains(s.EventKind) || Shipwreck.Kinds.Contains(s.EventKind) || s.EventKind == "stars" || NaturalEvents.Kinds.Contains(s.EventKind) || ResidentStories.Kinds.Contains(s.EventKind)) || s.EventId.Length == 0)
                || ResidentStories.Kinds.Contains(s.EventKind) && !ResidentStories.ValidSite(s)
                || s.EventKind=="meteor" && (s.Location!="Farm" || s.AnchorX<0 || s.AnchorY<0 || s.Points.Any(p=>!NaturalEvents.WithinMeteorRadius(new(p.X,p.Y),s.AnchorX,s.AnchorY)))))
            throw new InvalidDataException("Invalid archaeology progress; preserve original data.");
    }

    public CultureProgress Progress(ArchaeologyState state, string culture)
    {
        if (!Cultures.Contains(culture)) throw new ArgumentException("Unknown culture.");
        if (!state.Cultures.TryGetValue(culture, out var progress))
            state.Cultures[culture] = progress = new CultureProgress();
        return progress;
    }

    public ArtifactDefinition Project(ArchaeologyState state, string culture, Random random)
    {
        var progress = Progress(state, culture);
        var current = Artifacts.FirstOrDefault(p => p.Id == progress.Project && p.Culture == culture && p.Source == "research");
        if (current != null) return current;
        return NextProject(state, culture, random);
    }

    public ArtifactDefinition NextProject(ArchaeologyState state, string culture, Random random)
    {
        var progress = Progress(state, culture);
        var pool = Artifacts.Where(p => p.Culture == culture && p.Source == "research").ToList();
        var unseen = pool.Where(p => !state.Restored.ContainsKey(p.Id)).ToList();
        // The first project is approachable; every sixth solve guarantees an unseen rare, if available.
        var rares = unseen.Where(p => p.Rarity >= 2 && (p.Rarity < 4 || progress.Solves >= 7)).ToList();
        ArtifactDefinition selected;
        if (progress.Solves == 0)
            selected = pool.Where(p => p.Rarity == 0).OrderBy(p => p.Cost).First();
        else if (rares.Count > 0 && (progress.SinceRare >= 5 || random.NextDouble() < .28))
            selected = Weighted(rares, random);
        else
        {
            var commons = unseen.Where(p => p.Rarity < 2).ToList();
            if (commons.Count == 0) commons = pool.Where(p => p.Rarity < 2).ToList();
            selected = Weighted(commons, random);
        }
        progress.Project = selected.Id;
        return selected;
    }

    private static ArtifactDefinition Weighted(List<ArtifactDefinition> pool, Random random)
    {
        double Weight(ArtifactDefinition p) => p.Rarity switch { 0 => 5, 1 => 3, 2 => 2, 3 => 1, _ => .5 };
        double roll = random.NextDouble() * pool.Sum(Weight);
        foreach (var item in pool)
        {
            roll -= Weight(item);
            if (roll <= 0) return item;
        }
        return pool[^1];
    }

    public static int Required(ArtifactDefinition artifact, CultureProgress progress, bool useKeystone, ArchaeologyState? state = null)
        => Math.Max(1, Mastery.RestorationCost(state, artifact.Cost) - (useKeystone && progress.Keystones > 0 ? 12 : 0));

    public bool Restore(ArchaeologyState state, string culture, bool useKeystone, Random random, out ArtifactDefinition artifact)
    {
        artifact = Project(state, culture, random);
        var progress = Progress(state, culture);
        int cost = Required(artifact, progress, useKeystone, state);
        if (progress.Fragments < cost) return false;
        progress.Fragments -= cost;
        if (useKeystone && progress.Keystones > 0) progress.Keystones--;
        progress.Solves++;
        progress.SinceRare = artifact.Rarity >= 2 ? 0 : progress.SinceRare + 1;
        state.Restored[artifact.Id] = state.Restored.GetValueOrDefault(artifact.Id) + 1;
        state.Experience += 30 + artifact.Rarity * 20;
        progress.Project = "";
        NextProject(state, culture, random);
        return true;
    }

    public static int Level(int experience) => Math.Clamp(1 + (int)Math.Sqrt(Math.Max(0, experience) / 80d), 1, 10);

    public static int StableSeed(long save, long player, int day, string salt)
    {
        unchecked
        {
            uint hash = 2166136261;
            foreach (char c in $"{save}:{player}:{day}:{salt}") hash = (hash ^ c) * 16777619;
            return (int)(hash & 0x7fffffff);
        }
    }

    public static void ExpireSites(ArchaeologyState state, int day)
        => state.Sites.RemoveAll(p => p.Expires < day || p.Points.All(n => n.Dug));
}
