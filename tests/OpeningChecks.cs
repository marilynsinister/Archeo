using System.Text.Json;
using Archeo.Core;

internal static class OpeningChecks
{
    internal static void Run(Action<bool,string> check, List<ArtifactDefinition> catalog, Research research)
    {
        var state = new ArchaeologyState();
        Journey.Initialize(state,catalog);
        check(state.Opening.Stage==OpeningStage.Invitation && !OpeningStory.Active(state),"new story waits for the field kit");
        check(!OpeningStory.Conclude(state,true),"museum visit cannot skip the first excavation");
        state.Journey.Started=true; Journey.Refresh(state,catalog);
        var project=research.Project(state,"valley",new Random(4));
        check(project.Id==OpeningStory.Artifact && state.Cultures["valley"].Project==project.Id,"first cup is a visible stable project");
        var site=new DigSite{Culture="valley",Location="Forest",EventId=OpeningStory.SiteKey,Expires=int.MaxValue,
            Points=new(){new(){X=10,Y=10},new(){X=12,Y=10},new(){X=14,Y=10},new(){X=16,Y=10}}};
        state.Sites.Add(site);
        var goals=FieldJournal.Build(state,catalog,1);
        check(goals.Count==1 && goals[0].Kind==ObjectiveKind.Dig && goals[0].Expires==int.MaxValue,"opening presents one dig without a deadline");
        state.TrackedObjective=goals[0].Id;
        state.Cultures["valley"].Fragments=20;
        goals=FieldJournal.Build(state,catalog,500);
        FieldJournal.PruneTracking(state,goals);
        check(goals.Count==1 && goals[0].Id=="opening:restore" && state.TrackedObjective==goals[0].Id,"the first tracked dig becomes restoration even after a long break");
        var snapshot=JsonSerializer.Serialize(state);
        FieldJournal.Build(state,catalog,500);
        check(snapshot==JsonSerializer.Serialize(state),"reading the first objective never advances its story");
        research.Progress(state,"dwarf").Fragments=100;
        snapshot=JsonSerializer.Serialize(state);
        check(!research.Restore(state,"dwarf",false,new Random(5),out _) && snapshot==JsonSerializer.Serialize(state),"another culture cannot bypass the first story or mutate its resources and projects");
        check(research.Restore(state,"valley",false,new Random(5),out var cup) && cup.Id==OpeningStory.Artifact,"first restoration creates the expected cup");
        check(state.Opening.Stage==OpeningStage.Restored && state.Cultures["valley"].Project=="","next project waits for the conversation");
        goals=FieldJournal.Build(state,catalog,501); FieldJournal.PruneTracking(state,goals);
        check(goals.Count==1 && goals[0].Id=="opening:talk" && state.TrackedObjective=="opening:talk","restoration hands the tracked story to Gunther");
        state.Cultures["valley"].Fragments=50;
        snapshot=JsonSerializer.Serialize(state);
        check(!research.Restore(state,"valley",false,new Random(6),out _) && snapshot==JsonSerializer.Serialize(state),"repeated restoration cannot spend fragments or reroll while the story awaits Gunther");
        check(research.Project(state,"valley",new Random(7)).Id==cup.Id && state.Cultures["valley"].Project=="","reopening the catalog does not reveal a second project");
        check(!OpeningStory.Conclude(state,false) && state.Opening.Stage==OpeningStage.Restored,"concluding the story requires the museum");
        var copy=JsonSerializer.Deserialize<ArchaeologyState>(JsonSerializer.Serialize(state))!;
        Journey.Initialize(copy,catalog); Research.ValidateState(copy);
        check(copy.Opening.Stage==OpeningStage.Restored && FieldJournal.Build(copy,catalog,600).Single().Id=="opening:talk","an unfinished conversation survives save and load");
        int experience=copy.Experience,fragments=copy.Cultures["valley"].Fragments;
        check(OpeningStory.Conclude(copy,true),"recorded notes conclude the story without inspecting the inventory");
        Journey.Refresh(copy,catalog);
        check(Journey.IsOpen(copy,"dwarf") && !Journey.IsOpen(copy,"sea"),"the conversation explains exactly one new field");
        check(copy.Restored[cup.Id]==1 && copy.Donated.Count==0 && copy.Experience==experience && copy.Cultures["valley"].Fragments==fragments,"conversation keeps the cup and consumes or awards no extra resources");
        snapshot=JsonSerializer.Serialize(copy);
        check(!OpeningStory.Conclude(copy,true) && snapshot==JsonSerializer.Serialize(copy),"rereading cannot repeat progression or rewards");
        check(research.Project(copy,"valley",new Random(8)).Id!=cup.Id,"ordinary research resumes after the first story");
        var complete=JsonSerializer.Deserialize<ArchaeologyState>(JsonSerializer.Serialize(copy))!;
        Journey.Initialize(complete,catalog);
        check(complete.Opening.Stage==OpeningStage.Complete && Journey.IsOpen(complete,"opening"),"completed story stays readable after reload");
        foreach(var json in new[]{"{\"Finds\":1}","{\"Restored\":{\"settler_cup\":1}}","{\"Journey\":{\"Initialized\":true,\"Started\":true,\"Chapters\":[\"valley\",\"sea\"]}}"})
        {
            var legacy=JsonSerializer.Deserialize<ArchaeologyState>(json)!;
            var previous=legacy.Journey.Chapters.ToHashSet();
            Journey.Initialize(legacy,catalog);
            check(legacy.Opening.Stage==OpeningStage.Legacy && !OpeningStory.Active(legacy) && !Journey.IsOpen(legacy,"opening") && previous.IsSubsetOf(legacy.Journey.Chapters),"existing research keeps its chapters without receiving a forced introductory story");
        }
        state.TrackedObjective=""; FieldJournal.PruneTracking(state,FieldJournal.Build(state,catalog,900));
        check(state.TrackedObjective=="","the introductory story respects an explicitly unpinned objective");
        check(!OpeningStory.IsValid(null) && !OpeningStory.IsValid(new(){Stage=(OpeningStage)99}),"invalid first-story data is rejected");
    }
}
