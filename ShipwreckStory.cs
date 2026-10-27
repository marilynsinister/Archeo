using Archeo.Core;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using SObject = StardewValley.Object;

namespace Archeo;

public sealed partial class ModEntry
{
    internal Texture2D ShipwreckAtlas { get; private set; } = null!;
    private readonly PerScreen<BellPulse> bellPulses = new(() => new());
    private bool bellHookReady;
    internal bool NearWilly => Game1.currentLocation.characters.Any(p => p.Name == "Willy" && Vector2.Distance(p.Tile, Game1.player.Tile) <= 4);
    internal string SiteName(DigSite site) => site.EventKind == "stars" ? T("unusual.star-site") : Shipwreck.Kinds.Contains(site.EventKind) ? T("shipwreck.site." + site.EventKind)
        : WeatherExpeditions.Kinds.Contains(site.EventKind) ? T("weather.name." + site.EventKind)
        : ResidentStories.Kinds.Contains(site.EventKind) ? T("residents.site." + ResidentStories.All.First(p => p.SiteKind == site.EventKind).Id)
        : NaturalEvents.Kinds.Contains(site.EventKind) ? T("nature.name." + site.EventKind) : CultureName(site.Culture);

    private void DiscoverShipwreck()
    {
        if (!Core.Shipwreck.Discover(State.Shipwreck, State.Restored)) return;
        Notify(T("shipwreck.discovered"));
    }

    private void RememberShipwreckWeather()
        => Core.Shipwreck.Observe(State.Shipwreck, Today, Game1.getFarm().IsRainingHere());

    private void RefreshShipwreckSites()
    {
        DiscoverShipwreck();
        RememberShipwreckWeather();
        string? kind = Core.Shipwreck.SiteKind(State.Shipwreck, Today);
        State.Sites.RemoveAll(s => Core.Shipwreck.Kinds.Contains(s.EventKind) && (s.EventKind != kind || s.Expires < Today || s.Points.All(p => p.Dug)));
        if (kind == null || State.Sites.Any(s => s.EventKind == kind)) return;
        var location = Game1.getLocationFromName("Beach");
        if (location == null) return;
        // The western beach is accessible without repairing the tide-pool bridge.
        var tiles = ReachableTiles(location).Where(t => t.X < location.Map.Layers[0].LayerWidth / 2
            && !OccupiedByDigPoint("Beach", t)).ToList();
        int count = kind == "ship_bottle" ? 1 : 3;
        var random = RandomFor("ship:" + kind);
        var chosen = new List<Vector2>();
        if (tiles.Count == 0) return;
        var anchor = tiles[random.Next(tiles.Count)];
        tiles = tiles.OrderBy(t => Vector2.DistanceSquared(t, anchor)).Take(45).ToList();
        foreach (var tile in tiles.OrderBy(_ => random.Next()))
        {
            if (chosen.Any(p => Vector2.DistanceSquared(p, tile) < 4)) continue;
            chosen.Add(tile);
            if (chosen.Count == count) break;
        }
        if (chosen.Count < count) return; // An edited map can be retried without granting or consuming story progress.
        State.Sites.Add(new DigSite
        {
            Location = "Beach", Culture = "sea", EventKind = kind,
            EventId = kind == "ship_bottle" ? "ship:bottle:" + Today : "ship:cache",
            Expires = kind == "ship_bottle" ? State.Shipwreck.BottleUntil : int.MaxValue,
            Points = chosen.Select(t => new DigPoint { X = (int)t.X, Y = (int)t.Y }).ToList()
        });
        Notify(T("shipwreck.site-found", new { name = T("shipwreck.site." + kind) }));
    }

    private void CompleteShipwreckSite(DigSite site)
    {
        if (!Core.Shipwreck.CompleteSite(State.Shipwreck, site, Today)) return;
        State.Experience += site.EventKind == "ship_bottle" ? 60 : 120;
        Notify(T(site.EventKind == "ship_bottle" ? "shipwreck.bottle-found" : "shipwreck.cache-found"));
    }

    internal bool CanShipwreckAction(string action) => action switch
    {
        "study" => State.Shipwreck.Stage == ShipwreckStage.Buckle && Game1.currentLocation.Name == "ArchaeologyHouse",
        "consult" => State.Shipwreck.Stage == ShipwreckStage.PortRecord && NearWilly,
        "decode" => State.Shipwreck.Stage == ShipwreckStage.Letter && NearWilly,
        "claim" => State.Shipwreck.Stage == ShipwreckStage.Recovered && bellHookReady,
        _ => false
    };

    internal void ShipwreckAction(string action)
    {
        if (!CanShipwreckAction(action)) { Notify(T("shipwreck.requirements")); return; }
        var story = State.Shipwreck;
        string? dialogue = null;
        switch (action)
        {
            case "study":
                if (!Core.Shipwreck.Study(story, true)) return;
                dialogue = "gunther";
                break;
            case "consult":
                if (!Core.Shipwreck.Consult(story, true)) return;
                dialogue = "willy";
                break;
            case "decode":
                if (!Core.Shipwreck.Decode(story, true)) return;
                dialogue = "decode";
                RefreshShipwreckSites();
                break;
            case "claim":
                var item = ItemRegistry.Create("(BC)" + ItemId(Core.Shipwreck.BellId));
                if (!Game1.player.couldInventoryAcceptThisItem(item)) { Notify(T("message.inventory-full")); return; }
                if (!Game1.player.addItemToInventoryBool(item)) return;
                Core.Shipwreck.RecordClaim(story, true);
                Game1.playSound("reward");
                Notify(T("shipwreck.reward-received"));
                break;
        }
        SaveState();
        if (dialogue != null) Game1.drawObjectDialogue(T("shipwreck.dialogue." + dialogue));
    }

    private bool TryShipwreckInteraction(Vector2 tile)
    {
        if (CanShipwreckAction("study") && Vector2.Distance(Game1.player.Tile, tile) <= 2.5f)
        {
            string? action = Game1.currentLocation.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Action", "Buildings");
            if (action?.Contains("Gunther", StringComparison.OrdinalIgnoreCase) == true) { ShipwreckAction("study"); return true; }
        }
        if (NearWilly && State.Shipwreck.Stage is ShipwreckStage.PortRecord or ShipwreckStage.Letter)
        {
            var willy = Game1.currentLocation.characters.First(p => p.Name == "Willy");
            if (willy.GetBoundingBox().Intersects(new Rectangle((int)tile.X * 64, (int)tile.Y * 64, 64, 64)))
            { ShipwreckAction(State.Shipwreck.Stage == ShipwreckStage.Letter ? "decode" : "consult"); return true; }
        }
        return false;
    }

    internal void RingShipBell(SObject bell, Farmer who)
    {
        if (!Context.IsWorldReady || states.Value == null || !who.IsLocalPlayer || who != Game1.player) return;
        if (!Game1.currentLocation.Objects.TryGetValue(bell.TileLocation, out var placed) || placed != bell || Vector2.Distance(who.Tile, bell.TileLocation) > 3) return;
        int lastUse = State.LastUse.GetValueOrDefault(Core.Shipwreck.BellId, -1);
        if (lastUse == Today) { Notify(T("shipwreck.bell-used")); return; }
        if (!Core.Shipwreck.CanRing(lastUse, Today, who.Stamina, who.MaxStamina)) { Notify(T("relic.full-energy")); return; }
        float recovered = Math.Min(Core.Shipwreck.BellEnergy, who.MaxStamina - who.Stamina);
        who.Stamina += recovered;
        State.LastUse[Core.Shipwreck.BellId] = Today;
        bellPulses.Value = new BellPulse { Location = Game1.currentLocation.NameOrUniqueName, Position = bell.TileLocation * 64 + new Vector2(32, 0), Until = Game1.currentGameTime.TotalGameTime.TotalSeconds + 1.4 };
        SaveState();
        Game1.playSound("crystal");
        Notify(T("shipwreck.bell-energy", new { energy = Math.Round(recovered, 1) }));
    }

    private bool TryShipBellInteraction(Vector2 tile)
    {
        if (!Game1.currentLocation.Objects.TryGetValue(tile, out var bell) || bell.QualifiedItemId != "(BC)" + ItemId(Core.Shipwreck.BellId)) return false;
        RingShipBell(bell, Game1.player);
        return true;
    }

    private void DrawBellPulse(SpriteBatch b)
    {
        var pulse = bellPulses.Value;
        double now = Game1.currentGameTime.TotalGameTime.TotalSeconds;
        if (pulse.Location != Game1.currentLocation.NameOrUniqueName || pulse.Until <= now) return;
        float age = 1.4f - (float)(pulse.Until - now);
        Vector2 at = Game1.GlobalToLocal(pulse.Position);
        for (int ring = 0; ring < 3; ring++)
        {
            float progress = age - ring * .2f;
            if (progress < 0 || progress > 1) continue;
            float radius = 14 + progress * 55;
            for (int i = 0; i < 16; i++)
            {
                float angle = i * MathF.PI / 8;
                var point = at + new Vector2(MathF.Cos(angle), MathF.Sin(angle) * .55f) * radius;
                b.Draw(Game1.staminaRect, new Rectangle((int)point.X / 4 * 4, (int)point.Y / 4 * 4, 4, 4), new Color(255, 216, 112) * (1 - progress));
            }
        }
    }

    private void InstallShipwreckPatch(Harmony harmony)
    {
        ShipwreckPatches.Mod = this;
        harmony.Patch(AccessTools.DeclaredMethod(typeof(SObject), nameof(SObject.checkForAction), new[] { typeof(Farmer), typeof(bool) }),
            prefix: new HarmonyMethod(typeof(ShipwreckPatches), nameof(ShipwreckPatches.BellAction)));
        bellHookReady = true;
    }
}

internal sealed class BellPulse
{
    public string Location { get; set; } = "";
    public Vector2 Position { get; set; }
    public double Until { get; set; }
}

internal static class ShipwreckPatches
{
    internal static ModEntry Mod = null!;
    internal static bool BellAction(SObject __instance, Farmer __0, bool __1, ref bool __result)
    {
        if (__instance.QualifiedItemId != "(BC)" + ModEntry.ItemId(Shipwreck.BellId)) return true;
        __result = true;
        if (!__1) Mod.RingShipBell(__instance, __0);
        return false;
    }
}
