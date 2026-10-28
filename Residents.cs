using Archeo.Core;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace Archeo;

public sealed partial class ModEntry
{
    internal string ResidentName(ResidentStory story) => Game1.getCharacterFromName(story.Npc)?.displayName ?? story.Npc;

    private NPC? ResidentNearby(ResidentStory story)
        => Game1.currentLocation.characters.FirstOrDefault(p => p.Name == story.Npc && p.CanSocialize && !p.isSleeping.Value
            && Vector2.Distance(Game1.player.Tile,p.Tile) <= 4);

    private void DiscoverResidents()
    {
        if (OpeningStory.Active(State)) return;
        foreach (string id in ResidentStories.Discover(State.Residents,State.Restored))
        {
            var story = ResidentStories.All.First(p => p.Id == id);
            Notify(T("residents.discovered",new { name = ResidentName(story) }));
        }
    }

    private void RefreshResidentSites()
    {
        DiscoverResidents();
        foreach (var story in ResidentStories.All)
        {
            if (ResidentStories.Stage(State.Residents,story.Id) != ResidentStage.Searching
                || State.Sites.Any(p => p.EventKind == story.SiteKind)) continue;
            var location = Game1.getLocationFromName(story.Location);
            if (location == null) continue;
            var tiles = ReachableTiles(location).Where(t => !OccupiedByDigPoint(story.Location,t)
                && (story.Id != "willy" || t.X < location.Map.Layers[0].LayerWidth / 2)).ToList();
            if (tiles.Count < 2) continue;
            var random = RandomFor(story.SiteKey);
            var anchor = tiles[random.Next(tiles.Count)];
            var points = new List<DigPoint>();
            foreach (var tile in tiles.OrderBy(p => Vector2.DistanceSquared(p,anchor)).Take(50).OrderBy(_ => random.Next()))
            {
                if (points.Any(p => Vector2.DistanceSquared(new(p.X,p.Y),tile) < 4)) continue;
                points.Add(new DigPoint { X = (int)tile.X, Y = (int)tile.Y });
                if (points.Count == 2) break;
            }
            if (points.Count != 2) continue;
            State.Sites.Add(new DigSite { Culture = story.Culture, Location = story.Location, EventKind = story.SiteKind,
                EventId = story.SiteKey, Expires = int.MaxValue, Points = points });
            Notify(T("residents.site-found",new { name = T("residents.title."+story.Id), location = LocationName(story.Location) }));
        }
    }

    private void CompleteResidentSite(DigSite site)
    {
        if (!ResidentStories.CompleteSite(State.Residents,site)) return;
        Research.Progress(State,site.Culture).Fragments += ResidentStories.Fragments;
        var story = ResidentStories.All.First(p => p.SiteKind == site.EventKind);
        Notify(T("residents.recovered",new { name = ResidentName(story), fragments = ResidentStories.Fragments }));
    }

    internal bool CanResidentAction(string id, string action)
    {
        var story = ResidentStories.All.FirstOrDefault(p => p.Id == id);
        if (story == null || !Context.IsWorldReady || Game1.eventUp) return false;
        var stage = ResidentStories.Stage(State.Residents,id);
        return action switch
        {
            "start" => stage == ResidentStage.Clue && ResidentNearby(story) != null,
            "share" => stage == ResidentStage.Recovered && ResidentNearby(story) != null,
            "claim" => stage == ResidentStage.Reward,
            _ => false
        };
    }

    internal void ResidentAction(string id, string action)
    {
        if (!CanResidentAction(id,action)) { Notify(T("residents.requirements")); return; }
        var story = ResidentStories.All.First(p => p.Id == id);
        if (action == "claim")
        {
            var artifact = Research.Artifacts.First(p => p.Id == story.Artifact);
            var item = ItemRegistry.Create(QualifiedId(artifact));
            if (!Game1.player.couldInventoryAcceptThisItem(item)) { Notify(T("message.inventory-full")); return; }
            if (!Game1.player.addItemToInventoryBool(item)) return;
            ResidentStories.RecordClaim(State,id,true);
            RefreshExpeditionSites();
            SaveState();
            Game1.playSound("reward");
            Notify(T("message.restored",new { name = ArtifactName(artifact) }));
            return;
        }
        var npc = ResidentNearby(story);
        if (npc == null) return;
        RecognizeStoryPartner(npc);
        if (action == "start")
        {
            if (!ResidentStories.Start(State.Residents,id,true)) return;
            RefreshResidentSites();
        }
        else
        {
            if (!ResidentStories.Share(State.Residents,id,true)) return;
            int friendship = Mastery.HistoryReward(State, ResidentStories.Friendship);
            int experience = Mastery.HistoryReward(State, ResidentStories.Experience);
            Game1.player.changeFriendship(friendship,npc);
            State.Experience += experience;
            Notify(T("residents.shared",new { name = ResidentName(story), friendship, experience }));
        }
        SaveState();
        // A standalone portrait dialogue leaves the resident's normal dialogue stack and schedule intact.
        string key = "residents.dialogue."+id+"."+action;
        Game1.activeClickableMenu = new DialogueBox(new Dialogue(npc,Id+"/"+key,T(key)));
    }
}
