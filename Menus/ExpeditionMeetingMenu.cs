using Archeo.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;

namespace Archeo.Menus;

/// <summary>A local field conversation with two portraits; it never moves the world's scheduled NPCs.</summary>
internal sealed class ExpeditionMeetingMenu:IClickableMenu
{
    private readonly ModEntry mod;
    private readonly PuzzleExpedition story;
    private readonly List<Rectangle> choices=new();
    private int page;
    private int focus;
    private int scroll;
    private int maxScroll;
    private static readonly Color Ink=new(62,52,39);
    internal ExpeditionMeetingMenu(ModEntry mod,PuzzleExpedition story):base(0,0,100,100,true){this.mod=mod;this.story=story;Layout();}
    private void Layout()
    {
        width=Math.Min(960,Game1.uiViewport.Width-40);height=Math.Min(610,Game1.uiViewport.Height-40);
        xPositionOnScreen=(Game1.uiViewport.Width-width)/2;yPositionOnScreen=(Game1.uiViewport.Height-height)/2;initializeUpperRightCloseButton();
        choices.Clear();int count=page<3?1:3;
        for(int i=0;i<count;i++)choices.Add(new(xPositionOnScreen+28,yPositionOnScreen+height-48-(count-1-i)*42,width-56,34));
    }
    private void Return()=>mod.OpenExpedition(story.Id);
    public override void gameWindowSizeChanged(Rectangle oldBounds,Rectangle newBounds)=>Layout();
    public override void receiveLeftClick(int x,int y,bool playSound=true)
    {
        if(upperRightCloseButton?.containsPoint(x,y)==true){Return();return;}
        int choice=choices.FindIndex(r=>r.Contains(x,y));if(choice<0)return;
        if(page<3){page++;focus=scroll=0;Layout();Game1.playSound("smallSelect");}
        else if(mod.ExpeditionChoice(story.Id,"observation",choice))Return();
    }
    public override void receiveKeyPress(Keys key)
    {
        if(key==Keys.Escape){Return();return;}
        if(key is Keys.Up or Keys.Down){focus=(focus+choices.Count+(key==Keys.Up?-1:1))%choices.Count;return;}
        if(key is Keys.Enter or Keys.Space){var rect=choices[focus];receiveLeftClick(rect.Center.X,rect.Center.Y);}
        if(key is Keys.PageUp or Keys.PageDown)receiveScrollWheelAction(key==Keys.PageUp?1:-1);
    }
    public override void receiveGamePadButton(Buttons button)
    {
        if(button is Buttons.B or Buttons.Back){Return();return;}
        if(button==Buttons.A)receiveKeyPress(Keys.Enter);
        if(button is Buttons.DPadUp or Buttons.LeftThumbstickUp)receiveKeyPress(Keys.Up);
        if(button is Buttons.DPadDown or Buttons.LeftThumbstickDown)receiveKeyPress(Keys.Down);
        if(button is Buttons.RightThumbstickUp or Buttons.RightThumbstickDown)receiveScrollWheelAction(button==Buttons.RightThumbstickUp?1:-1);
    }
    public override void receiveScrollWheelAction(int direction)=>scroll=Math.Clamp(scroll-Math.Sign(direction),0,maxScroll);
    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect,new Rectangle(0,0,Game1.uiViewport.Width,Game1.uiViewport.Height),Color.Black*.7f);
        drawTextureBox(b,xPositionOnScreen,yPositionOnScreen,width,height,Color.White);
        float scale=width<900?.8f:.9f;int x=xPositionOnScreen+28,y=yPositionOnScreen+24;
        b.DrawString(Game1.smallFont,mod.T("exp.meeting-title"),new(x,y),Ink,0,Vector2.Zero,scale,SpriteEffects.None,1);
        for(int i=0;i<2;i++)
        {
            var npc=Game1.getCharacterFromName(i==0?story.Primary:story.Secondary);if(npc==null)continue;
            int px=x+i*(width/2);b.Draw(npc.Portrait,new Rectangle(px,y+44,96,96),new Rectangle(0,0,64,64),Color.White);
            b.DrawString(Game1.smallFont,mod.ExpeditionPartnerName(npc.Name),new Vector2(px+106,y+70),Ink,0,Vector2.Zero,scale,SpriteEffects.None,1);
        }
        string text=mod.T("exp.meeting."+story.Id+"."+page);var lines=new List<string>();
        foreach(string paragraph in text.Split('\n'))
        {
            string line="";foreach(string word in paragraph.Split(' '))
            {string next=(line+" "+word).Trim();if(Game1.smallFont.MeasureString(next).X*scale>width-56&&line!=""){lines.Add(line);line=word;}else line=next;}lines.Add(line);
        }
        int top=y+168,bottom=choices[0].Y-18,lineH=(int)(Game1.smallFont.LineSpacing*scale);int visible=Math.Max(1,(bottom-top)/lineH);
        maxScroll=Math.Max(0,lines.Count-visible);scroll=Math.Clamp(scroll,0,maxScroll);
        for(int i=scroll;i<Math.Min(lines.Count,scroll+visible);i++)b.DrawString(Game1.smallFont,lines[i],new Vector2(x,top+(i-scroll)*lineH),Ink,0,Vector2.Zero,scale,SpriteEffects.None,1);
        if(maxScroll>0)b.DrawString(Game1.smallFont,"v",new Vector2(x+width-76,bottom-18),Ink);
        for(int i=0;i<choices.Count;i++)
        {
            var rect=choices[i];b.Draw(Game1.staminaRect,rect,i==focus?new Color(109,137,103):new Color(167,144,107));
            b.Draw(Game1.staminaRect,new Rectangle(rect.X+2,rect.Y+2,rect.Width-4,rect.Height-4),new Color(239,226,195));
            string label=mod.T(page<3?"exp.continue":"exp.observation."+story.Id+"."+i);
            float fit=Math.Min(scale,(rect.Width-20)/Math.Max(1,Game1.smallFont.MeasureString(label).X));
            b.DrawString(Game1.smallFont,label,new Vector2(rect.X+10,rect.Y+5),Ink,0,Vector2.Zero,fit,SpriteEffects.None,1);
        }
        upperRightCloseButton?.draw(b);drawMouse(b);
    }
}
