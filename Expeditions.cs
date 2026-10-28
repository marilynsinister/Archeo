using Archeo.Core;
using Archeo.Menus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace Archeo;

public sealed partial class ModEntry
{
    internal Texture2D ExpeditionAtlas { get; private set; } = null!;
    internal string ExpeditionTitle(string id)=>T("exp.title."+id);
    internal string ExpeditionPartnerName(string name)=>T("exp.npc."+name);
    private NPC? ExpeditionNpc(PuzzleExpedition story,string name)=>Game1.currentLocation.characters.FirstOrDefault(n=>n.Name==name&&n.CanSocialize&&!n.isSleeping.Value&&Vector2.Distance(n.Tile,Game1.player.Tile)<=4);
    private static void RecognizeStoryPartner(NPC npc)
    {
        // Standalone portrait conversations bypass the vanilla introduction handler.
        if(!Game1.player.friendshipData.ContainsKey(npc.Name))Game1.player.friendshipData[npc.Name]=new Friendship();
    }
    private void RefreshExpeditionSites()
    {
        foreach(string id in PuzzleExpeditions.Discover(State))Notify(T("exp.discovered",new{name=ExpeditionTitle(id)}));
        Core.Journey.Refresh(State,Research.Artifacts);
        foreach(var story in PuzzleExpeditions.All)
        {
            int leg=PuzzleExpeditions.Leg(PuzzleExpeditions.Stage(State.Expeditions,story.Id));
            State.Sites.RemoveAll(s=>(s.EventKind==story.Kind(1)||s.EventKind==story.Kind(2))&&(leg==0||s.EventKind!=story.Kind(leg)));
            if(leg==0||State.Sites.Any(s=>s.EventKind==story.Kind(leg)))continue;
            string name=leg==1?story.First:story.Second;var location=Game1.getLocationFromName(name);if(location==null)continue;
            var site=new DigSite{Location=name,Culture=story.Culture,EventKind=story.Kind(leg),EventId=story.Key(leg),Expires=int.MaxValue};
            var tiles=ReachableTiles(location).Where(t=>InSiteArea(site,t)&&!OccupiedByDigPoint(name,t)).ToList();
            if(tiles.Count<2)continue;
            var random=RandomFor(site.EventId);var anchor=tiles[random.Next(tiles.Count)];
            foreach(var tile in tiles.OrderBy(t=>Vector2.DistanceSquared(t,anchor)).Take(70).OrderBy(_=>random.Next()))
            {
                if(site.Points.Any(p=>Vector2.DistanceSquared(new(p.X,p.Y),tile)<4))continue;
                site.Points.Add(new(){X=(int)tile.X,Y=(int)tile.Y});if(site.Points.Count==2)break;
            }
            if(site.Points.Count==2){State.Sites.Add(site);Notify(T("exp.site-ready",new{location=LocationName(name)}));}
        }
    }
    private void CompleteExpeditionSite(DigSite site)
    {
        if(PuzzleExpeditions.CompleteSite(State.Expeditions,site))Notify(T("exp.parts-found"));
    }
    internal bool CanExpeditionAction(string id,string action)
    {
        var story=PuzzleExpeditions.All.FirstOrDefault(s=>s.Id==id);if(story==null||Game1.eventUp)return false;
        var stage=PuzzleExpeditions.Stage(State.Expeditions,id);
        return action switch{
            "talk"=>stage is ExpeditionStage.Invitation or ExpeditionStage.Finale?ExpeditionNpc(story,story.Primary)!=null:stage==ExpeditionStage.Consult&&ExpeditionNpc(story,story.Secondary)!=null,
            "meeting"=>stage==ExpeditionStage.Meeting&&Game1.currentLocation.Name==story.Second&&WalkableForSearch(Game1.currentLocation,Game1.player.Tile),
            "claim"=>stage==ExpeditionStage.Reward,
            _=>false};
    }
    internal void OpenExpedition(string id)
    {
        var book=new JournalMenu(this,"expeditions");book.FocusExpedition(id);Game1.activeClickableMenu=book;
    }
    internal void ExpeditionAction(string id,string action)
    {
        if(!CanExpeditionAction(id,action))return;
        var story=PuzzleExpeditions.All.First(s=>s.Id==id);var stage=PuzzleExpeditions.Stage(State.Expeditions,id);
        if(action=="meeting"){Game1.activeClickableMenu=new ExpeditionMeetingMenu(this,story);return;}
        if(action=="claim")
        {
            var reward=ItemRegistry.Create("(F)"+ItemId(story.Reward));
            if(!Game1.player.couldInventoryAcceptThisItem(reward)){Notify(T("message.inventory-full"));return;}
            if(!Game1.player.addItemToInventoryBool(reward))return;
            PuzzleExpeditions.Claim(State.Expeditions,id,true);SaveState();Notify(T("exp.reward-received"));Game1.playSound("reward");return;
        }
        string npcName=stage==ExpeditionStage.Consult?story.Secondary:story.Primary;
        var npc=ExpeditionNpc(story,npcName);if(npc==null)return;
        if(stage==ExpeditionStage.Finale)
        {
            if(!PuzzleExpeditions.Finish(State,id,true))return;
            foreach(string name in new[]{story.Primary,story.Secondary})
            {var partner=Game1.getCharacterFromName(name);if(partner!=null){RecognizeStoryPartner(partner);Game1.player.changeFriendship(Mastery.HistoryReward(State,150),partner);}}
        }
        else if(!PuzzleExpeditions.Talk(State.Expeditions,id,true))return;
        RecognizeStoryPartner(npc);
        RefreshExpeditionSites();SaveState();
        Game1.activeClickableMenu=new DialogueBox(new Dialogue(npc,Id+"/Expedition/"+id+"/"+stage,T("exp.dialogue."+id+"."+(int)stage)));
    }
    internal bool ExpeditionChoice(string id,string action,int choice)
    {
        var story=PuzzleExpeditions.All.FirstOrDefault(s=>s.Id==id);if(story==null||Game1.eventUp)return false;
        bool accepted=action switch{
            "location"=>choice is >=0 and <3&&PuzzleExpeditions.ChooseLocation(State.Expeditions,id,story.Locations(PuzzleExpeditions.Stage(State.Expeditions,id)==ExpeditionStage.FirstClue?1:2)[choice]),
            "observation"=>PuzzleExpeditions.Observe(State.Expeditions,id,Game1.currentLocation.Name,choice),
            "assembly"=>PuzzleExpeditions.Assemble(State.Expeditions,id),
            "purpose"=>PuzzleExpeditions.Interpret(State.Expeditions,id,choice),
            _=>false};
        if(!accepted){Notify(T("exp.reconsider"));return false;}
        RefreshExpeditionSites();SaveState();Game1.playSound("newArtifact");return true;
    }
    internal void CycleExpeditionPiece(string id,int slot)
    {
        if(!State.Expeditions.Stories.TryGetValue(id,out var record)||slot is <0 or >3)return;
        if(PuzzleExpeditions.Place(State.Expeditions,id,slot,(record.Assembly[slot]+1)%4))SaveState();
    }
    internal void RevealExpeditionHint(string id){if(PuzzleExpeditions.RevealHint(State.Expeditions,id)>0)SaveState();}
}
