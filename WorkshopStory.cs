using Archeo.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;

namespace Archeo;

public sealed partial class ModEntry
{
    internal Texture2D WorkshopAtlas { get; private set; } = null!;
    internal Texture2D MoleTexture { get; private set; } = null!;
    internal bool NearRobin => Game1.currentLocation.characters.Any(p => p.Name == "Robin" && Vector2.Distance(p.Tile, Game1.player.Tile) <= 4);
    internal bool AtWorkshopHome => Game1.currentLocation is Farm or FarmHouse;
    internal int MaterialCount(string id) => Game1.player.Items.Where(p => p?.QualifiedItemId == "(O)" + id).Sum(p => p!.Stack);

    private void ConsumeMaterial(string id, int amount)
    {
        foreach (var item in Game1.player.Items.Where(p => p?.QualifiedItemId == "(O)" + id).ToList())
        {
            int take = Math.Min(amount, item.Stack);
            if (take == item.Stack) Game1.player.removeItemFromInventory(item); else item.Stack -= take;
            amount -= take;
            if (amount == 0) break;
        }
    }

    private void OnWorkshopFind(string culture)
    {
        if (OpeningStory.Active(State)) return;
        string? found = Workshop.Find(State.Workshop, culture);
        if (found == null) return;
        Notify(T("workshop.found", new { name = T("workshop.part." + found) }));
        if (State.Workshop.Stage == WorkshopStage.Ready) Notify(T("workshop.ready"));
        SaveState();
    }

    internal bool TryWorkshopInteraction(Vector2 tile)
    {
        if (State.Workshop.Stage == WorkshopStage.Blueprint && Game1.currentLocation.Name == "ArchaeologyHouse"
            && Vector2.Distance(Game1.player.Tile, tile) <= 2.5f)
        {
            string? action = Game1.currentLocation.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Action", "Buildings");
            if (action?.Contains("Gunther", StringComparison.OrdinalIgnoreCase) == true)
                return WorkshopAction("study");
        }
        if (State.Workshop.Stage == WorkshopStage.MuseumNotes && NearRobin)
        {
            var robin = Game1.currentLocation.characters.First(p => p.Name == "Robin");
            var target = new Rectangle((int)tile.X * 64, (int)tile.Y * 64, 64, 64);
            if (robin.GetBoundingBox().Intersects(target)) return WorkshopAction("consult");
        }
        return false;
    }

    internal bool CanWorkshopAction(string action)
    {
        var story = State.Workshop;
        return action switch
        {
            "study" => story.Stage == WorkshopStage.Blueprint && Game1.currentLocation.Name == "ArchaeologyHouse",
            "consult" => story.Stage == WorkshopStage.MuseumNotes && NearRobin,
            "repair" => Workshop.CanRepair(story, AtWorkshopHome, MaterialCount("388"), MaterialCount("334"), true),
            "case" => Workshop.CanCraftCase(story, MaterialCount("388"), MaterialCount("335"), true),
            "companion" => story.Stage == WorkshopStage.Complete,
            _ => false
        };
    }

    internal bool WorkshopAction(string action)
    {
        if (!CanWorkshopAction(action)) { Notify(T("workshop.requirements")); return false; }
        var story = State.Workshop;
        switch (action)
        {
            case "study":
                if (!Workshop.Study(story, true)) return false;
                SaveState();
                Game1.drawObjectDialogue(T("workshop.dialogue.gunther"));
                return true;
            case "consult":
                if (!Workshop.Consult(story, true)) return false;
                SaveState();
                Game1.drawObjectDialogue(T("workshop.dialogue.robin"));
                return true;
            case "repair":
            case "case":
                var display = ItemRegistry.Create("(F)" + ItemId("display_case"));
                if (!Game1.player.couldInventoryAcceptThisItem(display)) { Notify(T("message.inventory-full")); return false; }
                int wood = MaterialCount("388"), copper = MaterialCount("334");
                if (!Game1.player.addItemToInventoryBool(display)) return false;
                ConsumeMaterial("388", action == "repair" ? Workshop.RepairWood : Workshop.CaseWood);
                ConsumeMaterial(action == "repair" ? "334" : "335", action == "repair" ? Workshop.RepairCopper : Workshop.CaseIron);
                if (action == "repair")
                {
                    Workshop.Repair(story, AtWorkshopHome, wood, copper, true);
                    State.Experience += Mastery.HistoryReward(State,180);
                    Game1.playSound("reward");
                    SaveState();
                    Game1.drawObjectDialogue(T("workshop.dialogue.complete"));
                }
                else { Game1.playSound("crafting"); SaveState(); Notify(T("workshop.case-crafted")); }
                return true;
            case "companion":
                story.CompanionEnabled = !story.CompanionEnabled;
                companions.Value.Reset();
                SaveState();
                Notify(T(story.CompanionEnabled ? "workshop.companion-on" : "workshop.companion-off"));
                return true;
        }
        return false;
    }
}
