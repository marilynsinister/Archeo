using System.Text.Json;
using Archeo.Core;
using Archeo.Menus;
using Microsoft.Xna.Framework;
using StardewValley;

namespace Archeo.Smoke;

public sealed partial class SmokeEntry
{
    private void CheckOpeningSummary(ModEntry mod,JournalMenu menu,string name)
    {
        var left=Field<Rectangle>(menu,"left");
        var lines=(IEnumerable<string>)Call(menu,"Wrap",mod.T("opening.summary."+(int)mod.State.Opening.Stage),left.Width-36,1f)!;
        Check(lines.Count()*(Game1.smallFont.LineSpacing+4)<=left.Height-198,"first-story summary fits completely: "+name);
    }

    private void CaptureOpeningLayouts(ModEntry mod,string stage)
    {
        var viewport=Game1.uiViewport;
        var language=LocalizedContentManager.CurrentLanguageCode;
        float originalScale=mod.Config.JournalTextScale;
        foreach(var code in new[]{LocalizedContentManager.LanguageCode.ru,LocalizedContentManager.LanguageCode.en})
        foreach(var size in new[]{new Point(900,640),new Point(1280,720)})
        foreach(float scale in new[]{1f,1.3f})
        {
            LocalizedContentManager.CurrentLanguageCode=code;
            Game1.uiViewport=new xTile.Dimensions.Rectangle(0,0,size.X,size.Y);
            mod.Config.JournalTextScale=scale;
            var menu=new JournalMenu(mod,"opening");
            string name=$"opening-{stage}-{code}-{size.X}-{scale:0.0}";
            Capture(menu,name+".png",size.X,size.Y);
            CheckOpeningSummary(mod,menu,name);
            int max=Field<int>(menu,"maxScroll");
            for(int i=0;i<max+1;i++)menu.receiveScrollWheelAction(-1);
            Check(Field<int>(menu,"scroll")==max,"opening story scrolls completely: "+name);
            Capture(menu,name+"-bottom.png",size.X,size.Y);
        }
        Game1.uiViewport=viewport;
        LocalizedContentManager.CurrentLanguageCode=language;
        mod.Config.JournalTextScale=originalScale;
    }

    private void CompleteOpeningDig(ModEntry mod)
    {
        mod.SaveState();
        string saved=JsonSerializer.Serialize(mod.State);
        Call(mod,"LoadState");
        Check(JsonSerializer.Serialize(mod.State)==saved && mod.State.Opening.Stage==OpeningStage.Searching,"partly excavated first site and its fragments survive a real reload");
        var site=mod.State.Sites.Single(s=>s.Key==OpeningStory.SiteKey);
        var hoe=Game1.player.Items.OfType<StardewValley.Tools.Hoe>().First();
        while(site.Points.Any(p=>!p.Dug))
        {
            Move(site.Location);
            var point=site.Points.First(p=>!p.Dug); var at=new Vector2(point.X,point.Y);
            Game1.player.setTileLocation(new[]{new Vector2(0,1),new Vector2(1,0),new Vector2(0,-1),new Vector2(-1,0)}.Select(v=>at+v).First(v=>(bool)Call(mod,"WalkableForSearch",Game1.currentLocation,v)!));
            Equip("surveyor");Field<StardewModdingAPI.Utilities.PerScreen<double>>(mod,"nextSurvey").Value=0;mod.Survey();
            var signal=Field<StardewModdingAPI.Utilities.PerScreen<SurveySignal>>(mod,"signals").Value;
            Check(signal.Revealed && signal.SiteKey==OpeningStory.SiteKey,"real survey continues the first marked site");
            int remaining=site.Points.Count(p=>!p.Dug);
            Game1.player.CurrentToolIndex=Game1.player.Items.ToList().IndexOf(hoe);Game1.player.toolPower.Value=0;
            hoe.DoFunction(Game1.currentLocation,(int)signal.Target.X*64+32,(int)signal.Target.Y*64+32,1,Game1.player);
            Check(site.Points.Count(p=>!p.Dug)==remaining-1,"real hoe continues the first cup's excavation");
        }
        Check(mod.State.Cultures["valley"].Fragments>=20 && mod.State.Cultures["valley"].Keystones==0 && mod.FieldObjectives().Single().Id=="opening:restore","one real beginner site funds the cup without introducing ancient parts");
        mod.Config.FragmentMultiplier=1;mod.Config.FindsPerSite=4;
    }

    private void CheckOpeningStory(ModEntry mod)
    {
        Check(FieldLearning.Current(mod.State,mod.Research.Artifacts)=="homecoming","restored cup offers the return-to-Gunther lesson");
        Check(mod.FieldObjectives().Count==1 && mod.FieldObjectives()[0].Id=="opening:talk","restored cup presents only its concluding conversation");
        Check(!mod.ConcludeOpeningStory(),"conversation cannot conclude outside the museum");
        string before=JsonSerializer.Serialize(mod.State);
        Call(mod,"LoadState");
        Check(JsonSerializer.Serialize(mod.State)==before && mod.State.Opening.Stage==OpeningStage.Restored,"pending first-story conclusion survives a real mod reload");
        var viewport=Game1.uiViewport;
        var language=LocalizedContentManager.CurrentLanguageCode;
        foreach(var code in new[]{LocalizedContentManager.LanguageCode.ru,LocalizedContentManager.LanguageCode.en})
        foreach(var size in new[]{new Point(900,640),new Point(1280,720)})
        {
            LocalizedContentManager.CurrentLanguageCode=code;
            Game1.uiViewport=new xTile.Dimensions.Rectangle(0,0,size.X,size.Y);
            mod.Config.JournalTextScale=1.3f;
            var menu=new JournalMenu(mod,"opening");
            Capture(menu,$"opening-return-{code}-{size.X}.png",size.X,size.Y);
            CheckOpeningSummary(mod,menu,"return/"+code+"/"+size.X);
            Check(Field<string>(menu,"tab")=="opening" && ButtonsOf(menu).ContainsKey("opening:talk"),"first story has a visible contextual conversation button: "+code+"/"+size.X);
            int max=Field<int>(menu,"maxScroll");
            for(int i=0;i<max+1;i++)menu.receiveScrollWheelAction(-1);
            Check(Field<int>(menu,"scroll")==max,"first story's enlarged text reaches the final line: "+code+"/"+size.X);
            Capture(menu,$"opening-return-bottom-{code}-{size.X}.png",size.X,size.Y);
        }
        mod.Config.JournalTextScale=1;
        Game1.uiViewport=viewport;
        LocalizedContentManager.CurrentLanguageCode=language;
        var cup=Game1.player.Items.First(p=>p?.ItemId==ModEntry.ItemId(OpeningStory.Artifact))!;
        Game1.player.removeItemFromInventory(cup);
        int experience=mod.State.Experience, fragments=mod.State.Cultures["valley"].Fragments;
        Move("ArchaeologyHouse");
        var finish=new JournalMenu(mod,"opening"); Capture(finish,"opening-museum.png");
        Click(finish,"opening:talk");
        Check(mod.State.Opening.Stage==OpeningStage.Complete && Journey.IsOpen(mod.State,"dwarf"),"the actual journal button concludes the first story and opens the explained mountain clue");
        Check(mod.State.Experience==experience && mod.State.Cultures["valley"].Fragments==fragments && mod.State.Restored[OpeningStory.Artifact]==1 && !mod.State.Donated.Contains(OpeningStory.Artifact),"notes suffice without the physical cup, extra rewards or forced donation");
        Check(!mod.ConcludeOpeningStory(),"repeating the completion action cannot advance or reward twice");
        Game1.activeClickableMenu=null; Game1.dialogueUp=false;
        Check(Game1.player.addItemToInventoryBool(cup),"test cup returns to the backpack unchanged");
        var archive=new JournalMenu(mod,"opening"); Capture(archive,"opening-complete.png");
        before=JsonSerializer.Serialize(mod.State);
        Click(archive,"opening:replay");
        Check(JsonSerializer.Serialize(mod.State)==before,"rereading the first conversation has no progression side effects");
        Game1.activeClickableMenu=null; Game1.dialogueUp=false;
        Call(mod,"LoadState");
        Check(mod.State.Opening.Stage==OpeningStage.Complete && Journey.IsOpen(mod.State,"opening"),"the completed first story stays archived after real reload");
        CaptureOpeningLayouts(mod,"complete");
        Move("Forest");
    }
}
