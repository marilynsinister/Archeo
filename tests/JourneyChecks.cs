using System.Text.Json;
using Archeo.Core;

internal static class JourneyChecks
{
    internal static void Run(Action<bool,string> check,List<ArtifactDefinition> catalog,Research research)
    {
        var state=new ArchaeologyState();
        // Legacy versions rolled three projects and sites immediately. Empty rolls do not count as discoveries.
        foreach(string culture in Research.Cultures) research.Project(state,culture,new Random(2));
        foreach(string culture in Research.Cultures) state.Sites.Add(new(){Culture=culture,Location="Forest",Expires=4,Points=new(){new(){X=10,Y=12}}});
        Journey.Initialize(state,catalog);
        check(!state.Journey.Started && state.Journey.Chapters.Count==0,"empty pre-1.1.1 projects and sites do not skip Gunther's invitation");
        check(catalog.All(p=>!Journey.VisibleArtifact(state,p)),"opening invitation reveals no future artifact or reward");
        state.Journey.Started=true; Journey.Refresh(state,catalog);
        check(state.Journey.Chapters.SetEquals(new[]{"valley","opening"}),"receiving the kit opens valley research and its first story");
        check(catalog.Count(p=>Journey.VisibleArtifact(state,p))==1,"first book contains one current project instead of thirty-three silhouettes");
        check(state.Sites.Count(p=>Journey.VisibleSite(state,p))==1,"old rolled sites stay stored but only the discovered culture is active");
        var valley=research.Progress(state,"valley"); valley.Fragments=100;
        research.Restore(state,"valley",false,new Random(3),out var first);
        Journey.Refresh(state,catalog);
        check(!Journey.IsOpen(state,"dwarf") && !Journey.IsOpen(state,"sea"),"first restoration waits for its concluding conversation");
        check(OpeningStory.Conclude(state,true),"Gunther concludes the first cup's story"); Journey.Refresh(state,catalog);
        check(Journey.IsOpen(state,"dwarf") && !Journey.IsOpen(state,"sea"),"the explained clue opens one new research field");
        check(!Journey.IsOpen(state,"workshop") && !Journey.IsOpen(state,"shipwreck") && !Journey.IsOpen(state,"residents"),"restoration does not automatically expose unrelated stories");
        research.Restore(state,"valley",false,new Random(4),out _); Journey.Refresh(state,catalog);
        check(!Journey.IsOpen(state,"sea"),"a restoration count alone does not open shore research");
        research.Progress(state,"dwarf").Fragments=20;
        research.Restore(state,"dwarf",false,new Random(5),out _); Journey.Refresh(state,catalog);
        check(RouteStories.Pending(state)?.Id=="sea" && !Journey.IsOpen(state,"sea"),"restored lifting gear records a real coastal clue");
        check(RouteStories.Explain(state,"sea",true),"Willy explains the coastal connection"); Journey.Refresh(state,catalog);
        check(Journey.IsOpen(state,"sea"),"shore research follows its conversation");
        state.Workshop.Stage=WorkshopStage.Blueprint; Journey.Refresh(state,catalog);
        check(Journey.IsOpen(state,"workshop"),"workshop chapter requires a discovered blueprint");
        state.Residents.Stages["penny"]=ResidentStage.Clue; Journey.Refresh(state,catalog);
        check(Journey.IsOpen(state,"residents"),"residents appear with their first real clue");
        var open=state.Journey.Chapters.ToHashSet(); state.Workshop.Stage=WorkshopStage.Complete; state.Workshop.Parts.UnionWith(Workshop.Parts); Journey.Refresh(state,catalog);
        check(open.IsSubsetOf(state.Journey.Chapters),"discovered chapters never disappear as stories advance");
        foreach(string oldJson in new[]{
            "{\"Version\":1,\"Finds\":3,\"Cultures\":{\"sea\":{\"Fragments\":12}}}",
            "{\"Version\":1,\"Restored\":{\"sailor_buckle\":1},\"Experience\":180}",
            "{\"Version\":1,\"Nature\":{\"PendingRewards\":[\"star_glass\"]}}"})
        {
            var old=JsonSerializer.Deserialize<ArchaeologyState>(oldJson)!;
            var restored=JsonSerializer.Serialize(old.Restored); var fragments=JsonSerializer.Serialize(old.Cultures);
            Research.ValidateState(old); Journey.Initialize(old,catalog); Research.ValidateState(old);
            check(old.Journey.Started && restored==JsonSerializer.Serialize(old.Restored) && fragments==JsonSerializer.Serialize(old.Cultures),"legacy finds and queued rewards resume without resetting progress");
            var copy=JsonSerializer.Deserialize<ArchaeologyState>(JsonSerializer.Serialize(old))!;
            check(copy.Journey.Chapters.SetEquals(old.Journey.Chapters),"discovery unlocks survive reload");
        }
        var rewarded=new ArchaeologyState(); rewarded.Nature.PendingRewards.Add("star_glass"); Journey.Initialize(rewarded,catalog);
        check(Journey.VisibleArtifact(rewarded,catalog.Single(p=>p.Id=="star_glass")),"old queued reward stays visible even when its culture had no fragments");
        var known=new ArchaeologyState(); Journey.Initialize(known,catalog,true);
        check(known.Journey.Started,"an existing field instrument preserves access when updating");
        known.Experience=1280;
        check(!Journey.CanUpgrade(known,false,99,9999) && !Journey.CanUpgrade(known,true,4,9999) && !Journey.CanUpgrade(known,true,5,249),"instrument improvement requires museum, materials and gold");
        check(Journey.CanUpgrade(known,true,5,250),"first improvement becomes available with the correct resources");
        known.Journey.InstrumentLevel=2;
        check(Journey.CanUpgrade(known,true,5,1000) && Journey.RevealRadius(known)==3,"second tier increases detection without changing ordinary tools");
        known.Journey.InstrumentLevel=3;
        check(!Journey.CanUpgrade(known,true,99,9999) && Journey.RevealRadius(known)==3.5f,"improvement caps at three tiers");
        Mastery.Choose(known,5,"search"); check(Journey.RevealRadius(known)==4.25f,"Seeker mastery stacks with instrument improvements");
        check(!Journey.IsValid(new(){InstrumentLevel=0}) && !Journey.IsValid(new(){InstrumentLevel=4}) && !Journey.IsValid(new(){Chapters=new(){"unrecognized"}}),"invalid new save fields are rejected for data preservation");
        var revealSave=new ArchaeologyState{Sites=new(){new(){Culture="valley",Location="Forest",Expires=9,Points=new(){new(){X=12,Y=13,Revealed=true}}}}};
        var revealed=JsonSerializer.Deserialize<ArchaeologyState>(JsonSerializer.Serialize(revealSave))!;
        check(revealed.Sites[0].Points[0].Revealed && !revealed.Sites[0].Points[0].Dug,"a discovered tile persists independently of the temporary arrow");

        var land=new HashSet<GridPoint>();
        for(int x=0;x<9;x++)for(int y=0;y<7;y++) if(x!=4 || y==0)land.Add(new(x,y));
        var start=new GridPoint(1,4); var goal=new GridPoint(7,4);
        var route=new FieldRoute(start,land.Contains); var path=route.PathTo(goal);
        check(path.Count>0 && path.Any(p=>p==new GridPoint(4,0)),"search routes around water through the only dry crossing");
        check(path.All(land.Contains),"every route tile is traversable");
        for(int i=1;i<path.Count;i++)check(Math.Abs(path[i].X-path[i-1].X)+Math.Abs(path[i].Y-path[i-1].Y)==1,"route never cuts diagonally across blocked corners");
        var waypoint=FieldRoute.Waypoint(path);
        check(waypoint.X==start.X && waypoint.Y<start.Y,"arrow points to the safe first turn instead of straight through the river");
        land.Remove(new(4,0)); route=new FieldRoute(start,land.Contains);
        check(!route.Contains(goal) && route.PathTo(goal).Count==0,"unreachable land cannot receive a survey arrow");
        var bounded=new FieldRoute(start,_=>true,20);
        check(bounded.Reachable.Count()==20 && !bounded.Contains(new(900,900)),"search is bounded even on malformed or unbounded maps");
        check(FieldRoute.Waypoint(new[]{start})==start && FieldRoute.Waypoint(new[]{start,new GridPoint(1,3)})==new GridPoint(1,3),"route handles standing on and beside a find");
    }
}
