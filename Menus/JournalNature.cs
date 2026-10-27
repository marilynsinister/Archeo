using Archeo.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace Archeo.Menus;

internal sealed partial class JournalMenu
{
    private void DrawNature(SpriteBatch b)
    {
        buttons.Remove("tools");
        if (!NaturalEvents.Kinds.Contains(selected)) selected = "snow";
        Text(b,mod.T("nature.title"),left.X+12,left.Y+8,Ink,fontScale*1.08f);
        int cardH = Math.Min(95,(left.Height-45)/3);
        for (int i=0;i<NaturalEvents.Kinds.Length;i++)
        {
            string kind = NaturalEvents.Kinds[i];
            var rect = new Rectangle(left.X+8,left.Y+44+i*cardH,left.Width-16,cardH-6);
            Button(b,"nature:"+kind,mod.T("nature.name."+kind),rect,active:selected==kind);
        }
        var artifact = mod.Research.Artifacts.First(p=>p.Source==selected);
        bool pending = mod.State.Nature.PendingRewards.Contains(artifact.Id);
        bool known = pending || mod.State.Restored.ContainsKey(artifact.Id);
        int x=right.X+12,w=right.Width-24,top=right.Y+96,bottom=right.Bottom-52;
        b.Draw(mod.Atlas,new Vector2(x+4,right.Y+8),Sprite(artifact.Sprite),known?Color.White:new Color(67,55,43),0,Vector2.Zero,4,SpriteEffects.None,1);
        Paragraph(b,known?mod.ArtifactName(artifact):mod.T("journal.unknown"),new Rectangle(x+82,right.Y+10,w-82,64),ModEntry.RarityColor(artifact.Rarity));
        var active=mod.State.Sites.FirstOrDefault(s=>s.EventKind==selected && s.Expires>=ModEntry.Today && s.Points.Any(p=>!p.Dug));
        var lead=mod.State.Nature.Leads.FirstOrDefault(p=>p.Kind==selected && p.Expires>=ModEntry.Today);
        string status=active!=null ? mod.T("weather.active",new { location=mod.LocationName(active.Location),count=active.Points.Count(p=>!p.Dug),days=active.Expires-ModEntry.Today+1 })
            : lead!=null ? mod.T(lead.Day>ModEntry.Today?"nature.tomorrow":"nature.queued",new { days=lead.Expires-ModEntry.Today+1 }) : mod.T("nature.waiting");
        string text=status+"\n\n"+mod.T("nature.guide."+selected,new { days=mod.Config.NatureSiteLifetimeDays })
            +"\n\n"+mod.T("nature.rules");
        if (known) text+="\n\n"+mod.T("artifact."+artifact.Id+".description")+"\n\n"+mod.T(mod.State.Restored.ContainsKey(artifact.Id)?"artifact."+artifact.Id+".lore":"journal.lore-locked");
        if (!mod.Config.EnableNatureEvents) text+="\n\n"+mod.T("nature.disabled");
        text+="\n\n"+mod.T("weather.record",new { count=mod.State.Nature.Completed.GetValueOrDefault(selected) });
        var lines=Wrap(text,w).ToList();
        int lineH=(int)(Game1.smallFont.LineSpacing*fontScale),visible=Math.Max(1,(bottom-top)/lineH);
        maxScroll=Math.Max(0,lines.Count-visible);
        scroll=Math.Clamp(scroll,0,maxScroll);
        for(int i=scroll;i<Math.Min(lines.Count,scroll+visible);i++) Text(b,lines[i],x,top+(i-scroll)*lineH);
        if(maxScroll>0) Text(b,(scroll>0?"↑ ":"")+(scroll<maxScroll?"↓":""),right.Right-24,bottom-16,Muted,.65f);
        if(pending) Button(b,"claim:"+artifact.Id,mod.T("weather.claim"),new Rectangle(x,right.Bottom-42,w,34));
    }
}
