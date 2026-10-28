using Archeo.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace Archeo.Menus;

internal sealed partial class JournalMenu
{
    private void DrawRoutes(SpriteBatch b)
    {
        var stories=RouteStories.Visible(mod.State);
        if (!stories.Any(p=>p.Id==selected)) selected=RouteStories.Pending(mod.State)?.Id ?? stories.FirstOrDefault()?.Id ?? "";
        Text(b,mod.T("routes.title"),left.X+12,left.Y+10,Ink,fontScale*1.05f);
        int row=0;
        foreach(var story in stories)
        {
            var rect=new Rectangle(left.X+8,left.Y+60+row++*82,left.Width-16,72);
            Button(b,"route:select:"+story.Id,"",rect,active:selected==story.Id);
            string title=mod.T("routes.title."+story.Id);
            Text(b,title,rect.X+10,rect.Y+8,Ink,Math.Min(fontScale,(rect.Width-20f)/Math.Max(1,Game1.smallFont.MeasureString(title).X)));
            Text(b,mod.T(mod.State.Routes.Explained.Contains(story.Id)?"routes.read":"routes.pending"),rect.X+10,rect.Y+41,Muted,fontScale*.8f);
        }
        var current=stories.FirstOrDefault(p=>p.Id==selected);
        if(current==null) { DrawPageText(b,mod.T("routes.title"),mod.T("routes.empty"),right.Bottom-22);return; }
        bool explained=mod.State.Routes.Explained.Contains(current.Id);
        string source=mod.ArtifactName(mod.Research.Artifacts.Single(p=>p.Id==current.Trigger));
        string text=mod.T("routes.source",new{artifact=source})+"\n\n"+(explained
            ? mod.T("routes.conversation."+current.Id).Replace("^","\n\n")
            : mod.T("routes.clue."+current.Id)+"\n\n"+mod.T("routes.instructions."+current.Id));
        DrawPageText(b,mod.T("routes.title."+current.Id),text,right.Bottom-68);
        Button(b,explained?"route:replay":"route:talk",mod.T(explained?"routes.replay":"routes.talk."+current.Id),
            new Rectangle(right.X+12,right.Bottom-46,right.Width-24,38),explained || mod.CanExplainRoute(current.Id));
    }

    private void HandleRouteClick(string id)
    {
        if(id.StartsWith("route:select:")) { selected=id[13..];scroll=0; }
        else if(id=="route:talk") mod.ExplainRoute(selected);
        else if(id=="route:replay" && mod.State.Routes.Explained.Contains(selected) && mod.State.Routes.Clues.ContainsKey(selected))
            Game1.drawObjectDialogue(mod.T("routes.conversation."+selected));
    }
}
