using Archeo.Core;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buffs;

namespace Archeo;

public sealed partial class ModEntry
{
    private bool RefreshFrontiers()
    {
        string here=Game1.currentLocation?.Name??"";
        // These are the same completion flags checked by the installed bus and boat ticket handlers.
        bool desert=Game1.MasterPlayer.mailReceived.Contains("ccVault") || here=="Desert";
        bool island=Game1.MasterPlayer.mailReceived.Contains("willyBoatFixed") || Game1.player.mailReceived.Contains("Visited_Island") || here.StartsWith("Island",StringComparison.Ordinal);
        return FrontierResearch.Observe(State,desert,island);
    }
    private void OnFrontierWarp(object? sender,WarpedEventArgs e)
    {
        if(!e.IsLocalPlayer || states.Value==null)return;
        if(RefreshFrontiers())RefreshSites(true);
        RefreshExpeditionSites();
        SaveState();
    }

    private bool UseFrontierRelic(ArtifactDefinition artifact)
    {
        switch(artifact.Effect)
        {
            case "sunstep":
                if(!FrontierResearch.CanUseSunstep(Game1.currentLocation.IsOutdoors,Game1.timeOfDay)) { Notify(T("frontier.sun-conditions"));return false; }
                var sun=new BuffEffects();sun.Speed.Value=1;
                Game1.player.applyBuff(new Buff(Id+"/Sunstep",CultureName(artifact.Culture),ArtifactName(artifact),90000,Atlas,artifact.Sprite,sun,false,ArtifactName(artifact),T("frontier.sun-buff")));
                Notify(T("frontier.sun-used"));break;
            case "shellguard":
                var shell=new BuffEffects();shell.Defense.Value=3;shell.MagneticRadius.Value=64;
                Game1.player.applyBuff(new Buff(Id+"/Shellguard",CultureName(artifact.Culture),ArtifactName(artifact),90000,Atlas,artifact.Sprite,shell,false,ArtifactName(artifact),T("frontier.shell-buff")));
                Notify(T("frontier.shell-used"));break;
            case "oasis":
                if(!FrontierResearch.CanUseOasis(Game1.player.Stamina,Game1.player.MaxStamina,Game1.player.health,Game1.player.maxHealth)) { Notify(T("frontier.oasis-full"));return false; }
                Game1.player.Stamina=Math.Min(Game1.player.MaxStamina,Game1.player.Stamina+FrontierResearch.OasisEnergy);
                Game1.player.health=Math.Min(Game1.player.maxHealth,Game1.player.health+FrontierResearch.OasisHealth);
                Notify(T("frontier.oasis-used"));break;
            case "tidecall":
                _=FindSurveyTarget(Game1.currentLocation); // Repair old or newly blocked dig points before choosing targets.
                var route=RoutesFromPlayer(Game1.currentLocation);
                var targets=ActiveDigSites(Game1.currentLocation.Name).SelectMany(site=>site.Points.Where(p=>!p.Dug&&!p.Revealed
                        && ValidDigTile(Game1.currentLocation,new(p.X,p.Y))&&route.Contains(new(p.X,p.Y))).Select(point=>(Site:site,Point:point)))
                    .OrderBy(p=>route.PathTo(new(p.Point.X,p.Point.Y)).Count).Take(2).ToList();
                if(targets.Count==0){Notify(T("frontier.conch-empty"));return false;}
                foreach(var target in targets)target.Point.Revealed=true;
                var first=targets[0];
                signals.Value=new SurveySignal{Location=first.Site.Location,SiteKey=first.Site.Key,Target=new(first.Point.X,first.Point.Y),Revealed=true,Until=Game1.currentGameTime.TotalGameTime.TotalSeconds+180};
                Notify(T("frontier.conch-used",new{count=targets.Count}));break;
            default:return false;
        }
        StartRelicAura("stars",Game1.player.Position+new Vector2(32,-16));
        return true;
    }
}
