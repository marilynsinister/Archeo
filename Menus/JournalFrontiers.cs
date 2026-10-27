using Archeo.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace Archeo.Menus;

internal sealed partial class JournalMenu
{
    private void DrawFrontiers(SpriteBatch b)
    {
        if(!FrontierResearch.Cultures.Contains(selected))selected="calico";
        Text(b,mod.T("frontier.title"),left.X+12,left.Y+10,Ink,fontScale*1.05f);
        for(int i=0;i<FrontierResearch.Cultures.Length;i++)
        {
            string culture=FrontierResearch.Cultures[i];
            var rect=new Rectangle(left.X+8,left.Y+64+i*88,left.Width-16,78);
            Button(b,"frontier:select:"+culture,"",rect,active:selected==culture);
            Text(b,mod.CultureName(culture),rect.X+12,rect.Y+10,Ink,fontScale);
            Text(b,mod.T(Journey.IsOpen(mod.State,culture)?"frontier.open":"frontier.closed"),rect.X+12,rect.Y+44,Muted,fontScale*.8f);
        }
        bool open=Journey.IsOpen(mod.State,selected);
        string text=mod.T("frontier.intro."+selected)+"\n\n"+mod.T(open?"frontier.available."+selected:"frontier.requires."+selected)
            +"\n\n"+mod.T("frontier.rules");
        DrawPageText(b,mod.CultureName(selected),text,right.Bottom-66);
        Button(b,"frontier:open",mod.T("frontier.open-page"),new Rectangle(right.X+12,right.Bottom-46,right.Width-24,38),open);
    }
}
