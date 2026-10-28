using Archeo.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace Archeo.Menus;

internal sealed partial class JournalMenu
{
    private int chapterPage;
    private bool AccessibleTab(string key) => key is "overview" or "settings" or "help" || mod.State.Journey.Started
        && (key is "catalog" or "equipment" or "chapters" || Journey.IsOpen(mod.State, key));
    private static string TabSection(string key) => key is "help" or "opening" ? "overview" : key is "overview" or "catalog" or "equipment" or "settings" or "chapters" ? key
        : Research.Cultures.Contains(key) ? "catalog" : "chapters";
    private List<ArtifactDefinition> CultureItems() => mod.Research.Artifacts.Where(p=>p.Culture==tab && Journey.VisibleArtifact(mod.State,p)).OrderBy(p=>p.Rarity).ToList();

    private void DrawInvitation(SpriteBatch b)
    {
        b.Draw(mod.Atlas,new Vector2(left.Center.X-40,left.Y+26),Sprite(21),Color.White,0,Vector2.Zero,5,SpriteEffects.None,1);
        Center(b,mod.T("journey.invitation-title"),new Rectangle(left.X+12,left.Y+124,left.Width-24,36),Ink,fontScale*1.05f);
        Paragraph(b,mod.T("journey.invitation-task"),new Rectangle(left.X+22,left.Y+184,left.Width-44,left.Height-200),Muted);
        DrawPageText(b,mod.T("journey.letter-heading"),mod.T("journey.letter"),right.Bottom-68);
        Button(b,"book:kit",mod.T(mod.InMuseum?"journey.accept-kit":"journey.visit-museum"),new Rectangle(right.X+12,right.Bottom-46,right.Width-24,38),mod.InMuseum);
    }

    private void DrawEquipment(SpriteBatch b)
    {
        b.Draw(mod.Atlas,new Vector2(left.Center.X-48,left.Y+28),Sprite(22),Color.White,0,Vector2.Zero,6,SpriteEffects.None,1);
        Center(b,mod.T("journey.instrument-name"),new Rectangle(left.X+10,left.Y+144,left.Width-20,32));
        Center(b,mod.T("journey.instrument-level",new { level=mod.State.Journey.InstrumentLevel }),new Rectangle(left.X+10,left.Y+180,left.Width-20,30),Muted);
        for(int i=0;i<3;i++)
            b.Draw(Game1.staminaRect,new Rectangle(left.Center.X-62+i*44,left.Y+232,36,8),i<mod.State.Journey.InstrumentLevel?new Color(102,137,111):new Color(208,197,169));
        Paragraph(b,mod.T("journey.instrument-use"),new Rectangle(left.X+22,left.Y+276,left.Width-44,left.Height-286),Muted);
        var cost=Journey.UpgradeCost(mod.State.Journey.InstrumentLevel);
        string text=mod.T("journey.equipment-guide")+"\n\n"+mod.T("journey.radius",new { radius=Journey.RevealRadius(mod.State).ToString("0.##") });
        if(mod.State.Journey.InstrumentLevel<3)
            text+="\n\n"+mod.T("journey.upgrade-cost",new { level=cost.Level,count=cost.Count,material=mod.T(cost.Material=="334"?"journey.copper":"journey.iron"),gold=cost.Gold });
        else text+="\n\n"+mod.T("journey.fully-upgraded");
        DrawPageText(b,mod.T("journey.equipment-title"),text,right.Bottom-110);
        Button(b,"book:upgrade",mod.T("journey.upgrade"),new Rectangle(right.X+12,right.Bottom-90,right.Width-24,36),mod.CanUpgradeInstrument);
        Button(b,"book:kit",mod.T("journey.replace-kit"),new Rectangle(right.X+12,right.Bottom-46,right.Width-24,36),mod.InMuseum);
    }

    private void DrawChapters(SpriteBatch b)
    {
        var chapters=Journey.Chapters.Where(p=>Journey.IsOpen(mod.State,p) && p!="catalog").ToList();
        int size=Math.Max(1,(left.Height-100)/54),pages=Math.Max(1,(chapters.Count+size-1)/size);
        chapterPage=Math.Clamp(chapterPage,0,pages-1);
        Text(b,mod.T("journey.chapters-title"),left.X+12,left.Y+10,Ink,fontScale*1.05f);
        int index=0;
        foreach(string chapter in chapters.Skip(chapterPage*size).Take(size))
        {
            var rect=new Rectangle(left.X+8,left.Y+54+index++*54,left.Width-16,46);
            Button(b,"book:chapter:"+chapter,mod.T("journey.chapter."+chapter),rect);
        }
        if(pages>1)
        {
            Button(b,"book:prev",mod.T("unusual.page-prev"),new Rectangle(left.X+8,left.Bottom-35,left.Width/2-12,30),chapterPage>0);
            Button(b,"book:next",mod.T("unusual.page-next"),new Rectangle(left.Center.X+4,left.Bottom-35,left.Width/2-12,30),chapterPage+1<pages);
        }
        DrawPageText(b,mod.T("journey.next-heading"),NextDiscoveryText(),right.Bottom-22);
    }

    private string NextDiscoveryText()
    {
        if (OpeningStory.Active(mod.State)) return mod.T(mod.State.Opening.Stage == OpeningStage.Restored ? "opening.goal.talk" : "opening.next-search");
        if (RouteStories.Pending(mod.State) is { } route)
            return mod.T("routes.goal."+route.Id,new{artifact=mod.ArtifactName(mod.Research.Artifacts.Single(p=>p.Id==route.Trigger))});
        if (mod.State.Routes.Initialized && !Journey.IsOpen(mod.State,"sea") && Journey.IsOpen(mod.State,"dwarf")) return mod.T("routes.next-mountain");
        string key=mod.State.Restored.Count==0?"journey.next-first":!Journey.IsOpen(mod.State,"sea")?"journey.next-second":"journey.next-explore";
        return mod.T(key)+"\n\n"+mod.T("journey.chapters-note");
    }
}
