using Archeo.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using NVector2 = System.Numerics.Vector2;

namespace Archeo;

public sealed partial class ModEntry
{
    private readonly PerScreen<CompanionVisual> companions = new(() => new());

    private void UpdateCompanion(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || states.Value?.Workshop is not { Stage: WorkshopStage.Complete, CompanionEnabled: true }
            || !Context.IsPlayerFree || Game1.eventUp) return;
        var visual = companions.Value;
        var owner = Game1.player.getStandingPosition();
        var location = Game1.currentLocation;
        if (visual.Location != location.NameOrUniqueName)
        {
            visual.Reset();
            visual.Location = location.NameOrUniqueName;
            visual.Trail.Reset(new NVector2(owner.X, owner.Y));
        }
        NVector2 old = visual.Trail.Position;
        visual.Trail.Update(new NVector2(owner.X, owner.Y), (float)Game1.currentGameTime.ElapsedGameTime.TotalSeconds, position =>
        {
            var tile = new Vector2(MathF.Floor(position.X / 64), MathF.Floor(position.Y / 64));
            return location.isTileOnMap(tile) && location.isTilePassable(tile) && !location.isWaterTile((int)tile.X, (int)tile.Y);
        });
        if (visual.Trail.Position.X - old.X is > .1f or < -.1f) visual.Flip = visual.Trail.Position.X < old.X;
        double now = Game1.currentGameTime.TotalGameTime.TotalSeconds;
        if (visual.Trail.Moving) visual.LastMove = now;
        visual.Row = visual.Trail.Moving ? 1 : now - visual.LastMove > 4 && Game1.timeOfDay >= 2000 && AtWorkshopHome ? 3 : 0;
        if (visual.Trail.Moving || visual.Row == 3) return;
        var tilePos = new Vector2(visual.Trail.Position.X / 64, visual.Trail.Position.Y / 64);
        var point = State.Sites.Where(s => s.Location == location.Name && s.Expires >= Today).SelectMany(s => s.Points).Where(p => !p.Dug)
            .OrderBy(p => Vector2.DistanceSquared(tilePos, new(p.X, p.Y))).FirstOrDefault();
        if (point == null || Vector2.DistanceSquared(tilePos, new(point.X, point.Y)) > 64) return;
        visual.Row = 2;
        visual.Flip = point.X + .5f < tilePos.X;
        if (now > visual.NextHint)
        {
            visual.NextHint = now + 30;
            visual.DustUntil = now + 1;
            Notify(T("workshop.mole-hint", new { direction = Direction(new Vector2(point.X, point.Y) - tilePos) }));
        }
    }

    private void DrawCompanion(SpriteBatch b)
    {
        if (State.Workshop is not { Stage: WorkshopStage.Complete, CompanionEnabled: true }) return;
        var visual = companions.Value;
        if (visual.Location != Game1.currentLocation.NameOrUniqueName) return;
        double now = Game1.currentGameTime.TotalGameTime.TotalSeconds;
        int frame = (int)(now * (visual.Row == 1 ? 9 : visual.Row == 2 ? 5 : 2)) % 4;
        Vector2 feet = new(MathF.Round(visual.Trail.Position.X / 4) * 4, MathF.Round(visual.Trail.Position.Y / 4) * 4);
        Vector2 at = Game1.GlobalToLocal(feet);
        b.Draw(MoleTexture, at, new Rectangle(frame * 16, visual.Row * 16, 16, 16), Color.White, 0, new Vector2(8, 14), 3,
            visual.Flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, Math.Clamp(feet.Y / 10000, 0, 1));
        if (visual.Row == 2)
            b.Draw(Atlas, at + new Vector2(-8, -56), new Rectangle(7 * 16, 2 * 16, 16, 16), Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 1);
        if (now < visual.DustUntil)
        {
            float age = 1 - (float)(visual.DustUntil - now);
            for (int i = 0; i < 3; i++)
                b.Draw(Game1.staminaRect, new Rectangle((int)at.X + (visual.Flip ? -1 : 1) * (20 + (int)(age * 18) + i * 3), (int)at.Y - 18 - (int)(age * (8 + i * 3)), 3, 3), new Color(207, 177, 121) * (1 - age));
        }
    }
}

internal sealed class CompanionVisual
{
    public Trail Trail { get; } = new();
    public string Location { get; set; } = "";
    public bool Flip { get; set; }
    public int Row { get; set; }
    public double LastMove { get; set; }
    public double NextHint { get; set; }
    public double DustUntil { get; set; }
    public void Reset() { Location = ""; Row = 0; Flip = false; LastMove = NextHint = DustUntil = 0; }
}
