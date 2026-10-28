using Archeo.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace Archeo.Menus;

internal sealed partial class JournalMenu
{
    private string museumTheme="valley";
    private string museumTrade="";
    private int museumAchievement;
    private static readonly string[] MuseumPages={"orders","shows","exchange","achievements"};
    private List<ArtifactDefinition> MuseumDuplicates()=>mod.Research.Artifacts.Where(a=>a.Source=="research"&&a.Rarity==0&&a.Effect==""&&mod.State.Restored.GetValueOrDefault(a.Id)>=2).ToList();
    private void MuseumText(SpriteBatch b,string title,string text,int bottom,int start=62)
    {
        Text(b,title,right.X+12,right.Y+8,Ink,Math.Min(fontScale,(right.Width-24)/Math.Max(1,Game1.smallFont.MeasureString(title).X)));
        var lines=Wrap(text,right.Width-24).ToList();int lineH=ReadingLineHeight;
        int visible=Math.Max(1,(bottom-right.Y-start)/lineH);maxScroll=Math.Max(0,lines.Count-visible);scroll=Math.Clamp(scroll,0,maxScroll);
        for(int i=scroll;i<Math.Min(lines.Count,scroll+visible);i++)ReadingText(b, lines[i],right.X+12,right.Y+start+(i-scroll)*lineH);
        if(maxScroll>0)Text(b,(scroll>0?"^ ":"")+(scroll<maxScroll?"v":""),right.Right-20,bottom-18,Muted,.65f);
    }
    private void DrawMuseum(SpriteBatch b)
    {
        if(!MuseumPages.Contains(selected))selected="orders";
        Text(b,mod.T("museum.title"),left.X+12,left.Y+8,Ink,fontScale*1.05f);
        for(int i=0;i<MuseumPages.Length;i++)Button(b,"museum:page:"+MuseumPages[i],mod.T("museum.page."+MuseumPages[i]),new Rectangle(left.X+8,left.Y+54+i*54,left.Width-16,46),active:selected==MuseumPages[i]);
        Text(b,mod.T("museum.marks",new{count=mod.State.Museum.Marks}),left.X+14,left.Y+282,Muted,fontScale*.88f);
        if(selected is "shows" or "exchange")
            Button(b,"museum:theme",mod.CultureName(museumTheme),new Rectangle(left.X+8,left.Bottom-46,left.Width-16,38));
        else Paragraph(b,mod.T("museum.visit"),new Rectangle(left.X+14,left.Y+316,left.Width-28,left.Height-320),Muted);
        int x=right.X+12,w=right.Width-24;
        if(selected=="orders")
        {
            var order=mod.State.Museum.Order;string text=mod.T("museum.order-intro");
            if(order!=null&&order.Week==LivingMuseum.Week(ModEntry.Today))
            {
                text=string.Join("\n",LivingMuseum.OrderItems(order.Theme,mod.Research.Artifacts).Select(id=>mod.T("museum.require-item",new{name=mod.ArtifactName(mod.Research.Artifacts.Single(a=>a.Id==id)),count=mod.MuseumItemCount(id)})))
                    +"\n\n"+mod.T(order.Completed?"museum.order-done":"museum.order-reward",new{days=LivingMuseum.WeekEnd(ModEntry.Today)-ModEntry.Today+1,culture=mod.CultureName(order.Theme)})
                    +"\n\n"+mod.T("museum.order-story."+order.Theme)+"\n\n"+text;
            }
            else text+="\n\n"+mod.T("museum.no-order");
            MuseumText(b,mod.T("museum.page.orders"),text,right.Bottom-58);
            Button(b,"museum:deliver",mod.T(order?.Completed==true?"museum.done":"museum.deliver"),new Rectangle(x,right.Bottom-46,w,38),mod.CanDeliverMuseumOrder);
        }
        else if(selected=="shows")
        {
            var show=mod.State.Museum.Exhibition;
            string text=mod.T("museum.show-guide");
            if(show!=null)text=mod.T("museum.show-status",new{culture=mod.CultureName(show.Theme),days=show.DisplayDays,total=LivingMuseum.RequiredDays})+"\n\n"
                +mod.T(show.Claimed?"museum.show-thanks":show.Ready?"museum.show-ready":show.Ends<ModEntry.Today?"museum.show-expired":"museum.show-deadline",new{days=Math.Max(0,show.Ends-ModEntry.Today+1)})+"\n\n"+text;
            string visitorTheme=show is {Claimed:false}?show.Theme:museumTheme;
            text+="\n\n"+mod.T("museum.visitor-guide",new{names=string.Join(", ",LivingMuseum.Visitors(visitorTheme).Select(n=>Game1.getCharacterFromName(n)?.displayName??n))});
            if(show==null||show.Claimed||!show.Ready&&show.Ends<ModEntry.Today)
            {
                var set=MuseumCollections.All.Single(c=>c.Id==museumTheme);
                text+="\n\n"+mod.CultureName(museumTheme)+":\n"+string.Join("\n",set.Artifacts.Select(id=>{var a=mod.Research.Artifacts.Single(a=>a.Id==id);return mod.State.Restored.ContainsKey(id)?mod.ArtifactName(a):mod.T("journal.unknown");}));
                if(mod.State.Museum.NextExhibitionDay>ModEntry.Today)text+="\n\n"+mod.T("museum.show-wait",new{days=mod.State.Museum.NextExhibitionDay-ModEntry.Today});
            }
            var visitor=mod.NearbyMuseumVisitor();
            MuseumText(b,mod.T("museum.page.shows"),text,right.Bottom-102);
            Button(b,"museum:react",mod.T("museum.discuss",new{name=visitor?.displayName??mod.T("museum.visitor")}),new Rectangle(x,right.Bottom-90,w,36),visitor!=null);
            bool claim=show is {Ready:true,Claimed:false};
            Button(b,claim?"museum:show-claim":"museum:show-start",mod.T(claim?"museum.show-claim":"museum.show-start"),new Rectangle(x,right.Bottom-46,w,38),claim||LivingMuseum.CanStart(mod.State,museumTheme,ModEntry.Today,mod.NearbyMuseumDisplay(museumTheme)));
        }
        else if(selected=="exchange")
        {
            var duplicates=MuseumDuplicates();if(!duplicates.Any(a=>a.Id==museumTrade))museumTrade=duplicates.FirstOrDefault()?.Id??"";
            var item=duplicates.FirstOrDefault(a=>a.Id==museumTrade);
            string text=(item==null?mod.T("museum.no-duplicates"):mod.T("museum.exchange-item",new{name=mod.ArtifactName(item),count=mod.MuseumItemCount(item.Id)}))
                +"\n\n"+mod.T("museum.supplies",new{culture=mod.CultureName(museumTheme)})+"\n\n"+mod.T("museum.exchange-guide");
            MuseumText(b,mod.T("museum.page.exchange"),text,right.Bottom-140);
            Button(b,"museum:trade-prev",mod.T("unusual.page-prev"),new Rectangle(x,right.Bottom-132,w/2-4,28),duplicates.Count>1);
            Button(b,"museum:trade-next",mod.T("unusual.page-next"),new Rectangle(x+w/2+4,right.Bottom-132,w/2-4,28),duplicates.Count>1);
            Button(b,"museum:trade",mod.T("museum.exchange"),new Rectangle(x,right.Bottom-90,w,36),mod.InMuseum&&item!=null&&LivingMuseum.CanExchange(mod.State,item,mod.MuseumItemCount(item.Id)));
            Button(b,"museum:supplies",mod.T("museum.buy-supplies"),new Rectangle(x,right.Bottom-46,w,38),mod.InMuseum&&mod.State.Museum.Marks>=2);
        }
        else
        {
            var achievement=LivingMuseum.Achievements[museumAchievement];bool ready=LivingMuseum.AchievementReady(mod.State.Museum,achievement.Id);
            string text=mod.T("museum.achievement."+achievement.Id)+"\n\n"+mod.T("museum.achievement-progress",new{orders=mod.State.Museum.OrdersCompleted,shows=mod.State.Museum.ExhibitionsCompleted})
                +"\n\n"+mod.T(ready?"museum.achievement-unlocked":"museum.achievement-hidden");
            MuseumText(b,mod.T("museum.page.achievements"),text,right.Bottom-102,ready?142:62);
            if(ready)
            {
                var data=ItemRegistry.GetDataOrErrorItem("(F)"+ModEntry.ItemId(achievement.Reward));
                b.Draw(data.GetTexture(),new Vector2(x,right.Y+52),data.GetSourceRect(),Color.White,0,Vector2.Zero,2,SpriteEffects.None,1);
                string name=mod.T("museum.furniture."+achievement.Reward);
                Text(b,name,x+76,right.Y+66,Ink,Math.Min(fontScale,(w-76)/Math.Max(1,Game1.smallFont.MeasureString(name).X)));
            }
            Button(b,"museum:award-prev",mod.T("unusual.page-prev"),new Rectangle(x,right.Bottom-90,w/2-4,32),museumAchievement>0);
            Button(b,"museum:award-next",mod.T("unusual.page-next"),new Rectangle(x+w/2+4,right.Bottom-90,w/2-4,32),museumAchievement+1<LivingMuseum.Achievements.Length);
            bool claimed=mod.State.Museum.AchievementClaims.Contains(achievement.Id);
            Button(b,"museum:award",mod.T(claimed?"museum.done":"museum.claim-award"),new Rectangle(x,right.Bottom-46,w,38),ready&&!claimed);
        }
    }
    private void HandleMuseumClick(string id)
    {
        if(id.StartsWith("museum:page:")){selected=id[12..];scroll=0;return;}
        if(id=="museum:theme")
        {
            var themes=Research.Cultures.Where(c=>Journey.IsOpen(mod.State,c)).ToArray();museumTheme=themes[(Array.IndexOf(themes,museumTheme)+1)%themes.Length];
        }
        else if(id=="museum:deliver")mod.DeliverMuseumOrder();
        else if(id=="museum:trade")mod.ExchangeMuseumDuplicate(museumTrade);
        else if(id=="museum:supplies")mod.BuyMuseumSupplies(museumTheme);
        else if(id=="museum:show-start")mod.StartMuseumExhibition(museumTheme);
        else if(id=="museum:show-claim")mod.ClaimMuseumExhibition();
        else if(id=="museum:react")mod.ReactToMuseumExhibition();
        else if(id=="museum:award")mod.ClaimMuseumAchievement(LivingMuseum.Achievements[museumAchievement].Id);
        else if(id is "museum:award-prev" or "museum:award-next")museumAchievement=Math.Clamp(museumAchievement+(id.EndsWith("next")?1:-1),0,LivingMuseum.Achievements.Length-1);
        else if(id is "museum:trade-prev" or "museum:trade-next")
        {
            var items=MuseumDuplicates();if(items.Count>0)museumTrade=items[(Math.Max(0,items.FindIndex(a=>a.Id==museumTrade))+items.Count+(id.EndsWith("next")?1:-1))%items.Count].Id;
        }
        scroll=0;
    }
}
