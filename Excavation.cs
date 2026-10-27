using Archeo.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Archeo;

public sealed partial class ModEntry
{
    private static readonly Dictionary<string, string[]> SiteLocations = new()
    {
        ["valley"] = new[] { "Forest", "Town", "BusStop", "Backwoods" },
        ["dwarf"] = new[] { "Mountain", "Railroad" },
        ["sea"] = new[] { "Beach" }
    };

    internal string LocationName(string name) => Game1.getLocationFromName(name)?.DisplayName ?? name;

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
            && location.isTileLocationOpen(tile)
            && !BlockedByWorldStructures(location,tile)
            && !location.Objects.ContainsKey(tile) && !location.terrainFeatures.ContainsKey(tile)
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
        var seeds = location.warps.Select(p => new Vector2(p.X, p.Y)).ToList();
        if (location == Game1.currentLocation) seeds.Insert(0, Game1.player.Tile);
        foreach (var seed in seeds)
        foreach (var delta in new[] { Vector2.Zero, new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1) })
        {
            var tile = seed + delta;
            if (location.isTileOnMap(tile) && !location.isWaterTile((int)tile.X, (int)tile.Y) && location.isTilePassable(tile)
                && location.isTileLocationOpen(tile) && !location.Objects.ContainsKey(tile) && !location.terrainFeatures.ContainsKey(tile)
                && !BlockedByWorldStructures(location,tile) && visited.Add(tile)) queue.Enqueue(tile);
        }
        while (queue.Count > 0)
        {
            var tile = queue.Dequeue();
            if (ValidDigTile(location, tile)) result.Add(tile);
            foreach (var delta in new[] { new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1) })
            {
                var next = tile + delta;
                if (!location.isTileOnMap(next) || !visited.Add(next) || location.isWaterTile((int)next.X, (int)next.Y) || !location.isTilePassable(next)) continue;
                if (!location.isTileLocationOpen(next) || location.Objects.ContainsKey(next) || location.terrainFeatures.ContainsKey(next) || BlockedByWorldStructures(location,next)) continue;
                queue.Enqueue(next);
            }
        }
        return result;
    }

    private void RefreshSites()
    {
        if (State.LastSiteDay == Today) return;
        Core.Research.ExpireSites(State, Today);
        State.VanillaSpots.Clear();
        foreach (string culture in Core.Research.Cultures)
        {
            if (State.Sites.Any(p => p.Culture == culture && p.EventKind == "")) continue;
            var random = RandomFor("site:" + culture);
            foreach (string locationName in SiteLocations[culture].OrderBy(_ => random.Next()))
            {
                var location = Game1.getLocationFromName(locationName);
                // The railroad is physically closed before the summer earthquake.
                if (location == null || locationName == "Railroad" && Game1.stats.DaysPlayed < 31) continue;
                var tiles = ReachableTiles(location).Where(tile => !OccupiedByDigPoint(locationName, tile)).ToList();
                if (tiles.Count < Config.FindsPerSite) continue;
                var anchor = tiles[random.Next(tiles.Count)];
                var candidates = tiles.OrderBy(p => Vector2.DistanceSquared(p, anchor)).Take(90).OrderBy(_ => random.Next()).ToList();
                var site = new DigSite { Location = locationName, Culture = culture, Expires = Today + Config.SiteLifetimeDays - 1 };
                foreach (var tile in candidates)
                {
                    if (site.Points.Any(p => Vector2.DistanceSquared(new Vector2(p.X, p.Y), tile) < 4)) continue;
                    site.Points.Add(new DigPoint { X = (int)tile.X, Y = (int)tile.Y });
                    if (site.Points.Count == Config.FindsPerSite) break;
                }
                if (site.Points.Count < 2) continue;
                State.Sites.Add(site);
                break;
            }
        }
        State.LastSiteDay = Today;
    }

    private void RepairSite(DigSite site, GameLocation location)
    {
        List<Vector2>? replacements = null;
        var random = RandomFor("repair:" + site.Culture);
        foreach (var point in site.Points.Where(p => !p.Dug))
        {
            var tile = new Vector2(point.X, point.Y);
            if (ValidDigTile(location, tile) && InSiteArea(site,tile) && !OccupiedByDigPoint(site.Location, tile, site)) continue;
            replacements ??= ReachableTiles(location);
            var available = replacements.Where(v => InSiteArea(site,v) && !OccupiedByDigPoint(site.Location, v, site)
                && !site.Points.Any(p => p != point && !p.Dug && Vector2.DistanceSquared(v, new Vector2(p.X, p.Y)) < 4)).ToList();
            if (available.Count == 0) continue;
            tile = available[random.Next(available.Count)];
            point.X = (int)tile.X; point.Y = (int)tile.Y;
        }
    }

    internal void Survey()
    {
        if (!Context.IsPlayerFree) return;
        double now = Game1.currentGameTime.TotalGameTime.TotalSeconds;
        if (now < nextSurvey.Value) return;
        nextSurvey.Value = now + .7;
        var location = Game1.currentLocation;
        var found = FindSurveyTarget(location);
        if (found == null) { Notify(T("survey.no-site", new { key = Config.JournalKey.ToString() })); return; }
        var (site, point) = found.Value;
        var target = new Vector2(point.X, point.Y);
        float distance = Vector2.Distance(Game1.player.Tile, target);
        bool reveal = distance <= 2.5f;
        signals.Value = new SurveySignal { Location = location.Name, SiteKey = site.Key, Target = target, Until = now + (reveal ? 90 : 8), Revealed = reveal };
        string distanceKey = reveal ? "here" : distance <= 7 ? "near" : distance <= 16 ? "medium" : "far";
        Notify(T("survey." + distanceKey, new { direction = Direction(target - Game1.player.Tile) }));
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
        string? key = signal.Revealed && signal.Location == location.Name && signal.Until >= now && signal.Target == tile ? signal.SiteKey : "";
        if (key.Length == 0 && resonance.Value.Location == location.Name && resonance.Value.Until >= now)
            resonance.Value.Targets.TryGetValue(tile, out key);
        if (string.IsNullOrEmpty(key) || Vector2.Distance(who.Tile, tile) > 3.5f) return;
        var site = State.Sites.FirstOrDefault(p => p.Key == key && p.Location == location.Name && p.Expires >= Today);
        var point = site?.Points.FirstOrDefault(p => p.X == x && p.Y == y && !p.Dug);
        if (site == null || point == null) return;
        point.Dug = true;
        if (signal.SiteKey == key && signal.Target == tile) signal.Until = 0;
        resonance.Value.Targets.Remove(tile);
        var random = RandomFor($"find:{location.Name}:{x}:{y}:{site.Expires}");
        AwardFragments(site.Culture, random, false);
        if (site.Points.All(p => p.Dug))
        {
            Research.Progress(State, site.Culture).Keystones++;
            Notify(T("message.site-complete"));
            CompleteWeatherSite(site);
            CompleteShipwreckSite(site);
            CompleteStarSite(site);
            CompleteNatureSite(site);
            CompleteResidentSite(site);
        }
        SaveState();
    }

    internal void OnVanillaDig(GameLocation location, int x, int y, Farmer who)
    {
        if (!Config.EnableVanillaSpotFragments || !Context.IsWorldReady || states.Value == null || who != Game1.player) return;
        string spot = $"{location.NameOrUniqueName}:{x}:{y}";
        if (!State.VanillaSpots.Add(spot)) return;
        string culture = location.Name.Contains("Beach", StringComparison.OrdinalIgnoreCase) || location.Name.StartsWith("Island") ? "sea"
            : location.Name.Contains("Mine", StringComparison.OrdinalIgnoreCase) || location.Name is "Mountain" or "Railroad" or "Desert" ? "dwarf" : "valley";
        AwardFragments(culture, RandomFor("vanilla:" + spot), true);
        SaveState();
    }

    private void AwardFragments(string culture, Random random, bool vanilla)
    {
        var progress = Research.Progress(State, culture);
        int amount = Math.Max(1, (int)Math.Round((vanilla ? random.Next(2, 5) : random.Next(5, 10)) * Config.FragmentMultiplier));
        amount += Core.Research.Level(State.Experience) / 4;
        progress.Fragments += amount;
        State.Finds++;
        State.Experience += vanilla ? 8 : 15;
        if (random.NextDouble() < .12) { progress.Keystones++; Notify(T("message.keystone", new { culture = CultureName(culture) })); }
        Notify(T("message.fragments", new { amount, culture = CultureName(culture) }));
        Game1.playSound("coin");
        OnWorkshopFind(culture);
    }

    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (!Context.IsWorldReady || states.Value == null || Game1.eventUp) return;
        DrawCompanion(e.SpriteBatch);
        DrawBellPulse(e.SpriteBatch);
        DrawUnusualRelics(e.SpriteBatch);
        DrawResonance(e.SpriteBatch);
        var signal = signals.Value;
        double now = Game1.currentGameTime.TotalGameTime.TotalSeconds;
        if (signal.Until <= now || signal.Location != Game1.currentLocation.Name) return;
        var b = e.SpriteBatch;
        float distance = Vector2.Distance(Game1.player.Tile, signal.Target);
        Color color = signal.Revealed ? new Color(255, 216, 112) : distance < 7 ? Color.LightGreen : distance < 16 ? Color.Gold : Color.IndianRed;
        Vector2 origin = Game1.GlobalToLocal(Game1.player.Position + new Vector2(32, -28));
        if (signal.Revealed)
        {
            Vector2 tile = Game1.GlobalToLocal(signal.Target * 64);
            float pulse = .65f + .25f * (float)Math.Sin(now * 5);
            b.Draw(Game1.staminaRect, new Rectangle((int)tile.X + 7, (int)tile.Y + 7, 50, 3), color * pulse);
            b.Draw(Game1.staminaRect, new Rectangle((int)tile.X + 7, (int)tile.Y + 54, 50, 3), color * pulse);
            b.Draw(Game1.staminaRect, new Rectangle((int)tile.X + 7, (int)tile.Y + 7, 3, 50), color * pulse);
            b.Draw(Game1.staminaRect, new Rectangle((int)tile.X + 54, (int)tile.Y + 7, 3, 50), color * pulse);
            b.Draw(Atlas, tile + new Vector2(16, -12 + (float)Math.Sin(now * 4) * 3), new Rectangle(7 * 16, 2 * 16, 16, 16), color, 0, Vector2.Zero, 2, SpriteEffects.None, 1);
        }
        else
        {
            float rotation = MathF.Atan2(signal.Target.Y - Game1.player.Tile.Y, signal.Target.X - Game1.player.Tile.X);
            b.Draw(Game1.staminaRect, origin, new Rectangle(0, 0, 1, 1), color, rotation, new Vector2(0, .5f), new Vector2(45, 5), SpriteEffects.None, 1);
            b.Draw(Game1.staminaRect, origin + new Vector2(MathF.Cos(rotation), MathF.Sin(rotation)) * 44, null, color, rotation + MathF.PI / 4, new Vector2(.5f), 12, SpriteEffects.None, 1);
        }
    }

    private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
    {
        if (!Config.ShowHud || !Context.IsWorldReady || states.Value == null || Game1.activeClickableMenu != null || Game1.eventUp) return;
        DrawTrackedObjective(e.SpriteBatch);
        var site = ActiveDigSites(Game1.currentLocation.Name).OrderBy(p => p.EventKind.Length == 0 ? 1 : 0).FirstOrDefault();
        if (site == null) return;
        string text = T("hud.site", new { culture = SiteName(site), count = site.Points.Count(p => !p.Dug), key = Config.SurveyKey.ToString() });
        var size = Game1.smallFont.MeasureString(text) * .85f;
        var rect = new Rectangle(20, Game1.uiViewport.Height - 164, (int)size.X + 30, 40);
        e.SpriteBatch.Draw(Game1.staminaRect, rect, new Color(41, 31, 28) * .85f);
        e.SpriteBatch.DrawString(Game1.smallFont, text, new Vector2(rect.X + 15, rect.Y + 7), new Color(255, 230, 178), 0, Vector2.Zero, .85f, SpriteEffects.None, 1);
    }
}
