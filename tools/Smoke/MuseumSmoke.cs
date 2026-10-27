using System.Text.Json;
using Archeo.Core;
using Archeo.Menus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Objects;

namespace Archeo.Smoke;

public sealed partial class SmokeEntry
{
    private void CheckMuseum(ModEntry mod)
    {
        Check(!Journey.IsOpen(mod.State,"museum"),"living museum stays hidden before a first real donation");
        for(int i=0;i<Game1.player.Items.Count;i++)Game1.player.Items[i]=null;
        foreach(string id in MuseumCollections.All[0].Artifacts)mod.State.Restored[id]=2;
        Game1.player.addItemToInventoryBool(ItemRegistry.Create("(O)"+ModEntry.ItemId("settler_cup"),2));
        var cup=mod.Research.Artifacts.Single(a=>a.Id=="settler_cup");Move("ArchaeologyHouse");mod.Donate(cup);
        Check(Journey.IsOpen(mod.State,"museum")&&mod.State.Donated.Contains(cup.Id)&&mod.MuseumItemCount(cup.Id)==1,"real museum donation opens the friends circle without duplicating the item");
        Check(mod.State.Museum.Order!=null,"first eligible weekly commission is stored on discovery");
        string offer=JsonSerializer.Serialize(mod.State.Museum.Order);Call(mod,"RefreshMuseum");
        Check(JsonSerializer.Serialize(mod.State.Museum.Order)==offer,"reopening museum cannot reroll a weekly offer");
        var ids=LivingMuseum.OrderItems(mod.State.Museum.Order!.Theme,mod.Research.Artifacts);
        foreach(string id in ids)Game1.player.addItemToInventoryBool(ItemRegistry.Create("(O)"+ModEntry.ItemId(id),2));
        int money=Game1.player.Money;Move("Forest");mod.DeliverMuseumOrder();
        Check(!mod.State.Museum.Order.Completed&&Game1.player.Money==money,"commission delivery away from Gunther leaves items and money untouched");
        Move("ArchaeologyHouse");var counts=ids.ToDictionary(id=>id,mod.MuseumItemCount);string theme=mod.State.Museum.Order.Theme;
        int fragments=mod.Research.Progress(mod.State,theme).Fragments;mod.DeliverMuseumOrder();
        Check(ids.All(id=>mod.MuseumItemCount(id)==counts[id]-1)&&Game1.player.Money==money+500&&mod.State.Museum.Marks==3&&mod.Research.Progress(mod.State,theme).Fragments==fragments+8,"real commission consumes each required object and awards the exact reward");
        mod.DeliverMuseumOrder();Check(Game1.player.Money==money+500&&mod.State.Museum.OrdersCompleted==1,"completed weekly commission cannot be paid again");
        Game1.player.addItemToInventoryBool(ItemRegistry.Create("(O)"+ModEntry.ItemId(cup.Id),3));
        while(mod.MuseumItemCount(cup.Id)>1)mod.ExchangeMuseumDuplicate(cup.Id);
        int marks=mod.State.Museum.Marks;mod.ExchangeMuseumDuplicate(cup.Id);
        Check(mod.MuseumItemCount(cup.Id)==1&&mod.State.Museum.Marks==marks,"real duplicate exchange leaves one copy in the backpack");
        var protectedCopy=ItemRegistry.Create("(O)"+ModEntry.ItemId(cup.Id));protectedCopy.modData["qa:keep"]="original";
        Game1.player.Items[Game1.player.Items.ToList().FindIndex(p=>p==null)]=protectedCopy;
        mod.ExchangeMuseumDuplicate(cup.Id);Check(protectedCopy.Stack==1&&protectedCopy.modData["qa:keep"]=="original"&&mod.State.Museum.Marks==marks,"exchange excludes objects with personal mod data");
        var rare=mod.Research.Artifacts.Single(a=>a.Id=="herbalist_flask");Game1.player.addItemToInventoryBool(ItemRegistry.Create(ModEntry.QualifiedId(rare),2));mod.ExchangeMuseumDuplicate(rare.Id);
        Check(mod.State.Museum.Marks==marks,"exchange never accepts a functional rare relic");
        fragments=mod.Research.Progress(mod.State,"valley").Fragments;mod.BuyMuseumSupplies("valley");
        Check(mod.State.Museum.Marks==marks-2&&mod.Research.Progress(mod.State,"valley").Fragments==fragments+12,"museum marks buy twelve fragments in the chosen culture");
        var inventory=Game1.player.Items.ToArray();
        for(int i=0;i<Game1.player.Items.Count;i++)if(Game1.player.Items[i]==null)Game1.player.Items[i]=ItemRegistry.Create("(O)390",999);
        mod.ClaimMuseumAchievement("patron");Check(!mod.State.Museum.AchievementClaims.Contains("patron"),"full backpack preserves the earned museum case");
        for(int i=0;i<inventory.Length;i++)Game1.player.Items[i]=inventory[i];
        mod.ClaimMuseumAchievement("patron");mod.ClaimMuseumAchievement("patron");
        Check(Game1.player.Items.Count(p=>p?.ItemId==ModEntry.ItemId("scholar_case"))==1&&mod.State.Museum.AchievementClaims.Contains("patron"),"first commission grants one registered functional case");
        Move("FarmHouse");Game1.player.setTileLocation(new Vector2(5,6));
        var display=(Furniture)ItemRegistry.Create("(F)"+ModEntry.ItemId("scholar_case"));display.TileLocation=new(5,5);
        var chest=new Chest(true);foreach(string id in MuseumCollections.All[0].Artifacts)chest.Items.Add(ItemRegistry.Create("(O)"+ModEntry.ItemId(id)));
        display.heldObject.Value=chest;Game1.currentLocation.furniture.Add(display);
        Check(ModEntry.IsGallery(display)&&mod.NearbyMuseumDisplay("valley"),"new case holds three real collection objects and qualifies beside the player");
        mod.StartMuseumExhibition("valley");Check(mod.State.Museum.Exhibition is {Theme:"valley",DisplayDays:0},"physical collection starts its exhibition without removing exhibits");
        Call(mod,"ObserveMuseumDisplays");Call(mod,"ObserveMuseumDisplays");
        Check(mod.State.Museum.Exhibition!.DisplayDays==1&&chest.Items.Count==3,"interior display is observed once per evening and objects remain stored");
        var held=chest.Items[2];chest.Items[2]=null;Game1.stats.DaysPlayed++;
        Call(mod,"ObserveMuseumDisplays");Check(mod.State.Museum.Exhibition.DisplayDays==1,"removing an exhibit pauses the next evening's progress");
        chest.Items[2]=held;
        var penny=Game1.getCharacterFromName("Penny");var home=penny.currentLocation;var position=penny.Position;
        home?.characters.Remove(penny);penny.currentLocation=Game1.currentLocation;Game1.currentLocation.characters.Add(penny);penny.setTileLocation(new Vector2(6,6));penny.isSleeping.Value=false;
        Check(mod.ReactToMuseumExhibition(penny)&&!mod.ReactToMuseumExhibition(penny),"nearby resident gives one standalone exhibition response");Game1.activeClickableMenu=null;
        Game1.currentLocation.characters.Remove(penny);penny.currentLocation=home;penny.Position=position;home?.characters.Add(penny);
        Game1.stats.DaysPlayed++;Call(mod,"ObserveMuseumDisplays");Game1.stats.DaysPlayed++;Call(mod,"ObserveMuseumDisplays");
        Check(mod.State.Museum.Exhibition.Ready,"three actual display observations complete the exhibition");
        int xp=mod.State.Experience;marks=mod.State.Museum.Marks;mod.ClaimMuseumExhibition();mod.ClaimMuseumExhibition();
        Check(mod.State.Museum.Marks==marks+5&&mod.State.Experience==xp+150&&mod.State.Museum.ExhibitionsCompleted==1,"exhibition pays five marks and 150 XP only once");
        for(int run=0;run<2;run++)
        {
            Game1.stats.DaysPlayed=(uint)mod.State.Museum.NextExhibitionDay;mod.StartMuseumExhibition("valley");
            for(int evening=0;evening<3;evening++){Call(mod,"ObserveMuseumDisplays");Game1.stats.DaysPlayed++;}
            mod.ClaimMuseumExhibition();
        }
        Check(mod.State.Museum.ExhibitionsCompleted==3&&LivingMuseum.AchievementReady(mod.State.Museum,"curator"),"repeat exhibitions unlock a separate curator milestone");
        var curatorGoal=mod.FieldObjectives().Single(g=>g.Id=="museum:award:curator");
        var goalBook=new JournalMenu(mod);Call(goalBook,"OpenObjective",curatorGoal);
        Check(Field<string>(goalBook,"tab")=="museum"&&Field<string>(goalBook,"selected")=="achievements"&&Field<int>(goalBook,"museumAchievement")==2,"field objective opens its exact unclaimed museum keepsake");
        mod.ClaimMuseumAchievement("exhibitor");mod.ClaimMuseumAchievement("curator");
        Check(Game1.player.Items.Any(p=>p?.ItemId==ModEntry.ItemId("museum_banner"))&&Game1.player.Items.Any(p=>p?.ItemId==ModEntry.ItemId("curator_case")),"exhibition milestones grant their distinct registered furniture");
        Check(LivingMuseum.Achievements.All(a=>mod.MuseumCaptionOptions().Contains("memory_"+a.Id)),"achievement captions unlock independently of the weekly commission");
        Call(mod,"RefreshMuseum");mod.SaveState();string museum=JsonSerializer.Serialize(mod.State.Museum);Call(mod,"LoadState");
        Check(JsonSerializer.Serialize(mod.State.Museum)==museum&&chest.Items.Count==3,"museum ledger reloads exactly while contents remain normal saved game objects");
        var viewport=Game1.uiViewport;
        foreach(var language in new[]{LocalizedContentManager.LanguageCode.ru,LocalizedContentManager.LanguageCode.en})
        foreach(var size in new[]{new Point(1280,720),new Point(900,640)})
        {
            LocalizedContentManager.CurrentLanguageCode=language;Game1.uiViewport=new xTile.Dimensions.Rectangle(0,0,size.X,size.Y);
            var book=new JournalMenu(mod,"museum");
            foreach(string page in new[]{"orders","shows","exchange","achievements"})
            {
                if(page!="orders"){Capture(book,"museum-transition.png",size.X,size.Y);Click(book,"museum:page:"+page);}
                Capture(book,$"museum-{page}-{language}-{size.X}.png",size.X,size.Y);
                Check(ButtonsOf(book).Values.All(rect=>rect.X>=0&&rect.Y>=0&&rect.Right<=size.X&&rect.Bottom<=size.Y),"museum page controls fit the viewport: "+page+"/"+language+"/"+size.X);
                if(page=="achievements") {Click(book,"museum:award-next");Capture(book,$"museum-banner-{language}-{size.X}.png",size.X,size.Y);}
            }
            book.receiveGamePadButton(Buttons.RightShoulder);Check(Field<string>(book,"tab")=="equipment","controller shoulders leave the museum through its research section");
        }
        Game1.uiViewport=viewport;LocalizedContentManager.CurrentLanguageCode=LocalizedContentManager.LanguageCode.ru;Move("Forest");
    }
}
