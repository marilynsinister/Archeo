using Archeo.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace Archeo.Menus;

internal sealed partial class JournalMenu
{
    private string objectiveSelection = "";
    private bool overviewGuide;
    private int ObjectivePageSize => ObjectiveListLayout.Create(left.Height,0).PageSize;

    private void DrawOverview(SpriteBatch b)
    {
        var goals = mod.FieldObjectives();
        if (!goals.Any(p => p.Id == objectiveSelection))
            objectiveSelection = FieldJournal.Tracked(mod.State,goals)?.Id ?? goals.FirstOrDefault()?.Id ?? "";
        Text(b,mod.T("field.title"),left.X+12,left.Y+7,Ink,fontScale*1.08f);
        int helpW = Math.Min(145,left.Width/2);
        Button(b,"field:guide",mod.T("field.guide"),new Rectangle(left.Right-helpW-8,left.Y+4,helpW,28),active:overviewGuide);
        var layout = ObjectiveListLayout.Create(left.Height,goals.Count);
        int size = layout.PageSize, pages = Math.Max(1,(goals.Count+size-1)/size);
        int index = goals.FindIndex(p => p.Id == objectiveSelection), page = Math.Max(0,index)/size;
        int rowH = layout.RowHeight;
        for (int i=0;i<Math.Min(size,goals.Count-page*size);i++)
        {
            var goal = goals[page*size+i];
            var rect = new Rectangle(left.X+6,left.Y+layout.FirstRowY+i*rowH,left.Width-12,rowH-4);
            Button(b,"field:select:"+goal.Id,"",rect,active:!overviewGuide && goal.Id==objectiveSelection);
            string title = (goal.Id == mod.State.TrackedObjective ? "* " : "")+mod.ObjectiveTitle(goal);
            string status = mod.ObjectiveStatus(goal);
            float fit = Math.Min(fontScale,(rect.Width-20f)/Math.Max(1,Game1.smallFont.MeasureString(title).X));
            Text(b,title,rect.X+10,rect.Y+4,Ink,fit);
            float statusScale = Math.Min(fontScale*.75f,(rect.Width-20f)/Math.Max(1,Game1.smallFont.MeasureString(status).X));
            Text(b,status,rect.X+10,rect.Bottom-23,goal.Expires==ModEntry.Today?new Color(161,71,46):Muted,statusScale);
        }
        if (goals.Count == 0) Paragraph(b,mod.T("field.empty"),new Rectangle(left.X+12,left.Y+56,left.Width-24,left.Height-155),Muted);
        int pageY = left.Y+layout.PageControlsY, buttonW = Math.Min(96,(left.Width-105)/2);
        Button(b,"field:prev",mod.T("unusual.page-prev"),new Rectangle(left.X+8,pageY,buttonW,30),page>0);
        Button(b,"field:next",mod.T("unusual.page-next"),new Rectangle(left.Right-buttonW-8,pageY,buttonW,30),page+1<pages);
        Center(b,mod.T("unusual.page",new { page=page+1,pages }),new Rectangle(left.X+buttonW+12,pageY,left.Width-buttonW*2-24,30),Muted,fontScale*.75f);
        Button(b,"book:equipment",mod.T("journey.equipment-title"),new Rectangle(left.X+8,left.Y+layout.ToolsY,left.Width-16,36));

        var current = goals.FirstOrDefault(p => p.Id == objectiveSelection);
        bool guide = overviewGuide || current == null;
        int x=right.X+12,w=right.Width-24;
        string heading = guide ? mod.T("journal.field-guide") : mod.ObjectiveTitle(current!);
        Paragraph(b,heading,new Rectangle(x,right.Y+8,w,74),Ink);
        string text;
        if (guide)
        {
            text = mod.T("journal.guide",new { journal=mod.Config.JournalKey.ToString(),survey=mod.Config.SurveyKey.ToString() })
                +"\n\n"+mod.T("field.guide-notes");
            if (mod.State.Stars.Lead is { } lead)
                text += "\n\n"+mod.T("unusual.forecast-note",new { location=mod.LocationName(lead.Location),culture=mod.CultureName(lead.Culture) });
        }
        else
        {
            text = mod.ObjectiveStatus(current!)+"\n\n"+mod.ObjectiveDescription(current!)+"\n\n"+mod.T("field.navigation");
            if (current!.Id == mod.State.TrackedObjective && (!mod.Config.ShowTrackedObjective || !mod.Config.ShowHud))
                text += "\n\n"+mod.T("field.hidden-tracker");
        }
        int top=right.Y+90,bottom=right.Bottom-(guide?16:94),lineH=(int)(Game1.smallFont.LineSpacing*fontScale);
        var lines=Wrap(text,w).ToList();
        int visible=Math.Max(1,(bottom-top)/lineH);
        maxScroll=Math.Max(0,lines.Count-visible);
        scroll=Math.Clamp(scroll,0,maxScroll);
        for(int i=scroll;i<Math.Min(lines.Count,scroll+visible);i++) Text(b,lines[i],x,top+(i-scroll)*lineH);
        if(maxScroll>0) Text(b,(scroll>0?"^ ":"")+(scroll<maxScroll?"v":""),right.Right-24,bottom-16,Muted,.65f);
        if(!guide)
        {
            if(current!.Tab!="overview") Button(b,"field:open",mod.T("field.open"),new Rectangle(x,right.Bottom-82,w,34));
            Button(b,"field:track",mod.T(current.Id==mod.State.TrackedObjective?"field.unpin":"field.pin"),new Rectangle(x,right.Bottom-42,w,34),active:current.Id==mod.State.TrackedObjective);
        }
    }

    private void HandleObjectiveClick(string id)
    {
        if(id=="field:guide") { overviewGuide=!overviewGuide;scroll=0;return; }
        var goals=mod.FieldObjectives();
        var current=goals.FirstOrDefault(p=>p.Id==objectiveSelection);
        if(id.StartsWith("field:select:"))
        {
            string target=id[13..];
            if(goals.Any(p=>p.Id==target)) { objectiveSelection=target;overviewGuide=false;scroll=0; }
        }
        else if(id is "field:prev" or "field:next")
        {
            int page=Math.Max(0,goals.FindIndex(p=>p.Id==objectiveSelection))/ObjectivePageSize+(id=="field:next"?1:-1);
            if(page>=0 && page*ObjectivePageSize<goals.Count) { objectiveSelection=goals[page*ObjectivePageSize].Id;overviewGuide=false;scroll=0; }
        }
        else if(id=="field:track" && current!=null && !overviewGuide) mod.ToggleTrackedObjective(current.Id);
        else if(id=="field:open" && current!=null && !overviewGuide) OpenObjective(current);
    }

    private void OpenObjective(FieldObjective goal)
    {
        if (!AccessibleTab(goal.Tab)) return;
        tab=goal.Tab;
        selected=goal.Selection;
        if(tab=="museum")
        {
            if(Research.Cultures.Contains(goal.Culture))museumTheme=goal.Culture;
            if(goal.Topic=="museum_award")museumAchievement=Math.Max(0,Array.FindIndex(LivingMuseum.Achievements,a=>a.Reward==goal.Artifact));
        }
        keystone=goal.UseKeystone;
        residentArchive=false;
        scroll=0;
        if(tab=="overview") overviewGuide=true;
    }
}
