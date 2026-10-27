namespace Archeo.Core;

public enum WorkshopStage { Undiscovered, Blueprint, MuseumNotes, Components, Ready, Complete }

public sealed class WorkshopState
{
    public WorkshopStage Stage { get; set; }
    public HashSet<string> Parts { get; set; } = new();
    public bool CompanionEnabled { get; set; }
}

/// <summary>Story transitions have no dependency on maps, clocks, or the game process.</summary>
public static class Workshop
{
    public static readonly string[] Parts = { "spring", "lens", "core" };
    public const int RepairWood = 20;
    public const int RepairCopper = 5;
    public const int CaseWood = 30;
    public const int CaseIron = 1;

    public static string? Find(WorkshopState state, string culture)
    {
        if (state.Stage == WorkshopStage.Undiscovered && culture == "dwarf")
        {
            state.Stage = WorkshopStage.Blueprint;
            return "blueprint";
        }
        if (state.Stage != WorkshopStage.Components) return null;
        string? part = culture switch { "dwarf" => "spring", "sea" => "lens", "valley" => "core", _ => null };
        if (part == null || !state.Parts.Add(part)) return null;
        if (Parts.All(state.Parts.Contains)) state.Stage = WorkshopStage.Ready;
        return part;
    }

    public static bool Study(WorkshopState state, bool inMuseum)
    {
        if (!inMuseum || state.Stage != WorkshopStage.Blueprint) return false;
        state.Stage = WorkshopStage.MuseumNotes;
        return true;
    }

    public static bool Consult(WorkshopState state, bool nearRobin)
    {
        if (!nearRobin || state.Stage != WorkshopStage.MuseumNotes) return false;
        state.Stage = WorkshopStage.Components;
        return true;
    }

    public static bool CanRepair(WorkshopState state, bool atHome, int wood, int copper, bool inventorySpace)
        => state.Stage == WorkshopStage.Ready && Parts.All(state.Parts.Contains) && atHome
            && wood >= RepairWood && copper >= RepairCopper && inventorySpace;

    public static bool Repair(WorkshopState state, bool atHome, int wood, int copper, bool inventorySpace)
    {
        if (!CanRepair(state, atHome, wood, copper, inventorySpace)) return false;
        state.Stage = WorkshopStage.Complete;
        state.CompanionEnabled = true;
        return true;
    }

    public static bool CanCraftCase(WorkshopState state, int wood, int iron, bool inventorySpace)
        => state.Stage == WorkshopStage.Complete && wood >= CaseWood && iron >= CaseIron && inventorySpace;

    public static bool IsValid(WorkshopState? state)
        => state != null && Enum.IsDefined(typeof(WorkshopStage), state.Stage) && state.Parts != null
            && state.Parts.All(Parts.Contains)
            && (state.Stage < WorkshopStage.Ready || Parts.All(state.Parts.Contains))
            && (!state.CompanionEnabled || state.Stage == WorkshopStage.Complete);
}
