using System.Text.Json;
using Archeo.Core;

internal static class FrontierChecks
{
    internal static void Run(Action<bool,string> check,List<ArtifactDefinition> catalog,Research research)
    {
        var state=new ArchaeologyState();
        FrontierResearch.Observe(state,true,true);
        Journey.Initialize(state,catalog);
        check(!state.Journey.Started && state.Journey.Chapters.Count==0,"region access alone does not skip Gunther's invitation");
        state.Journey.Started=true; Journey.Refresh(state,catalog);
        check(!Journey.IsOpen(state,"frontiers") && !Journey.IsOpen(state,"calico") && !Journey.IsOpen(state,"island"),"late research waits for the first two restorations even with both routes open");
        var valley=research.Progress(state,"valley");valley.Fragments=200;
        research.Restore(state,"valley",false,new Random(5),out _);Journey.Refresh(state,catalog);
        check(!Journey.IsOpen(state,"calico"),"one restoration does not open frontier research");
        check(OpeningStory.Conclude(state,true),"frontier progression follows the first story's conclusion"); Journey.Refresh(state,catalog);
        research.Restore(state,"valley",false,new Random(6),out _);Journey.Refresh(state,catalog);
        check(!Journey.IsOpen(state,"sea") && FrontierResearch.Cultures.All(c=>!Journey.IsOpen(state,c)),"two restorations and working transport do not skip route stories");
        research.Progress(state,"dwarf").Fragments=20;research.Restore(state,"dwarf",false,new Random(7),out _);Journey.Refresh(state,catalog);
        RouteStories.Explain(state,"sea",true);Journey.Refresh(state,catalog);
        research.Progress(state,"sea").Fragments=20;research.Restore(state,"sea",false,new Random(8),out _);Journey.Refresh(state,catalog);
        check(RouteStories.Explain(state,"calico",true),"Gunther explains a desert route after a coastal find");Journey.Refresh(state,catalog);
        state.Restored["sailor_buckle"]=1;Journey.Refresh(state,catalog);
        check(RouteStories.Explain(state,"island",true),"Willy explains the island route after the marked buckle");Journey.Refresh(state,catalog);
        check(FrontierResearch.Cultures.All(c=>Journey.IsOpen(state,c)) && Journey.IsOpen(state,"frontiers"),"distant fields require both real roads and explained finds");
        FrontierResearch.Observe(state,false,false);
        check(state.Frontiers.DesertAccess && state.Frontiers.IslandAccess,"observed routes never relock after a warp or reload");

        foreach(string region in FrontierResearch.Cultures)
        {
            var explorer=new ArchaeologyState{Journey=new(){Initialized=true,Started=true,Chapters=new(){"valley","sea"}}};
            FrontierResearch.Observe(explorer,region=="calico",region=="island");
            check(Journey.IsOpen(explorer,region) && !Journey.IsOpen(explorer,region=="calico"?"island":"calico"),"bus and boat access unlock independently: "+region);
            research.Project(explorer,region,new Random(1));
            check(catalog.Count(a=>a.Culture==region && Journey.VisibleArtifact(explorer,a))==1,"new culture exposes one initial project without future silhouettes: "+region);
            var pool=catalog.Where(a=>a.Culture==region).ToList();
            check(pool.Count==5 && pool.All(a=>a.Source=="research") && pool.Count(a=>FrontierResearch.RelicEffects.Contains(a.Effect))==2,"frontier pool has five research finds and two functional relics: "+region);
            var collection=MuseumCollections.All.Single(c=>c.Id==region);
            check(!FrontierResearch.CollectionVisible(explorer,collection),"collection is hidden until its first real restoration: "+region);
            var progress=research.Progress(explorer,region);progress.Fragments=5000;
            research.Restore(explorer,region,false,new Random(2),out _);
            check(FrontierResearch.CollectionVisible(explorer,collection),"first restored collection piece reveals its museum page: "+region);
            check(!MuseumCollections.RecordClaim(explorer,region,true),"incomplete frontier collection cannot award furniture: "+region);
            for(int i=0;i<70;i++) research.Restore(explorer,region,false,new Random(i+3),out _);
            check(pool.All(a=>explorer.Restored.ContainsKey(a.Id)),"all five frontier finds are reachable: "+region);
            check(!MuseumCollections.RecordClaim(explorer,region,false) && !explorer.CollectionRewards.Contains(region),"full inventory preserves the frontier reward: "+region);
            check(MuseumCollections.RecordClaim(explorer,region,true) && !MuseumCollections.RecordClaim(explorer,region,true),"frontier furniture can be claimed exactly once: "+region);
            foreach(string other in Research.Cultures.Where(c=>c!=region))
                check(!explorer.Cultures.ContainsKey(other),"frontier restoration never spends another culture's fragments: "+region+"/"+other);
            explorer.LastUse[pool.First(a=>a.Effect!="").Id]=32;
            var copy=JsonSerializer.Deserialize<ArchaeologyState>(JsonSerializer.Serialize(explorer))!;
            Research.ValidateState(copy);
            check(JsonSerializer.Serialize(copy)==JsonSerializer.Serialize(explorer),"frontier discoveries, currency, cooldown and collection survive reload: "+region);
        }
        var legacy=JsonSerializer.Deserialize<ArchaeologyState>("{\"Version\":1,\"Finds\":12,\"Cultures\":{\"sea\":{\"Fragments\":18,\"Solves\":3}},\"Restored\":{\"clay_whistle\":1},\"CollectionRewards\":[\"valley\"]}")!;
        Journey.Initialize(legacy,catalog);Research.ValidateState(legacy);
        check(legacy.Frontiers!=null && !legacy.Frontiers.DesertAccess && !legacy.Frontiers.IslandAccess && legacy.Cultures["sea"].Fragments==18 && legacy.CollectionRewards.Contains("valley"),"1.1 saves gain default route records without losing old progress");
        check(!Journey.IsOpen(legacy,"calico") && !Journey.IsOpen(legacy,"island"),"legacy projects do not fabricate access to a late region");
        check(new ArchaeologyState().Frontiers!=state.Frontiers,"farmhands keep independent archaeology route records");
        bool rejected=false;
        try{Research.ValidateState(new(){Frontiers=null!});}catch(InvalidDataException){rejected=true;}
        check(rejected,"explicitly damaged frontier state is rejected without resetting the save");
        check(FrontierResearch.SpotCulture("Desert",true,false,"valley")=="calico" && FrontierResearch.SpotCulture("IslandEast",false,true,"sea")=="island","vanilla artifact spots award the region's own fragments");
        check(FrontierResearch.SpotCulture("Desert",false,false,"valley")=="valley" && FrontierResearch.SpotCulture("Forest",true,true,"valley")=="valley","locked routes and unrelated maps retain ordinary spot routing");
        check(!FrontierResearch.CanUseSunstep(false,1200) && !FrontierResearch.CanUseSunstep(true,1800) && !FrontierResearch.CanUseSunstep(true,2300),"sun dial rejects indoor and night use before a charge is committed");
        check(FrontierResearch.CanUseSunstep(true,600) && FrontierResearch.CanUseSunstep(true,1750),"sun dial works throughout daylight");
        check(!FrontierResearch.CanUseOasis(270,270,100,100) && FrontierResearch.CanUseOasis(269,270,100,100) && FrontierResearch.CanUseOasis(270,270,99,100),"oasis flask only consumes a charge when energy or health can increase");
        check(MuseumCollections.All.Take(3).Select(c=>c.Reward).SequenceEqual(new[]{"ancient_map","mechanical_clock","sea_mosaic"}),"existing museum reward IDs remain stable");
        foreach(int height in new[]{390,412,492,672})
        {
            var layout=CollectionListLayout.Create(height);
            check(layout.FirstRowY+layout.PageSize*layout.RowHeight<=layout.PageControlsY,"collection rows fit above pagination: "+height);
            check(layout.PageControlsY+28<=height-115,"pagination clears the crafting notes: "+height);
            var visited=Enumerable.Range(0,(5+layout.PageSize-1)/layout.PageSize).SelectMany(page=>MuseumCollections.All.Skip(page*layout.PageSize).Take(layout.PageSize)).ToList();
            check(visited.Count==5 && visited.Distinct().Count()==5,"every collection is reachable on compact and large book pages: "+height);
        }
    }
}
