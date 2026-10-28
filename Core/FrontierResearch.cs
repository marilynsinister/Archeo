namespace Archeo.Core;

public sealed class FrontierProgress
{
    public bool DesertAccess { get; set; }
    public bool IslandAccess { get; set; }
}

/// <summary>World access is recorded independently from the player's gradual archaeology progression.</summary>
public static class FrontierResearch
{
    public static readonly string[] Cultures = { "calico", "island" };
    public static readonly string[] RelicEffects = { "sunstep", "oasis", "shellguard", "tidecall" };
    public static bool Observe(ArchaeologyState state,bool desert,bool island)
    {
        state.Frontiers.DesertAccess |= desert;
        state.Frontiers.IslandAccess |= island;
        return Unlock(state);
    }
    public static bool Unlock(ArchaeologyState state)
    {
        if(!state.Journey.Started || !state.Journey.Chapters.Contains("sea"))return false;
        if(state.Routes.Initialized)
        {
            bool opened=false;
            if(state.Frontiers.DesertAccess && state.Routes.Explained.Contains("calico"))opened|=state.Journey.Chapters.Add("calico");
            if(state.Frontiers.IslandAccess && state.Routes.Explained.Contains("island"))opened|=state.Journey.Chapters.Add("island");
            if(state.Journey.Chapters.Contains("calico") || state.Journey.Chapters.Contains("island"))opened|=state.Journey.Chapters.Add("frontiers");
            return opened;
        }
        bool changed=state.Journey.Chapters.Add("frontiers");
        if(state.Frontiers.DesertAccess)changed|=state.Journey.Chapters.Add("calico");
        if(state.Frontiers.IslandAccess)changed|=state.Journey.Chapters.Add("island");
        return changed;
    }
    public static string SpotCulture(string location,bool calicoOpen,bool islandOpen,string fallback)
        => location=="Desert"&&calicoOpen?"calico":location.StartsWith("Island",StringComparison.Ordinal)&&islandOpen?"island":fallback;
    public static bool CanUseSunstep(bool outdoors,int time) => outdoors && time is >= 600 and < 1800;
    public static bool CanUseOasis(float stamina,float maximum,int health,int maximumHealth) => stamina<maximum || health<maximumHealth;
    public static int OasisEnergy => 60;
    public static int OasisHealth => 25;
    public static bool CollectionVisible(ArchaeologyState state,MuseumCollection collection)
        => !Cultures.Contains(collection.Id) || Journey.IsOpen(state,collection.Id) && collection.Artifacts.Any(state.Restored.ContainsKey);
}

public readonly record struct CollectionListLayout(int PageSize,int FirstRowY,int RowHeight,int PageControlsY)
{
    public static CollectionListLayout Create(int height) => new(Math.Max(1,(height-205)/60),44,60,height-155);
}
