using System.Text.Json;
using Archeo.Core;
using Archeo.Menus;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Tools;

namespace Archeo.Smoke;

public sealed partial class SmokeEntry
{
    private void DigNormalRouteField(ModEntry mod,string culture)
    {
        var site=mod.State.Sites.Single(p=>p.Culture==culture && p.EventKind=="" && p.Points.Any(n=>!n.Dug));
        var hoe=Game1.player.Items.OfType<Hoe>().First();
        mod.State.TrackedObjective="dig:"+site.Key;mod.SaveState();
        while(site.Points.Any(p=>!p.Dug))
        {
            Move(site.Location);var point=site.Points.First(p=>!p.Dug);var at=new Vector2(point.X,point.Y);
            Game1.player.setTileLocation(new[]{new Vector2(0,1),new Vector2(1,0),new Vector2(0,-1),new Vector2(-1,0)}.Select(v=>v+at).First(v=>(bool)Call(mod,"WalkableForSearch",Game1.currentLocation,v)!));
            Equip("surveyor");Field<PerScreen<double>>(mod,"nextSurvey").Value=0;mod.Survey();
            var signal=Field<PerScreen<SurveySignal>>(mod,"signals").Value;
            Check(signal.Revealed && signal.SiteKey==site.Key,"real survey follows the field that supplies route evidence: "+culture);
            int remaining=site.Points.Count(p=>!p.Dug);
            Game1.player.CurrentToolIndex=Game1.player.Items.ToList().IndexOf(hoe);Game1.player.toolPower.Value=0;
            hoe.DoFunction(Game1.currentLocation,(int)signal.Target.X*64+32,(int)signal.Target.Y*64+32,1,Game1.player);
            Check(site.Points.Count(p=>!p.Dug)==remaining-1,"real excavation recovers route evidence fragments: "+culture);
        }
        Check(mod.State.Cultures[culture].Fragments>=20,"a real ordinary site funds the first route-evidence artifact: "+culture);
    }

    private void CaptureRoutes(ModEntry mod,string id,string stage)
    {
        var viewport=Game1.uiViewport;var language=LocalizedContentManager.CurrentLanguageCode;float scale=mod.Config.JournalTextScale;
        string before=JsonSerializer.Serialize(mod.State);
        foreach(var code in new[]{LocalizedContentManager.LanguageCode.ru,LocalizedContentManager.LanguageCode.en})
        foreach(var size in new[]{new Point(900,640),new Point(1280,720)})
        foreach(float reading in new[]{1f,1.3f})
        {
            LocalizedContentManager.CurrentLanguageCode=code;Game1.uiViewport=new xTile.Dimensions.Rectangle(0,0,size.X,size.Y);mod.Config.JournalTextScale=reading;
            var menu=new JournalMenu(mod,"routes");Set(menu,"selected",id);
            string name=$"route-{id}-{stage}-{code}-{size.X}-{reading:0.0}";
            Capture(menu,name+".png",size.X,size.Y);
            Check(ButtonsOf(menu).Keys.Count(p=>p.StartsWith("route:select:"))==RouteStories.Visible(mod.State).Count,"route book hides all unintroduced entries: "+name);
            Check(ButtonsOf(menu).Values.All(r=>r.Left>=0 && r.Top>=0 && r.Right<=size.X && r.Bottom<=size.Y),"route controls fit: "+name);
            int max=Field<int>(menu,"maxScroll");for(int i=0;i<max+1;i++)menu.receiveScrollWheelAction(-1);
            Check(Field<int>(menu,"scroll")==max,"route notes remain readable to the final line: "+name);
            Capture(menu,name+"-bottom.png",size.X,size.Y);
        }
        Game1.uiViewport=viewport;LocalizedContentManager.CurrentLanguageCode=language;mod.Config.JournalTextScale=scale;
        Check(JsonSerializer.Serialize(mod.State)==before,"reading route entries never changes evidence or progress: "+id+"/"+stage);
    }

    private void CheckShoreRoute(ModEntry mod)
    {
        DigNormalRouteField(mod,"dwarf");mod.Restore("dwarf",false);
        Check(mod.State.Restored.ContainsKey("brass_gear") && RouteStories.Pending(mod.State)?.Id=="sea" && !Journey.IsOpen(mod.State,"sea"),"real lifting gear supplies the first coastal clue without opening the field");
        mod.SaveState();string before=JsonSerializer.Serialize(mod.State);Call(mod,"LoadState");
        Check(JsonSerializer.Serialize(mod.State)==before && RouteStories.Visible(mod.State).Single().Id=="sea","pending route and its actual artifact source survive reload without future regions");
        CaptureRoutes(mod,"sea","clue");
        Move("Forest");Game1.player.setTileLocation(new Vector2(51,18));
        BesideNpc("Willy",()=>
        {
            var willy=Game1.getCharacterFromName("Willy");willy.isSleeping.Value=true;
            Check(!mod.CanExplainRoute("sea") && !mod.ExplainRoute("sea"),"sleeping route partner cannot be used for remote progression");willy.isSleeping.Value=false;
            willy.setTileLocation(Game1.player.Tile+new Vector2(8,0));Check(!mod.CanExplainRoute("sea"),"route conversation requires the actual nearby partner");willy.setTileLocation(Game1.player.Tile+new Vector2(1,0));
            var items=Game1.player.Items.ToArray();
            for(int i=0;i<Game1.player.Items.Count;i++)if(Game1.player.Items[i]?.ItemId==ModEntry.ItemId("brass_gear") || Game1.player.Items[i]==null)Game1.player.Items[i]=ItemRegistry.Create("(O)390",999);
            int xp=mod.State.Experience,money=Game1.player.Money;mod.State.TrackedObjective="route:sea";
            var menu=new JournalMenu(mod,"routes");Capture(menu,"route-sea-near-willy.png");Click(menu,"route:talk");
            Check(Journey.IsOpen(mod.State,"sea") && mod.State.Routes.Explained.Contains("sea") && mod.State.Sites.Any(s=>s.Culture=="sea" && s.EventKind==""),"journal conversation opens actual shore fieldwork");
            Check(mod.State.Experience==xp && Game1.player.Money==money && !mod.State.Donated.Contains("brass_gear"),"sketch and full backpack suffice without consuming the gear or granting rewards");
            Check(FieldJournal.Tracked(mod.State,mod.FieldObjectives())?.Culture=="sea","tracked conversation continues with the newly explained coastal site");
            for(int i=0;i<items.Length;i++)Game1.player.Items[i]=items[i];
        });
        Check(!mod.ExplainRoute("sea"),"completed coastal conversation cannot repeat progression");
        CaptureRoutes(mod,"sea","read");
    }

    private void CheckDistantRoute(ModEntry mod,string id)
    {
        CaptureRoutes(mod,id,"clue");
        mod.State.TrackedObjective="route:"+id;mod.SaveState();
        if(id=="calico")
        {
            Move("ArchaeologyHouse");
            var map=Game1.currentLocation.Map.Layers[0];Vector2? counter=null;
            for(int y=0;y<map.LayerHeight && counter==null;y++)for(int x=0;x<map.LayerWidth;x++)
                if(Game1.currentLocation.doesTileHaveProperty(x,y,"Action","Buildings")?.Contains("Gunther",StringComparison.OrdinalIgnoreCase)==true){counter=new(x,y);break;}
            Check(counter!=null,"museum counter exposes its real Gunther interaction");
            Game1.player.setTileLocation(counter!.Value+new Vector2(0,1));
            Check((bool)Call(mod,"TryRouteInteraction",counter.Value)!,"normal museum counter action explains the caravan route");
        }
        else
        {
            Move("Forest");Game1.player.setTileLocation(new Vector2(51,18));
            BesideNpc("Willy",()=>Check((bool)Call(mod,"TryRouteInteraction",Game1.getCharacterFromName("Willy").Tile)!,"normal nearby NPC action explains the island route"));
        }
        Game1.activeClickableMenu=null;Game1.dialogueUp=false;
        Check(Journey.IsOpen(mod.State,id) && mod.State.Sites.Any(s=>s.Culture==id && s.EventKind==""),"explained distant route creates its own real site: "+id);
        Check(FieldJournal.Tracked(mod.State,mod.FieldObjectives())?.Culture==id,"tracked route follows its new distant site: "+id);
        CaptureRoutes(mod,id,"read");
        var archive=new JournalMenu(mod,"routes");Set(archive,"selected",id);Capture(archive,"route-"+id+"-archive.png");
        string before=JsonSerializer.Serialize(mod.State);Click(archive,"route:replay");
        Check(before==JsonSerializer.Serialize(mod.State),"rereading a route conversation is side-effect free: "+id);
        Game1.activeClickableMenu=null;Game1.dialogueUp=false;Call(mod,"LoadState");
        Check(mod.State.Routes.Clues.ContainsKey(id) && mod.State.Routes.Explained.Contains(id),"route source and archive survive a real mod reload: "+id);
    }
}
