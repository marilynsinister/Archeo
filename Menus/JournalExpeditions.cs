using Archeo.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace Archeo.Menus;

internal sealed partial class JournalMenu
{
    private bool expeditionArchive;
    internal void FocusExpedition(string id){if(PuzzleExpeditions.Stage(mod.State.Expeditions,id)!=ExpeditionStage.Locked)selected=id;}
    private void DrawExpeditions(SpriteBatch b)
    {
        var stories=PuzzleExpeditions.All.Where(s=>PuzzleExpeditions.Stage(mod.State.Expeditions,s.Id)!=ExpeditionStage.Locked).ToList();
        if(stories.Count==0)return;if(!stories.Any(s=>s.Id==selected))selected=stories[0].Id;
        var story=stories.First(s=>s.Id==selected);var record=mod.State.Expeditions.Stories[story.Id];var stage=record.Stage;
        Text(b,mod.T("exp.section"),left.X+12,left.Y+8,Ink,fontScale*1.05f);
        for(int i=0;i<stories.Count;i++)
        {
            var rect=new Rectangle(left.X+8,left.Y+54+i*88,left.Width-16,78);
            Button(b,"exp:select:"+stories[i].Id,"",rect,active:stories[i].Id==selected);
            string title=mod.ExpeditionTitle(stories[i].Id);Text(b,title,rect.X+10,rect.Y+8,Ink,Math.Min(fontScale,(rect.Width-20)/Math.Max(1,Game1.smallFont.MeasureString(title).X)));
            Text(b,mod.T("exp.status."+(int)PuzzleExpeditions.Stage(mod.State.Expeditions,stories[i].Id)),rect.X+10,rect.Y+44,Muted,fontScale*.72f);
        }
        string names=string.Join(" · ",new[]{story.Primary,story.Secondary}.Select(mod.ExpeditionPartnerName));
        Paragraph(b,names,new Rectangle(left.X+14,left.Y+244,left.Width-28,54),Muted);
        Button(b,"exp:archive",mod.T(expeditionArchive?"exp.back":"exp.archive"),new Rectangle(left.X+8,left.Bottom-88,left.Width-16,36));
        int hints=record.Hints.GetValueOrDefault((int)stage);
        Button(b,"exp:hint",mod.T(hints==0?"exp.hint":hints==1?"exp.answer":"exp.hints-open"),new Rectangle(left.X+8,left.Bottom-44,left.Width-16,36),!expeditionArchive&&PuzzleExpeditions.HintStages.Contains(stage)&&hints<2);
        if(stage==ExpeditionStage.Restoration&&!expeditionArchive){DrawExpeditionRestoration(b,story,record);return;}
        string text;
        if(expeditionArchive)
        {
            text=mod.T("exp.intro."+story.Id);
            if(stage>=ExpeditionStage.FirstClue)text+="\n\n"+mod.T("exp.dialogue."+story.Id+".1");
            if(stage>=ExpeditionStage.Consult)text+="\n\n"+mod.T("exp.evidence."+story.Id+".1");
            if(stage>=ExpeditionStage.SecondClue)text+="\n\n"+mod.T("exp.dialogue."+story.Id+".4");
            if(stage>=ExpeditionStage.Meeting)text+="\n\n"+mod.T("exp.evidence."+story.Id+".2");
            if(stage>=ExpeditionStage.Restoration)for(int i=0;i<3;i++)text+="\n\n"+mod.T("exp.meeting."+story.Id+"."+i);
            if(stage>=ExpeditionStage.Reward)text+="\n\n"+mod.T("exp.dialogue."+story.Id+".10");
        }
        else
        {
            string npc=stage==ExpeditionStage.Consult?story.Secondary:story.Primary;
            text=mod.T("exp.stage."+(int)stage,new{name=mod.ExpeditionPartnerName(npc),location=mod.LocationName(story.Second)});
            if(stage==ExpeditionStage.Invitation)text+="\n\n"+mod.T("exp.intro."+story.Id);
            if(stage is ExpeditionStage.FirstClue or ExpeditionStage.SecondClue)text+="\n\n"+mod.T("exp.clue."+story.Id+"."+(stage==ExpeditionStage.FirstClue?1:2));
            if(stage is ExpeditionStage.FirstDig or ExpeditionStage.SecondDig)
            {
                int leg=PuzzleExpeditions.Leg(stage);var site=mod.State.Sites.FirstOrDefault(s=>s.EventKind==story.Kind(leg));
                text+="\n\n"+mod.T(site==null?"exp.no-space":"exp.active",new{location=mod.LocationName(leg==1?story.First:story.Second),count=site?.Points.Count(p=>!p.Dug)??2});
            }
            if(stage>=ExpeditionStage.Consult&&stage<ExpeditionStage.SecondDig)text+="\n\n"+mod.T("exp.evidence."+story.Id+".1");
            if(stage>=ExpeditionStage.Meeting&&stage<=ExpeditionStage.Interpretation)text+="\n\n"+mod.T("exp.evidence."+story.Id+".2");
            if(stage>=ExpeditionStage.Reward)text+="\n\n"+mod.T("exp.reward."+story.Id);
            if(hints>0)text=mod.T("exp.hint."+story.Id+"."+(int)stage+"."+hints)+"\n\n"+text;
        }
        bool choices=!expeditionArchive&&stage is ExpeditionStage.FirstClue or ExpeditionStage.SecondClue or ExpeditionStage.Interpretation;
        string? action=expeditionArchive?null:stage switch{ExpeditionStage.Invitation or ExpeditionStage.Consult or ExpeditionStage.Finale=>"talk",ExpeditionStage.Meeting=>"meeting",ExpeditionStage.Reward=>"claim",_=>null};
        DrawPageText(b,mod.ExpeditionTitle(story.Id),text,right.Bottom-(choices?146:action!=null?62:16));
        if(choices)
            for(int i=0;i<3;i++)
            {
                string label=stage==ExpeditionStage.Interpretation?mod.T("exp.purpose."+story.Id+"."+i):mod.LocationName(story.Locations(stage==ExpeditionStage.FirstClue?1:2)[i]);
                Button(b,"exp:choice:"+i,label,new Rectangle(right.X+12,right.Bottom-132+i*44,right.Width-24,36));
            }
        if(action!=null)Button(b,"exp:action:"+action,mod.T("exp.action."+action),new Rectangle(right.X+12,right.Bottom-46,right.Width-24,38),mod.CanExpeditionAction(story.Id,action));
    }
    private void DrawExpeditionRestoration(SpriteBatch b,PuzzleExpedition story,ExpeditionRecord record)
    {
        int x=right.X+12,w=right.Width-24;Text(b,mod.T("exp.assembly"),x,right.Y+8,Ink,fontScale);
        Text(b,mod.T("exp.cycle"),x,right.Y+42,Muted,fontScale*.76f);
        int cardH=Math.Min(82,(right.Height-176)/2);
        for(int slot=0;slot<4;slot++)
        {
            var rect=new Rectangle(x+slot%2*(w/2+4),right.Y+72+slot/2*cardH,w/2-6,cardH-6);
            Button(b,"exp:slot:"+slot,"",rect);
            Text(b,mod.T("exp.slot."+slot),rect.X+8,rect.Y+4,Muted,fontScale*.65f);
            int piece=record.Assembly[slot];
            if(piece>=0)
            {
                int index=Array.IndexOf(PuzzleExpeditions.All,story)*4+piece;
                b.Draw(mod.ExpeditionAtlas,new Vector2(rect.X+8,rect.Y+29),new Rectangle(index*16,0,16,16),Color.White,0,Vector2.Zero,2,SpriteEffects.None,1);
                string name=mod.T("exp.piece."+story.Id+"."+piece);
                Text(b,name,rect.X+47,rect.Y+34,Ink,Math.Min(fontScale*.8f,(rect.Width-53)/Math.Max(1,Game1.smallFont.MeasureString(name).X)));
            }
        }
        int hint=record.Hints.GetValueOrDefault((int)record.Stage);
        string instruction=mod.T(hint==0?"exp.assembly-guide."+story.Id:"exp.hint."+story.Id+".8."+hint);
        Paragraph(b,instruction,new Rectangle(x,right.Y+76+cardH*2,w,Math.Max(0,right.Height-140-cardH*2)),Muted);
        Button(b,"exp:assemble",mod.T("exp.confirm"),new Rectangle(x,right.Bottom-46,w,38),record.Assembly.All(p=>p>=0));
    }
    private void HandleExpeditionClick(string id)
    {
        if(id.StartsWith("exp:select:")){selected=id[11..];scroll=0;expeditionArchive=false;return;}
        if(id=="exp:archive")expeditionArchive=!expeditionArchive;
        else if(id=="exp:hint")mod.RevealExpeditionHint(selected);
        else if(id.StartsWith("exp:action:"))mod.ExpeditionAction(selected,id[11..]);
        else if(id.StartsWith("exp:slot:"))mod.CycleExpeditionPiece(selected,int.Parse(id[9..]));
        else if(id=="exp:assemble")mod.ExpeditionChoice(selected,"assembly",0);
        else if(id.StartsWith("exp:choice:"))mod.ExpeditionChoice(selected,PuzzleExpeditions.Stage(mod.State.Expeditions,selected)==ExpeditionStage.Interpretation?"purpose":"location",int.Parse(id[11..]));
        scroll=0;
    }
}
