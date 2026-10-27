using System.Text.Json;
using System.Text.RegularExpressions;
using Archeo.Core;

string root = Path.GetFullPath(args.FirstOrDefault() ?? ".");
var catalog = JsonSerializer.Deserialize<List<ArtifactDefinition>>(File.ReadAllText(Path.Combine(root, "assets/artifacts.json")))!;
var research = new Research(catalog);
int checks = 0;
void Check(bool condition, string description) { checks++; if (!condition) throw new Exception("FAIL: " + description); }

Check(catalog.Count == 33 && catalog.Count(p => p.Source == "research") == 25, "25 research artifacts and 8 event or story relics");
Check(catalog.Select(p => p.Sprite).Distinct().Count() == 33 && catalog.All(p => p.Sprite is not (21 or 22 or 23)), "unique sprites preserve original tool slots");
foreach (string culture in Research.Cultures)
{
    var state = new ArchaeologyState();
    var project = research.Project(state, culture, new Random(10));
    Check(project.Culture == culture && project.Rarity == 0 && project.Cost == 20, "accessible first project: " + culture);
    Check(research.Project(state, culture, new Random(99)).Id == project.Id, "project does not reroll when opening journal");
    var progress = research.Progress(state, culture);
    progress.Fragments = 7; progress.Keystones = 1;
    Check(!research.Restore(state, culture, true, new Random(1), out _), "insufficient fragments reject restore");
    Check(progress.Fragments == 7 && progress.Keystones == 1 && state.Restored.Count == 0, "failed restoration is atomic");
    progress.Fragments = 8;
    Check(research.Restore(state, culture, true, new Random(1), out _), "keystone replaces 12 fragments");
    Check(progress.Fragments == 0 && progress.Keystones == 0 && progress.Solves == 1, "correct currency consumption");
    progress.Project = ""; progress.Solves = 6; progress.SinceRare = 5;
    project = research.Project(state, culture, new Random(22));
    Check(project.Rarity >= 2 && project.Rarity < 4 && !state.Restored.ContainsKey(project.Id), "rare pity picks an unseen, unlocked artifact");
    Check(research.Project(state, culture, new Random(4)).Id == project.Id, "rare persists across reopen");
    progress.Fragments = 999;
    Check(research.Restore(state, culture, false, new Random(1), out _), "rare restoration");
    Check(progress.SinceRare == 0, "rare resets pity counter");

    // Every artifact is reachable and rare rewards never repeat; simulate a long expedition.
    for (int i = 0; i < 200; i++)
    {
        progress.Fragments += 200;
        research.Restore(state, culture, false, new Random(i * 17 + 9), out var artifact);
        if (artifact.Rarity >= 2) Check(state.Restored[artifact.Id] == 1, "rare artifact uniqueness");
        Check(progress.Fragments >= 0 && progress.Keystones >= 0, "currencies never negative");
    }
    Check(catalog.Where(p => p.Culture == culture && p.Source == "research").All(p => state.Restored.ContainsKey(p.Id)), "all research artifacts are reachable");
    Check(catalog.Where(p => p.Source != "research").All(p => !state.Restored.ContainsKey(p.Id)), "weather rewards never replace research projects");
    state.Donated.Add(project.Id); state.LastUse[project.Id] = 42;
    state.Sites.Add(new DigSite { Culture = culture, Location = "Forest", Expires = 8, Points = new() { new() { X = 10, Y = 12, Dug = true }, new() { X = 8, Y = 9 } } });
    state.VanillaSpots.Add("Forest:1:2");
    string json = JsonSerializer.Serialize(state);
    var copy = JsonSerializer.Deserialize<ArchaeologyState>(json)!;
    Check(JsonSerializer.Serialize(copy) == json, "save round trip preserves all progression and finds");
    Research.ExpireSites(copy, 8);
    Check(copy.Sites.Count == 1, "site remains through its final day");
    Research.ExpireSites(copy, 9);
    Check(copy.Sites.Count == 0, "expired sites removed");
    copy.Sites.Add(new DigSite { Expires = 30, Points = new() { new() { Dug = true } } });
    Research.ExpireSites(copy, 10);
    Check(copy.Sites.Count == 0, "exhausted sites removed on refresh");
}
Check(Research.StableSeed(1, 2, 3, "Forest") == Research.StableSeed(1, 2, 3, "Forest"), "stable seeds");
Check(Research.StableSeed(1, 2, 3, "Forest") != Research.StableSeed(1, 3, 3, "Forest"), "per-player randomness");
Check(Research.Level(0) == 1 && Research.Level(-1) == 1 && Research.Level(int.MaxValue) == 10, "level bounds");
var story = new WorkshopState();
Check(Workshop.Find(story, "sea") == null && story.Stage == WorkshopStage.Undiscovered, "story starts only from dwarven excavation");
Check(!Workshop.Study(story, true) && !Workshop.Consult(story, true), "cannot skip story discovery");
Check(Workshop.Find(story, "dwarf") == "blueprint" && story.Stage == WorkshopStage.Blueprint, "first dwarven find guarantees blueprint");
Check(Workshop.Find(story, "dwarf") == null && story.Parts.Count == 0, "components do not drop before research");
Check(!Workshop.Study(story, false) && story.Stage == WorkshopStage.Blueprint, "museum location required");
Check(Workshop.Study(story, true) && !Workshop.Study(story, true), "museum research advances exactly once");
Check(!Workshop.Consult(story, false), "Robin must be nearby");
Check(Workshop.Consult(story, true) && !Workshop.Consult(story, true), "Robin advances exactly once");
Check(Workshop.Find(story, "dwarf") == "spring", "dwarven spring");
Check(Workshop.Find(story, "dwarf") == null && story.Parts.Count == 1, "no duplicate component");
Check(Workshop.Find(story, "sea") == "lens", "coastal lens");
Check(!Workshop.CanRepair(story, true, 20, 5, true), "all three parts required");
Check(Workshop.Find(story, "valley") == "core" && story.Stage == WorkshopStage.Ready, "third part enables assembly");
foreach (var context in new[] { (false,20,5,true), (true,19,5,true), (true,20,4,true), (true,20,5,false) })
    Check(!Workshop.Repair(story,context.Item1,context.Item2,context.Item3,context.Item4) && story.Stage == WorkshopStage.Ready, "failed repair preserves story and parts");
Check(Workshop.Repair(story,true,20,5,true) && story.CompanionEnabled, "assembly unlocks companion");
Check(!Workshop.Repair(story,true,20,5,true), "assembly rewards cannot repeat");
Check(Workshop.Find(story, "dwarf") == null, "completed quest produces no more quest finds");
Check(Workshop.CanCraftCase(story,30,1,true) && !Workshop.CanCraftCase(story,29,1,true) && !Workshop.CanCraftCase(story,30,1,false), "case crafting requirements");
var completedSave = new ArchaeologyState { Workshop = story };
var restoredStory = JsonSerializer.Deserialize<ArchaeologyState>(JsonSerializer.Serialize(completedSave))!;
Check(Workshop.IsValid(restoredStory.Workshop) && restoredStory.Workshop.Stage == WorkshopStage.Complete, "story and companion survive save round trip");
var legacySave = JsonSerializer.Deserialize<ArchaeologyState>("{\"Version\":1,\"Experience\":123,\"Restored\":{\"brass_gear\":2}}")!;
Research.ValidateState(legacySave);
Check(legacySave.Experience == 123 && legacySave.Restored["brass_gear"] == 2 && legacySave.Workshop.Stage == WorkshopStage.Undiscovered, "0.1 saves retain collections and receive a fresh story");
Check(!Workshop.IsValid(new WorkshopState { Stage = WorkshopStage.Complete }), "incomplete story reward state rejected");
Check(!Workshop.IsValid(new WorkshopState { CompanionEnabled = true }), "companion cannot be enabled before unlock");

var trail = new Trail();
trail.Reset(System.Numerics.Vector2.Zero);
for (int i = 0; i <= 120; i++) trail.Update(new System.Numerics.Vector2(i*2,0),1f/60,_=>true);
Check(trail.Position.X > 120 && trail.Position.X < 240 && trail.Position.Y == 0, "companion follows actual movement with a gap");
for (int i = 0; i < 100; i++) trail.Update(new System.Numerics.Vector2(240,i*2),1f/60,_=>true);
Check(trail.Position.Y > 0 && trail.Position.X == 240, "companion follows the corner rather than cutting it");
trail.Update(new System.Numerics.Vector2(5000,5000),1f/60,_=>true);
Check(trail.Position == new System.Numerics.Vector2(5000,5000), "warp resets follower trail");
for (int i = 0; i < 100; i++) trail.Update(new System.Numerics.Vector2(5000+i*2,5000),1f/60,_=>false);
Check(trail.Position.Y == 5000 && !trail.Moving, "blocked follower rejoins without moving through obstacle");
Research.ValidateState(new ArchaeologyState());
Check(true, "new save state validates");
foreach (var badState in new[] {
    new ArchaeologyState { Version = 99 },
    new ArchaeologyState { Cultures = null! },
    new ArchaeologyState { Experience = -1 },
    new ArchaeologyState { Cultures = new() { ["sea"] = new() { Fragments = -5 } } },
    new ArchaeologyState { Sites = new() { new() { Culture = "invalid" } } }
})
{
    bool rejected = false;
    try { Research.ValidateState(badState); } catch (InvalidDataException) { rejected = true; }
    Check(rejected, "malformed or newer saves are rejected for preservation");
}

var en = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(root,"i18n/default.json")))!;
var ru = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(root,"i18n/ru.json")))!;
Check(en.Keys.OrderBy(p=>p).SequenceEqual(ru.Keys.OrderBy(p=>p)), "translation key parity");
foreach (string key in en.Keys)
{
    var a = Regex.Matches(en[key], @"\{\{\w+\}\}").Select(m=>m.Value).OrderBy(p=>p);
    var b = Regex.Matches(ru[key], @"\{\{\w+\}\}").Select(m=>m.Value).OrderBy(p=>p);
    Check(a.SequenceEqual(b), "translation token parity: " + key);
}
foreach (var artifact in catalog)
foreach (string suffix in new[] { "name", "description", "lore" })
    Check(en.ContainsKey($"artifact.{artifact.Id}.{suffix}"), "artifact text: " + artifact.Id);
for (int stage = 0; stage <= 5; stage++)
{
    Check(en.ContainsKey("workshop.heading." + stage), "story heading translation");
    Check(en.ContainsKey("workshop.text." + stage), "story step translation");
}
foreach (string file in Directory.GetFiles(root,"*.cs").Concat(Directory.GetFiles(Path.Combine(root,"Menus"),"*.cs")))
foreach (Match match in Regex.Matches(File.ReadAllText(file), "\\bT\\(\"([^\"]+)\""))
{
    string key = match.Groups[1].Value;
    if (!key.EndsWith('.')) Check(en.ContainsKey(key), "literal UI translation: " + key);
}
Check(WeatherExpeditions.IsValid(legacySave.Weather) && legacySave.Weather.LastObservedDay == -1, "0.1/0.2 saves gain empty weather history");
var weather = new WeatherProgress();
WeatherExpeditions.Observe(weather,10,true,true,true,false,3,4);
Check(weather.Lead == null && weather.Storm, "new install records storm without inventing yesterday");
WeatherExpeditions.Observe(weather,10,false,false,true,false,3,4);
Check(weather.Storm && weather.Raining, "same-day weather observations preserve evidence");
WeatherExpeditions.Observe(weather,11,false,false,true,false,3,4);
var lead = weather.Lead!;
Check(lead is { Kind: "storm", Day:11, Expires:13 }, "morning after storm creates one three-day lead, with storm priority");
string leadKey = lead.Key;
WeatherExpeditions.Observe(weather,11,false,false,true,false,3,4);
Check(weather.Lead?.Key == leadKey, "reloading morning does not reroll weather lead");
Check(WeatherExpeditions.Attach(weather,lead,12) && weather.LastEventDay == 12 && weather.Lead == null, "cooldown begins when a safe site is actually placed");
Check(!WeatherExpeditions.Attach(weather,lead,12), "pending weather lead attaches at most once");
WeatherExpeditions.Observe(weather,12,true,false,true,false,3,4);
WeatherExpeditions.Observe(weather,13,true,false,true,false,3,4);
Check(weather.Lead == null, "weather creation cooldown");
WeatherExpeditions.Observe(weather,14,true,false,true,true,3,4);
WeatherExpeditions.Observe(weather,15,true,false,true,true,3,4);
WeatherExpeditions.Observe(weather,16,true,false,true,true,3,4);
Check(weather.Lead == null, "existing special site prevents another special site");
WeatherExpeditions.Observe(weather,17,false,false,true,false,3,4);
Check(weather.Lead is {Kind:"rain",Expires:19}, "rain creates a new lead when capacity and cooldown permit");
var weatherSave = new ArchaeologyState { Weather = weather };
var weatherSite = new DigSite { Location="Forest",Culture="valley",EventKind="rain",EventId=weather.Lead!.Key,Expires=19,Points=new(){new(){X=1,Y=2,Dug=true},new(){X=3,Y=4,Dug=true},new(){X=5,Y=6}}};
Check(!WeatherExpeditions.Complete(weatherSave,weatherSite), "partial weather dig grants no completion reward");
weatherSite.Points[^1].Dug = true;
Check(WeatherExpeditions.Complete(weatherSave,weatherSite) && weather.PendingRewards.SetEquals(new[]{"rain_pitcher"}), "completion banks the unique relic in the journal");
Check(!WeatherExpeditions.Complete(weatherSave,weatherSite) && weather.Completed["rain"] == 1, "weather completion cannot be rewarded twice");
weatherSave.Sites.Add(weatherSite);
Research.ExpireSites(weatherSave,100);
Check(weatherSave.Sites.Count == 0 && weather.PendingRewards.Contains("rain_pitcher"), "banked relic survives site expiry and full inventory");
weatherSave = JsonSerializer.Deserialize<ArchaeologyState>(JsonSerializer.Serialize(weatherSave))!;
Research.ValidateState(weatherSave);
Check(weatherSave.Weather.PendingRewards.Contains("rain_pitcher") && weatherSave.Weather.CompletedSites.Contains(weatherSite.Key), "weather history and queued reward survive serialization");
Check(WeatherExpeditions.RecordClaim(weatherSave,"rain_pitcher") && !WeatherExpeditions.RecordClaim(weatherSave,"rain_pitcher") && weatherSave.Restored["rain_pitcher"] == 1, "relic claim is one-time");
weatherSite.EventId = "weather:rain:200";
Check(WeatherExpeditions.Complete(weatherSave,weatherSite) && !weatherSave.Weather.PendingRewards.Any(), "repeat event grants progress but no duplicate unique relic");
weatherSite.EventKind = "storm"; weatherSite.EventId="weather:storm:201";
Check(WeatherExpeditions.Complete(weatherSave,weatherSite) && weatherSave.Weather.PendingRewards.Contains("storm_fork"), "storm has an independent unique reward");
var gap = new WeatherProgress();
WeatherExpeditions.Observe(gap,2,true,false,true,false,3,4);
WeatherExpeditions.Observe(gap,5,false,false,true,false,3,4);
Check(gap.Lead == null, "unobserved skipped days do not invent rain events");
var disabled = new WeatherProgress();
WeatherExpeditions.Observe(disabled,2,true,false,false,false,3,4);
WeatherExpeditions.Observe(disabled,3,false,false,false,false,3,4);
Check(disabled.Lead == null, "disabled events record history without creating sites");
var expired = new WeatherProgress { Lead=new(){Kind="rain",Day=1,Expires=3},LastObservedDay=1 };
WeatherExpeditions.Observe(expired,4,false,false,true,false,3,4);
Check(expired.Lead == null && !WeatherExpeditions.Attach(expired,new(){Kind="rain",Day=1,Expires=3},4), "unplaceable lead expires instead of persisting forever");
Check(!WeatherExpeditions.IsValid(new WeatherProgress { PendingRewards=new(){"not-a-relic"} }), "invalid weather data is rejected for preservation");
Check(legacySave.CollectionRewards.Count == 0, "old saves gain an empty collection reward ledger");
Check(MuseumCollections.All.Length == 3 && MuseumCollections.All.Select(p => p.Reward).Distinct().Count() == 3, "three collections have distinct furniture rewards");
foreach (var collection in MuseumCollections.All)
{
    Check(collection.Artifacts.Length == 3 && collection.Artifacts.Distinct().Count() == 3, "three distinct requirements per collection");
    Check(collection.Artifacts.All(id => catalog.Any(p => p.Id == id && p.Culture == collection.Id && p.Effect != "lamp")), "collection requirements are real displayable artifacts of that culture");
    Check(en.ContainsKey("gallery.collection." + collection.Id) && en.ContainsKey("gallery.reward." + collection.Reward), "collection and reward translations exist");
    var museum = new ArchaeologyState();
    Check(!MuseumCollections.RecordClaim(museum, collection.Id, true) && museum.CollectionRewards.Count == 0, "incomplete collection grants nothing");
    museum.Restored[collection.Artifacts[0]] = 20;
    Check(MuseumCollections.Count(museum, collection) == 1, "duplicate restorations count as one distinct exhibit");
    Check(!MuseumCollections.RecordClaim(museum, collection.Id, true), "twenty copies cannot replace the other two finds");
    foreach (string id in collection.Artifacts) { museum.Restored[id] = 1; museum.Donated.Add(id); }
    Check(MuseumCollections.CanClaim(museum, collection.Id, true), "museum donations preserve eligibility without requiring physical items");
    string beforeClaim = JsonSerializer.Serialize(museum);
    Check(!MuseumCollections.RecordClaim(museum, collection.Id, false) && JsonSerializer.Serialize(museum) == beforeClaim, "full inventory preserves the unclaimed reward and all progress");
    Check(!MuseumCollections.RecordClaim(museum, "unknown", true) && JsonSerializer.Serialize(museum) == beforeClaim, "unknown reward cannot mutate progress");
    var restoredBefore = museum.Restored.ToDictionary(p => p.Key, p => p.Value);
    Check(MuseumCollections.RecordClaim(museum, collection.Id, true), "completed collection can be claimed");
    Check(museum.Restored.OrderBy(p => p.Key).SequenceEqual(restoredBefore.OrderBy(p => p.Key)) && museum.Donated.Count == 3, "claim consumes no restoration credit or museum donations");
    Check(!MuseumCollections.RecordClaim(museum, collection.Id, true), "repeated reward claim is rejected");
    museum = JsonSerializer.Deserialize<ArchaeologyState>(JsonSerializer.Serialize(museum))!;
    Research.ValidateState(museum);
    Check(!MuseumCollections.CanClaim(museum, collection.Id, true), "reward stays claimed after saving and reloading");
    Check(MuseumCollections.All.Where(p => p.Id != collection.Id).All(p => !museum.CollectionRewards.Contains(p.Id)), "claim never marks unrelated collections as rewarded");
    var otherPlayer = new ArchaeologyState { Restored = restoredBefore };
    Check(MuseumCollections.CanClaim(otherPlayer, collection.Id, true), "another player can claim their own reward independently");
}
Check(MuseumCollections.CanCraftCase(story,60,2,2,true), "completed workshop unlocks three-exhibit case");
foreach (var resources in new[] { (59,2,2,true), (60,1,2,true), (60,2,1,true), (60,2,2,false) })
    Check(!MuseumCollections.CanCraftCase(story,resources.Item1,resources.Item2,resources.Item3,resources.Item4), "case requires all materials and inventory space");
Check(!MuseumCollections.CanCraftCase(new WorkshopState(),999,999,999,true), "resources cannot bypass the workshop story");
foreach (var badLedger in new HashSet<string>?[] { null, new() { "unknown" } })
{
    bool rejected = false;
    try { Research.ValidateState(new ArchaeologyState { CollectionRewards = badLedger! }); } catch (InvalidDataException) { rejected = true; }
    Check(rejected, "invalid collection reward ledger is rejected for preservation");
}
Check(Shipwreck.IsValid(legacySave.Shipwreck) && legacySave.Shipwreck.Stage == ShipwreckStage.Undiscovered, "legacy saves receive a fresh shipwreck story without changing old progress");
var voyage = new ShipwreckState();
Check(!Shipwreck.Discover(voyage,new Dictionary<string,int> { ["clay_whistle"]=1 }), "only the three-star buckle starts the story");
Check(!Shipwreck.Study(voyage,true) && !Shipwreck.Consult(voyage,true) && !Shipwreck.Decode(voyage,true) && !Shipwreck.RecordClaim(voyage,true), "story steps cannot be skipped");
var oldBuckle = new ArchaeologyState { Restored = new() { ["sailor_buckle"]=1 }, Donated=new(){"sailor_buckle"} };
Check(Shipwreck.Discover(voyage,oldBuckle.Restored) && voyage.Stage == ShipwreckStage.Buckle, "previously restored and donated buckle starts the story without a physical item");
Check(!Shipwreck.Discover(voyage,oldBuckle.Restored), "discovery does not restart an active story");
Check(!Shipwreck.Study(voyage,false) && voyage.Stage == ShipwreckStage.Buckle, "museum required for harbour research");
Check(Shipwreck.Study(voyage,true) && !Shipwreck.Study(voyage,true), "Gunther research advances once");
Check(!Shipwreck.Consult(voyage,false) && voyage.Stage == ShipwreckStage.PortRecord, "Willy must be nearby for the knot");
Check(Shipwreck.Consult(voyage,true) && !Shipwreck.Consult(voyage,true), "Willy starts the bottle search once");
Shipwreck.Observe(voyage,20,true);
Check(Shipwreck.SiteKind(voyage,20) == null, "first rainy observation does not invent an earlier rainy day");
Shipwreck.Observe(voyage,20,false);
Check(voyage.Raining && voyage.BottleUntil == -1, "same-day observation retains rain without spawning a bottle");
Shipwreck.Observe(voyage,21,false);
Check(voyage.BottleUntil == 23 && Shipwreck.SiteKind(voyage,21) == "ship_bottle", "the next morning opens a three-day guaranteed bottle window");
string shipBefore = JsonSerializer.Serialize(voyage);
Shipwreck.Observe(voyage,21,false);
Check(JsonSerializer.Serialize(voyage) == shipBefore, "reloading does not extend the bottle window");
Shipwreck.Observe(voyage,23,false);
Check(Shipwreck.SiteKind(voyage,23) == "ship_bottle" && Shipwreck.SiteKind(voyage,24) == null, "bottle remains through its last day and expires afterward");
Shipwreck.Observe(voyage,24,true);
Shipwreck.Observe(voyage,25,false);
Check(voyage.BottleUntil == 27 && Shipwreck.SiteKind(voyage,25) == "ship_bottle", "another rain retries an expired opportunity");
var noRainHistory = new ShipwreckState { Stage=ShipwreckStage.AwaitBottle };
Shipwreck.Observe(noRainHistory,1,true);
Shipwreck.Observe(noRainHistory,4,false);
Check(Shipwreck.SiteKind(noRainHistory,4) == null, "missing observation days do not create fictional bottle windows");
var bottle = new DigSite { Location="Beach",Culture="sea",EventKind="ship_bottle",EventId="ship:bottle:25",Expires=27,Points=new(){new(){X=8,Y=12}} };
Check(!Shipwreck.CompleteSite(voyage,bottle,25) && voyage.Stage == ShipwreckStage.AwaitBottle, "a bottle must actually be excavated");
bottle.Points[0].Dug = true;
Check(!Shipwreck.CompleteSite(voyage,bottle,28) && voyage.Stage == ShipwreckStage.AwaitBottle, "expired bottle cannot advance the story");
bottle.Location="Forest";
Check(!Shipwreck.CompleteSite(voyage,bottle,25), "unrelated location cannot stand in for the beach");
bottle.Location="Beach";
Check(Shipwreck.CompleteSite(voyage,bottle,25) && voyage.Stage == ShipwreckStage.Letter && voyage.BottleUntil == -1, "excavation archives the letter and closes the bottle window");
Check(!Shipwreck.CompleteSite(voyage,bottle,25), "same bottle cannot advance twice");
Check(!Shipwreck.Decode(voyage,false) && voyage.Stage == ShipwreckStage.Letter, "Willy is required to decode the captain's clue");
Check(Shipwreck.Decode(voyage,true) && Shipwreck.SiteKind(voyage,900) == "ship_cache", "decoded cache never expires while waiting for excavation");
Check(!Shipwreck.Decode(voyage,true), "decoding cannot restart or duplicate the cache");
Check(!Shipwreck.CompleteSite(voyage,new DigSite{Location="Beach",Culture="sea",EventKind="ship_cache",EventId="ship:cache",Expires=int.MaxValue,Points=new(){new(){Dug=true}}},900), "cache requires all three actual excavation points");
var cache = new DigSite { Location="Beach",Culture="sea",EventKind="ship_cache",EventId="ship:cache",Expires=int.MaxValue,Points=new(){new(){X=10,Y=12,Dug=true},new(){X=12,Y=12,Dug=true},new(){X=14,Y=12}} };
var voyageSave = new ArchaeologyState { Shipwreck=voyage, Sites=new(){bottle,cache} };
Research.ValidateState(voyageSave);
Research.ExpireSites(voyageSave,900);
Check(voyageSave.Sites.Count == 1 && voyageSave.Sites[0].Points.Count(p=>p.Dug) == 2, "cache and partial excavation survive many days while the bottle disappears");
Check(!WeatherExpeditions.Complete(voyageSave,cache) && voyageSave.Weather.PendingRewards.Count == 0, "ship story cannot grant ordinary weather relics");
Check(!Shipwreck.CompleteSite(voyage,cache,900) && !Shipwreck.RecordClaim(voyage,true), "partial cache does not grant a bell");
cache.Points[^1].Dug = true;
Check(Shipwreck.CompleteSite(voyage,cache,900) && voyage.Stage == ShipwreckStage.Recovered, "last excavation banks the bell outside backpack capacity");
Check(!Shipwreck.CompleteSite(voyage,cache,900), "completed cache grants its reward once");
Check(!Shipwreck.RecordClaim(voyage,false) && voyage.Stage == ShipwreckStage.Recovered, "full backpack preserves the banked bell");
voyageSave = JsonSerializer.Deserialize<ArchaeologyState>(JsonSerializer.Serialize(voyageSave))!;
Research.ValidateState(voyageSave);
Check(voyageSave.Shipwreck.Stage == ShipwreckStage.Recovered, "unclaimed bell survives a save round trip");
Check(Shipwreck.RecordClaim(voyageSave.Shipwreck,true) && !Shipwreck.RecordClaim(voyageSave.Shipwreck,true), "bell is claimed once per player");
voyageSave = JsonSerializer.Deserialize<ArchaeologyState>(JsonSerializer.Serialize(voyageSave))!;
Check(!Shipwreck.RecordClaim(voyageSave.Shipwreck,true) && voyageSave.Shipwreck.Stage == ShipwreckStage.Complete, "reload does not duplicate the bell");
var anotherVoyage = new ShipwreckState { Stage=ShipwreckStage.Recovered };
Check(Shipwreck.RecordClaim(anotherVoyage,true), "another player can claim their own story reward");
foreach (ShipwreckStage stage in Enum.GetValues(typeof(ShipwreckStage)))
{
    var stageSave=new ArchaeologyState { Shipwreck=new(){Stage=stage,LastObservedDay=40} };
    var stageReload=JsonSerializer.Deserialize<ArchaeologyState>(JsonSerializer.Serialize(stageSave))!;
    Research.ValidateState(stageReload);
    Check(stageReload.Shipwreck.Stage==stage && stageReload.Shipwreck.LastObservedDay==40, "every story stage survives serialization");
}
Check(Shipwreck.CanRing(-1,30,50,270) && !Shipwreck.CanRing(-1,30,270,270), "bell requires missing energy and preserves a charge at full energy");
Check(!Shipwreck.CanRing(30,30,50,270) && Shipwreck.CanRing(30,31,50,270), "all bell copies share a daily player charge that renews next day");
Check(Shipwreck.CanRing(-1,30,-10,270), "exhausted players can use the bell");
voyageSave.LastUse[Shipwreck.BellId]=30;
var voyageReload=JsonSerializer.Deserialize<ArchaeologyState>(JsonSerializer.Serialize(voyageSave))!;
Check(!Shipwreck.CanRing(voyageReload.LastUse[Shipwreck.BellId],30,50,270), "bell daily charge survives reload");
foreach (var badVoyage in new ShipwreckState?[] { null, new(){Stage=(ShipwreckStage)99}, new(){LastObservedDay=-2}, new(){BottleUntil=5}, new(){Stage=ShipwreckStage.Complete,LastObservedDay=1,BottleUntil=5} })
{
    bool rejected=false;
    try { Research.ValidateState(new ArchaeologyState{Shipwreck=badVoyage!}); } catch (InvalidDataException) { rejected=true; }
    Check(rejected,"invalid shipwreck data is rejected for preservation");
}
for (int stage=0;stage<=7;stage++)
    Check(en.ContainsKey("shipwreck.heading."+stage) && en.ContainsKey("shipwreck.text."+stage), "shipwreck stage has bilingual journal content");
for (int step=0;step<6;step++) Check(en.ContainsKey("shipwreck.step."+step), "shipwreck timeline label exists");
foreach (string dialogue in new[]{"gunther","willy","decode"}) Check(en.ContainsKey("shipwreck.dialogue."+dialogue), "shipwreck dialogue exists");
foreach (string kind in Shipwreck.Kinds) Check(en.ContainsKey("shipwreck.site."+kind), "story site has its own HUD name");
Check(UnusualRelics.IsValid(legacySave.Stars) && legacySave.Stars.Lead == null, "old saves gain empty star research without invented expeditions");
Check(catalog.Where(p=>UnusualRelics.Effects.Contains(p.Effect)).Count()==4, "exactly four unusual relics are registered");
foreach (string effect in UnusualRelics.Effects)
    Check(catalog.Count(p=>p.Effect==effect && p.Source=="research" && p.Rarity>=2)==1, "each unusual effect has one reachable rare research artifact");
for (int i=0;i<23;i++) Check(catalog[i].Sprite==(i<21?i:i+3), "0.1–0.5 artifact sprite slots are preserved");
Check(UnusualRelics.DaysUntilReady(-1,10,"garden")==0, "new flute begins charged");
Check(UnusualRelics.DaysUntilReady(10,10,"garden")==7 && UnusualRelics.DaysUntilReady(10,16,"garden")==1 && UnusualRelics.DaysUntilReady(10,17,"garden")==0, "flute uses seven rolling game days rather than calendar weeks");
foreach (string effect in UnusualRelics.Effects.Where(e=>e!="garden"))
    Check(UnusualRelics.DaysUntilReady(10,10,effect)==1 && UnusualRelics.DaysUntilReady(10,11,effect)==0, "other relics recharge the next day");
Check(UnusualRelics.CanGrowFruit(0,false,0,true,0) && UnusualRelics.CanGrowFruit(-120,false,0,true,2), "mature healthy in-season trees have room for a bonus fruit");
foreach (var conditions in new[]{ (1,false,0,true,0), (0,true,0,true,0), (0,false,1,true,0), (0,false,0,false,0), (0,false,0,true,3) })
    Check(!UnusualRelics.CanGrowFruit(conditions.Item1,conditions.Item2,conditions.Item3,conditions.Item4,conditions.Item5), "flute rejects saplings, stumps, lightning, wrong season, and full trees");
var extendedSite=new DigSite { Location="Forest",Culture="valley",Expires=30,Points=new(){new(){X=10,Y=10},new(){X=12,Y=10,Dug=true}} };
string originalSiteKey=extendedSite.Key;
Check(UnusualRelics.Extend(extendedSite,30) && extendedSite.Expires==31 && extendedSite.Key==originalSiteKey && extendedSite.EventId==originalSiteKey, "hourglass pins the original key so existing survey signals remain valid");
string afterExtension=JsonSerializer.Serialize(extendedSite);
Check(!UnusualRelics.Extend(extendedSite,31) && JsonSerializer.Serialize(extendedSite)==afterExtension, "copies and the next day's charge cannot extend the same site twice");
var reloadedSite=JsonSerializer.Deserialize<DigSite>(afterExtension)!;
Check(reloadedSite.TimeExtended && reloadedSite.Key==originalSiteKey && !UnusualRelics.Extend(reloadedSite,31), "site extension and stable signal key survive reload");
foreach (var unavailableSite in new[]{
    new DigSite{Expires=29,Points=new(){new()}},
    new DigSite{Expires=31,Points=new(){new(){Dug=true}}},
    new DigSite{EventKind="ship_bottle",EventId="ship:bottle:30",Expires=31,Points=new(){new()}},
    new DigSite{EventKind="ship_cache",EventId="ship:cache",Expires=int.MaxValue,Points=new(){new()}},
    new DigSite{Expires=int.MaxValue,Points=new(){new()}}
})
{
    string before=JsonSerializer.Serialize(unavailableSite);
    Check(!UnusualRelics.Extend(unavailableSite,30) && JsonSerializer.Serialize(unavailableSite)==before, "expired, completed, story, and infinite sites are unchanged by the hourglass");
}
var extendedRain=new DigSite{Location="Forest",Culture="valley",EventKind="rain",EventId="weather:rain:30",Expires=31,Points=new(){new()}};
Check(UnusualRelics.Extend(extendedRain,30) && extendedRain.Key=="weather:rain:30" && extendedRain.Expires==32, "weather sites can be extended without changing their reward identity");
bool Walkable(GridPoint p)=>p.X>=0 && p.X<5 && p.Y>=0 && p.Y<4 && (p.X!=2 || p.Y==3);
var path=UnusualRelics.FindTrail(new(0,1),new(4,1),Walkable);
Check(path.Count>5 && path.First()==new GridPoint(0,1) && path.Last()==new GridPoint(4,1) && path.Contains(new(2,3)), "past lens follows the opening in a wall instead of crossing it");
Check(path.All(Walkable) && path.Zip(path.Skip(1)).All(p=>Math.Abs(p.First.X-p.Second.X)+Math.Abs(p.First.Y-p.Second.Y)==1), "every trail step is walkable and cardinally adjacent");
Check(UnusualRelics.FindTrail(new(0,1),new(4,1),p=>Walkable(p) && p.X!=2).Count==0, "unreachable find produces no imaginary trail");
Check(UnusualRelics.FindTrail(new(0,1),new(4,1),Walkable,3).Count==0, "trail search respects its node limit");
Check(UnusualRelics.FindTrail(new(1,1),new(1,1),Walkable).Count==1, "standing at the find is a valid zero-distance trail");
Check(UnusualRelics.FindTrail(new(0,1),new(2,1),Walkable).Count==0, "blocked goal never receives a trail");
var stargazer=new ArchaeologyState();
Check(!UnusualRelics.ReserveStars(stargazer,40,1950,true,"valley","Forest") && !UnusualRelics.ReserveStars(stargazer,40,2100,false,"valley","Forest") && stargazer.Stars.Lead==null, "observatory requires outdoors after 20:00");
Check(!UnusualRelics.ReserveStars(stargazer,40,2100,true,"invalid","Forest") && !UnusualRelics.ReserveStars(stargazer,40,2100,true,"sea",""), "invalid cultures and locations never create forecasts");
Check(UnusualRelics.ReserveStars(stargazer,40,2000,true,"valley","Forest"), "night observation reserves a real morning lead");
var starLead=stargazer.Stars.Lead!;
Check(starLead.Day==41 && starLead.Expires==43 && starLead.Location=="Forest" && starLead.Culture=="valley", "forecast records destination, culture and the original three-day window");
Check(!UnusualRelics.ReserveStars(stargazer,40,2200,true,"sea","Beach") && !UnusualRelics.AttachStars(stargazer.Stars,starLead,40), "forecast cannot be replaced or excavated on its observation night");
stargazer=JsonSerializer.Deserialize<ArchaeologyState>(JsonSerializer.Serialize(stargazer))!;
Research.ValidateState(stargazer);
Check(stargazer.Stars.Lead!.Key==starLead.Key && stargazer.Stars.Lead.Expires==43, "forecast destination and deadline survive reload");
Check(UnusualRelics.AttachStars(stargazer.Stars,starLead,41) && !UnusualRelics.AttachStars(stargazer.Stars,starLead,41), "forecast attaches once on or after its morning");
var starSite=new DigSite{Location="Forest",Culture="valley",EventKind="stars",EventId=starLead.Key,Expires=43,Points=new(){new(){X=8,Y=8,Dug=true},new(){X=10,Y=8}}};
stargazer.Sites.Add(starSite);
Check(!UnusualRelics.ReserveStars(stargazer,41,2200,true,"sea","Beach"), "an unfinished star expedition prevents stacking another forecast");
Check(UnusualRelics.Extend(starSite,43) && starSite.Expires==44 && starSite.Key==starLead.Key, "hourglass can extend a star expedition once without changing its identity");
Check(!UnusualRelics.CompleteStars(stargazer,starSite), "partial star expeditions grant no completion reward");
starSite.Points[1].Dug=true;
Check(UnusualRelics.CompleteStars(stargazer,starSite) && !UnusualRelics.CompleteStars(stargazer,starSite), "star completion can be rewarded only once");
Check(!WeatherExpeditions.Complete(stargazer,starSite) && !Shipwreck.CompleteSite(stargazer.Shipwreck,starSite,43), "star finds cannot advance weather or shipwreck rewards");
stargazer.LastUse["garden_flute"]=40;
stargazer=JsonSerializer.Deserialize<ArchaeologyState>(JsonSerializer.Serialize(stargazer))!;
Research.ValidateState(stargazer);
Check(stargazer.Stars.Completed.SetEquals(new[]{starLead.Key}) && !UnusualRelics.CompleteStars(stargazer,stargazer.Sites[0]), "star reward ledger prevents duplicate rewards after reload");
Check(UnusualRelics.DaysUntilReady(stargazer.LastUse["garden_flute"],46,"garden")==1, "weekly flute charge survives reload");
Research.ExpireSites(stargazer,45);
Check(stargazer.Sites.Count==0 && stargazer.Stars.Lead==null, "completed star site expires without recreating the consumed forecast");
Check(UnusualRelics.ReserveStars(stargazer,45,2200,true,"sea","Beach"), "a completed expedition permits a later forecast");
var expiredStar=stargazer.Stars.Lead!;
UnusualRelics.ExpireLead(stargazer.Stars,expiredStar.Expires+1);
Check(stargazer.Stars.Lead==null && !UnusualRelics.AttachStars(stargazer.Stars,expiredStar,expiredStar.Expires+1), "unplaceable forecast expires at its original deadline");
foreach(var invalidStars in new StarResearch?[]{null,new(){Completed=null!},new(){Lead=new(){Day=1,Expires=0,Culture="valley",Location="Forest"}},new(){Lead=new(){Day=1,Expires=3,Culture="bad",Location="Forest"}},new(){Completed=new(){"wrong"}}})
{
    bool rejected=false;
    try{Research.ValidateState(new ArchaeologyState{Stars=invalidStars!});}catch(InvalidDataException){rejected=true;}
    Check(rejected,"invalid star research is rejected for preservation");
}
Check(NaturalEvents.IsValid(legacySave.Nature) && legacySave.Nature.LastObservedDay==-1 && legacySave.Nature.Leads.Count==0, "legacy saves start fresh natural observations without changing their collection");
foreach(string kind in NaturalEvents.Kinds)
{
    Check(catalog.Count(p=>p.Source==kind && p.Id==NaturalEvents.Artifact(kind) && p.Culture==NaturalEvents.Culture(kind) && p.Rarity>=2)==1,"each natural event has one unique rare collectible");
    foreach(string prefix in new[]{"nature.name.","nature.guide.","nature.artifact-hint."}) Check(en.ContainsKey(prefix+kind),"natural event translations exist");
}
for(int i=0;i<27;i++) Check(catalog[i].Sprite==(i<21?i:i+3),"all 0.6 atlas slots remain stable");
void ObserveNature(NaturalProgress nature,int day,int year,int season,int monthDay,bool snow=false,bool enabled=true,params GridPoint[] impacts)
    => NaturalEvents.Observe(nature,day,year,season,monthDay,snow,impacts,enabled,5);
var nature=new NaturalProgress();
ObserveNature(nature,84,1,2,28,impacts:new[]{new GridPoint(20,20)});
Check(nature.Leads.Count==0 && nature.MeteorSnapshot.SetEquals(new[]{"20:20"}),"new installation baselines existing meteors");
ObserveNature(nature,85,1,3,1,true,impacts:new[]{new GridPoint(20,20),new GridPoint(40,40)});
Check(nature.Leads.Count==3 && nature.Leads.Select(p=>p.Kind).ToHashSet().SetEquals(NaturalEvents.Kinds),"snow, season change and a fresh impact coexist");
var snowLead=nature.Leads.Single(p=>p.Kind=="snow");
var seasonLead=nature.Leads.Single(p=>p.Kind=="season");
var meteorLead=nature.Leads.Single(p=>p.Kind=="meteor");
Check(snowLead.Day==86 && snowLead.Expires==90 && seasonLead.Day==85 && seasonLead.Expires==89 && meteorLead.AnchorX==40 && meteorLead.AnchorY==40,"deadlines begin on each event's opening morning; impact anchors are recorded");
string observations=JsonSerializer.Serialize(nature);
ObserveNature(nature,85,1,3,1,true,impacts:new[]{new GridPoint(20,20),new GridPoint(40,40)});
Check(JsonSerializer.Serialize(nature)==observations,"same-day reload neither duplicates natural leads nor extends their deadlines");
Check(!NaturalEvents.Attach(nature,snowLead,85) && NaturalEvents.Attach(nature,snowLead,86) && !NaturalEvents.Attach(nature,snowLead,86),"snow opens next morning and attaches once");
ObserveNature(nature,86,1,3,2,true,impacts:new[]{new GridPoint(20,20),new GridPoint(40,40)});
Check(nature.Leads.All(p=>p.Kind!="snow"),"continued snowfall does not create another annual snow event");
Check(NaturalEvents.Attach(nature,meteorLead,89) && !NaturalEvents.Attach(nature,seasonLead,90),"leads attach through their last day but never after it");
ObserveNature(nature,90,1,3,6);
Check(nature.Leads.Count==0,"expired unplaced leads are removed without renewed lifetime");
var baseline=new NaturalProgress();
ObserveNature(baseline,85,1,3,1,true,impacts:new[]{new GridPoint(10,10)});
Check(baseline.Leads.Count==1 && baseline.Leads[0].Kind=="snow","installing on snowy season day one records snow without retroactive season or meteor events");
ObserveNature(baseline,112,1,3,28);
ObserveNature(baseline,113,2,0,1);
Check(baseline.Leads.Count==1 && baseline.Leads[0].Kind=="season" && baseline.Leads[0].Key=="nature:season:2:0","winter to spring is a new-year season event");
ObserveNature(baseline,197,2,3,1,true);
Check(baseline.Leads.Any(p=>p.Kind=="snow" && p.Key=="nature:snow:2"),"next year's first snowfall unlocks another snow expedition");
var muted=new NaturalProgress();
ObserveNature(muted,84,1,2,28);
ObserveNature(muted,85,1,3,1,true,false,new GridPoint(20,20));
ObserveNature(muted,85,1,3,1,true,true,new GridPoint(20,20));
ObserveNature(muted,86,1,3,2,true,true,new GridPoint(20,20));
Check(muted.Leads.Count==0 && muted.SnowYears.Contains(1),"disabled observations do not replay events when reenabled");
var gaps=new NaturalProgress();
ObserveNature(gaps,1,1,0,1);
ObserveNature(gaps,29,1,1,1,impacts:new[]{new GridPoint(20,20)});
Check(gaps.Leads.Count==0,"missing observation days rebaseline impacts and do not invent season changes");
ObserveNature(gaps,29,1,1,1,impacts:new[]{new GridPoint(20,20),new GridPoint(40,40)});
Check(gaps.Leads.Count==1 && gaps.Leads[0].AnchorX==40,"a genuinely new same-day impact is detected once");
ObserveNature(gaps,29,1,1,1,impacts:new[]{new GridPoint(20,20)});
ObserveNature(gaps,30,1,1,2,impacts:new[]{new GridPoint(20,20),new GridPoint(40,40)});
Check(gaps.Leads.Count==2 && gaps.Leads.Select(p=>p.Key).Distinct().Count()==2,"after mining, a new meteor at the same coordinates has a new identity");
Check(NaturalEvents.WithinMeteorRadius(new(48,40),40,40) && !NaturalEvents.WithinMeteorRadius(new(49,40),40,40) && !NaturalEvents.WithinMeteorRadius(new(48,48),40,40),"meteor area is a circle centered on the two-tile impact");
foreach(string kind in NaturalEvents.Kinds)
{
    var field=new ArchaeologyState();
    var naturalSite=new DigSite { Culture=NaturalEvents.Culture(kind), Location=kind=="meteor"?"Farm":kind=="season"?"Beach":"Forest",EventKind=kind,EventId="nature:"+kind+":test",Expires=20,AnchorX=40,AnchorY=40,
        Points=new(){new(){X=38,Y=40,Dug=true},new(){X=40,Y=38,Dug=true},new(){X=42,Y=40}} };
    field.Sites.Add(naturalSite);
    Research.ValidateState(field);
    Check(!NaturalEvents.Complete(field,naturalSite,20) && field.Nature.PendingRewards.Count==0,"partial natural digs never grant rewards");
    naturalSite.Points[^1].Dug=true;
    Check(!NaturalEvents.Complete(field,naturalSite,21),"expired natural digs never grant rewards");
    Check(NaturalEvents.Complete(field,naturalSite,20) && field.Nature.PendingRewards.Contains(NaturalEvents.Artifact(kind)),"final find banks the natural artifact independent of backpack space");
    Check(!NaturalEvents.Complete(field,naturalSite,20) && field.Nature.Completed[kind]==1,"each natural site completion is rewarded once");
    Check(!WeatherExpeditions.Complete(field,naturalSite) && !UnusualRelics.CompleteStars(field,naturalSite) && !Shipwreck.CompleteSite(field.Shipwreck,naturalSite,20),"natural sites never grant other event rewards");
    Research.ExpireSites(field,21);
    field=JsonSerializer.Deserialize<ArchaeologyState>(JsonSerializer.Serialize(field))!;
    Research.ValidateState(field);
    Check(field.Sites.Count==0 && field.Nature.PendingRewards.Contains(NaturalEvents.Artifact(kind)),"banked artifacts survive expiry and save roundtrip");
    Check(!NaturalEvents.RecordClaim(field,"rain_pitcher") && NaturalEvents.RecordClaim(field,NaturalEvents.Artifact(kind)) && !NaturalEvents.RecordClaim(field,NaturalEvents.Artifact(kind)),"only a natural pending reward is claimable, exactly once");
    Check(field.Restored[NaturalEvents.Artifact(kind)]==1 && field.Nature.PendingRewards.Count==0,"claim registers the artifact for journal, display and donation");
    naturalSite.EventId+="-repeat";
    Check(NaturalEvents.Complete(field,naturalSite,20) && field.Nature.PendingRewards.Count==0 && field.Nature.Completed[kind]==2,"repeat events still count without duplicating unique artifacts");
    naturalSite.Points[0].Dug=false;
    Check(UnusualRelics.Extend(naturalSite,20) && naturalSite.Expires==21 && naturalSite.Key.EndsWith("-repeat"),"natural dig extension preserves event identity and anchor");
}
foreach(var invalidNature in new NaturalProgress?[]{null,new(){Leads=null!},new(){MeteorSnapshot=null!},new(){LastObservedDay=-2},new(){LastSeason=4},new(){SnowYears=new(){0}},new(){Completed=new(){["wrong"]=1}},new(){PendingRewards=new(){"past_lens"}},new(){Leads=new(){new(){Kind="meteor",Key="nature:meteor:1",Day=1,Expires=5}}}})
{
    bool rejected=false;
    try { Research.ValidateState(new ArchaeologyState{Nature=invalidNature!}); } catch(InvalidDataException) { rejected=true; }
    Check(rejected,"invalid natural progress is rejected for preservation");
}
foreach(var point in new[]{new GridPoint(49,40),new GridPoint(10,10)})
{
    var outside=new DigSite { Culture="dwarf",Location="Farm",EventKind="meteor",EventId="nature:meteor:1",Expires=20,AnchorX=40,AnchorY=40,Points=new(){new(){X=point.X,Y=point.Y,Dug=true}} };
    bool rejected=false;
    try { Research.ValidateState(new ArchaeologyState{Sites=new(){outside}}); } catch(InvalidDataException) { rejected=true; }
    Check(rejected,"meteor finds cannot migrate away from the impact");
}
Check(ResidentStories.IsValid(legacySave.Residents) && legacySave.Residents.Stages.Count==0,"legacy saves gain empty personal stories without altering prior progress");
Check(ResidentStories.Discover(new(),new Dictionary<string,int>()).Count==0,"personal stories never invent missing artifact discoveries");
Check(ResidentStories.Sources.Distinct().Count()==3 && ResidentStories.Kinds.Distinct().Count()==3,"each resident has a distinct story and cache kind");
for(int i=0;i<30;i++) Check(catalog[i].Sprite==(i<21?i:i+3),"all 0.7 artifact slots remain stable");
foreach(var definition in ResidentStories.All)
{
    Check(catalog.Any(p=>p.Id==definition.Trigger && p.Source=="research"),"resident story begins with an accessible existing research artifact");
    Check(catalog.Count(p=>p.Id==definition.Artifact && p.Source==definition.Id && p.Culture==definition.Culture && p.Rarity==2)==1,"each story registers one rare reward outside research projects");
    var personal=new ArchaeologyState();
    string id=definition.Id;
    Check(!ResidentStories.Start(personal.Residents,id,true) && !ResidentStories.Share(personal.Residents,id,true) && !ResidentStories.RecordClaim(personal,id,true),"story stages cannot be skipped");
    personal.Restored[definition.Trigger]=1;
    personal.Donated.Add(definition.Trigger);
    var discoveries=ResidentStories.Discover(personal.Residents,personal.Restored);
    Check(discoveries.SequenceEqual(new[]{id}) && ResidentStories.Stage(personal.Residents,id)==ResidentStage.Clue,"previously donated discoveries open their own resident's story");
    Check(ResidentStories.Discover(personal.Residents,personal.Restored).Count==0,"discovery and notifications are one-time");
    string beforeTalk=JsonSerializer.Serialize(personal);
    Check(!ResidentStories.Start(personal.Residents,id,false) && JsonSerializer.Serialize(personal)==beforeTalk,"distant resident cannot advance interpretation");
    Check(ResidentStories.Start(personal.Residents,id,true) && !ResidentStories.Start(personal.Residents,id,true),"first interpretation advances exactly once");
    var residentSite=new DigSite{Culture=definition.Culture,Location=definition.Location,EventKind=definition.SiteKind,EventId=definition.SiteKey,Expires=int.MaxValue,Points=new(){new(){X=12,Y=14,Dug=true},new(){X=14,Y=14}}};
    personal.Sites.Add(residentSite);
    Research.ValidateState(personal);
    Check(!ResidentStories.CompleteSite(personal.Residents,residentSite),"one recovered note cannot finish the two-piece personal cache");
    personal=JsonSerializer.Deserialize<ArchaeologyState>(JsonSerializer.Serialize(personal))!;
    Research.ValidateState(personal);
    Research.ExpireSites(personal,9000);
    residentSite=personal.Sites.Single();
    Check(residentSite.Points.Count(p=>p.Dug)==1 && ResidentStories.Stage(personal.Residents,id)==ResidentStage.Searching,"partial personal caches survive saves and arbitrarily many days");
    Check(!UnusualRelics.Extend(residentSite,9000),"permanent personal caches do not consume hourglass extensions");
    residentSite.Points[^1].Dug=true;
    foreach(string bad in new[]{"location","culture","event","kind","count","expiry","duplicate"})
    {
        var wrong=JsonSerializer.Deserialize<DigSite>(JsonSerializer.Serialize(residentSite))!;
        if(bad=="location")wrong.Location="wrong";
        if(bad=="culture")wrong.Culture=Research.Cultures.First(p=>p!=definition.Culture);
        if(bad=="event")wrong.EventId+="-wrong";
        if(bad=="kind")wrong.EventKind="rain";
        if(bad=="count")wrong.Points.RemoveAt(1);
        if(bad=="expiry")wrong.Expires=9001;
        if(bad=="duplicate"){wrong.Points[1].X=wrong.Points[0].X;wrong.Points[1].Y=wrong.Points[0].Y;}
        Check(!ResidentStories.CompleteSite(personal.Residents,wrong),"wrong "+bad+" cannot complete personal story");
    }
    Check(ResidentStories.CompleteSite(personal.Residents,residentSite) && !ResidentStories.CompleteSite(personal.Residents,residentSite),"last note advances exactly once, preventing repeated completion bonuses");
    Check(!WeatherExpeditions.Complete(personal,residentSite) && !NaturalEvents.Complete(personal,residentSite,9000) && !UnusualRelics.CompleteStars(personal,residentSite) && !Shipwreck.CompleteSite(personal.Shipwreck,residentSite,9000),"resident caches never grant another event's rewards");
    Check(!ResidentStories.PendingArtifact(personal.Residents,definition.Artifact) && !ResidentStories.RecordClaim(personal,id,true),"final conversation required before keepsake claim");
    Check(!ResidentStories.Share(personal.Residents,id,false),"return conversation requires the correct resident nearby");
    Check(ResidentStories.Share(personal.Residents,id,true) && !ResidentStories.Share(personal.Residents,id,true),"friendship and experience can be awarded by final conversation only once");
    Check(ResidentStories.PendingArtifact(personal.Residents,definition.Artifact),"completed scene banks the keepsake in journal");
    string fullBackpack=JsonSerializer.Serialize(personal);
    Check(!ResidentStories.RecordClaim(personal,id,false) && JsonSerializer.Serialize(personal)==fullBackpack,"full backpack preserves the banked keepsake and all story progress");
    Research.ExpireSites(personal,9000);
    personal=JsonSerializer.Deserialize<ArchaeologyState>(JsonSerializer.Serialize(personal))!;
    Research.ValidateState(personal);
    Check(personal.Sites.Count==0 && ResidentStories.PendingArtifact(personal.Residents,definition.Artifact),"banked reward survives exhausted-cache removal and reload");
    Check(ResidentStories.RecordClaim(personal,id,true) && !ResidentStories.RecordClaim(personal,id,true) && personal.Restored[definition.Artifact]==1,"keepsake claim is unique and registers journal, display and donation credit");
    Check(!ResidentStories.PendingArtifact(personal.Residents,definition.Artifact) && !ResidentStories.Share(personal.Residents,id,true),"claimed story never reopens its reward or final conversation");
    var otherPlayer=new ArchaeologyState{Residents=new(){Stages=new(){[id]=ResidentStage.Reward}}};
    Check(ResidentStories.RecordClaim(otherPlayer,id,true),"another player has their own personal keepsake");
    foreach(ResidentStage stage in Enum.GetValues(typeof(ResidentStage)))
    {
        var saved=new ArchaeologyState{Residents=new(){Stages=new(){[id]=stage}}};
        var loaded=JsonSerializer.Deserialize<ArchaeologyState>(JsonSerializer.Serialize(saved))!;
        Research.ValidateState(loaded);
        Check(ResidentStories.Stage(loaded.Residents,id)==stage,"every personal story stage survives serialization");
    }
    foreach(string key in new[]{"residents.title.","residents.site.","residents.goal.","residents.note.","residents.artifact-hint."})Check(en.ContainsKey(key+id),"personal story note and journal labels exist");
    foreach(string action in new[]{"start","share"})
    {
        string key="residents.dialogue."+id+"."+action;
        Check(en[key].Split("#$b#").Length==3 && ru[key].Split("#$b#").Length==3,"portrait scene has three translated dialogue pages");
        Check(!Regex.IsMatch(en[key]+ru[key],@"\$(?!b)"),"scene uses only known page breaks and no unescaped dialogue commands");
    }
}
var reunited=new ResidentProgress();
var oldCollection=ResidentStories.All.ToDictionary(p=>p.Trigger,_=>2);
Check(ResidentStories.Discover(reunited,oldCollection).Count==3 && reunited.Stages.Values.All(p=>p==ResidentStage.Clue),"all three old discoveries unlock independent stories together");
Check(!ResidentStories.Start(reunited,"missing",true) && !ResidentStories.Share(reunited,"missing",true) && !ResidentStories.RecordClaim(new(),"missing",true),"unknown personal story IDs are inert");
foreach(var corrupt in new ResidentProgress?[]{null,new(){Stages=null!},new(){Stages=new(){["missing"]=ResidentStage.Clue}},new(){Stages=new(){["penny"]=(ResidentStage)100}}})
{
    bool rejected=false;
    try { Research.ValidateState(new ArchaeologyState{Residents=corrupt!}); } catch(InvalidDataException){rejected=true;}
    Check(rejected,"invalid personal story progress is rejected for preservation");
}
foreach(int stage in Enumerable.Range(0,6))Check(en.ContainsKey("residents.stage."+stage),"every resident stage has bilingual journal guidance");
Check(legacySave.TrackedObjective=="", "old saves receive empty tracking without changing progression");
var fieldNotes=new ArchaeologyState();
Check(FieldJournal.Build(fieldNotes,catalog,10).Count==0 && fieldNotes.Cultures.Count==0,"building empty objectives does not create projects, currency or stories");
DigSite FieldSite(string kind,string culture,int expires,string identity,string location="Forest")
    =>new(){EventKind=kind,Culture=culture,Expires=expires,EventId=identity,Location=location,Points=new(){new(){X=10,Y=12,Dug=true},new(){X=12,Y=12},new(){X=14,Y=12}}};
fieldNotes.Sites.AddRange(new[]{FieldSite("","valley",12,"ordinary"),FieldSite("storm","dwarf",10,"storm"),FieldSite("snow","valley",11,"snow"),
    FieldSite("stars","sea",12,"stars","Beach"),FieldSite("ship_cache","sea",int.MaxValue,"ship:cache","Beach"),
    new DigSite{EventKind="resident_penny",EventId="resident:penny:cache",Culture="dwarf",Location="Town",Expires=int.MaxValue,Points=new(){new(){X=10,Y=10},new(){X=12,Y=10}}},
    FieldSite("rain","valley",9,"expired"),new DigSite{Culture="valley",Expires=12,EventId="dug",Points=new(){new(){Dug=true}}}});
fieldNotes.Weather.PendingRewards.Add("rain_pitcher");
fieldNotes.Nature.PendingRewards.Add("frozen_leaf");
fieldNotes.Residents.Stages["willy"]=ResidentStage.Reward;
fieldNotes.Residents.Stages["evelyn"]=ResidentStage.Clue;
fieldNotes.Residents.Stages["penny"]=ResidentStage.Searching;
fieldNotes.Shipwreck.Stage=ShipwreckStage.Recovered;
fieldNotes.Workshop.Stage=WorkshopStage.Blueprint;
foreach(string id in MuseumCollections.All[0].Artifacts)fieldNotes.Restored[id]=1;
fieldNotes.Cultures["dwarf"]=new(){Project="rune_tablet",Fragments=13,Keystones=1};
fieldNotes.Cultures["sea"]=new(){Project="clay_whistle",Fragments=20,Keystones=1};
string beforeNotes=JsonSerializer.Serialize(fieldNotes);
var allGoals=FieldJournal.Build(fieldNotes,catalog,10);
Check(JsonSerializer.Serialize(fieldNotes)==beforeNotes,"building a full objective list never changes projects, claims, pins or story stages");
Check(allGoals.Select(p=>p.Id).Distinct().Count()==allGoals.Count,"simultaneous stories, caches, projects and rewards have unique objective identities");
Check(allGoals.Count(p=>p.Kind==ObjectiveKind.Dig)==6 && allGoals.All(p=>p.Id!="dig:expired" && p.Id!="dig:dug"),"all active site kinds appear; expired and exhausted sites are excluded");
Check(allGoals.Where(p=>p.Kind==ObjectiveKind.Dig).All(p=>p.Remaining==2),"site objectives count only undug finds");
Check(allGoals.Count(p=>p.Kind==ObjectiveKind.Reward)==5 && allGoals.Take(5).All(p=>p.Kind==ObjectiveKind.Reward),"banked weather, nature, resident, bell and collection rewards come before other objectives");
Check(allGoals[5].Id=="dig:storm" && allGoals[6].Id=="dig:snow","finite dig sites sort by original expiry, with today's deadline first");
Check(allGoals.Single(p=>p.Id=="dig:ordinary").Tab=="valley" && allGoals.Single(p=>p.Id=="dig:stars").Tab=="overview", "ordinary and star sites have valid journal destinations");
Check(allGoals.Single(p=>p.Id=="dig:resident:penny:cache") is {Tab:"residents",Selection:"penny",Expires:int.MaxValue},"resident cache navigation selects the exact resident and retains an unlimited deadline");
Check(allGoals.Single(p=>p.Id=="dig:snow") is {Tab:"nature",Selection:"snow"} && allGoals.Single(p=>p.Id=="dig:storm").Tab=="weather","special digs open the correct event pages");
Check(allGoals.Single(p=>p.Id=="restore:dwarf:rune_tablet") is {UseKeystone:true,Cost:13,Selection:"rune_tablet"},"restoration objective accounts for one keystone when fragments alone are insufficient");
Check(allGoals.Single(p=>p.Id=="restore:sea:clay_whistle") is {UseKeystone:false,Cost:20},"restoration with enough fragments does not recommend consuming a keystone");
Check(allGoals.Single(p=>p.Id=="reward:harbor_whistle") is {Tab:"residents",Selection:"willy"} && allGoals.Single(p=>p.Id=="reward:frozen_leaf") is {Tab:"nature",Selection:"snow"},"reward navigation selects the matching story or event");
Check(allGoals.Single(p=>p.Id=="collection:valley") is {Tab:"gallery",Selection:"valley",Artifact:"ancient_map"},"completed collection points to its unclaimed furniture");
Check(allGoals.Any(p=>p.Id=="workshop:study" && p.Npc=="Gunther" && p.Location=="ArchaeologyHouse") && allGoals.Any(p=>p.Id=="resident:evelyn:start" && p.Npc=="Evelyn"),"workshop and resident conversations are independent objectives");
fieldNotes.TrackedObjective="dig:storm";
var trackedBefore=FieldJournal.Tracked(fieldNotes,allGoals);
Check(trackedBefore?.Expires==10 && FieldJournal.Tracked(new(),allGoals)==null,"tracking selects the exact goal, with no implicit selection for another player");
FieldJournal.PruneTracking(fieldNotes,allGoals);
Check(fieldNotes.TrackedObjective=="dig:storm","valid tracking survives saves");
fieldNotes=JsonSerializer.Deserialize<ArchaeologyState>(JsonSerializer.Serialize(fieldNotes))!;
Check(FieldJournal.Tracked(fieldNotes,FieldJournal.Build(fieldNotes,catalog,10))?.Id=="dig:storm","tracking survives serialization with its site deadline");
FieldJournal.PruneTracking(fieldNotes,FieldJournal.Build(fieldNotes,catalog,11));
Check(fieldNotes.TrackedObjective=="" && fieldNotes.Weather.PendingRewards.Contains("rain_pitcher"),"expired tracking clears without removing other goals or unclaimed rewards");
fieldNotes.TrackedObjective="dig:ordinary";
var stillDigging=fieldNotes.Sites.Single(p=>p.EventId=="ordinary");
Check(UnusualRelics.Extend(stillDigging,10),"tracked site can be extended");
var extensionGoals=FieldJournal.Build(fieldNotes,catalog,10);
Check(FieldJournal.Tracked(fieldNotes,extensionGoals)?.Expires==13,"hourglass extension updates a tracked site's deadline without losing its identity");
stillDigging.Points[1].Dug=true;
Check(FieldJournal.Tracked(fieldNotes,FieldJournal.Build(fieldNotes,catalog,10))?.Remaining==1,"tracker reflects a partial excavation immediately");
stillDigging.Points[2].Dug=true;
FieldJournal.PruneTracking(fieldNotes,FieldJournal.Build(fieldNotes,catalog,10));
Check(fieldNotes.TrackedObjective=="","fully excavated objective is unpinned without tracking the next arbitrary site");
fieldNotes.TrackedObjective="reward:rain_pitcher";
Check(WeatherExpeditions.RecordClaim(fieldNotes,"rain_pitcher"),"claim a tracked reward");
FieldJournal.PruneTracking(fieldNotes,FieldJournal.Build(fieldNotes,catalog,10));
Check(fieldNotes.TrackedObjective=="" && fieldNotes.Restored.ContainsKey("rain_pitcher"),"claimed reward disappears from objectives and leaves discovery credit intact");
fieldNotes.Cultures["dwarf"].Fragments=12;
Check(FieldJournal.Build(fieldNotes,catalog,10).All(p=>!p.Id.StartsWith("restore:dwarf:")),"insufficient fragments do not produce a ready restoration objective");
fieldNotes.Cultures["dwarf"].Fragments=100;fieldNotes.Cultures["dwarf"].Project="star_glass";
Check(FieldJournal.Build(fieldNotes,catalog,10).All(p=>!p.Id.StartsWith("restore:dwarf:")),"event artifacts cannot masquerade as research projects");
fieldNotes.CollectionRewards.Add("valley");
Check(FieldJournal.Build(fieldNotes,catalog,10).All(p=>p.Id!="collection:valley"),"claimed collection reward is absent even though discovery credit remains");
var reminders=new ArchaeologyState{Workshop=new(){Stage=WorkshopStage.Components,Parts=new(){"spring"}},Shipwreck=new(){Stage=ShipwreckStage.Cache}};
var reminderGoals=FieldJournal.Build(reminders,catalog,50);
Check(reminderGoals.Count(p=>p.Kind==ObjectiveKind.Part)==2 && reminderGoals.Any(p=>p.Topic=="lens" && p.Culture=="sea") && reminderGoals.Any(p=>p.Topic=="core" && p.Culture=="valley"),"workshop lists only missing components and their correct cultures");
Check(reminderGoals.Single(p=>p.Id=="ship:wait-cache").Expires==int.MaxValue,"unplaced story cache keeps a permanent placement reminder");
reminders.Workshop.Stage=WorkshopStage.Ready;
Check(FieldJournal.Build(reminders,catalog,50).Any(p=>p.Kind==ObjectiveKind.Repair && p.Tab=="workshop" && p.Location=="Farm"),"assembled components create a workshop repair objective");
foreach(var shipStage in new[]{ShipwreckStage.Buckle,ShipwreckStage.PortRecord,ShipwreckStage.Letter})
{
    reminders.Shipwreck.Stage=shipStage;
    var talk=FieldJournal.Build(reminders,catalog,50).Single(p=>p.Kind==ObjectiveKind.Talk);
    Check(talk.Tab=="shipwreck" && talk.Npc==(shipStage==ShipwreckStage.Buckle?"Gunther":"Willy"),"each ship conversation links to the right resident");
}
reminders.Shipwreck.Stage=ShipwreckStage.AwaitBottle;
Check(FieldJournal.Build(reminders,catalog,50).Any(p=>p.Id=="ship:wait-bottle" && p.DetailKey=="shipwreck.text.3"),"missing rain window explains waiting rather than inventing a bottle dig");
var leadsOverview=new ArchaeologyState{Weather=new(){Lead=new(){Kind="rain",Day=50,Expires=52}},Stars=new(){Lead=new(){Day=51,Expires=53,Culture="sea",Location="Beach"}},
    Nature=new(){Leads=new(){new(){Kind="snow",Key="nature:snow:1",Day=51,Expires=55},new(){Kind="meteor",Key="nature:meteor:50:10:10",Day=50,Expires=54},new(){Kind="season",Key="nature:season:1:1",Day=45,Expires=49}}}};
var forecastGoals=FieldJournal.Build(leadsOverview,catalog,50);
Check(forecastGoals.Count==4 && forecastGoals.Count(p=>p.DetailKey=="field.detail.tomorrow")==2,"snow and star forecasts distinguish tomorrow from queued placement; expired leads are omitted");
Check(forecastGoals.Single(p=>p.Id=="lead:stars:51") is {Location:"Beach",Culture:"sea",Expires:53},"star forecast objective retains destination and original deadline");
var disabledGoals=FieldJournal.Build(leadsOverview,catalog,50,false,false);
Check(disabledGoals.Count(p=>p.DetailKey=="field.detail.disabled")==3 && disabledGoals.Single(p=>p.Topic=="stars").DetailKey=="field.detail.tomorrow","disabled weather and nature leads remain visible with accurate settings; stars stay independent");
leadsOverview.Sites.Add(FieldSite("rain","valley",52,"active-rain"));
Check(FieldJournal.Build(leadsOverview,catalog,50).All(p=>p.Id!="lead:weather:rain:50"),"an active weather site suppresses its placement reminder");
leadsOverview.Residents.Stages["penny"]=ResidentStage.Searching;
Check(FieldJournal.Build(leadsOverview,catalog,50).Any(p=>p.Id=="resident:penny:wait" && p.Location=="Town" && p.Expires==int.MaxValue),"unplaced resident cache remains an unlimited objective");
var allowedTabs=new[]{"overview","valley","dwarf","sea","workshop","weather","gallery","shipwreck","nature","residents"};
foreach(var goal in allGoals.Concat(reminderGoals).Concat(forecastGoals).Concat(disabledGoals))
{
    Check(allowedTabs.Contains(goal.Tab),"every generated goal has an existing journal destination");
    Check(en.ContainsKey(goal.DetailKey),"every generated goal has bilingual detail text");
}
foreach(string npc in new[]{"Gunther","Robin","Willy","Penny","Evelyn"})Check(en.ContainsKey("field.npc."+npc),"NPC objective name has localized fallback");
foreach(string? pin in new string?[]{null,new string('x',2049)})
{
    bool rejected=false;
    try{Research.ValidateState(new ArchaeologyState{TrackedObjective=pin!});}catch(InvalidDataException){rejected=true;}
    Check(rejected,"invalid tracking data is rejected while preserving the original save");
}
var staleTracking=new ArchaeologyState{TrackedObjective="missing-goal"};
FieldJournal.PruneTracking(staleTracking,FieldJournal.Build(staleTracking,catalog,10));
Check(staleTracking.TrackedObjective=="" && staleTracking.Experience==0,"stale goal identity is safely cleared without changing progress");
foreach(var origin in new[]{("lead:weather:rain:50","rain","weather:rain:50"),("lead:nature:snow:1","snow","nature:snow:1"),("lead:stars:51","stars","stars:51"),
    ("resident:penny:wait","resident_penny","resident:penny:cache"),("ship:wait-cache","ship_cache","ship:cache"),("ship:wait-bottle","ship_bottle","ship:bottle:51")})
{
    var openedLead=new ArchaeologyState{TrackedObjective=origin.Item1,Sites=new(){FieldSite(origin.Item2,"valley",53,origin.Item3)}};
    FieldJournal.PruneTracking(openedLead,FieldJournal.Build(openedLead,catalog,51));
    Check(openedLead.TrackedObjective=="dig:"+origin.Item3,"a tracked lead follows its exact newly placed excavation");
}
foreach(int viewportHeight in new[]{640,720,900})
foreach(int count in new[]{0,1,3,4,5,7,9,12,33,80})
{
    int panelHeight=Math.Min(760,viewportHeight-40)-228;
    var layout=ObjectiveListLayout.Create(panelHeight,count);
    int visible=Math.Min(layout.PageSize,count);
    Check(layout.PageSize>=1 && layout.RowHeight>=48,"objective rows stay readable at supported compact and full heights");
    Check(layout.FirstRowY+visible*layout.RowHeight<=layout.PageControlsY,"long objective lists cannot overlap pagination controls");
    Check(layout.PageControlsY+30<layout.ToolsY && layout.ToolsY+36<=panelHeight,"page buttons and tool collection stay separate and inside the sidebar");
    int pages=Math.Max(1,(count+layout.PageSize-1)/layout.PageSize);
    Check(Enumerable.Range(0,pages).SelectMany(p=>Enumerable.Range(p*layout.PageSize,Math.Min(layout.PageSize,Math.Max(0,count-p*layout.PageSize)))).SequenceEqual(Enumerable.Range(0,count)),"every objective is reachable exactly once across pages at each supported height");
}
Console.WriteLine($"PASS: {checks} checks. Progression, objectives, navigation, compact layouts, deadlines, tracking, stories, rewards, migration and translations.");
