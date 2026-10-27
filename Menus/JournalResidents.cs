using Archeo.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace Archeo.Menus;

internal sealed partial class JournalMenu
{
    private bool residentArchive;

    private void DrawResidents(SpriteBatch b)
    {
        buttons.Remove("tools");
        if (!ResidentStories.Sources.Contains(selected)) selected = "penny";
        Text(b,mod.T("residents.title"),left.X+12,left.Y+8,Ink,fontScale*1.08f);
        int cardH = Math.Min(94,(left.Height-100)/3);
        for (int i=0;i<ResidentStories.All.Length;i++)
        {
            var entry = ResidentStories.All[i];
            var rect = new Rectangle(left.X+8,left.Y+44+i*cardH,left.Width-16,cardH-6);
            Button(b,"resident:select:"+entry.Id,mod.ResidentName(entry),rect,active:entry.Id==selected);
        }
        var story = ResidentStories.All.First(p => p.Id == selected);
        var stage = ResidentStories.Stage(mod.State.Residents,selected);
        if (stage >= ResidentStage.Searching)
            Button(b,"resident:archive",mod.T(residentArchive?"residents.return":"residents.read"),new Rectangle(left.X+8,left.Bottom-44,left.Width-16,36));
        var artifact = mod.Research.Artifacts.First(p => p.Id == (stage >= ResidentStage.Reward ? story.Artifact : story.Trigger));
        int x=right.X+12,w=right.Width-24;
        b.Draw(mod.Atlas,new Vector2(x+4,right.Y+8),Sprite(artifact.Sprite),stage==ResidentStage.Locked?new Color(67,55,43):Color.White,0,Vector2.Zero,4,SpriteEffects.None,1);
        Paragraph(b,mod.T("residents.title."+selected),new Rectangle(x+82,right.Y+8,w-82,75));
        bool archive = residentArchive && stage >= ResidentStage.Searching;
        string text;
        if (archive)
        {
            text = mod.T("residents.dialogue."+selected+".start");
            if (stage >= ResidentStage.Recovered) text += "\n\n"+mod.T("residents.note."+selected);
            if (stage >= ResidentStage.Reward) text += "\n\n"+mod.T("residents.dialogue."+selected+".share");
            text = text.Replace("#$b#","\n\n");
        }
        else
        {
            text = mod.T("residents.stage."+(int)stage,new { name = mod.ResidentName(story), artifact = mod.ArtifactName(mod.Research.Artifacts.First(p=>p.Id==story.Trigger)), location = mod.LocationName(story.Location) });
            text += "\n\n"+mod.T("residents.goal."+selected);
            if (stage == ResidentStage.Searching)
            {
                var site = mod.State.Sites.FirstOrDefault(p=>p.EventKind==story.SiteKind && p.Points.Any(n=>!n.Dug));
                text += "\n\n"+(site == null ? mod.T("residents.no-space") : mod.T("residents.active",new { location=mod.LocationName(site.Location),count=site.Points.Count(p=>!p.Dug),key=mod.Config.SurveyKey.ToString() }));
            }
            if (stage >= ResidentStage.Recovered) text += "\n\n"+mod.T("residents.note."+selected);
            text += "\n\n"+mod.T("residents.reward",new { artifact=mod.ArtifactName(mod.Research.Artifacts.First(p=>p.Id==story.Artifact)),friendship=ResidentStories.Friendship,experience=ResidentStories.Experience,fragments=ResidentStories.Fragments });
            if (stage >= ResidentStage.Reward) text += "\n\n"+mod.T("artifact."+story.Artifact+".description");
        }
        string? action = archive ? null : stage switch { ResidentStage.Clue=>"start",ResidentStage.Recovered=>"share",ResidentStage.Reward=>"claim",_=>null };
        int top=right.Y+96,bottom=right.Bottom-(action==null?14:58),lineH=(int)(Game1.smallFont.LineSpacing*fontScale);
        var lines=Wrap(text,w).ToList();
        int visible=Math.Max(1,(bottom-top)/lineH);
        maxScroll=Math.Max(0,lines.Count-visible);
        scroll=Math.Clamp(scroll,0,maxScroll);
        for(int i=scroll;i<Math.Min(lines.Count,scroll+visible);i++) Text(b,lines[i],x,top+(i-scroll)*lineH);
        if(maxScroll>0) Text(b,(scroll>0?"↑ ":"")+(scroll<maxScroll?"↓":""),right.Right-24,bottom-16,Muted,.65f);
        if(action!=null) Button(b,"resident:action:"+selected+":"+action,mod.T("residents.action."+action),new Rectangle(x,right.Bottom-44,w,38),mod.CanResidentAction(selected,action));
    }
}
