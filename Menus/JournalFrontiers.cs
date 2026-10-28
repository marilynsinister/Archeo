using Archeo.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace Archeo.Menus;

internal sealed partial class JournalMenu
{
    private void DrawFrontiers(SpriteBatch b)
    {
        var cultures=FrontierResearch.Cultures.Where(c=>mod.State.Routes.LegacyFrontierIndex || Journey.IsOpen(mod.State,c)
            || RouteStories.Pending(mod.State)?.Id==c).ToList();
        if(!cultures.Contains(selected))selected=cultures.FirstOrDefault()??"";
        Text(b,mod.T("frontier.title"),left.X+12,left.Y+10,Ink,fontScale*1.05f);
        for(int i=0;i<cultures.Count;i++)
        {
            string culture=cultures[i];
            var rect=new Rectangle(left.X+8,left.Y+64+i*88,left.Width-16,78);
            Button(b,"frontier:select:"+culture,"",rect,active:selected==culture);
            Text(b,mod.CultureName(culture),rect.X+12,rect.Y+10,Ink,fontScale);
            Text(b,mod.T(Journey.IsOpen(mod.State,culture)?"frontier.open":"frontier.closed"),rect.X+12,rect.Y+44,Muted,fontScale*.8f);
        }
        if(selected=="") { DrawPageText(b,mod.T("frontier.title"),mod.T("routes.empty"),right.Bottom-22);return; }
        bool open=Journey.IsOpen(mod.State,selected);
        bool clue=RouteStories.Pending(mod.State)?.Id==selected;
        bool access=selected=="calico"?mod.State.Frontiers.DesertAccess:mod.State.Frontiers.IslandAccess;
        string detail=open?mod.T("frontier.available."+selected):clue?mod.T("routes.clue."+selected)+"\n\n"+mod.T("routes.instructions."+selected)
            :mod.T(access?"routes.wait."+selected:"frontier.requires."+selected);
        string text=mod.T("frontier.intro."+selected)+"\n\n"+detail
            +"\n\n"+mod.T("frontier.rules");
        DrawPageText(b,mod.CultureName(selected),text,right.Bottom-66);
        Button(b,clue?"frontier:clue":"frontier:open",mod.T(clue?"routes.view":"frontier.open-page"),new Rectangle(right.X+12,right.Bottom-46,right.Width-24,38),open||clue);
    }
}
