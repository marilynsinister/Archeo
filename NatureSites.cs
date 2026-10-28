using Archeo.Core;
using Microsoft.Xna.Framework;
using StardewValley;

namespace Archeo;

public sealed partial class ModEntry
{
    private void RememberNature()
    {
        var farm = Game1.getFarm();
        var date = Game1.Date;
        NaturalEvents.Observe(State.Nature, Today, date.Year, date.SeasonIndex, date.DayOfMonth, farm.IsSnowingHere(),
            farm.resourceClumps.Where(p => p.parentSheetIndex.Value == 622).Select(p => new GridPoint((int)p.Tile.X, (int)p.Tile.Y)),
            Config.EnableNatureEvents && State.Journey.Started && !OpeningStory.Active(State) && State.Finds >= 6, Config.NatureSiteLifetimeDays);
    }

    private static bool InSiteArea(DigSite site, Vector2 tile)
        => (site.EventKind != "meteor" || NaturalEvents.WithinMeteorRadius(new((int)tile.X, (int)tile.Y), site.AnchorX, site.AnchorY))
            && (site.EventKind is not ("resident_willy" or "expedition_tide_1") || tile.X < Game1.getLocationFromName("Beach").Map.Layers[0].LayerWidth / 2);

    private void RefreshNatureSites()
    {
        RememberNature();
        if (!Config.EnableNatureEvents) return;
        foreach (var lead in State.Nature.Leads.Where(p => p.Day <= Today && p.Expires >= Today).ToList())
        {
            if (State.Sites.Any(p => p.EventKind == lead.Kind && p.Expires >= Today && p.Points.Any(n => !n.Dug))) continue;
            string[] names = lead.Kind switch
            {
                "snow" => new[] { "Forest", "Backwoods", "Town" },
                "season" => new[] { "Beach" },
                _ => new[] { "Farm" }
            };
            var random = RandomFor(lead.Key);
            foreach (string name in names)
            {
                var location = Game1.getLocationFromName(name);
                if (location == null) continue;
                var site = new DigSite { Location = name, Culture = NaturalEvents.Culture(lead.Kind), EventKind = lead.Kind,
                    EventId = lead.Key, Expires = lead.Expires, AnchorX = lead.AnchorX, AnchorY = lead.AnchorY };
                var tiles = ReachableTiles(location).Where(t => InSiteArea(site,t) && !OccupiedByDigPoint(name,t)).ToList();
                if (tiles.Count < 3) continue;
                var center = lead.Kind == "meteor" ? new Vector2(lead.AnchorX,lead.AnchorY) : tiles[random.Next(tiles.Count)];
                foreach (var tile in tiles.OrderBy(t => Vector2.DistanceSquared(t,center)).Take(70).OrderBy(_ => random.Next()))
                {
                    if (site.Points.Any(p => Vector2.DistanceSquared(tile,new(p.X,p.Y)) < 4)) continue;
                    site.Points.Add(new DigPoint { X = (int)tile.X, Y = (int)tile.Y });
                    if (site.Points.Count == 3) break;
                }
                if (site.Points.Count != 3) continue;
                if (!NaturalEvents.Attach(State.Nature,lead,Today)) break;
                State.Sites.Add(site);
                Notify(T("nature.discovered",new { name = T("nature.name."+lead.Kind), location = LocationName(name) }));
                break;
            }
            // Occupied maps keep the lead, with its original deadline and impact anchor.
        }
    }

    private void CompleteNatureSite(DigSite site)
    {
        if (!NaturalEvents.Complete(State,site,Today)) return;
        bool meteor = site.EventKind == "meteor";
        int fragments = meteor ? 18 : 12, experience = Mastery.HistoryReward(State, meteor ? 75 : 45);
        Research.Progress(State,site.Culture).Fragments += fragments;
        State.Experience += experience;
        Notify(T("nature.completed",new { name = T("nature.name."+site.EventKind), fragments, experience }));
        if (State.Nature.PendingRewards.Contains(NaturalEvents.Artifact(site.EventKind))) Notify(T("nature.reward-waiting"));
    }

    internal void ClaimNatureReward(string artifactId)
    {
        if (!State.Nature.PendingRewards.Contains(artifactId)) return;
        var artifact = Research.Artifacts.FirstOrDefault(p => p.Id == artifactId && NaturalEvents.Kinds.Contains(p.Source));
        if (artifact == null) return;
        var item = ItemRegistry.Create(QualifiedId(artifact));
        if (!Game1.player.couldInventoryAcceptThisItem(item)) { Notify(T("message.inventory-full")); return; }
        if (!Game1.player.addItemToInventoryBool(item)) return;
        NaturalEvents.RecordClaim(State,artifactId);
        SaveState();
        Game1.playSound("reward");
        Notify(T("message.restored",new { name = ArtifactName(artifact) }));
    }
}
