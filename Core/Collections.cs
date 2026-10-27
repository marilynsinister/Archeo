namespace Archeo.Core;

public sealed record MuseumCollection(string Id, string Reward, string[] Artifacts);

/// <summary>Personal collections track discoveries, so donating or displaying an item never erases credit.</summary>
public static class MuseumCollections
{
    public const int CaseCapacity = 3;
    public const int CaseWood = 60;
    public const int CaseIron = 2;
    public const int CaseQuartz = 2;
    public static readonly MuseumCollection[] All =
    {
        new("valley", "ancient_map", new[] { "settler_cup", "copper_brooch", "herbalist_flask" }),
        new("dwarf", "mechanical_clock", new[] { "brass_gear", "rune_tablet", "miner_goblet" }),
        new("sea", "sea_mosaic", new[] { "clay_whistle", "sailor_buckle", "shell_comb" })
    };

    public static int Count(ArchaeologyState state, MuseumCollection collection)
        => collection.Artifacts.Count(id => state.Restored.TryGetValue(id, out int count) && count > 0);

    public static bool CanClaim(ArchaeologyState state, string id, bool inventorySpace)
    {
        var collection = All.FirstOrDefault(p => p.Id == id);
        return inventorySpace && collection != null && !state.CollectionRewards.Contains(id)
            && Count(state, collection) == collection.Artifacts.Length;
    }

    public static bool RecordClaim(ArchaeologyState state, string id, bool inventorySpace)
        => CanClaim(state, id, inventorySpace) && state.CollectionRewards.Add(id);

    public static bool CanCraftCase(WorkshopState workshop, int wood, int iron, int quartz, bool inventorySpace)
        => workshop.Stage == WorkshopStage.Complete && wood >= CaseWood && iron >= CaseIron
            && quartz >= CaseQuartz && inventorySpace;
}
