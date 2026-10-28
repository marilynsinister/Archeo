using System.Text.Json;
using Archeo.Core;

internal static class RouteChecks
{
    internal static void Run(Action<bool,string> check,List<ArtifactDefinition> catalog,Research research)
    {
        var state=new ArchaeologyState();Journey.Initialize(state,catalog);
        check(state.Routes.Initialized && state.Routes.Clues.Count==0 && state.Routes.Explained.Count==0,"new archaeology initializes route stories without future clues");
        state.Journey.Started=true;Journey.Refresh(state,catalog);
        state.Restored[OpeningStory.Artifact]=1;OpeningStory.Restored(state,OpeningStory.Artifact);OpeningStory.Conclude(state,true);Journey.Refresh(state,catalog);
        FrontierResearch.Observe(state,true,true);Journey.Refresh(state,catalog);
        state.Cultures["sea"]=new(){Fragments=300,Solves=20,Project="clay_whistle"};Journey.Refresh(state,catalog);
        check(!Journey.IsOpen(state,"sea") && !Journey.IsOpen(state,"frontiers") && RouteStories.Pending(state)==null,"fragment balances, solve counts and repaired roads never invent route clues");
        var snapshot=JsonSerializer.Serialize(state);
        check(!research.Restore(state,"sea",false,new Random(1),out _) && snapshot==JsonSerializer.Serialize(state),"unexplained field cannot restore projects or spend its stored fragments");
        // Use the real reward definition for a shore event, independently of field access.
        var reward=catalog.First(a=>a.Source!="research"&&a.Culture=="sea");
        if(WeatherExpeditions.Kinds.Contains(reward.Source))state.Weather.PendingRewards.Add(reward.Id);
        else if(NaturalEvents.Kinds.Contains(reward.Source))state.Nature.PendingRewards.Add(reward.Id);
        else state.Residents.Stages[reward.Source]=ResidentStage.Reward;
        state.Sites.Add(new(){Culture="sea",Location="Beach",EventKind="season",EventId="qa:coast",Expires=10,Points=new(){new(){X=10,Y=10}}});
        Journey.Refresh(state,catalog);
        check(!Journey.IsOpen(state,"sea") && Journey.VisibleArtifact(state,reward) && Journey.VisibleSite(state,state.Sites[0]),"earned event rewards and special sites stay visible without bypassing field conversations");
        check(!FieldJournal.Build(state,catalog,1).Any(p=>p.Kind==ObjectiveKind.Restore&&p.Culture=="sea"),"journal does not offer restoration in an unexplained field");
        state.Restored["brass_gear"]=1;Journey.Refresh(state,catalog);
        check(RouteStories.Pending(state)?.Id=="sea" && RouteStories.Visible(state).Select(p=>p.Id).SequenceEqual(new[]{"sea"}),"first route entry names a real restored gear and hides future regions");
        var goal=FieldJournal.Build(state,catalog,1).Single(p=>p.Topic=="route:sea");
        check(goal.Artifact=="brass_gear" && goal.Npc=="Willy" && goal.Expires==int.MaxValue,"route objective records the actual source and partner without a deadline");
        snapshot=JsonSerializer.Serialize(state);RouteStories.Visible(state);RouteStories.Pending(state);FieldJournal.Build(state,catalog,1);
        check(snapshot==JsonSerializer.Serialize(state),"reading route notes never reveals or completes another route");
        check(!RouteStories.Explain(state,"sea",false) && !RouteStories.Explain(state,"island",true),"distance and unrelated route attempts cannot skip the current conversation");
        check(RouteStories.Explain(state,"sea",true),"recorded gear sketch unlocks the coast through its conversation");Journey.Refresh(state,catalog);
        check(Journey.IsOpen(state,"sea") && !Journey.IsOpen(state,"frontiers"),"coast conversation opens one field without listing distant regions");
        state.Restored["clay_whistle"]=1;state.Restored["sailor_buckle"]=1;Journey.Refresh(state,catalog);
        check(state.Routes.Clues.Keys.ToHashSet().SetEquals(new[]{"sea","calico","island"}) && RouteStories.Pending(state)?.Id=="calico","simultaneous evidence is saved while only one new route is introduced");
        check(RouteStories.Visible(state).Select(p=>p.Id).SequenceEqual(new[]{"sea","calico"}),"unintroduced island clue is hidden behind the current conversation");
        state.TrackedObjective="route:calico";
        int xp=state.Experience;var restored=JsonSerializer.Serialize(state.Restored);var donated=JsonSerializer.Serialize(state.Donated);
        check(RouteStories.Explain(state,"calico",true),"map comparison explains the available desert route");Journey.Refresh(state,catalog);
        state.Sites.Add(new(){Culture="calico",Location="Desert",EventId="normal-desert",Expires=15,Points=new(){new(){X=8,Y=8}}});
        FieldJournal.PruneTracking(state,FieldJournal.Build(state,catalog,1));
        check(state.TrackedObjective=="dig:normal-desert" && state.Routes.Explained.Contains("calico"),"tracked route conversation continues with its own field site");
        check(RouteStories.Pending(state)?.Id=="island" && Journey.IsOpen(state,"calico") && !Journey.IsOpen(state,"island"),"next clue appears after the first without opening another region");
        check(RouteStories.Explain(state,"island",true),"buckle and coastal records explain the island route");Journey.Refresh(state,catalog);
        check(state.Experience==xp && restored==JsonSerializer.Serialize(state.Restored) && donated==JsonSerializer.Serialize(state.Donated),"route explanations consume no finds and grant no duplicate rewards");
        snapshot=JsonSerializer.Serialize(state);
        check(!RouteStories.Explain(state,"sea",true) && !RouteStories.Explain(state,"calico",true) && snapshot==JsonSerializer.Serialize(state),"recorded conversations cannot repeat progression");
        var copy=JsonSerializer.Deserialize<ArchaeologyState>(snapshot)!;Journey.Initialize(copy,catalog);RouteStories.Initialize(copy);Research.ValidateState(copy);
        check(copy.Routes.Clues.Count==3 && copy.Routes.Explained.SetEquals(new[]{"sea","calico","island"}),"sources, archive and explained routes survive save and load");
        foreach(var id in new[]{"calico","island"})
        {
            var isolated=new ArchaeologyState();Journey.Initialize(isolated,catalog);isolated.Journey.Started=true;isolated.Opening.Stage=OpeningStage.Complete;
            isolated.Restored[OpeningStory.Artifact]=1;isolated.Routes.Explained.Add("sea");isolated.Journey.Chapters.UnionWith(new[]{"valley","dwarf","sea"});
            var story=RouteStories.All.Single(p=>p.Id==id);isolated.Restored[story.Trigger]=1;
            Journey.Refresh(isolated,catalog);
            check(RouteStories.Pending(isolated)==null,"a restored clue cannot invent unavailable transport: "+id);
            FrontierResearch.Observe(isolated,id=="calico",id=="island");Journey.Refresh(isolated,catalog);
            check(RouteStories.Pending(isolated)?.Id==id && RouteStories.Explain(isolated,id,true),"each distant route can be introduced independently: "+id);
            Journey.Refresh(isolated,catalog);
            check(Journey.IsOpen(isolated,id) && !Journey.IsOpen(isolated,id=="calico"?"island":"calico"),"an explained route never grants the other destination: "+id);
        }
        var legacy=JsonSerializer.Deserialize<ArchaeologyState>("{\"Journey\":{\"Initialized\":true,\"Started\":true,\"Chapters\":[\"valley\",\"dwarf\",\"sea\",\"frontiers\"]},\"Restored\":{\"settler_cup\":1},\"Opening\":{\"Stage\":4}}")!;
        Journey.Initialize(legacy,catalog);FrontierResearch.Observe(legacy,true,true);RouteStories.Initialize(legacy);Journey.Refresh(legacy,catalog);
        check(legacy.Routes.Explained.SetEquals(new[]{"sea","calico","island"}) && legacy.Routes.LegacyFrontierIndex && RouteStories.Pending(legacy)==null,"upgrade honors existing fields and available world routes without a backlog of conversations");
        check(RouteStories.Visible(legacy).Count==0 && !Journey.IsOpen(legacy,"routes"),"migration does not invent memories of conversations never played");
        foreach(var bad in new RouteProgress?[]{null,new(){Clues=null!},new(){Explained=null!},new(){Initialized=true,Clues=new(){["sea"]="sailor_buckle"}},new(){Initialized=true,Explained=new(){"unknown"}},new(){Clues=new(){["sea"]="brass_gear"}}})
            check(!RouteStories.IsValid(bad),"damaged route state is rejected for data preservation");
    }
}
