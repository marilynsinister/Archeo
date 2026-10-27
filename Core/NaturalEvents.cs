namespace Archeo.Core;

public sealed class NaturalLead
{
    public string Kind { get; set; } = "";
    public string Key { get; set; } = "";
    public int Day { get; set; }
    public int Expires { get; set; }
    public int AnchorX { get; set; }
    public int AnchorY { get; set; }
}

public sealed class NaturalProgress
{
    public int LastObservedDay { get; set; } = -1;
    public int LastYear { get; set; } = -1;
    public int LastSeason { get; set; } = -1;
    public HashSet<int> SnowYears { get; set; } = new();
    public HashSet<string> MeteorSnapshot { get; set; } = new();
    public HashSet<string> SeenEvents { get; set; } = new();
    public List<NaturalLead> Leads { get; set; } = new();
    public Dictionary<string, int> Completed { get; set; } = new();
    public HashSet<string> CompletedSites { get; set; } = new();
    public HashSet<string> PendingRewards { get; set; } = new();
}

/// <summary>Observation-based events, with stable identities and deadlines independent of reloads.</summary>
public static class NaturalEvents
{
    public static readonly string[] Kinds = { "snow", "season", "meteor" };
    public const int MeteorRadius = 8;
    public static string Artifact(string kind) => kind switch
    {
        "snow" => "frozen_leaf", "season" => "tide_calendar", "meteor" => "star_glass", _ => throw new ArgumentException("Unknown natural event.")
    };
    public static string Culture(string kind) => kind switch
    {
        "snow" => "valley", "season" => "sea", "meteor" => "dwarf", _ => throw new ArgumentException("Unknown natural event.")
    };

    public static void Observe(NaturalProgress state, int day, int year, int season, int dayOfMonth, bool snowing,
        IEnumerable<GridPoint> meteors, bool enabled, int lifetime)
    {
        state.Leads.RemoveAll(p => p.Expires < day);
        var impacts = meteors.ToList();
        var snapshot = impacts.Select(p => $"{p.X}:{p.Y}").ToHashSet();
        bool initialized = state.LastObservedDay >= 0;
        bool consecutive = initialized && state.LastObservedDay == day - 1;
        bool sameDay = state.LastObservedDay == day;
        if (consecutive && enabled && dayOfMonth == 1 && season != state.LastSeason)
            Add(state, new NaturalLead { Kind="season",Key=$"nature:season:{year}:{season}",Day=day,Expires=day+lifetime-1 });
        // The first observed snowy day of each year gives a lead the next morning, including on a new install.
        if (snowing && state.SnowYears.Add(year) && enabled)
            Add(state, new NaturalLead { Kind="snow",Key=$"nature:snow:{year}",Day=day+1,Expires=day+lifetime });
        // A new install or a gap only establishes a baseline; existing rocks are not new falls.
        if (enabled && (consecutive || sameDay))
        foreach (var impact in impacts.Where(p => !state.MeteorSnapshot.Contains($"{p.X}:{p.Y}")))
            Add(state,new NaturalLead { Kind="meteor",Key=$"nature:meteor:{day}:{impact.X}:{impact.Y}",Day=day,Expires=day+lifetime-1,AnchorX=impact.X,AnchorY=impact.Y });
        state.MeteorSnapshot=snapshot;
        state.LastObservedDay=day;
        state.LastYear=year;
        state.LastSeason=season;
    }

    private static void Add(NaturalProgress state, NaturalLead lead)
    { if (state.SeenEvents.Add(lead.Key)) state.Leads.Add(lead); }

    public static bool Attach(NaturalProgress state, NaturalLead lead, int day)
    {
        if (day < lead.Day || day > lead.Expires) return false;
        int index=state.Leads.FindIndex(p=>p.Key==lead.Key);
        if (index<0) return false;
        state.Leads.RemoveAt(index);
        return true;
    }

    public static bool WithinMeteorRadius(GridPoint tile, int anchorX, int anchorY)
    {
        double dx=tile.X-anchorX-.5,dy=tile.Y-anchorY-.5;
        return dx*dx+dy*dy <= MeteorRadius*MeteorRadius;
    }

    public static bool Complete(ArchaeologyState state, DigSite site, int day)
    {
        if (!Kinds.Contains(site.EventKind) || !site.EventId.StartsWith("nature:"+site.EventKind+":",StringComparison.Ordinal)
            || site.Culture!=Culture(site.EventKind) || site.Expires<day || site.Points.Count!=3 || site.Points.Any(p=>!p.Dug)
            || site.EventKind=="meteor" && (site.Location!="Farm" || site.Points.Any(p=>!WithinMeteorRadius(new(p.X,p.Y),site.AnchorX,site.AnchorY)))
            || !state.Nature.CompletedSites.Add(site.Key)) return false;
        string kind=site.EventKind;
        state.Nature.Completed[kind]=state.Nature.Completed.GetValueOrDefault(kind)+1;
        if (!state.Restored.ContainsKey(Artifact(kind))) state.Nature.PendingRewards.Add(Artifact(kind));
        return true;
    }

    public static bool RecordClaim(ArchaeologyState state, string artifact)
    {
        if (!Kinds.Any(k=>Artifact(k)==artifact) || !state.Nature.PendingRewards.Remove(artifact)) return false;
        state.Restored[artifact]=state.Restored.GetValueOrDefault(artifact)+1;
        return true;
    }

    public static bool IsValid(NaturalProgress? state)
        => state!=null && state.LastObservedDay>=-1 && state.LastYear>=-1 && state.LastSeason is >=-1 and <=3
            && state.SnowYears!=null && state.SnowYears.All(y=>y>=1) && state.MeteorSnapshot!=null && state.MeteorSnapshot.All(p=>!string.IsNullOrWhiteSpace(p))
            && state.SeenEvents!=null && state.Leads!=null && state.Completed!=null && state.CompletedSites!=null && state.PendingRewards!=null
            && state.Completed.All(p=>Kinds.Contains(p.Key) && p.Value>=0)
            && state.SeenEvents.All(p=>!string.IsNullOrWhiteSpace(p)) && state.CompletedSites.All(p=>!string.IsNullOrWhiteSpace(p))
            && state.PendingRewards.All(id=>Kinds.Any(k=>Artifact(k)==id))
            && state.Leads.All(p=>p!=null && Kinds.Contains(p.Kind) && p.Key!=null && p.Key.StartsWith("nature:"+p.Kind+":",StringComparison.Ordinal)
                && p.Day>=0 && p.Expires>=p.Day && p.AnchorX>=0 && p.AnchorY>=0 && state.SeenEvents.Contains(p.Key))
            && state.Leads.Select(p=>p.Key).Distinct().Count()==state.Leads.Count;
}
