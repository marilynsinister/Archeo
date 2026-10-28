using Archeo.Core;
using Microsoft.Xna.Framework;
using StardewValley;

namespace Archeo;

public sealed partial class ModEntry
{
    internal bool HoldingInstrument => State.Journey.Started && Game1.player.CurrentItem?.ItemId == ItemId("surveyor");
    internal bool InMuseum => Game1.currentLocation?.Name == "ArchaeologyHouse";
    internal bool CanUpgradeInstrument
    {
        get
        {
            var cost = Core.Journey.UpgradeCost(State.Journey.InstrumentLevel);
            return Core.Journey.CanUpgrade(State, InMuseum, cost.Material == "" ? 0 : MaterialCount(cost.Material), Game1.player.Money);
        }
    }

    internal bool ClaimFieldKit()
    {
        if (!InMuseum) { Notify(T("journey.visit-museum")); return false; }
        var missing = new[] { "journal", "surveyor" }.Where(id => !Game1.player.Items.Any(p => p?.ItemId == ItemId(id))).ToList();
        int free = Game1.player.MaxItems - Game1.player.Items.Count(p => p != null);
        if (free < missing.Count) { Notify(T("message.inventory-full")); return false; }
        foreach (string id in missing) Game1.player.addItemToInventoryBool(ItemRegistry.Create("(O)" + ItemId(id)));
        bool first = !State.Journey.Started;
        State.Journey.Started = true;
        Core.Journey.Refresh(State, Research.Artifacts);
        RefreshSites(true);
        if (first && OpeningStory.Active(State)) State.TrackedObjective = FieldObjectives().FirstOrDefault()?.Id ?? "";
        SaveState();
        Game1.playSound("reward");
        if (first) Game1.drawObjectDialogue(T(OpeningStory.Active(State) ? "opening.gunther-start" : "journey.gunther"));
        else Notify(T("journey.kit-replaced"));
        return true;
    }

    private bool TryIntroInteraction(Vector2 tile)
    {
        if (!InMuseum || Vector2.Distance(Game1.player.Tile, tile) > 3) return false;
        string? action = Game1.currentLocation.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Action", "Buildings");
        if (action?.Contains("Gunther", StringComparison.OrdinalIgnoreCase) != true) return false;
        return State.Journey.Started ? ConcludeOpeningStory() : ClaimFieldKit();
    }

    internal bool ConcludeOpeningStory()
    {
        if (!OpeningStory.Conclude(State, InMuseum)) return false;
        Core.Journey.Refresh(State, Research.Artifacts);
        DiscoverResidents();
        RefreshSites(true);
        SaveState();
        Game1.drawObjectDialogue(T("opening.conversation"));
        return true;
    }

    internal void UpgradeInstrument()
    {
        if (!CanUpgradeInstrument) return;
        var cost = Core.Journey.UpgradeCost(State.Journey.InstrumentLevel);
        ConsumeMaterial(cost.Material, cost.Count);
        Game1.player.Money -= cost.Gold;
        State.Journey.InstrumentLevel++;
        SaveState();
        Game1.playSound("crafting");
        Notify(T("journey.upgraded", new { level = State.Journey.InstrumentLevel }));
    }
}
