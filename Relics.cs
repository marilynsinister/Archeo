using Archeo.Core;
using StardewValley;

namespace Archeo;

public sealed partial class ModEntry
{
    internal void UseRelic(ArtifactDefinition artifact)
    {
        if (!Game1.player.Items.Any(p => p?.QualifiedItemId == QualifiedId(artifact))) { Notify(T("message.bring-artifact")); return; }
        if (artifact.Effect is "" or "lamp") return;
        int cooldown = RelicDaysUntilReady(artifact);
        if (cooldown > 0) { Notify(artifact.Effect == "garden" ? T("unusual.cooldown",new { days = cooldown }) : T("relic.used")); return; }
        bool used = true;
        switch (artifact.Effect)
        {
            case "past":
                if (!UsePastLens()) return;
                break;
            case "garden":
                BeginGardenFlute(artifact);
                return; // The shared tree lock commits the weekly charge only after fruit actually grows.
            case "hourglass":
                if (!UseKeeperHourglass()) return;
                break;
            case "stars":
                if (!UsePocketObservatory()) return;
                break;
            case "energy":
                if (Game1.player.Stamina >= Game1.player.MaxStamina) { Notify(T("relic.full-energy")); return; }
                Game1.player.Stamina = Math.Min(Game1.player.MaxStamina, Game1.player.Stamina + 60);
                Notify(T("relic.energy"));
                break;
            case "heal":
                if (Game1.player.health >= Game1.player.maxHealth) { Notify(T("relic.full-health")); return; }
                Game1.player.health = Math.Min(Game1.player.maxHealth, Game1.player.health + 40);
                Notify(T("relic.heal"));
                break;
            case "compass":
            case "survey":
                var found = FindSurveyTarget(Game1.currentLocation);
                if (found == null) { Notify(T("relic.travel")); return; }
                var (site, point) = found.Value;
                signals.Value = new SurveySignal { Location = site.Location, SiteKey = site.Key, Target = new(point.X, point.Y), Revealed = true, Until = Game1.currentGameTime.TotalGameTime.TotalSeconds + 180 };
                Notify(T("relic.reveal"));
                break;
            case "water":
                if (!UseRainPitcher()) return;
                break;
            case "resonance":
                if (!UseStormFork()) return;
                break;
            case "fragments":
                Research.Progress(State, artifact.Culture).Fragments += 8;
                Notify(T("message.fragments", new { amount = 8, culture = CultureName(artifact.Culture) }));
                break;
            case "coffee":
                var coffee = ItemRegistry.Create("(O)395");
                if (!Game1.player.couldInventoryAcceptThisItem(coffee)) { Notify(T("message.inventory-full")); return; }
                Game1.player.addItemToInventoryBool(coffee);
                Notify(T("relic.coffee"));
                break;
            case "pearl":
                var gift = ItemRegistry.Create("(O)372", 3);
                if (!Game1.player.couldInventoryAcceptThisItem(gift)) { Notify(T("message.inventory-full")); return; }
                Game1.player.addItemToInventoryBool(gift);
                Notify(T("relic.shells"));
                break;
            default: used = false; break;
        }
        if (!used) return;
        State.LastUse[artifact.Id] = Today;
        SaveState();
        Game1.playSound("yoba");
    }
}
