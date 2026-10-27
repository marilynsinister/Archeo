namespace Archeo.Core;

public sealed class ArtifactDefinition
{
    public string Id { get; set; } = "";
    public string Culture { get; set; } = "";
    public int Rarity { get; set; }
    public int Cost { get; set; }
    public int Price { get; set; }
    public int Sprite { get; set; }
    public string Effect { get; set; } = "";
    public string Source { get; set; } = "research";
}

public sealed class CultureProgress
{
    public int Fragments { get; set; }
    public int Keystones { get; set; }
    public string Project { get; set; } = "";
    public int Solves { get; set; }
    public int SinceRare { get; set; }
}

public sealed class DigPoint
{
    public int X { get; set; }
    public int Y { get; set; }
    public bool Dug { get; set; }
    public bool Revealed { get; set; }
}

public sealed class DigSite
{
    public string Location { get; set; } = "";
    public string Culture { get; set; } = "";
    public int Expires { get; set; }
    public List<DigPoint> Points { get; set; } = new();
    public string EventKind { get; set; } = "";
    public string EventId { get; set; } = "";
    public bool TimeExtended { get; set; }
    public int AnchorX { get; set; }
    public int AnchorY { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public string Key => EventId.Length > 0 ? EventId : $"normal:{Location}:{Culture}:{Expires}";
}

public sealed class ArchaeologyState
{
    public int Version { get; set; } = 1;
    public bool Introduced { get; set; }
    public int LastSiteDay { get; set; } = -1;
    public int Experience { get; set; }
    public int Finds { get; set; }
    public Dictionary<string, CultureProgress> Cultures { get; set; } = new();
    public Dictionary<string, int> Restored { get; set; } = new();
    public HashSet<string> Donated { get; set; } = new();
    public Dictionary<string, int> LastUse { get; set; } = new();
    public List<DigSite> Sites { get; set; } = new();
    public HashSet<string> VanillaSpots { get; set; } = new();
    public WorkshopState Workshop { get; set; } = new();
    public WeatherProgress Weather { get; set; } = new();
    public HashSet<string> CollectionRewards { get; set; } = new();
    public ShipwreckState Shipwreck { get; set; } = new();
    public StarResearch Stars { get; set; } = new();
    public NaturalProgress Nature { get; set; } = new();
    public ResidentProgress Residents { get; set; } = new();
    public string TrackedObjective { get; set; } = "";
    public MasteryProgress Mastery { get; set; } = new();
    public JourneyProgress Journey { get; set; } = new();
    public FrontierProgress Frontiers { get; set; } = new();
    public MuseumProgress Museum { get; set; } = new();
}
