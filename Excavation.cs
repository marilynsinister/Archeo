using Archeo.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace Archeo;

public sealed partial class ModEntry
{
    private static readonly Dictionary<string, string[]> SiteLocations = new()
    {
        ["valley"] = new[] { "Forest", "Town", "BusStop", "Backwoods" },
        ["dwarf"] = new[] { "Mountain", "Railroad" },
        ["sea"] = new[] { "Beach" },
        ["calico"] = new[] { "Desert" },
        ["island"] = new[] { "IslandSouth", "IslandEast" }
    };

    internal string LocationName(string name)
    {
        var localized=Helper.Translation.Get("location."+name);
        return localized.HasValue()?localized.ToString():Game1.getLocationFromName(name)?.DisplayName??name;
    }

    private static bool BlockedByWorldStructures(GameLocation location, Vector2 tile)
    {
        var center = new Point((int)tile.X * 64 + 32, (int)tile.Y * 64 + 32);
        return location.resourceClumps.Any(p => p.occupiesTile((int)tile.X,(int)tile.Y))
            || location.buildings.Any(p => p.GetBoundingBox().Contains(center))
            || location.furniture.Any(p => p.GetBoundingBox().Contains(center))
            || location.largeTerrainFeatures != null && location.largeTerrainFeatures.Any(p => p.getBoundingBox().Contains(center));
    }

    private bool ValidDigTile(GameLocation location, Vector2 tile)
    {
        int x = (int)tile.X, y = (int)tile.Y;
        return location.isTileOnMap(x, y) && x >= 2 && y >= 2
            && x < location.Map.Layers[0].LayerWidth - 2 && y < location.Map.Layers[0].LayerHeight - 2
            && !location.isWaterTile(x, y) && location.isTilePassable(tile)
            && !BlockedByWorldStructures(location,tile)
            && !location.Objects.ContainsKey(tile) && (!location.terrainFeatures.TryGetValue(tile, out var terrain) || terrain is HoeDirt { crop: null })
            && location.doesTileHaveProperty(x, y, "Diggable", "Back") != null
            && location.doesTileHaveProperty(x, y, "Action", "Buildings") == null
            && !location.warps.Any(w => Math.Abs(w.X - x) + Math.Abs(w.Y - y) < 3);
    }

    private List<Vector2> ReachableTiles(GameLocation location)
    {
        var result = new List<Vector2>();
        if (location.Map == null) return result;
        var visited = new HashSet<Vector2>();
        var queue = new Queue<Vector2>();
        var seeds = location == Game1.currentLocation ? new List<Vector2> { Game1.player.Tile } : location.warps.Select(p => new Vector2(p.X, p.Y)).ToList();
        foreach (var seed in seeds)
        foreach (var delta in new[] { Vector2.Zero, new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1) })
        {
            var tile = seed + delta;
            if (WalkableForSearch(location, tile) && visited.Add(tile)) queue.Enqueue(tile);
        }
        while (queue.Count > 0)
        {
            var tile = queue.Dequeue();
            if (ValidDigTile(location, tile)) result.Add(tile);
            foreach (var delta in new[] { new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1) })
            {
                var next = tile + delta;
                if (!visited.Add(next) || !WalkableForSearch(location, next)) continue;
                queue.Enqueue(next);
            }
        }
        return result;
    }

    private void RefreshSites(bool force = false)
    {
        if (State.LastSiteDay == Today && !force) return;
        Core.Research.ExpireSites(State, Today);
        if (State.LastSiteDay != Today) State.VanillaSpots.Clear();
        foreach (string culture in Core.Research.Cultures.Where(c => Core.Journey.IsOpen(State, c)))
        {
            Research.Project(State, culture, RandomFor("project:" + culture));
            bool opening = OpeningStory.Active(State);
            if (opening && (State.Opening.Stage != OpeningStage.Searching || FieldLearning.Ready(State, Research.Artifacts))) continue;
            var existingSite = State.Sites.FirstOrDefault(p => p.Culture == culture && p.EventKind == "");
            if (existingSite != null)
            {
                if (opening) { existingSite.EventId = OpeningStory.SiteKey; existingSite.Expires = int.MaxValue; }
                continue;
            }
            var random = RandomFor("site:" + culture);
            foreach (string locationName in SiteLocations[culture].OrderBy(_ => random.Next()))
            {
                var location = Game1.getLocationFromName(locationName);
                // The railroad is physically closed before the summer earthquake.
                if (location == null || locationName == "Railroad" && Game1.stats.DaysPlayed < 31) continue;
                var tiles = ReachableTiles(location).Where(tile => !OccupiedByDigPoint(locationName, tile)).ToList();
                int finds = opening ? OpeningStory.Finds : Config.FindsPerSite;
                if (tiles.Count < finds) continue;
                var anchor = tiles[random.Next(tiles.Count)];
                var candidates = tiles.OrderBy(p => Vector2.DistanceSquared(p, anchor)).Take(90).OrderBy(_ => random.Next()).ToList();
                var site = new DigSite { Location = locationName, Culture = culture, Expires = opening ? int.MaxValue : Today + Config.SiteLifetimeDays - 1,
                    EventId = opening ? OpeningStory.SiteKey : "" };
                foreach (var tile in candidates)
                {
                    if (site.Points.Any(p => Vector2.DistanceSquared(new Vector2(p.X, p.Y), tile) < 4)) continue;
                    site.Points.Add(new DigPoint { X = (int)tile.X, Y = (int)tile.Y });
                    if (site.Points.Count == finds) break;
                }
                if (site.Points.Count < 2) continue;
                State.Sites.Add(site);
                break;
            }
        }
        State.LastSiteDay = Today;
    }

    private void RepairSite(DigSite site, GameLocation location, HashSet<Vector2>? reachable = null)
    {
        List<Vector2>? replacements = null;
        var random = RandomFor("repair:" + site.Culture);
        foreach (var point in site.Points.Where(p => !p.Dug))
        {
            var tile = new Vector2(point.X, point.Y);
            if (ValidDigTile(location, tile) && (reachable == null || reachable.Contains(tile)) && InSiteArea(site,tile) && !OccupiedByDigPoint(site.Location, tile, site)) continue;
            replacements ??= reachable?.Where(p => ValidDigTile(location, p)).ToList() ?? ReachableTiles(location);
            var available = replacements.Where(v => InSiteArea(site,v) && !OccupiedByDigPoint(site.Location, v, site)
                && !site.Points.Any(p => p != point && !p.Dug && Vector2.DistanceSquared(v, new Vector2(p.X, p.Y)) < 4)).ToList();
            if (available.Count == 0) continue;
            tile = available[random.Next(available.Count)];
            point.X = (int)tile.X; point.Y = (int)tile.Y;
            point.Revealed = false;
        }
    }

    internal void Survey()
    {
        if (!Context.IsPlayerFree) return;
        if (!HoldingInstrument) { Notify(T(State.Journey.Started ? "journey.hold-instrument" : "journey.visit-museum")); return; }
        double now = Game1.currentGameTime.TotalGameTime.TotalSeconds;
        if (now < nextSurvey.Value) return;
        nextSurvey.Value = now + .7;
        var location = Game1.currentLocation;
        var found = FindSurveyTarget(location);
        if (found == null) { Notify(T("survey.no-site", new { key = Config.JournalKey.ToString() })); return; }
        var (site, point) = found.Value;
        State.Learning.Events.Add("survey");
        var target = new Vector2(point.X, point.Y);
        float distance = Vector2.Distance(Game1.player.Tile, target);
        bool reveal = point.Revealed || RoutesFromPlayer(location).PathTo(new(point.X, point.Y)).Count - 1 <= Core.Journey.RevealRadius(State);
        if (reveal) { point.Revealed = true; State.Learning.Events.Add("marked"); }
        signals.Value = new SurveySignal { Location = location.Name, SiteKey = site.Key, Target = target, Until = now + (reveal ? 180 : 20), Revealed = reveal, PulseUntil = now + 1.2 };
        RefreshSignalRoute(location, signals.Value);
        string distanceKey = reveal ? "here" : distance <= 7 ? "near" : distance <= 16 ? "medium" : "far";
        Notify(T("survey." + distanceKey, new { direction = Direction(signals.Value.Waypoint - Game1.player.Tile) }));
        Game1.playSound(reveal ? "newArtifact" : "clank");
        SaveState();
    }

    private string Direction(Vector2 offset)
    {
        if (offset.LengthSquared() < 1) return T("direction.here");
        double angle = Math.Atan2(offset.Y, offset.X);
        int octant = ((int)Math.Round(angle / (Math.PI / 4)) + 8) % 8;
        return T("direction." + new[] { "e", "se", "s", "sw", "w", "nw", "n", "ne" }[octant]);
    }

    internal void OnHoeDig(GameLocation location, int x, int y, Farmer who)
    {
        if (!Context.IsWorldReady || states.Value == null || who != Game1.player) return;
        var signal = signals.Value;
        double now = Game1.currentGameTime.TotalGameTime.TotalSeconds;
        var tile = new Vector2(x, y);
        string? key = ActiveDigSites(location.Name).FirstOrDefault(s => s.Points.Any(p => p.X == x && p.Y == y && !p.Dug && p.Revealed))?.Key;
        key ??= signal.Revealed && signal.Location == location.Name && signal.Until >= now && signal.Target == tile ? signal.SiteKey : "";
        if (string.IsNullOrEmpty(key) && resonance.Value.Location == location.Name && resonance.Value.Until >= now)
            resonance.Value.Targets.TryGetValue(tile, out key);
        if (string.IsNullOrEmpty(key))
        {
            if (signal.Location == location.Name && signal.Until >= now && Vector2.Distance(tile, signal.Target) <= 2)
                Notify(T("journey.dig-marked"));
            return;
        }
        if (Vector2.Distance(who.Tile, tile) > 12 || !ValidDigTile(location, tile)
            || !location.terrainFeatures.TryGetValue(tile,out var dugGround) || dugGround is not HoeDirt { crop: null }) return;
        var site = State.Sites.FirstOrDefault(p => p.Key == key && p.Location == location.Name && p.Expires >= Today);
        var point = site?.Points.FirstOrDefault(p => p.X == x && p.Y == y && !p.Dug);
        if (site == null || point == null) return;
        point.Dug = true;
        if (signal.SiteKey == key && signal.Target == tile) signal.Until = 0;
        resonance.Value.Targets.Remove(tile);
        var random = RandomFor($"find:{location.Name}:{x}:{y}:{site.Expires}");
        int amount = AwardFragments(site.Culture, random, false);
        ShowDigFeedback(site, point, amount);
        if (site.Points.All(p => p.Dug))
        {
            if (!OpeningStory.Active(State))
            {
                Research.Progress(State, site.Culture).Keystones++;
                Notify(T("message.site-complete"));
            }
            CompleteWeatherSite(site);
            CompleteShipwreckSite(site);
            CompleteStarSite(site);
            CompleteNatureSite(site);
            CompleteResidentSite(site);
            CompleteExpeditionSite(site);
        }
        SaveState();
    }

    internal void OnVanillaDig(GameLocation location, int x, int y, Farmer who)
    {
        if (!Config.EnableVanillaSpotFragments || !Context.IsWorldReady || states.Value == null || !State.Journey.Started || who != Game1.player) return;
        string spot = $"{location.NameOrUniqueName}:{x}:{y}";
        if (!State.VanillaSpots.Add(spot)) return;
        string culture = location.Name.Contains("Beach", StringComparison.OrdinalIgnoreCase) || location.Name.StartsWith("Island") ? "sea"
            : location.Name.Contains("Mine", StringComparison.OrdinalIgnoreCase) || location.Name is "Mountain" or "Railroad" or "Desert" ? "dwarf" : "valley";
        culture = FrontierResearch.SpotCulture(location.Name,Core.Journey.IsOpen(State,"calico"),Core.Journey.IsOpen(State,"island"),culture);
        if (!RouteStories.AllowsField(State,culture)) culture = "valley";
        AwardFragments(OpeningStory.Active(State) ? "valley" : culture, RandomFor("vanilla:" + spot), true);
        SaveState();
    }

    private int AwardFragments(string culture, Random random, bool vanilla)
    {
        var progress = Research.Progress(State, culture);
        int amount = Math.Max(1, (int)Math.Round((vanilla ? random.Next(2, 5) : random.Next(5, 10)) * Config.FragmentMultiplier));
        amount += Core.Research.Level(State.Experience) / 4 + Mastery.FragmentBonus(State);
        if (OpeningStory.Active(State) && !vanilla) amount = Math.Max(amount, 5);
        progress.Fragments += amount;
        State.Finds++;
        State.Experience += vanilla ? 8 : 15;
        if (!OpeningStory.Active(State) && random.NextDouble() < .12) { progress.Keystones++; Notify(T("message.keystone", new { culture = CultureName(culture) })); }
        if (vanilla) Notify(T("message.fragments", new { amount, culture = CultureName(culture) }));
        Game1.playSound("coin");
        OnWorkshopFind(culture);
        return amount;
    }

    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (!Context.IsWorldReady || states.Value == null || Game1.eventUp) return;
        DrawCompanion(e.SpriteBatch);
        DrawBellPulse(e.SpriteBatch);
        DrawUnusualRelics(e.SpriteBatch);
        DrawResonance(e.SpriteBatch);
        DrawFieldFeedback(e.SpriteBatch);
        var signal = signals.Value;
        double now = Game1.currentGameTime.TotalGameTime.TotalSeconds;
        DrawRevealedPoints(e.SpriteBatch, now);
        if (signal.Until <= now || signal.Location != Game1.currentLocation.Name) return;
        var b = e.SpriteBatch;
        float distance = Vector2.Distance(Game1.player.Tile, signal.Target);
        Color color = signal.Revealed ? new Color(255, 216, 112) : distance < 7 ? Color.LightGreen : distance < 16 ? Color.Gold : Color.IndianRed;
        Vector2 origin = Game1.GlobalToLocal(Game1.player.Position + new Vector2(32, -28));
        if (!signal.Revealed)
        {
            float rotation = MathF.Atan2(signal.Waypoint.Y - Game1.player.Tile.Y, signal.Waypoint.X - Game1.player.Tile.X);
            b.Draw(Game1.staminaRect, origin, new Rectangle(0, 0, 1, 1), color, rotation, new Vector2(0, .5f), new Vector2(45, 5), SpriteEffects.None, 1);
            b.Draw(Game1.staminaRect, origin + new Vector2(MathF.Cos(rotation), MathF.Sin(rotation)) * 44, null, color, rotation + MathF.PI / 4, new Vector2(.5f), 12, SpriteEffects.None, 1);
        }
    }

    private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
    {
        if (!Config.ShowHud || !Context.IsWorldReady || states.Value == null || Game1.activeClickableMenu != null || Game1.eventUp) return;
        DrawTrackedObjective(e.SpriteBatch);
        var site = HudDigSite();
        if (site == null) return;
        string text = T("journey.hud-progress", new { culture = SiteName(site), found = site.Points.Count(p => p.Dug), total = site.Points.Count });
        var size = Game1.smallFont.MeasureString(text) * .85f;
        var rect = new Rectangle(20, Game1.uiViewport.Height - 164, (int)size.X + 30, 40);
        e.SpriteBatch.Draw(Game1.staminaRect, rect, new Color(41, 31, 28) * .85f);
        e.SpriteBatch.DrawString(Game1.smallFont, text, new Vector2(rect.X + 15, rect.Y + 7), new Color(255, 230, 178), 0, Vector2.Zero, .85f, SpriteEffects.None, 1);
    }
}
