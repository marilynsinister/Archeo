using Archeo.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace Archeo;

public sealed partial class ModEntry
{
    private readonly PerScreen<List<DigFeedback>> digFeedback = new(() => new());
    private bool WalkableForSearch(GameLocation location, Vector2 tile)
        => location.isTileOnMap(tile) && !location.isWaterTile((int)tile.X, (int)tile.Y)
            && location.isTilePassable(new xTile.Dimensions.Location((int)tile.X, (int)tile.Y), Game1.viewport)
            && !location.Objects.ContainsKey(tile) && !BlockedByWorldStructures(location, tile)
            && (!location.terrainFeatures.TryGetValue(tile, out var terrain) || terrain.isPassable(Game1.player));

    private FieldRoute RoutesFromPlayer(GameLocation location)
        => new(new GridPoint((int)Game1.player.Tile.X, (int)Game1.player.Tile.Y), p => WalkableForSearch(location, new Vector2(p.X, p.Y)));

    private void RefreshSignalRoute(GameLocation location, SurveySignal signal)
    {
        var path = RoutesFromPlayer(location).PathTo(new((int)signal.Target.X, (int)signal.Target.Y));
        signal.LastPlayerTile = Game1.player.Tile;
        signal.NextRouteCheck = Game1.currentGameTime.TotalGameTime.TotalSeconds + .35;
        if (path.Count == 0) { signal.Until = 0; return; }
        var waypoint = FieldRoute.Waypoint(path);
        signal.Waypoint = new(waypoint.X, waypoint.Y);
        if (!signal.Revealed && path.Count - 1 <= Core.Journey.RevealRadius(State))
        {
            var point = State.Sites.FirstOrDefault(p => p.Key == signal.SiteKey)?.Points.FirstOrDefault(p => p.X == signal.Target.X && p.Y == signal.Target.Y && !p.Dug);
            if (point == null) { signal.Until = 0; return; }
            point.Revealed = signal.Revealed = true;
            State.Learning.Events.Add("marked");
            signal.Until = Game1.currentGameTime.TotalGameTime.TotalSeconds + 180;
            Game1.playSound("newArtifact");
            Notify(T("journey.marked"));
            SaveState();
        }
    }

    private void UpdateFieldSearch(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || states.Value == null || !e.IsMultipleOf(6) || Game1.activeClickableMenu != null || Game1.eventUp) return;
        var signal = signals.Value;
        double now = Game1.currentGameTime.TotalGameTime.TotalSeconds;
        digFeedback.Value.RemoveAll(p => now - p.Started > 6);
        if (signal.Location != Game1.currentLocation.Name || signal.Until < now || signal.Revealed) return;
        if (signal.LastPlayerTile != Game1.player.Tile && now >= signal.NextRouteCheck) RefreshSignalRoute(Game1.currentLocation, signal);
    }

    private DigSite? HudDigSite()
    {
        string location = Game1.currentLocation.Name;
        double now = Game1.currentGameTime.TotalGameTime.TotalSeconds;
        var recent = digFeedback.Value.LastOrDefault(p => p.Site.Location == location && now - p.Started <= 6);
        if (recent != null) return recent.Site;
        var signal = signals.Value;
        if (signal.Location == location && signal.Until >= now)
        {
            var target = State.Sites.FirstOrDefault(p => p.Key == signal.SiteKey && p.Expires >= Today);
            if (target != null) return target;
        }
        return ActiveDigSites(location).OrderBy(p => p.Points.Where(n => !n.Dug).Min(n => Vector2.DistanceSquared(Game1.player.Tile, new(n.X, n.Y)))).FirstOrDefault();
    }

    private void ShowDigFeedback(DigSite site, DigPoint point, int fragments)
    {
        digFeedback.Value.Add(new(site, new Vector2(point.X, point.Y), fragments, Game1.currentGameTime.TotalGameTime.TotalSeconds));
        Notify(T("journey.dug", new { amount = fragments, culture = CultureName(site.Culture), found = site.Points.Count(p => p.Dug), total = site.Points.Count }));
        var progress = Research.Progress(State, site.Culture);
        var project = Research.Project(State, site.Culture, RandomFor("project:" + site.Culture));
        int cost = Core.Research.Required(project, progress, false, State);
        Notify(T("learn.balance", new { name = ArtifactName(project), count = progress.Fragments, cost }));
        Game1.playSound("newArtifact");
    }

    private void DrawRevealedPoints(SpriteBatch b, double now)
    {
        foreach (var point in ActiveDigSites(Game1.currentLocation.Name).SelectMany(p => p.Points).Where(p => p.Revealed && !p.Dug))
        {
            var tile = new Vector2(point.X, point.Y);
            if (!ValidDigTile(Game1.currentLocation, tile)) continue;
            var at = Game1.GlobalToLocal(tile * 64);
            Color gold = new Color(255, 214, 106) * (.72f + .15f * (float)Math.Sin(now * 4));
            b.Draw(Game1.staminaRect, new Rectangle((int)at.X + 6, (int)at.Y + 6, 52, 3), gold);
            b.Draw(Game1.staminaRect, new Rectangle((int)at.X + 6, (int)at.Y + 55, 52, 3), gold);
            b.Draw(Game1.staminaRect, new Rectangle((int)at.X + 6, (int)at.Y + 6, 3, 52), gold);
            b.Draw(Game1.staminaRect, new Rectangle((int)at.X + 55, (int)at.Y + 6, 3, 52), gold);
            b.Draw(Atlas, at + new Vector2(16, -14 + (float)Math.Sin(now * 4) * 3), new Rectangle(112, 32, 16, 16), Color.White, 0, Vector2.Zero, 2, SpriteEffects.None, 1);
        }
    }

    private void DrawFieldFeedback(SpriteBatch b)
    {
        double now = Game1.currentGameTime.TotalGameTime.TotalSeconds;
        var signal = signals.Value;
        if (signal.Location == Game1.currentLocation.Name && signal.PulseUntil > now)
        {
            float progress = 1 - (float)(signal.PulseUntil - now) / 1.2f;
            Vector2 center = Game1.GlobalToLocal(Game1.player.Position + new Vector2(32, 32));
            for (int i = 0; i < 24; i++)
            {
                float angle = i * MathF.Tau / 24;
                Vector2 at = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * (18 + progress * 95);
                b.Draw(Game1.staminaRect, new Rectangle((int)at.X, (int)at.Y, 5, 5), new Color(109, 196, 186) * (1 - progress));
            }
            b.Draw(Atlas, center + new Vector2(-16, -82 - progress * 6), new Rectangle(96, 32, 16, 16), Color.White * (1 - progress), 0, Vector2.Zero, 2, SpriteEffects.None, 1);
        }
        foreach (var feedback in digFeedback.Value.Where(p => p.Site.Location == Game1.currentLocation.Name && now - p.Started < 2.2))
        {
            float age = (float)(now - feedback.Started), fade = Math.Clamp(2.2f - age, 0, 1);
            Vector2 center = Game1.GlobalToLocal(feedback.Tile * 64 + new Vector2(32));
            for (int i = 0; i < 12 && age < 1; i++)
            {
                float angle = i * MathF.Tau / 12;
                Vector2 at = center + new Vector2(MathF.Cos(angle) * age * 65, MathF.Sin(angle) * age * 36 - MathF.Sin(age * MathF.PI) * 38);
                b.Draw(Game1.staminaRect, new Rectangle((int)at.X, (int)at.Y, 5 + i % 3, 5 + i % 3), new Color(202, 165, 103) * (1 - age));
            }
            Vector2 chip = center + new Vector2(-16, -28 - Math.Min(age, 1) * 34);
            b.Draw(Atlas, chip, new Rectangle(112, 32, 16, 16), Color.White * fade, age * .25f, Vector2.Zero, 2.5f, SpriteEffects.None, 1);
            string label = T("journey.floating-find", new { count = feedback.Fragments });
            var size = Game1.smallFont.MeasureString(label) * .8f;
            var box = new Rectangle((int)(center.X - size.X / 2 - 10), (int)(center.Y - 88 - Math.Min(age, 1) * 20), (int)size.X + 20, (int)size.Y + 10);
            b.Draw(Game1.staminaRect, box, new Color(34, 49, 43) * fade);
            b.DrawString(Game1.smallFont, label, new Vector2(box.X + 10, box.Y + 5), new Color(255, 226, 148) * fade, 0, Vector2.Zero, .8f, SpriteEffects.None, 1);
        }
    }
}

internal sealed record DigFeedback(DigSite Site, Vector2 Tile, int Fragments, double Started);
