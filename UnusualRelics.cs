using Archeo.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace Archeo;

public sealed partial class ModEntry
{
    private const string GardenMutexKey = Id + "/GardenFlute";
    private readonly PerScreen<PastVision> pastVisions = new(() => new());
    private readonly PerScreen<RelicAura> relicAuras = new(() => new());
    private bool fluteRequestPending;
    internal Texture2D PastTexture { get; private set; } = null!;

    private void InitializeUnusualRelics()
    { if (Context.IsMainPlayer) Game1.player.team.GetOrCreateGlobalInventoryMutex(GardenMutexKey); }

    private void ResetUnusualRelics()
    { pastVisions.ResetAllScreens(); relicAuras.ResetAllScreens(); fluteRequestPending = false; }

    internal int RelicDaysUntilReady(ArtifactDefinition artifact)
        => Core.UnusualRelics.DaysUntilReady(State.LastUse.GetValueOrDefault(artifact.Id, -1), Today, artifact.Effect);

    private bool UsePastLens()
    {
        var location = Game1.currentLocation;
        var found = FindSurveyTarget(location);
        if (found == null) { Notify(T("relic.travel")); return false; }
        var (site, point) = found.Value;
        var start = new GridPoint((int)Game1.player.Tile.X, (int)Game1.player.Tile.Y);
        var goal = new GridPoint(point.X, point.Y);
        var trail = Core.UnusualRelics.FindTrail(start, goal, cell =>
        {
            var tile = new Vector2(cell.X, cell.Y);
            return location.isTileOnMap(tile) && !location.isWaterTile(cell.X, cell.Y) && location.isTilePassable(tile)
                && location.isTileLocationOpen(tile) && !location.Objects.ContainsKey(tile) && !location.terrainFeatures.ContainsKey(tile)
                && !BlockedByWorldStructures(location,tile);
        });
        if (trail.Count == 0) { Notify(T("unusual.no-trail")); return false; }
        double until = Game1.currentGameTime.TotalGameTime.TotalSeconds + 30;
        pastVisions.Value = new PastVision { Location = location.Name, SiteKey = site.Key, Target = goal, Trail = trail, Until = until };
        signals.Value = new SurveySignal { Location = location.Name, SiteKey = site.Key, Target = new(point.X,point.Y), Revealed = true, Until = until };
        Notify(T("unusual.past-revealed", new { culture = CultureName(site.Culture), count = site.Points.Count(p => !p.Dug) }));
        return true;
    }

    private static bool CanGrowWithFlute(FruitTree tree)
        => tree.growthStage.Value >= 4 && Core.UnusualRelics.CanGrowFruit(tree.daysUntilMature.Value, tree.stump.Value,
            tree.struckByLightningCountdown.Value, tree.IsInSeasonHere(), tree.fruit.Count);

    private void BeginGardenFlute(ArtifactDefinition artifact)
    {
        if (fluteRequestPending || !Game1.player.team.globalInventoryMutexes.TryGetValue(GardenMutexKey, out var mutex) || mutex.IsLocked())
        { Notify(T("unusual.garden-busy")); return; }
        var location = Game1.currentLocation;
        var target = location.terrainFeatures.Pairs.Where(p => p.Value is FruitTree tree && CanGrowWithFlute(tree)
            && Vector2.DistanceSquared(Game1.player.Tile, p.Key) <= 25)
            .OrderBy(p => Vector2.DistanceSquared(Game1.player.Tile,p.Key)).FirstOrDefault();
        if (target.Value is not FruitTree chosen) { Notify(T("unusual.no-tree")); return; }
        fluteRequestPending = true;
        mutex.RequestLock(() =>
        {
            fluteRequestPending = false;
            try
            {
                if (!Context.IsWorldReady || states.Value == null || !Context.IsPlayerFree || Game1.currentLocation != location
                    || !Game1.player.Items.Any(p => p?.QualifiedItemId == QualifiedId(artifact)) || RelicDaysUntilReady(artifact) > 0) return;
                if (!location.terrainFeatures.TryGetValue(target.Key, out var current) || current != chosen
                    || Vector2.DistanceSquared(Game1.player.Tile, target.Key) > 25 || !CanGrowWithFlute(chosen) || !chosen.TryAddFruit())
                { Notify(T("unusual.no-tree")); return; }
                State.LastUse[artifact.Id] = Today;
                SaveState();
                StartRelicAura("garden", target.Key * 64 + new Vector2(32,-32));
                Game1.playSound("yoba");
                Notify(T("unusual.fruit-grown"));
            }
            finally { if (mutex.IsLockHeld()) mutex.ReleaseLock(); }
        }, () => { fluteRequestPending = false; Notify(T("unusual.garden-busy")); });
    }

    private bool UseKeeperHourglass()
    {
        var site = ActiveDigSites(Game1.currentLocation.Name).Where(s => Core.UnusualRelics.CanExtend(s, Today))
            .OrderBy(s => s.Points.Where(p => !p.Dug).Min(p => Vector2.DistanceSquared(Game1.player.Tile,new(p.X,p.Y))))
            .FirstOrDefault();
        if (site == null) { Notify(T("unusual.no-hourglass-site")); return false; }
        if (!Core.UnusualRelics.Extend(site, Today)) return false;
        StartRelicAura("hourglass", Game1.player.Position + new Vector2(32,-16));
        Notify(T("unusual.extended", new { name = SiteName(site), count = site.Points.Count(p => !p.Dug), days = site.Expires - Today + 1 }));
        return true;
    }

    private bool UsePocketObservatory()
    {
        if (Game1.timeOfDay < 2000 || !Game1.currentLocation.IsOutdoors) { Notify(T("unusual.stars-night")); return false; }
        Core.UnusualRelics.ExpireLead(State.Stars, Today);
        if (State.Stars.Lead != null || ActiveStarSite() != null) { Notify(T("unusual.stars-existing")); return false; }
        var random = RandomFor("stars:forecast");
        foreach (string culture in Core.Research.Cultures.OrderBy(c => Research.Progress(State,c).Solves).ThenBy(_ => random.Next()))
        foreach (string name in SiteLocations[culture].OrderBy(_ => random.Next()))
        {
            if (name == "Railroad" && Today + 1 < 31) continue;
            var location = Game1.getLocationFromName(name);
            if (location == null || ChooseStarPoints(ReachableTiles(location).Where(t => !OccupiedByDigPoint(name,t)).ToList(),random).Count != 2) continue;
            if (!Core.UnusualRelics.ReserveStars(State,Today,Game1.timeOfDay,true,culture,name)) return false;
            StartRelicAura("stars", Game1.player.Position + new Vector2(32,-32));
            Notify(T("unusual.forecast", new { location = LocationName(name), culture = CultureName(culture) }));
            return true;
        }
        Notify(T("unusual.stars-no-space"));
        return false;
    }

    internal DigSite? ActiveStarSite() => State.Sites.FirstOrDefault(s => s.EventKind == "stars" && s.Expires >= Today && s.Points.Any(p => !p.Dug));

    private static List<DigPoint> ChooseStarPoints(List<Vector2> tiles, Random random)
    {
        foreach (var first in tiles.OrderBy(_ => random.Next()))
        {
            Vector2? second = tiles.Where(t => Vector2.DistanceSquared(t,first) >= 4)
                .OrderBy(t => Vector2.DistanceSquared(t,first)).Select(t => (Vector2?)t).FirstOrDefault();
            if (second.HasValue) return new() { new() { X=(int)first.X,Y=(int)first.Y },new() { X=(int)second.Value.X,Y=(int)second.Value.Y } };
        }
        return new();
    }

    private void RefreshStarSites()
    {
        Core.UnusualRelics.ExpireLead(State.Stars,Today);
        var lead = State.Stars.Lead;
        if (lead == null || Today < lead.Day || ActiveStarSite() != null) return;
        var location = Game1.getLocationFromName(lead.Location);
        if (location == null) return;
        var random = RandomFor(lead.Key);
        var points = ChooseStarPoints(ReachableTiles(location).Where(t => !OccupiedByDigPoint(lead.Location,t)).ToList(),random);
        if (points.Count != 2 || !Core.UnusualRelics.AttachStars(State.Stars,lead,Today)) return;
        State.Sites.Add(new DigSite { Location = lead.Location, Culture = lead.Culture, EventKind = "stars", EventId = lead.Key, Expires = lead.Expires, Points = points });
        Notify(T("unusual.stars-opened",new { location = LocationName(lead.Location) }));
    }

    private void CompleteStarSite(DigSite site)
    {
        if (!Core.UnusualRelics.CompleteStars(State,site)) return;
        Research.Progress(State,site.Culture).Fragments += 8;
        State.Experience += 30;
        Notify(T("unusual.stars-complete"));
    }

    private void StartRelicAura(string kind, Vector2 position)
        => relicAuras.Value = new RelicAura { Kind = kind, Position = position, Location = Game1.currentLocation.NameOrUniqueName, Until = Game1.currentGameTime.TotalGameTime.TotalSeconds + 2 };

    private void DrawUnusualRelics(SpriteBatch b)
    {
        double now = Game1.currentGameTime.TotalGameTime.TotalSeconds;
        var vision = pastVisions.Value;
        if (vision.Location == Game1.currentLocation.Name && vision.Until > now
            && State.Sites.Any(s => s.Key == vision.SiteKey && s.Expires >= Today && s.Points.Any(p => !p.Dug && p.X == vision.Target.X && p.Y == vision.Target.Y)))
        {
            float alpha = Math.Min(1,(float)(vision.Until-now)) * (.65f + .15f * MathF.Sin((float)now * 3));
            for (int i = 1; i < vision.Trail.Count; i += 2)
            {
                var tile = vision.Trail[i];
                var previous = vision.Trail[i-1];
                float angle = MathF.Atan2(tile.Y - previous.Y,tile.X - previous.X) + MathF.PI/2;
                Vector2 at = Game1.GlobalToLocal(new Vector2(tile.X*64+32,tile.Y*64+32));
                b.Draw(PastTexture,at,new Rectangle(0,0,16,16),Color.White * alpha,angle,new Vector2(8,8),2,SpriteEffects.None,1);
            }
            b.Draw(PastTexture,Game1.GlobalToLocal(new Vector2(vision.Target.X*64+32,vision.Target.Y*64-8)),new Rectangle(16,0,16,16),Color.White * alpha,0,new Vector2(8,8),2,SpriteEffects.None,1);
        }
        var aura = relicAuras.Value;
        if (aura.Location != Game1.currentLocation.NameOrUniqueName || aura.Until <= now) return;
        float age = 2-(float)(aura.Until-now), opacity = Math.Min(1,(float)(aura.Until-now));
        var origin = Game1.GlobalToLocal(aura.Position);
        Color color = (aura.Kind == "garden" ? new Color(166,220,132) : aura.Kind == "stars" ? new Color(190,202,255) : new Color(255,218,128)) * opacity;
        for (int i = 0; i < 7; i++)
        {
            float angle = i * MathF.PI*2/7+age;
            var at = origin + new Vector2(MathF.Cos(angle)*(18+age*10),MathF.Sin(angle)*12-age*22);
            int x = (int)at.X/2*2, y=(int)at.Y/2*2;
            if (aura.Kind == "garden")
            {
                b.Draw(Game1.staminaRect,new Rectangle(x,y+6,6,4),color);
                b.Draw(Game1.staminaRect,new Rectangle(x+4,y-4,2,12),color);
            }
            else if (aura.Kind == "stars")
            {
                b.Draw(Game1.staminaRect,new Rectangle(x-4,y,10,2),color);
                b.Draw(Game1.staminaRect,new Rectangle(x,y-4,2,10),color);
            }
            else b.Draw(Game1.staminaRect,new Rectangle(x,y,3,5),color);
        }
    }
}

internal sealed class PastVision
{
    public string Location { get; set; } = "";
    public string SiteKey { get; set; } = "";
    public GridPoint Target { get; set; }
    public List<GridPoint> Trail { get; set; } = new();
    public double Until { get; set; }
}

internal sealed class RelicAura
{
    public string Location { get; set; } = "";
    public string Kind { get; set; } = "";
    public Vector2 Position { get; set; }
    public double Until { get; set; }
}
