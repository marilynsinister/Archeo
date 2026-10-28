using Archeo.Core;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace Archeo;

public sealed partial class ModEntry
{
    internal bool CanExplainRoute(string id)
    {
        if (!Context.IsWorldReady || Game1.eventUp || Core.RouteStories.Pending(State)?.Id != id) return false;
        var story = Core.RouteStories.All.Single(p => p.Id == id);
        return story.Npc == "Gunther" ? InMuseum : Game1.currentLocation.characters.Any(p => p.Name == story.Npc
            && p.CanSocialize && !p.isSleeping.Value && Vector2.Distance(p.Tile,Game1.player.Tile) <= 4);
    }

    internal bool ExplainRoute(string id)
    {
        if (!CanExplainRoute(id) || !Core.RouteStories.Explain(State,id,true)) return false;
        Core.Journey.Refresh(State,Research.Artifacts);
        RefreshSites(true);
        RefreshExpeditionSites();
        SaveState();
        Game1.drawObjectDialogue(T("routes.conversation."+id));
        return true;
    }

    private bool TryRouteInteraction(Vector2 tile)
    {
        var story = Core.RouteStories.Pending(State);
        if (story == null || !CanExplainRoute(story.Id) || Vector2.Distance(tile,Game1.player.Tile) > 3) return false;
        if (story.Npc == "Gunther")
        {
            string? action = Game1.currentLocation.doesTileHaveProperty((int)tile.X,(int)tile.Y,"Action","Buildings");
            return action?.Contains("Gunther",StringComparison.OrdinalIgnoreCase)==true && ExplainRoute(story.Id);
        }
        var npc = Game1.currentLocation.characters.FirstOrDefault(p => p.Name == story.Npc);
        return npc != null && npc.GetBoundingBox().Contains((int)tile.X*64+32,(int)tile.Y*64+32) && ExplainRoute(story.Id);
    }
}
