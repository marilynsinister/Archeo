using System.Text.Json;
using Archeo.Core;
using Archeo.Menus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Tools;

namespace Archeo.Smoke;

public sealed partial class SmokeEntry
{
    private void BesideNpc(string name,Action action)
    {
        var npc=Game1.getCharacterFromName(name)??throw new Exception("Missing NPC: "+name);
        var home=npc.currentLocation;var position=npc.Position;bool asleep=npc.isSleeping.Value;
        home?.characters.Remove(npc);npc.currentLocation=Game1.currentLocation;Game1.currentLocation.characters.Add(npc);
        npc.setTileLocation(Game1.player.Tile+new Vector2(1,0));npc.isSleeping.Value=false;
        try{action();}finally{Game1.currentLocation.characters.Remove(npc);npc.currentLocation=home;npc.Position=position;npc.isSleeping.Value=asleep;home?.characters.Add(npc);Game1.activeClickableMenu=null;Game1.dialogueUp=false;}
    }
    private void CheckExpeditions(ModEntry mod)
    {
        Check(!Journey.IsOpen(mod.State,"expeditions"),"late expedition chapter is hidden before its actual prerequisites");
        mod.State.Residents.Stages["penny"]=ResidentStage.Complete;mod.State.Residents.Stages["willy"]=ResidentStage.Complete;
        mod.State.Restored["resonant_lens"]=1;mod.State.Restored["island_spindle"]=Math.Max(1,mod.State.Restored.GetValueOrDefault("island_spindle"));
        Call(mod,"RefreshExpeditionSites");mod.SaveState();
        Check(PuzzleExpeditions.All.All(s=>PuzzleExpeditions.Stage(mod.State.Expeditions,s.Id)==ExpeditionStage.Invitation)&&Journey.IsOpen(mod.State,"expeditions"),"old completed personal stories discover both eligible sequels");
        // The unsaved new-game world hasn't run the island's normal NPC population step.
        if(Game1.getCharacterFromName("Leo")==null)
        {
            var hut=Game1.getLocationFromName("IslandHut");
            var leo=new NPC(new AnimatedSprite("Characters\\ParrotBoy",0,16,32),new Vector2(5*64,5*64),"IslandHut",2,"Leo",false,Game1.content.Load<Microsoft.Xna.Framework.Graphics.Texture2D>("Portraits\\ParrotBoy"));
            leo.currentLocation=hut;hut.characters.Add(leo);
        }
        Game1.player.addItemToInventoryBool(ItemRegistry.Create("(O)"+ModEntry.ItemId("surveyor")));
        var hoe=new Hoe();Game1.player.addItemToInventoryBool(hoe);
        foreach(var story in PuzzleExpeditions.All)
        {
            var snapshots=new Dictionary<ExpeditionStage,string>();
            void Remember()=>snapshots[PuzzleExpeditions.Stage(mod.State.Expeditions,story.Id)]=JsonSerializer.Serialize(mod.State.Expeditions.Stories[story.Id]);
            Remember();BesideNpc(story.Primary,()=>mod.ExpeditionAction(story.Id,"talk"));Remember();
            Check(PuzzleExpeditions.Stage(mod.State.Expeditions,story.Id)==ExpeditionStage.FirstClue,"nearby resident opens the first riddle: "+story.Id);
            string before=JsonSerializer.Serialize(mod.State.Expeditions);int correct=Array.IndexOf(story.Locations(1),story.First);
            mod.ExpeditionChoice(story.Id,"location",(correct+1)%3);
            Check(JsonSerializer.Serialize(mod.State.Expeditions)==before,"wrong map answer changes no expedition progress: "+story.Id);
            mod.RevealExpeditionHint(story.Id);mod.RevealExpeditionHint(story.Id);Remember();
            Check(mod.ExpeditionChoice(story.Id,"location",correct),"first clue resolves its actual map: "+story.Id);Remember();
            for(int leg=1;leg<=2;leg++)
            {
                if(!mod.State.Sites.Any(s=>s.EventKind==story.Kind(leg)))
                {
                    string destination=leg==1?story.First:story.Second;Move(destination);Game1.currentLocation.reloadMap();
                    var entry=Game1.locations.SelectMany(l=>l.warps).FirstOrDefault(w=>w.TargetName==destination);
                    var ground=(from x in Enumerable.Range(2,Game1.currentLocation.Map.Layers[0].LayerWidth-4)
                                from y in Enumerable.Range(2,Game1.currentLocation.Map.Layers[0].LayerHeight-4)
                                let tile=new Vector2(x,y) where (bool)Call(mod,"ValidDigTile",Game1.currentLocation,tile)! select tile).ToList();
                    Check(ground.Count>=2,"queued cache has diggable ground after map entry: "+destination+"/"+ground.Count);
                    Game1.player.setTileLocation(entry==null?ground[0]:new Vector2(entry.TargetX,entry.TargetY));Call(mod,"RefreshExpeditionSites");
                }
                var site=mod.State.Sites.Single(s=>s.EventKind==story.Kind(leg));Move(site.Location);
                Check(site.Expires==int.MaxValue&&site.Points.All(p=>(bool)Call(mod,"ValidDigTile",Game1.currentLocation,new Vector2(p.X,p.Y))!),"expedition cache has two safe permanent dig points: "+story.Id+"/"+leg);
                Check(leg!=1||story.Id!="tide"||site.Points.All(p=>p.X<Game1.currentLocation.Map.Layers[0].LayerWidth/2),"coastal expedition never requires the unrepaired bridge");
                foreach(var candidate in site.Points.ToList())
                {
                    var at=new Vector2(candidate.X,candidate.Y);
                    var adjacent=new[]{new Vector2(0,1),new Vector2(1,0),new Vector2(0,-1),new Vector2(-1,0)};
                    Game1.player.setTileLocation(adjacent.Select(v=>v+at).First(v=>(bool)Call(mod,"WalkableForSearch",Game1.currentLocation,v)!));
                    mod.State.TrackedObjective="dig:"+site.Key;Field<PerScreen<SurveySignal>>(mod,"signals").Value=new();
                    Equip("surveyor");Field<PerScreen<double>>(mod,"nextSurvey").Value=0;mod.Survey();
                    var signal=Field<PerScreen<SurveySignal>>(mod,"signals").Value;
                    Check(signal.SiteKey==site.Key,"instrument prioritizes the player's pinned expedition: "+story.Id);
                    var point=site.Points.Single(p=>p.X==signal.Target.X&&p.Y==signal.Target.Y);
                    Check(point.Revealed,"seeker permanently marks the real quest tile");
                    Game1.player.CurrentToolIndex=Game1.player.Items.ToList().IndexOf(hoe);Game1.player.toolPower.Value=0;
                    int finds=mod.State.Finds;hoe.DoFunction(Game1.currentLocation,point.X*64+32,point.Y*64+32,1,Game1.player);
                    Check(point.Dug&&mod.State.Finds==finds+1,"real hoe excavates an expedition part: "+story.Id);
                    mod.OnHoeDig(Game1.currentLocation,point.X,point.Y,Game1.player);Check(mod.State.Finds==finds+1,"quest excavation cannot award a part twice");
                }
                Remember();mod.SaveState();string record=JsonSerializer.Serialize(mod.State.Expeditions);Call(mod,"LoadState");
                Check(JsonSerializer.Serialize(mod.State.Expeditions)==record,"excavated expedition parts survive real mod-data reload: "+story.Id);
                if(leg==1)
                {
                    BesideNpc(story.Secondary,()=>mod.ExpeditionAction(story.Id,"talk"));Remember();
                    Check(mod.ExpeditionChoice(story.Id,"location",Array.IndexOf(story.Locations(2),story.Second)),"second resident's evidence identifies the second map: "+story.Id);Remember();
                }
            }
            var actorPositions=new[]{story.Primary,story.Secondary}.ToDictionary(n=>n,n=>(Location:Game1.getCharacterFromName(n).currentLocation,Position:Game1.getCharacterFromName(n).Position));
            mod.ExpeditionAction(story.Id,"meeting");var meeting=Game1.activeClickableMenu as ExpeditionMeetingMenu;
            Check(meeting!=null,"field location opens the two-person inspection scene: "+story.Id);Capture(meeting!,"exp-meeting-first-"+story.Id+".png");
            meeting!.receiveGamePadButton(Buttons.B);
            Check(PuzzleExpeditions.Stage(mod.State.Expeditions,story.Id)==ExpeditionStage.Meeting&&Game1.activeClickableMenu is JournalMenu,"closing inspection returns to the book without advancing");
            mod.ExpeditionAction(story.Id,"meeting");meeting=(ExpeditionMeetingMenu)Game1.activeClickableMenu;
            for(int page=0;page<3;page++)meeting.receiveGamePadButton(Buttons.A);
            meeting.receiveGamePadButton(Buttons.A);
            Check(PuzzleExpeditions.Stage(mod.State.Expeditions,story.Id)==ExpeditionStage.Meeting,"incorrect shared observation costs no parts");
            for(int i=0;i<story.Observation;i++)meeting.receiveGamePadButton(Buttons.DPadDown);meeting.receiveGamePadButton(Buttons.A);Remember();
            Check(PuzzleExpeditions.Stage(mod.State.Expeditions,story.Id)==ExpeditionStage.Restoration&&actorPositions.All(p=>Game1.getCharacterFromName(p.Key).currentLocation==p.Value.Location&&Game1.getCharacterFromName(p.Key).Position==p.Value.Position),"successful shared scene unlocks restoration without moving scheduled NPCs");
            var book=new JournalMenu(mod,"expeditions");book.FocusExpedition(story.Id);Capture(book,"exp-assembly-empty-"+story.Id+".png");
            Click(book,"exp:slot:0");mod.RevealExpeditionHint(story.Id);mod.RevealExpeditionHint(story.Id);Remember();
            mod.SaveState();string partial=JsonSerializer.Serialize(mod.State.Expeditions.Stories[story.Id]);Call(mod,"LoadState");
            Check(JsonSerializer.Serialize(mod.State.Expeditions.Stories[story.Id])==partial,"actual partial assembly and help reload exactly");
            Check(!mod.ExpeditionChoice(story.Id,"assembly",0),"unfinished physical-layout puzzle cannot skip to interpretation");
            for(int slot=0;slot<4;slot++)PuzzleExpeditions.Place(mod.State.Expeditions,story.Id,slot,story.Layout[slot]);mod.SaveState();
            Check(mod.ExpeditionChoice(story.Id,"assembly",0),"correct four-part restoration completes in game");Remember();
            Check(!mod.ExpeditionChoice(story.Id,"purpose",(story.Purpose+1)%3)&&mod.ExpeditionChoice(story.Id,"purpose",story.Purpose),"purpose deduction accepts only the evidence-supported explanation");Remember();
            int xp=mod.State.Experience,marks=mod.State.Museum.Marks;
            int Friendship(string name)=>Game1.player.friendshipData.TryGetValue(name,out var friendship)?friendship.Points:0;
            var friends=new[]{story.Primary,story.Secondary}.ToDictionary(n=>n,Friendship);
            BesideNpc(story.Primary,()=>mod.ExpeditionAction(story.Id,"talk"));Remember();
            Check(mod.State.Experience==xp+Mastery.HistoryReward(mod.State,250)&&mod.State.Museum.Marks==marks+5&&friends.All(p=>Friendship(p.Key)==p.Value+Mastery.HistoryReward(mod.State,150)),"final story rewards: "+story.Id+" XP "+xp+" -> "+mod.State.Experience+", marks "+marks+" -> "+mod.State.Museum.Marks+", friendship "+string.Join(", ",friends.Select(p=>p.Key+" "+p.Value+" -> "+Friendship(p.Key))));
            BesideNpc(story.Primary,()=>mod.ExpeditionAction(story.Id,"talk"));Check(mod.State.Museum.Marks==marks+5,"repeat final dialogue cannot duplicate expedition rewards");
            var inventory=Game1.player.Items.ToArray();for(int i=0;i<Game1.player.Items.Count;i++)if(Game1.player.Items[i]==null)Game1.player.Items[i]=ItemRegistry.Create("(O)390",999);
            mod.ExpeditionAction(story.Id,"claim");Check(PuzzleExpeditions.Stage(mod.State.Expeditions,story.Id)==ExpeditionStage.Reward,"full backpack preserves expedition keepsake");
            for(int i=0;i<inventory.Length;i++)Game1.player.Items[i]=inventory[i];mod.ExpeditionAction(story.Id,"claim");mod.ExpeditionAction(story.Id,"claim");Remember();
            Check(Game1.player.Items.Count(i=>i?.ItemId==ModEntry.ItemId(story.Reward))==1,"registered expedition furniture is received once");
            RenderExpeditionPages(mod,story,snapshots);
            mod.SaveState();string complete=JsonSerializer.Serialize(mod.State.Expeditions);Call(mod,"LoadState");Check(JsonSerializer.Serialize(mod.State.Expeditions)==complete,"completed personal expedition reloads exactly");
        }
        Game1.activeClickableMenu=null;Move("Forest");
    }
    private void RenderExpeditionPages(ModEntry mod,PuzzleExpedition story,Dictionary<ExpeditionStage,string> snapshots)
    {
        var viewport=Game1.uiViewport;var completed=mod.State.Expeditions.Stories[story.Id];
        foreach(var language in new[]{LocalizedContentManager.LanguageCode.ru,LocalizedContentManager.LanguageCode.en})
        foreach(var size in new[]{new Point(1280,720),new Point(900,640)})
        {
            LocalizedContentManager.CurrentLanguageCode=language;Game1.uiViewport=new xTile.Dimensions.Rectangle(0,0,size.X,size.Y);
            foreach(var (stage,json) in snapshots)
            {
                mod.State.Expeditions.Stories[story.Id]=JsonSerializer.Deserialize<ExpeditionRecord>(json)!;
                var menu=new JournalMenu(mod,"expeditions");menu.FocusExpedition(story.Id);Capture(menu,$"exp-{story.Id}-{stage}-{language}-{size.X}.png",size.X,size.Y);
                Check(ButtonsOf(menu).Values.All(r=>r.X>=0&&r.Y>=0&&r.Right<=size.X&&r.Bottom<=size.Y),"expedition controls fit: "+story.Id+"/"+stage+"/"+language+"/"+size.X);
                if(stage==ExpeditionStage.Restoration)
                    Check(ButtonsOf(menu).Keys.Count(k=>k.StartsWith("exp:slot:"))==4,"restoration shows exactly four accessible parts slots");
                if(stage==ExpeditionStage.Meeting)
                {
                    var scene=new ExpeditionMeetingMenu(mod,story);Set(scene,"page",3);Call(scene,"Layout");Capture(scene,$"exp-scene-{story.Id}-{language}-{size.X}.png",size.X,size.Y);
                }
                if(stage==ExpeditionStage.Complete){Click(menu,"exp:archive");Capture(menu,$"exp-archive-{story.Id}-{language}-{size.X}.png",size.X,size.Y);}
            }
        }
        mod.State.Expeditions.Stories[story.Id]=completed;Game1.uiViewport=viewport;LocalizedContentManager.CurrentLanguageCode=LocalizedContentManager.LanguageCode.ru;
    }
}
