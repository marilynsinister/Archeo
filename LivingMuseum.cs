using Archeo.Core;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using SObject=StardewValley.Object;

namespace Archeo;

public sealed partial class ModEntry
{
    private void RefreshMuseum()
    {
        Core.Journey.Refresh(State,Research.Artifacts);
        LivingMuseum.EnsureOrder(State,Research.Artifacts,Today,Core.Research.StableSeed((long)Game1.uniqueIDForThisGame,Game1.player.UniqueMultiplayerID,LivingMuseum.Week(Today),"museum:order"));
    }
    private IEnumerable<Item> MuseumItems(string id)=>Game1.player.Items.Where(p=>p is SObject obj&&!obj.bigCraftable.Value&&!obj.questItem.Value
        &&p.QualifiedItemId=="(O)"+ItemId(id)&&!p.modData.Any()).Select(p=>p!);
    internal int MuseumItemCount(string id)=>MuseumItems(id).Sum(p=>p.Stack);
    private void ConsumeMuseumItem(string id)
    {
        var item=MuseumItems(id).First();
        if(item.Stack>1)item.Stack--;else Game1.player.removeItemFromInventory(item);
    }
    internal bool CanDeliverMuseumOrder=>LivingMuseum.CanDeliver(State,Research.Artifacts,Today,InMuseum,MuseumItemCount);
    internal void DeliverMuseumOrder()
    {
        if(!CanDeliverMuseumOrder)return;
        var ids=LivingMuseum.OrderItems(State.Museum.Order!.Theme,Research.Artifacts);
        if(!LivingMuseum.RecordDelivery(State,Research.Artifacts,Today,InMuseum,MuseumItemCount))return;
        foreach(string id in ids)ConsumeMuseumItem(id);
        Research.Progress(State,State.Museum.Order!.Theme).Fragments+=8;Game1.player.Money+=500;
        SaveState();Notify(T("museum.delivered"));Game1.playSound("reward");
    }
    internal void ExchangeMuseumDuplicate(string id)
    {
        if(!InMuseum)return;
        var artifact=Research.Artifacts.FirstOrDefault(a=>a.Id==id);
        if(artifact==null||!LivingMuseum.RecordExchange(State,artifact,MuseumItemCount(id)))return;
        ConsumeMuseumItem(id);SaveState();Notify(T("museum.exchanged"));Game1.playSound("coin");
    }
    internal void BuyMuseumSupplies(string culture)
    {
        if(!InMuseum||!LivingMuseum.BuySupplies(State,culture))return;
        SaveState();Notify(T("museum.supplies-received",new{culture=CultureName(culture)}));Game1.playSound("coin");
    }
    private IEnumerable<Furniture> MatchingDisplays(GameLocation location,string theme)
        => location.furniture.Where(f=>IsGallery(f)&&f.heldObject.Value is Chest chest
            &&LivingMuseum.MatchesDisplay(theme,chest.Items.Take(MuseumCollections.CaseCapacity).Select(p=>p?.ItemId.StartsWith(Id+"_",StringComparison.Ordinal)==true?p.ItemId[(Id.Length+1)..]:"")));
    internal bool NearbyMuseumDisplay(string theme)=>MatchingDisplays(Game1.currentLocation,theme).Any(f=>Vector2.Distance(Game1.player.Tile,f.TileLocation)<=4);
    private void ObserveMuseumDisplays()
    {
        if(State.Museum.Exhibition is not {Claimed:false,Ready:false} show)return;
        bool displayed=false;
        Utility.ForEachLocation(location=>{if(MatchingDisplays(location,show.Theme).Any())displayed=true;return true;},true,false);
        LivingMuseum.ObserveDisplay(State.Museum,Today,displayed);
    }
    internal void StartMuseumExhibition(string theme)
    {
        if(!LivingMuseum.Start(State,theme,Today,NearbyMuseumDisplay(theme)))return;
        SaveState();Notify(T("museum.show-started"));Game1.playSound("newArtifact");
    }
    internal void ClaimMuseumExhibition()
    {
        if(!LivingMuseum.ClaimExhibition(State))return;
        SaveState();Notify(T("museum.show-claimed"));Game1.playSound("reward");
    }
    internal void ClaimMuseumAchievement(string id)
    {
        var reward=LivingMuseum.Achievements.FirstOrDefault(a=>a.Id==id);
        if(reward==null||!LivingMuseum.AchievementReady(State.Museum,id)||State.Museum.AchievementClaims.Contains(id))return;
        var item=ItemRegistry.Create("(F)"+ItemId(reward.Reward));
        if(!Game1.player.couldInventoryAcceptThisItem(item)){Notify(T("message.inventory-full"));return;}
        if(!Game1.player.addItemToInventoryBool(item))return;
        LivingMuseum.ClaimAchievement(State.Museum,id,true);SaveState();Notify(T("museum.achievement-received"));Game1.playSound("reward");
    }
    internal NPC? NearbyMuseumVisitor()
    {
        var show=State.Museum.Exhibition;
        if(show==null||show.Claimed||Today>show.Ends||Today<show.Started)return null;
        var displays=MatchingDisplays(Game1.currentLocation,show.Theme).ToList();
        return Game1.currentLocation.characters.FirstOrDefault(n=>n.CanSocialize&&!n.isSleeping.Value&&LivingMuseum.Visitors(show.Theme).Contains(n.Name)
            &&!show.Reactions.Contains(n.Name)&&Vector2.Distance(n.Tile,Game1.player.Tile)<=4&&displays.Any(f=>Vector2.Distance(n.Tile,f.TileLocation)<=8));
    }
    internal bool ReactToMuseumExhibition(NPC? npc=null)
    {
        if(Game1.eventUp)return false;
        npc??=NearbyMuseumVisitor();
        if(npc==null||NearbyMuseumVisitor()!=npc||!LivingMuseum.React(State.Museum,npc.Name,Today,true))return false;
        string theme=State.Museum.Exhibition!.Theme;Game1.player.changeFriendship(10,npc);SaveState();
        Game1.activeClickableMenu=new DialogueBox(new Dialogue(npc,Id+"/Museum/"+theme,T("museum.reaction."+theme+"."+npc.Name)));return true;
    }
    private bool TryMuseumReaction(Vector2 tile)
    {
        var npc=NearbyMuseumVisitor();
        return npc!=null&&npc.GetBoundingBox().Contains(tile*64+new Vector2(32))&&ReactToMuseumExhibition(npc);
    }
    internal string[] MuseumCaptionOptions()=>new[]{"general"}.Concat(MuseumCollections.All.Where(c=>FrontierResearch.CollectionVisible(State,c)).Select(c=>c.Id))
        .Concat(LivingMuseum.Achievements.Where(a=>LivingMuseum.AchievementReady(State.Museum,a.Id)).Select(a=>"memory_"+a.Id)).ToArray();
}
