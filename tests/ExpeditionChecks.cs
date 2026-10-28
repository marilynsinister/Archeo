using System.Text.Json;
using Archeo.Core;

internal static class ExpeditionChecks
{
    internal static void Run(Action<bool,string> check,List<ArtifactDefinition> catalog)
    {
        var fresh=new ArchaeologyState();check(PuzzleExpeditions.Discover(fresh).Count==0,"new game reveals no late expeditions");
        fresh.Journey.Started=true;fresh.Residents.Stages["penny"]=ResidentStage.Complete;
        check(PuzzleExpeditions.Discover(fresh).Count==0,"Penny's sequel waits for the restored resonant lens");
        fresh.Restored["resonant_lens"]=1;check(PuzzleExpeditions.Discover(fresh).SequenceEqual(new[]{"river"}),"finished primer and lens reveal river expedition only");
        fresh.Residents.Stages["willy"]=ResidentStage.Complete;fresh.Restored["island_spindle"]=1;
        check(PuzzleExpeditions.Discover(fresh).Count==0,"island story never opens before its real route");
        fresh.Journey.Chapters.Add("island");check(PuzzleExpeditions.Discover(fresh).SequenceEqual(new[]{"tide"}),"harbour story and island craft reveal the second expedition");
        check(PuzzleExpeditions.Discover(fresh).Count==0,"discovery is idempotent");Journey.Refresh(fresh,catalog);
        check(Journey.IsOpen(fresh,"expeditions"),"real discovery opens one research chapter");
        foreach(var story in PuzzleExpeditions.All)
        {
            var state=JsonSerializer.Deserialize<ArchaeologyState>(JsonSerializer.Serialize(fresh))!;var progress=state.Expeditions;
            string Snapshot()=>JsonSerializer.Serialize(state);
            void Valid(){Research.ValidateState(state);check(true,"valid expedition state at every transition: "+story.Id);}
            check(!PuzzleExpeditions.Talk(progress,story.Id,false)&&PuzzleExpeditions.Talk(progress,story.Id,true),"opening conversation requires the real resident: "+story.Id);Valid();
            int xp=state.Experience,marks=state.Museum.Marks;
            string before=Snapshot();check(!PuzzleExpeditions.ChooseLocation(progress,story.Id,"invalid")&&Snapshot()==before,"wrong map choice is free and does not advance: "+story.Id);
            check(PuzzleExpeditions.RevealHint(progress,story.Id)==1&&PuzzleExpeditions.RevealHint(progress,story.Id)==2&&PuzzleExpeditions.RevealHint(progress,story.Id)==2,"hints reveal gradually and stop at a direct answer: "+story.Id);
            check(PuzzleExpeditions.ChooseLocation(progress,story.Id,story.First)&&!PuzzleExpeditions.ChooseLocation(progress,story.Id,story.First),"correct first deduction opens a cache once: "+story.Id);Valid();
            DigSite Site(int leg)=>new(){Culture=story.Culture,Location=leg==1?story.First:story.Second,EventKind=story.Kind(leg),EventId=story.Key(leg),Expires=int.MaxValue,Points=new(){new(){X=10,Y=11},new(){X=13,Y=11}}};
            for(int leg=1;leg<=2;leg++)
            {
                var cache=Site(leg);check(PuzzleExpeditions.ValidSite(cache),"permanent expedition cache validates: "+story.Id+"/"+leg);
                check(!PuzzleExpeditions.CompleteSite(progress,cache),"unexcavated cache awards no parts: "+story.Id);
                cache.Points[0].Dug=true;check(!PuzzleExpeditions.CompleteSite(progress,cache),"partial cache cannot skip remaining excavation: "+story.Id);
                cache.Points[1].Dug=true;cache.Expires=99;check(!PuzzleExpeditions.CompleteSite(progress,cache),"a timed forgery cannot stand in for a permanent cache");cache.Expires=int.MaxValue;
                cache.Location="wrong";check(!PuzzleExpeditions.CompleteSite(progress,cache),"cache requires its actual map");cache.Location=leg==1?story.First:story.Second;
                cache.EventId="wrong";check(!PuzzleExpeditions.ValidSite(cache),"cache identity is checked");cache.EventId=story.Key(leg);
                check(PuzzleExpeditions.CompleteSite(progress,cache)&&!PuzzleExpeditions.CompleteSite(progress,cache),"each complete cache grants its two parts once: "+story.Id);Valid();
                if(leg==1)
                {
                    check(!PuzzleExpeditions.ChooseLocation(progress,story.Id,story.Second)&&!PuzzleExpeditions.Talk(progress,story.Id,false),"secondary interpretation cannot be bypassed");
                    check(PuzzleExpeditions.Talk(progress,story.Id,true)&&PuzzleExpeditions.ChooseLocation(progress,story.Id,story.Second),"partner's observation opens the second map deduction");Valid();
                }
            }
            check(!PuzzleExpeditions.Observe(progress,story.Id,"wrong",story.Observation)&&!PuzzleExpeditions.Observe(progress,story.Id,story.Second,(story.Observation+1)%3),"field meeting needs the correct location and evidence-based observation");
            check(PuzzleExpeditions.Observe(progress,story.Id,story.Second,story.Observation),"joint field observation unlocks restoration");Valid();
            check(!PuzzleExpeditions.Place(progress,story.Id,-1,0)&&!PuzzleExpeditions.Place(progress,story.Id,0,4)&&!PuzzleExpeditions.Assemble(progress,story.Id),"invalid and incomplete assemblies are rejected without consuming pieces");
            for(int slot=0;slot<4;slot++)PuzzleExpeditions.Place(progress,story.Id,slot,slot);
            check(!PuzzleExpeditions.Assemble(progress,story.Id)&&state.Experience==xp&&state.Museum.Marks==marks,"wrong full layout is penalty-free and awards nothing");
            PuzzleExpeditions.Place(progress,story.Id,0,1);check(progress.Stories[story.Id].Assembly.Distinct().Count()==4,"placing an existing piece swaps it rather than duplicating it");
            var reloaded=JsonSerializer.Deserialize<ArchaeologyState>(Snapshot())!;Research.ValidateState(reloaded);
            check(reloaded.Expeditions.Stories[story.Id].Assembly.SequenceEqual(progress.Stories[story.Id].Assembly)&&reloaded.Expeditions.Stories[story.Id].Hints[(int)ExpeditionStage.FirstClue]==2,"partial layout and previously opened hints survive reload");
            for(int slot=0;slot<4;slot++)PuzzleExpeditions.Place(progress,story.Id,slot,story.Layout[slot]);
            check(PuzzleExpeditions.Assemble(progress,story.Id)&&!PuzzleExpeditions.Place(progress,story.Id,0,0),"correct restoration is committed once and then locked");Valid();
            check(!PuzzleExpeditions.Interpret(progress,story.Id,(story.Purpose+1)%3)&&PuzzleExpeditions.Interpret(progress,story.Id,story.Purpose),"purpose must explain the collected evidence");Valid();
            check(!PuzzleExpeditions.Finish(state,story.Id,false)&&PuzzleExpeditions.Finish(state,story.Id,true)&&!PuzzleExpeditions.Finish(state,story.Id,true),"final conversation awards experience and marks once");
            check(state.Experience==xp+250&&state.Museum.Marks==marks+5,"finish grants exactly its advertised rewards");Valid();
            check(!PuzzleExpeditions.Claim(progress,story.Id,false)&&PuzzleExpeditions.Stage(progress,story.Id)==ExpeditionStage.Reward,"full backpack preserves the unclaimed keepsake indefinitely");
            check(PuzzleExpeditions.Claim(progress,story.Id,true)&&!PuzzleExpeditions.Claim(progress,story.Id,true),"furniture can be claimed once");Valid();
            var copy=JsonSerializer.Deserialize<ArchaeologyState>(Snapshot())!;check(JsonSerializer.Serialize(copy)==Snapshot(),"complete expedition reloads exactly");
            check(PuzzleExpeditions.Stage(copy.Expeditions,story.Id=="river"?"tide":"river")==ExpeditionStage.Invitation,"completing one story never advances the other");
            var expiring=new ArchaeologyState{Sites=new(){Site(1)}};Research.ExpireSites(expiring,100000);
            check(expiring.Sites.Count==1&&!UnusualRelics.CanExtend(expiring.Sites[0],10),"key caches never expire and cannot be extended by relics");
        }
        var old=JsonSerializer.Deserialize<ArchaeologyState>("{\"Version\":1,\"Restored\":{\"settler_cup\":2},\"Museum\":{\"Marks\":7,\"OrdersCompleted\":1,\"AchievementClaims\":[\"patron\"]}}")!;Research.ValidateState(old);
        check(old.Expeditions.Stories.Count==0&&old.Museum.Marks==7&&old.Museum.AchievementClaims.Contains("patron"),"1.3 migration keeps existing museum records and adds empty expedition progress");
        foreach(var bad in new ExpeditionProgress?[]{null,new(){Stories=null!},new(){Stories=new(){["unknown"]=new()}},new(){Stories=new(){["river"]=new(){Stage=(ExpeditionStage)99}}},new(){Stories=new(){["river"]=new(){Stage=ExpeditionStage.Restoration,Assembly=new[]{0,0,2,3}}}},new(){Stories=new(){["river"]=new(){Stage=ExpeditionStage.FirstClue,Hints=new(){[8]=2}}}}})
            check(!PuzzleExpeditions.IsValid(bad),"malformed expedition data is rejected without resetting the save");
        var field=fresh;PuzzleExpeditions.Talk(field.Expeditions,"river",true);var goals=FieldJournal.Build(field,catalog,1);
        check(goals.Any(g=>g.Tab=="expeditions"&&g.Selection=="river"),"deductions join the read-only field objective list");
        string saved=JsonSerializer.Serialize(field);FieldJournal.Build(field,catalog,1000);check(saved==JsonSerializer.Serialize(field),"reading objectives never solves, rerolls or expires expedition progress");
        check(new ArchaeologyState().Expeditions!=field.Expeditions,"expedition state is personal per player");
    }
}
