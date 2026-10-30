using System.Reflection;
using System.Text.Json;
using Archeo.Core;
using Archeo.Menus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Tools;

namespace Archeo.Smoke;

/// <summary>Runs only in an unsaved world with an isolated mod directory.</summary>
public sealed partial class SmokeEntry : Mod
{
    private bool started;
    private int ticks;
    private int captures;
    private readonly List<string> results = new();
    private string Out => Path.GetFullPath(Path.Combine(Helper.DirectoryPath,"..",".."));
    private const BindingFlags Instance = BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    public override void Entry(IModHelper helper) => helper.Events.GameLoop.UpdateTicked += Tick;
    private void Check(bool ok,string text) { if(!ok)throw new Exception("FAIL: "+text); results.Add("PASS: "+text); }
    private static object? Call(object obj,string name,params object[] args)=>obj.GetType().GetMethod(name,Instance)!.Invoke(obj,args);
    private static T Field<T>(object obj,string name)=>(T)obj.GetType().GetField(name,Instance)!.GetValue(obj)!;
    private static void Set(object obj,string name,object value)=>obj.GetType().GetField(name,Instance)!.SetValue(obj,value);
    private static void Move(string name) { Game1.currentLocation=Game1.getLocationFromName(name);Game1.player.currentLocation=Game1.currentLocation;Game1.activeClickableMenu=null;Game1.dialogueUp=false; }
    private static void Equip(string id)=>Game1.player.CurrentToolIndex=Game1.player.Items.ToList().FindIndex(p=>p?.ItemId==ModEntry.ItemId(id));
    private void Tick(object? sender,UpdateTickedEventArgs e)
    {
        if(started||++ticks<90)return; started=true;
        try
        {
            var mod=(ModEntry)typeof(ModEntry).Assembly.GetType("Archeo.DigPatches")!.GetField("Mod",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
            Call(mod,"OnCommand","archeo",new[]{"validate"});
            Check(true,"all registrations, furniture textures and hooks load");
            LocalizedContentManager.CurrentLanguageCode=LocalizedContentManager.LanguageCode.ru;
            Game1.game1.loadForNewGame(false);Game1.player.Name="Archeo QA";Game1.player.farmName.Value="Unsaved QA";
            Game1.hasLoadedGame=true;Game1.gameMode=3;Move("Forest");
            typeof(Context).GetProperty("IsWorldReady")!.SetValue(null,true);
            Call(mod,"LoadState");
            Check(!mod.State.Journey.Started && mod.State.Sites.Count==0,"new world starts with an invitation and no active digs");
            Capture(new JournalMenu(mod),"journey-invitation-ru.png");
            Check(!mod.ClaimFieldKit() && !mod.State.Journey.Started,"kit requires Gunther's museum");
            Move("ArchaeologyHouse");
            mod.Config.FragmentMultiplier=.25f;mod.Config.FindsPerSite=2;
            var original=Game1.player.Items.ToArray();
            for(int i=0;i<Game1.player.Items.Count;i++)if(Game1.player.Items[i]==null)Game1.player.Items[i]=ItemRegistry.Create("(O)390",999);
            Check(!mod.ClaimFieldKit() && !mod.State.Journey.Started,"full backpack preserves the unclaimed kit");
            for(int i=0;i<original.Length;i++)Game1.player.Items[i]=original[i];
            Check(mod.ClaimFieldKit(),"Gunther provides the real kit");Game1.activeClickableMenu=null;
            Check(mod.State.Sites.Count==1 && mod.State.Sites[0].Culture=="valley","kit opens only the valley field");
            Check(mod.State.Opening.Stage==OpeningStage.Searching && mod.State.Sites[0].Key==OpeningStory.SiteKey && mod.State.Sites[0].Expires==int.MaxValue,"first story starts with one persistent introductory site");
            Check(mod.FieldObjectives().Count==1 && mod.State.TrackedObjective=="dig:"+OpeningStory.SiteKey,"the kit pins exactly one first step");
            Check(FieldLearning.Current(mod.State,mod.Research.Artifacts)=="search","new kit starts with the instrument lesson");
            CaptureLearning(mod,"kit");
            var early=new JournalMenu(mod,"catalog");Capture(early,"journey-first-project.png");
            Check(ButtonsOf(early).Keys.Count(p=>p.StartsWith("artifact:"))==1,"early catalog shows one project and no future silhouettes");
            Check(Field<string>(new JournalMenu(mod,"shipwreck"),"tab")=="overview","direct page request cannot expose a future story");
            var dig=mod.State.Sites[0];Move(dig.Location);var point=dig.Points[0];
            var adjacent=new[]{new Vector2(0,1),new Vector2(1,0),new Vector2(0,-1),new Vector2(-1,0)};
            Game1.player.setTileLocation(adjacent.Select(v=>v+new Vector2(point.X,point.Y)).First(v=>(bool)Call(mod,"WalkableForSearch",Game1.currentLocation,v)!));
            Game1.player.CurrentToolIndex=0;mod.Survey();
            var signals=Field<StardewModdingAPI.Utilities.PerScreen<SurveySignal>>(mod,"signals");
            Check(signals.Value.Until==0,"search requires the instrument in the player's hand");
            Equip("surveyor");mod.Survey();
            point=dig.Points.Single(p=>p.X==signals.Value.Target.X&&p.Y==signals.Value.Target.Y);
            Check(point.Revealed && signals.Value.Revealed,"nearby instrument scan permanently marks the real tile");
            Check(FieldLearning.Current(mod.State,mod.Research.Artifacts)=="dig","real survey advances the lesson to using the hoe");
            CaptureLearning(mod,"marked");
            signals.Value.Until=0;
            var hoe=Game1.player.Items.OfType<Hoe>().FirstOrDefault()??new Hoe();
            if(!Game1.player.Items.Contains(hoe))Game1.player.addItemToInventoryBool(hoe);
            Game1.player.CurrentToolIndex=Game1.player.Items.ToList().IndexOf(hoe);
            Game1.player.toolPower.Value=0;int before=mod.State.Finds;
            hoe.DoFunction(Game1.currentLocation,point.X*64+32,point.Y*64+32,1,Game1.player);
            Check(point.Dug&&mod.State.Finds==before+1,"actual hoe excavates a marked tile after the arrow expires");
            Check(mod.State.Cultures["valley"].Fragments>=5 && dig.Points.Count==4 && mod.State.Cultures["valley"].Keystones==0,"introductory excavation remains sufficient and simple at minimum fragment and site settings");
            Check(FieldLearning.Available(mod.State,mod.Research.Artifacts).Contains("fragments"),"real excavation opens the fragment explanation");
            CaptureLearning(mod,"fragments");
            mod.OnHoeDig(Game1.currentLocation,point.X,point.Y,Game1.player);
            Check(mod.State.Finds==before+1,"a find cannot award twice");
            var decoy=new DigSite{Culture="valley",Location=dig.Location,Expires=ModEntry.Today+3,EventKind="rain",EventId="qa:rain",Points=new(){new(){X=4,Y=4}}};
            mod.State.Sites.Add(decoy);mod.SaveState();
            Check(ReferenceEquals(Call(mod,"HudDigSite"),dig),"HUD shows the excavated site instead of an unrelated event");
            Check(Field<StardewModdingAPI.Utilities.PerScreen<List<DigFeedback>>>(mod,"digFeedback").Value.Count>0,"successful dig produces animated fragment feedback");
            CaptureField(mod,point);mod.State.Sites.Remove(decoy);
            var location=Game1.currentLocation;Game1.player.FacingDirection=2;
            var affected=typeof(Tool).GetMethod("tilesAffected",Instance)!;
            List<Vector2> Area(Vector2 at)=>(List<Vector2>)affected.Invoke(hoe,new object[]{at,1,Game1.player})!;
            var land=(List<Vector2>)Call(mod,"ReachableTiles",location)!;
            var anchor=land.First(at=>(bool)Call(mod,"WalkableForSearch",location,at-new Vector2(0,1))! && Area(at).All(v=>(bool)Call(mod,"ValidDigTile",location,v)! && !(bool)Call(mod,"OccupiedByDigPoint",location.Name,v,null!)!));
            var marked=Area(anchor).Last();
            var charged=new DigSite{Culture="valley",Location=location.Name,Expires=ModEntry.Today+7,EventId="qa:charged",Points=new(){new(){X=(int)marked.X,Y=(int)marked.Y,Revealed=true}}};
            mod.State.Sites.Add(charged);Game1.player.setTileLocation(anchor-new Vector2(0,1));
            hoe.UpgradeLevel=1;Game1.player.toolPower.Value=1;before=mod.State.Finds;
            hoe.DoFunction(location,(int)anchor.X*64+32,(int)anchor.Y*64+32,1,Game1.player);
            Check(marked!=anchor&&charged.Points[0].Dug&&mod.State.Finds==before+1,"charged hoe excavates marked tiles beyond its original cursor");
            Game1.player.toolPower.Value=0;
            Move("Forest");location=Game1.currentLocation;
            Game1.player.setTileLocation(new Vector2(51,18));
            var water=(from x in Enumerable.Range(2,location.Map.Layers[0].LayerWidth-4) from y in Enumerable.Range(2,location.Map.Layers[0].LayerHeight-4) where location.isWaterTile(x,y) select new Vector2(x,y)).First();
            var invalid=new DigSite{Culture="valley",Location=location.Name,Expires=ModEntry.Today+8,EventId="qa:water",Points=new(){new(){X=(int)water.X,Y=(int)water.Y}}};
            mod.State.Sites.Add(invalid);Call(mod,"FindSurveyTarget",location);
            Check(!location.isWaterTile(invalid.Points[0].X,invalid.Points[0].Y),"legacy water target is relocated to reachable dry land");
            var routes=(FieldRoute)Call(mod,"RoutesFromPlayer",location)!;
            Check(mod.State.Sites.Where(s=>s.Location==location.Name).SelectMany(s=>s.Points).Where(p=>!p.Dug).All(p=>routes.Contains(new(p.X,p.Y))),"offered local points belong to the player's reachable land component");
            mod.State.Sites.Remove(invalid);mod.State.Sites.Remove(charged);
            CompleteOpeningDig(mod);
            var progress=mod.Research.Progress(mod.State,"valley");
            mod.Restore("valley",false);
            Check(!Journey.IsOpen(mod.State,"dwarf") && mod.State.Opening.Stage==OpeningStage.Restored && mod.State.TrackedObjective=="opening:talk","first restoration leads to Gunther before unlocking another field");
            CheckOpeningStory(mod);
            if(File.Exists(Path.Combine(Helper.DirectoryPath,"opening-only.flag")))
            {
                File.WriteAllLines(Path.Combine(Out,"smoke-results.txt"),results);Game1.game1.Exit();return;
            }
            Check(Journey.IsOpen(mod.State,"dwarf")&&!Journey.IsOpen(mod.State,"sea"),"the concluding conversation opens one explained field");
            mod.Research.Progress(mod.State,"valley").Fragments=200;
            mod.Restore("valley",false);
            Check(!Journey.IsOpen(mod.State,"sea"),"restoration count alone no longer opens the coast");
            CheckShoreRoute(mod);
            mod.State.Sites.First(s=>s.Points.Any(p=>!p.Dug)).Points.First(p=>!p.Dug).Revealed=true;mod.SaveState();
            string saved=Game1.player.modData[ModEntry.Id+"/Progress"];Call(mod,"LoadState");
            Check(JsonSerializer.Serialize(mod.State)==saved&&mod.State.Sites.SelectMany(s=>s.Points).Any(p=>p.Revealed&&!p.Dug),"reload preserves progress and undug permanent markers");
            mod.State.Experience=1280;Game1.player.MaxItems=36;
            while(Game1.player.Items.Count<36)Game1.player.Items.Add(null);
            Game1.player.addItemToInventoryBool(ItemRegistry.Create("(O)334",5));Game1.player.addItemToInventoryBool(ItemRegistry.Create("(O)335",5));Game1.player.Money=2000;
            Move("ArchaeologyHouse");mod.UpgradeInstrument();
            Check(mod.State.Journey.InstrumentLevel==2&&Game1.player.Money==1750&&mod.MaterialCount("334")==0,"first instrument improvement consumes its shown costs");
            mod.UpgradeInstrument();
            Check(mod.State.Journey.InstrumentLevel==3&&Game1.player.Money==750&&!mod.CanUpgradeInstrument,"second improvement reaches the cap");
            Move("Forest");CheckFrontiers(mod);CheckMuseum(mod);CheckExpeditions(mod);RenderPages(mod);CheckLearningLayouts(mod);
            Check(true,"Russian and English book pages rendered at 1280x720 and 900x640");
        }
        catch(Exception ex){results.Add(ex.ToString());Monitor.Log(ex.ToString(),LogLevel.Error);}
        File.WriteAllLines(Path.Combine(Out,"smoke-results.txt"),results);Game1.game1.Exit();
    }
    private void CheckFrontiers(ModEntry mod)
    {
        Check(mod.Research.Artifacts.Count==43 && mod.Atlas.Height==96,"1.2 registers all 43 finds and the expanded native atlas");
        Check(!Journey.IsOpen(mod.State,"frontiers") && !Journey.IsOpen(mod.State,"calico") && !Journey.IsOpen(mod.State,"island")
            && mod.State.Sites.All(s=>!FrontierResearch.Cultures.Contains(s.Culture)),"loading all maps never unlocks unopened bus and boat routes");
        Capture(new JournalMenu(mod,"frontiers"),"frontier-locked-ru.png");
        DigNormalRouteField(mod,"sea");mod.Restore("sea",false);
        Check(mod.State.Restored.ContainsKey("clay_whistle") && RouteStories.Pending(mod.State)==null,"coastal find does not invent a route before transport is available");
        // Favor the locked cultures by solve count: the star forecast must still ignore them.
        foreach(string culture in new[]{"valley","dwarf","sea"})mod.Research.Progress(mod.State,culture).Solves=8;
        Game1.timeOfDay=2100;
        Check((bool)Call(mod,"UsePocketObservatory")! && !FrontierResearch.Cultures.Contains(mod.State.Stars.Lead!.Culture),"star forecasts never reveal a locked frontier region");
        mod.State.Stars.Lead=null;Game1.timeOfDay=1200;
        Game1.MasterPlayer.mailReceived.Add("ccVault");
        mod.SaveState();Call(mod,"LoadState");
        Check(RouteStories.Pending(mod.State)?.Id=="calico" && !Journey.IsOpen(mod.State,"calico") && mod.State.Sites.All(s=>s.Culture!="calico"),"working bus and real whistle lead to Gunther before any desert site is created");
        CheckDistantRoute(mod,"calico");
        Check(Journey.IsOpen(mod.State,"calico") && !Journey.IsOpen(mod.State,"island") && mod.State.Sites.Any(s=>s.Culture=="calico"&&s.Location=="Desert"),"bus access creates a desert site without unlocking the island");
        Check(mod.State.LastSiteDay==ModEntry.Today,"same-day upgrade creates the newly unlocked site without waiting overnight");
        Game1.MasterPlayer.mailReceived.Add("willyBoatFixed");
        // Prepare the named second coastal artifact only in this unsaved test world.
        var shore=mod.Research.Progress(mod.State,"sea");shore.Project="sailor_buckle";shore.Fragments=24;mod.Restore("sea",false);
        Call(mod,"RefreshFrontiers");mod.SaveState();
        Check(RouteStories.Pending(mod.State)?.Id=="island" && !Journey.IsOpen(mod.State,"island"),"actual boat access and restored buckle record an island clue before fieldwork");
        CheckDistantRoute(mod,"island");
        Check(mod.State.Sites.Any(s=>s.Culture=="island"&&new[]{"IslandSouth","IslandEast"}.Contains(s.Location)),"island site uses an accessible first-visit map");
        var hoe=Game1.player.Items.OfType<Hoe>().First();
        foreach(string culture in FrontierResearch.Cultures)
        {
            var site=mod.State.Sites.Single(s=>s.Culture==culture&&s.EventKind=="");Move(site.Location);
            Check(site.Points.All(p=>(bool)Call(mod,"ValidDigTile",Game1.currentLocation,new Vector2(p.X,p.Y))!),"all new site points are dry, free and diggable: "+culture);
            var point=site.Points[0];var at=new Vector2(point.X,point.Y);
            Game1.player.setTileLocation(new[]{new Vector2(0,1),new Vector2(1,0),new Vector2(0,-1),new Vector2(-1,0)}.Select(v=>at+v).First(v=>(bool)Call(mod,"WalkableForSearch",Game1.currentLocation,v)!));
            Call(mod,"FindSurveyTarget",Game1.currentLocation);
            var route=(FieldRoute)Call(mod,"RoutesFromPlayer",Game1.currentLocation)!;
            Check(site.Points.All(p=>route.Contains(new(p.X,p.Y))),"survey repairs new region points into the player's land component: "+culture);
            Equip("surveyor");Field<StardewModdingAPI.Utilities.PerScreen<double>>(mod,"nextSurvey").Value=0;mod.Survey();
            var signal=Field<StardewModdingAPI.Utilities.PerScreen<SurveySignal>>(mod,"signals").Value;
            point=site.Points.Single(p=>p.X==signal.Target.X&&p.Y==signal.Target.Y);
            Check(point.Revealed,"instrument marks a real frontier tile: "+culture);
            var before=Research.Cultures.ToDictionary(c=>c,c=>mod.Research.Progress(mod.State,c).Fragments);
            Game1.player.CurrentToolIndex=Game1.player.Items.ToList().IndexOf(hoe);Game1.player.toolPower.Value=0;
            hoe.DoFunction(Game1.currentLocation,point.X*64+32,point.Y*64+32,1,Game1.player);
            Check(point.Dug && mod.Research.Progress(mod.State,culture).Fragments>before[culture],"real hoe awards the frontier culture's own fragments: "+culture);
            Check(Research.Cultures.Where(c=>c!=culture).All(c=>mod.Research.Progress(mod.State,c).Fragments==before[c]),"frontier excavation leaves the other fragment balances intact: "+culture);
            mod.Research.Progress(mod.State,culture).Fragments=200;mod.Restore(culture,false);
            string first=culture=="calico"?"caravan_token":"island_spindle";
            Check(mod.State.Restored.ContainsKey(first)&&Game1.player.Items.Any(p=>p?.ItemId==ModEntry.ItemId(first)),"first frontier restoration gives a registered physical artifact: "+culture);
        }
        // Fresh relics are injected only into this unsaved QA world.
        foreach(var relic in mod.Research.Artifacts.Where(a=>FrontierResearch.RelicEffects.Contains(a.Effect)))
        {Check(Game1.player.addItemToInventoryBool(ItemRegistry.Create(ModEntry.QualifiedId(relic))),"test backpack contains the physical relic: "+relic.Id);mod.State.Restored[relic.Id]=1;}
        ArtifactDefinition Relic(string effect)=>mod.Research.Artifacts.Single(a=>a.Effect==effect);
        var sun=Relic("sunstep");Move("ArchaeologyHouse");Game1.timeOfDay=1200;mod.UseRelic(sun);
        Check(!mod.State.LastUse.ContainsKey(sun.Id),"indoor sun dial use preserves its daily charge");
        Move("Desert");Game1.timeOfDay=1900;mod.UseRelic(sun);Check(!mod.State.LastUse.ContainsKey(sun.Id),"nighttime sun dial use preserves its daily charge");
        Game1.timeOfDay=1200;mod.UseRelic(sun);
        var sunBuff=Game1.player.buffs.AppliedBuffs[ModEntry.Id+"/Sunstep"];
        Check(sunBuff.effects.Speed.Value==1&&sunBuff.millisecondsDuration==90000&&mod.State.LastUse[sun.Id]==ModEntry.Today,"sun dial grants its real 90-second movement buff");
        mod.UseRelic(sun);Check(Game1.player.buffs.AppliedBuffs.Count(p=>p.Key==ModEntry.Id+"/Sunstep")==1,"sun dial cannot stack with repeated same-day use");
        var oasis=Relic("oasis");Game1.player.Stamina=Game1.player.MaxStamina;Game1.player.health=Game1.player.maxHealth;mod.UseRelic(oasis);
        Check(!mod.State.LastUse.ContainsKey(oasis.Id),"full health and energy preserve the oasis charge");
        Game1.player.Stamina=Game1.player.MaxStamina-10;Game1.player.health=Game1.player.maxHealth-7;mod.UseRelic(oasis);
        Check(Game1.player.Stamina==Game1.player.MaxStamina&&Game1.player.health==Game1.player.maxHealth&&mod.State.LastUse.ContainsKey(oasis.Id),"oasis recovery clamps both resources to their real maxima");
        Game1.player.Stamina-=20;mod.UseRelic(oasis);Check(Game1.player.Stamina==Game1.player.MaxStamina-20,"oasis cannot restore twice in one day");
        var shell=Relic("shellguard");mod.UseRelic(shell);
        var shellBuff=Game1.player.buffs.AppliedBuffs[ModEntry.Id+"/Shellguard"];
        Check(shellBuff.effects.Defense.Value==3&&shellBuff.effects.MagneticRadius.Value==64&&shellBuff.millisecondsDuration==90000,"turtle charm applies real defense and pickup-range buffs");
        var conch=Relic("tidecall");Move("ArchaeologyHouse");mod.UseRelic(conch);
        Check(!mod.State.LastUse.ContainsKey(conch.Id),"conch preserves its charge without an available find");
        var island=mod.State.Sites.Single(s=>s.Culture=="island"&&s.EventKind=="");Move(island.Location);
        var remaining=island.Points.First(p=>!p.Dug);Game1.player.setTileLocation(new(remaining.X,remaining.Y));
        foreach(var p in island.Points.Where(p=>!p.Dug))p.Revealed=false;
        mod.UseRelic(conch);
        Check(island.Points.Count(p=>!p.Dug&&p.Revealed)==2&&mod.State.LastUse.ContainsKey(conch.Id),"conch permanently reveals exactly two accessible undug finds");
        mod.UseRelic(conch);Check(island.Points.Count(p=>!p.Dug&&p.Revealed)==2,"same-day conch reuse cannot reveal extra finds");
        mod.SaveState();string saved=Game1.player.modData[ModEntry.Id+"/Progress"];Call(mod,"LoadState");
        Check(JsonSerializer.Serialize(mod.State)==saved&&mod.State.Frontiers.DesertAccess&&mod.State.Frontiers.IslandAccess,"all new route unlocks, relic charges and permanent marks reload exactly");
        foreach(string culture in FrontierResearch.Cultures)
        {
            var set=MuseumCollections.All.Single(c=>c.Id==culture);
            foreach(string id in set.Artifacts)mod.State.Restored[id]=1;
            var inventory=Game1.player.Items.ToArray();
            for(int i=0;i<Game1.player.Items.Count;i++)if(Game1.player.Items[i]==null)Game1.player.Items[i]=ItemRegistry.Create("(O)390",999);
            mod.ClaimCollection(culture);Check(!mod.State.CollectionRewards.Contains(culture),"full backpack preserves new collection furniture: "+culture);
            for(int i=0;i<inventory.Length;i++)Game1.player.Items[i]=inventory[i];
            mod.ClaimCollection(culture);
            Check(mod.State.CollectionRewards.Contains(culture)&&Game1.player.Items.Count(p=>p?.ItemId==ModEntry.ItemId(set.Reward))==1,"new collection grants registered furniture: "+culture);
            mod.ClaimCollection(culture);Check(Game1.player.Items.Count(p=>p?.ItemId==ModEntry.ItemId(set.Reward))==1,"new collection furniture cannot be duplicated: "+culture);
        }
        Journey.Refresh(mod.State,mod.Research.Artifacts);mod.SaveState();
        var originalViewport=Game1.uiViewport;
        foreach(var language in new[]{LocalizedContentManager.LanguageCode.ru,LocalizedContentManager.LanguageCode.en})
        foreach(var size in new[]{new Point(1280,720),new Point(900,640)})
        {
            LocalizedContentManager.CurrentLanguageCode=language;Game1.uiViewport=new xTile.Dimensions.Rectangle(0,0,size.X,size.Y);
            foreach(string page in new[]{"frontiers","calico","island","gallery"})
            {
                var book=new JournalMenu(mod,page);Capture(book,$"frontier-{page}-{language}-{size.X}.png",size.X,size.Y);
                if(FrontierResearch.Cultures.Contains(page))
                    Check((string)typeof(JournalMenu).GetMethod("TabSection",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,new object[]{page})! == "catalog","frontier research retains the finds navigation section");
                if(page=="frontiers")
                {
                    Click(book,"frontier:select:island");Capture(book,$"frontier-route-island-{language}-{size.X}.png",size.X,size.Y);Click(book,"frontier:open");
                    Check(Field<string>(book,"tab")=="island","region page opens the selected research field");
                }
                if(page=="gallery")
                {
                    if(ButtonsOf(book).ContainsKey("gallery:next")) {Click(book,"gallery:next");Capture(book,$"frontier-gallery-next-{language}-{size.X}.png",size.X,size.Y);}
                    Check(ButtonsOf(book).Keys.Any(k=>k=="gallery:select:calico"||k=="gallery:select:island"),"pagination reaches frontier collections on a compact book");
                }
            }
        }
        Game1.uiViewport=originalViewport;LocalizedContentManager.CurrentLanguageCode=LocalizedContentManager.LanguageCode.ru;Move("Forest");
    }
    private void RenderPages(ModEntry mod)
    {
        var viewport=Game1.uiViewport;
        foreach(var language in new[]{LocalizedContentManager.LanguageCode.ru,LocalizedContentManager.LanguageCode.en})
        {
            LocalizedContentManager.CurrentLanguageCode=language;
            foreach(var size in new[]{new Point(1280,720),new Point(900,640)})
            {
                Game1.uiViewport=new xTile.Dimensions.Rectangle(0,0,size.X,size.Y);
                foreach(string page in new[]{"overview","catalog","chapters","equipment","settings"})
                {
                    var menu=new JournalMenu(mod,page);Capture(menu,$"journey-{page}-{language}-{size.X}.png",size.X,size.Y);
                    Check(ButtonsOf(menu).Count(p=>p.Key.StartsWith("tab:"))==5,"navigation keeps five main sections");
                }
            }
        }
        Game1.uiViewport=viewport;LocalizedContentManager.CurrentLanguageCode=LocalizedContentManager.LanguageCode.ru;
        var catalog=new JournalMenu(mod,"catalog");Capture(catalog,"journey-search-before.png");
        Field<StardewValley.Menus.TextBox>(catalog,"searchBox").Text="неизвестная находка";
        Capture(catalog,"journey-search-empty.png");
        Check(ButtonsOf(catalog).Keys.All(p=>!p.StartsWith("artifact:")&&p!="restore"),"empty search leaves no artifact actions or spoilers");
        var settings=new JournalMenu(mod,"settings");Capture(settings,"journey-settings-before.png");
        bool hud=mod.Config.ShowHud;Click(settings,"settings:plus");
        Check(mod.Config.ShowHud!=hud,"in-game settings apply");
        Set(settings,"bindingSetting","journal-key");settings.receiveKeyPress(Keys.J);
        Check(mod.Config.JournalKey.ToString().Contains("J"),"journal keyboard binding persists");
        mod.Config.ResetDefaults();mod.SaveSettings();settings.receiveGamePadButton(Buttons.LeftShoulder);
        Capture(settings,"journey-controller.png");
        Check(Field<string>(settings,"tab")=="equipment","controller shoulders navigate the new main sections");
    }
    private static Dictionary<string,Rectangle> ButtonsOf(JournalMenu menu)=>Field<Dictionary<string,Rectangle>>(menu,"buttons");
    private static void Click(JournalMenu menu,string id){var rect=ButtonsOf(menu)[id];menu.receiveLeftClick(rect.Center.X,rect.Center.Y);}
    private void Capture(StardewValley.Menus.IClickableMenu menu,string name,int width=0,int height=0)
    {
        width=width==0?Game1.uiViewport.Width:width;height=height==0?Game1.uiViewport.Height:height;
        var device=Game1.graphics.GraphicsDevice;using var target=new RenderTarget2D(device,width,height);
        device.SetRenderTarget(target);device.Clear(new Color(36,44,39));
        Game1.spriteBatch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.PointClamp);
        menu.draw(Game1.spriteBatch);Game1.spriteBatch.End();device.SetRenderTarget(null);
        // Functional runs still draw every page, but can avoid the native PNG encoder's memory growth.
        if (!File.Exists(Path.Combine(Helper.DirectoryPath,"no-screenshots.flag")))
        {
            using var file=File.Create(Path.Combine(Out,name));target.SaveAsPng(file,width,height);
        }
        target.Dispose();
        // The whole visual suite runs in one update: release large PNG buffers between batches.
        if (++captures % 4 == 0) { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }
    }
    private void CaptureField(ModEntry mod,DigPoint point)
    {
        var effects=Field<StardewModdingAPI.Utilities.PerScreen<List<DigFeedback>>>(mod,"digFeedback").Value;
        var effect=effects[0];effects[0]=effect with { Started=effect.Started-.35 };
        var oldViewport=Game1.viewport;Game1.viewport=new xTile.Dimensions.Rectangle(point.X*64-450,point.Y*64-320,900,640);
        var device=Game1.graphics.GraphicsDevice;using var target=new RenderTarget2D(device,900,640);
        device.SetRenderTarget(target);device.Clear(new Color(62,80,49));
        Game1.spriteBatch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.PointClamp);
        Call(mod,"DrawFieldFeedback",Game1.spriteBatch);
        Call(mod,"DrawRevealedPoints",Game1.spriteBatch,Game1.currentGameTime.TotalGameTime.TotalSeconds);
        Game1.spriteBatch.End();device.SetRenderTarget(null);
        if (!File.Exists(Path.Combine(Helper.DirectoryPath,"no-screenshots.flag")))
        {
            using var file=File.Create(Path.Combine(Out,"journey-dig-feedback.png"));target.SaveAsPng(file,900,640);
        }
        Game1.viewport=oldViewport;effects[0]=effect;
    }
}
