using Archeo.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace Archeo;

public sealed partial class ModEntry
{
    private readonly PerScreen<ResonancePulse> resonance = new(() => new());
    internal IEnumerable<DigSite> ActiveDigSites(string location)
        => State.Sites.Where(s => s.Location == location && s.Expires >= Today && s.Points.Any(p => !p.Dug));

    private bool OccupiedByDigPoint(string location, Vector2 tile, DigSite? except = null)
        => ActiveDigSites(location).Where(s => s != except).SelectMany(s => s.Points).Any(p => !p.Dug && Vector2.DistanceSquared(tile, new(p.X, p.Y)) < 4);

    private (DigSite Site, DigPoint Point)? FindSurveyTarget(GameLocation location)
    {
        var sites = ActiveDigSites(location.Name).ToList();
        foreach (var site in sites) RepairSite(site, location);
        return sites.SelectMany(site => site.Points.Where(p => !p.Dug && ValidDigTile(location, new(p.X, p.Y)))
                .Select(point => (Site: site, Point: point)))
            .OrderBy(p => Shipwreck.Kinds.Contains(p.Site.EventKind) || ResidentStories.Kinds.Contains(p.Site.EventKind) ? 0 : p.Site.EventKind.Length > 0 ? 1 : 2)
            .ThenBy(p => Vector2.DistanceSquared(Game1.player.Tile, new(p.Point.X, p.Point.Y)))
            .Select(p => ((DigSite Site, DigPoint Point)?)p).FirstOrDefault();
    }

    private void RememberWeather()
    {
        var valley = Game1.getFarm();
        WeatherExpeditions.Observe(State.Weather, Today, valley.IsRainingHere(), valley.IsLightningHere(), Config.EnableWeatherEvents,
            State.Sites.Any(s => WeatherExpeditions.Kinds.Contains(s.EventKind) && s.Expires >= Today && s.Points.Any(p => !p.Dug)), Config.WeatherSiteLifetimeDays, Config.WeatherEventCooldownDays);
    }

    private void RefreshWeatherSites()
    {
        RememberWeather();
        var lead = State.Weather.Lead;
        if (!Config.EnableWeatherEvents || lead == null || lead.Expires < Today
            || State.Sites.Any(s => WeatherExpeditions.Kinds.Contains(s.EventKind) && s.Expires >= Today && s.Points.Any(p => !p.Dug))) return;
        string[] names = lead.Kind == "rain" ? new[] { "Forest", "Backwoods", "Town" } : new[] { "Mountain", "Forest", "Backwoods" };
        var random = RandomFor(lead.Key);
        foreach (string name in names)
        {
            var location = Game1.getLocationFromName(name);
            if (location == null) continue;
            var tiles = ReachableTiles(location).Where(tile => !OccupiedByDigPoint(name, tile)).ToList();
            if (tiles.Count < 3) continue;
            var anchor = tiles[random.Next(tiles.Count)];
            var site = new DigSite { Location = name, Culture = WeatherExpeditions.Culture(lead.Kind), EventKind = lead.Kind, EventId = lead.Key, Expires = lead.Expires };
            foreach (var tile in tiles.OrderBy(t => Vector2.DistanceSquared(t, anchor)).Take(70).OrderBy(_ => random.Next()))
            {
                if (site.Points.Any(p => Vector2.DistanceSquared(tile, new(p.X, p.Y)) < 4)) continue;
                site.Points.Add(new DigPoint { X = (int)tile.X, Y = (int)tile.Y });
                if (site.Points.Count == 3) break;
            }
            if (site.Points.Count < 3) continue;
            if (!WeatherExpeditions.Attach(State.Weather, lead, Today)) return;
            State.Sites.Add(site);
            Notify(T("weather.discovered", new { name = T("weather.name." + lead.Kind), location = LocationName(name) }));
            return;
        }
        // A lead survives until its original expiry if an edited map has no safe spot yet.
    }

    private void CompleteWeatherSite(DigSite site)
    {
        if (!WeatherExpeditions.Complete(State, site)) return;
        Research.Progress(State, site.Culture).Fragments += 12;
        State.Experience += 45;
        Notify(T("weather.completed", new { name = T("weather.name." + site.EventKind) }));
        if (State.Weather.PendingRewards.Contains(WeatherExpeditions.Artifact(site.EventKind))) Notify(T("weather.reward-waiting"));
    }

    internal void ClaimWeatherReward(string artifactId)
    {
        if (!State.Weather.PendingRewards.Contains(artifactId)) return;
        var artifact = Research.Artifacts.FirstOrDefault(p => p.Id == artifactId && WeatherExpeditions.Kinds.Contains(p.Source));
        if (artifact == null) return;
        var item = ItemRegistry.Create(QualifiedId(artifact));
        if (!Game1.player.couldInventoryAcceptThisItem(item)) { Notify(T("message.inventory-full")); return; }
        if (!Game1.player.addItemToInventoryBool(item)) return;
        WeatherExpeditions.RecordClaim(State, artifactId);
        SaveState();
        Game1.playSound("reward");
        Notify(T("message.restored", new { name = ArtifactName(artifact) }));
    }

    private bool UseRainPitcher()
    {
        var center = Game1.player.GetGrabTile();
        int watered = 0;
        for (int x = -1; x <= 1; x++)
        for (int y = -1; y <= 1; y++)
            if (Game1.currentLocation.terrainFeatures.TryGetValue(center + new Vector2(x, y), out var feature)
                && feature is HoeDirt soil && soil.state.Value == 0)
            { soil.state.Value = 1; watered++; }
        if (watered == 0) { Notify(T("weather.no-soil")); return false; }
        Game1.playSound("wateringCan");
        Notify(T("weather.watered", new { count = watered }));
        return true;
    }

    private bool UseStormFork()
    {
        var location = Game1.currentLocation;
        var pulse = new ResonancePulse { Location = location.Name, Until = Game1.currentGameTime.TotalGameTime.TotalSeconds + 30 };
        foreach (var site in ActiveDigSites(location.Name))
        {
            RepairSite(site, location);
            foreach (var point in site.Points.Where(p => !p.Dug && Vector2.DistanceSquared(Game1.player.Tile, new(p.X, p.Y)) <= 64))
                if (ValidDigTile(location, new(point.X, point.Y))) pulse.Targets[new(point.X, point.Y)] = site.Key;
        }
        if (pulse.Targets.Count == 0) { Notify(T("weather.no-resonance")); return false; }
        resonance.Value = pulse;
        Notify(T("weather.resonance", new { count = pulse.Targets.Count }));
        return true;
    }

    private void DrawResonance(SpriteBatch b)
    {
        var pulse = resonance.Value;
        double now = Game1.currentGameTime.TotalGameTime.TotalSeconds;
        if (pulse.Location != Game1.currentLocation.Name || pulse.Until <= now) return;
        foreach (var target in pulse.Targets)
        {
            var site = State.Sites.FirstOrDefault(s => s.Key == target.Value && s.Expires >= Today);
            if (site == null || !site.Points.Any(p => !p.Dug && p.X == target.Key.X && p.Y == target.Key.Y)) continue;
            Vector2 at = Game1.GlobalToLocal(target.Key * 64);
            Color color = new Color(138, 212, 255) * (.7f + .2f * (float)Math.Sin(now * 5));
            foreach (var rect in new[] { new Rectangle(6,6,52,3), new Rectangle(6,55,52,3), new Rectangle(6,6,3,52), new Rectangle(55,6,3,52) })
                b.Draw(Game1.staminaRect, new Rectangle((int)at.X + rect.X, (int)at.Y + rect.Y, rect.Width, rect.Height), color);
        }
    }
}

internal sealed class ResonancePulse
{
    public string Location { get; set; } = "";
    public double Until { get; set; }
    public Dictionary<Vector2, string> Targets { get; } = new();
}
