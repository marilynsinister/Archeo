namespace Archeo.Core;

public enum ExpeditionStage { Locked, Invitation, FirstClue, FirstDig, Consult, SecondClue, SecondDig, Meeting, Restoration, Interpretation, Finale, Reward, Complete }
public sealed class ExpeditionRecord
{
    public ExpeditionStage Stage { get; set; }
    public Dictionary<int,int> Hints { get; set; } = new();
    public int[] Assembly { get; set; } = new[]{-1,-1,-1,-1};
}
public sealed class ExpeditionProgress
{
    public Dictionary<string,ExpeditionRecord> Stories { get; set; } = new();
}
public sealed record PuzzleExpedition(string Id,string Primary,string Secondary,string Culture,string First,string Second,string Reward,int[] Layout,int Purpose,int Observation)
{
    public string Kind(int leg)=>$"expedition_{Id}_{leg}";
    public string Key(int leg)=>$"expedition:{Id}:{leg}";
    public string[] Locations(int leg)=>Id=="river" ? (leg==1?new[]{"Town","Forest","Backwoods"}:new[]{"Forest","BusStop","Mountain"})
        :leg==1?new[]{"Forest","Beach","Town"}:new[]{"IslandSouth","IslandEast","IslandNorth"};
}

/// <summary>Persistent evidence, penalty-free deductions and personal expedition rewards.</summary>
public static class PuzzleExpeditions
{
    public static readonly PuzzleExpedition[] All={
        new("river","Penny","Maru","valley","Forest","Mountain","river_model",new[]{2,0,3,1},1,2),
        new("tide","Willy","Leo","island","Beach","IslandSouth","voyage_mobile",new[]{1,3,0,2},2,1)
    };
    public static readonly string[] Kinds=All.SelectMany(s=>new[]{s.Kind(1),s.Kind(2)}).ToArray();
    public static readonly ExpeditionStage[] HintStages={ExpeditionStage.FirstClue,ExpeditionStage.SecondClue,ExpeditionStage.Meeting,ExpeditionStage.Restoration,ExpeditionStage.Interpretation};
    public static ExpeditionStage Stage(ExpeditionProgress progress,string id)=>progress.Stories.TryGetValue(id,out var r)?r.Stage:ExpeditionStage.Locked;
    public static List<string> Discover(ArchaeologyState state)
    {
        var result=new List<string>();
        foreach(var story in All)
        {
            bool eligible=story.Id=="river"?ResidentStories.Stage(state.Residents,"penny")==ResidentStage.Complete&&state.Restored.ContainsKey("resonant_lens")
                :ResidentStories.Stage(state.Residents,"willy")==ResidentStage.Complete&&Journey.IsOpen(state,"island")&&state.Restored.ContainsKey("island_spindle");
            if(!state.Journey.Started||!eligible||Stage(state.Expeditions,story.Id)!=ExpeditionStage.Locked)continue;
            state.Expeditions.Stories[story.Id]=new(){Stage=ExpeditionStage.Invitation};result.Add(story.Id);
        }
        return result;
    }
    private static bool Advance(ExpeditionProgress progress,string id,ExpeditionStage from,ExpeditionStage to,bool allowed)
    {
        if(!allowed||Stage(progress,id)!=from||!All.Any(s=>s.Id==id))return false;
        progress.Stories[id].Stage=to;return true;
    }
    public static bool Talk(ExpeditionProgress progress,string id,bool nearby)
    {
        var stage=Stage(progress,id);
        return stage==ExpeditionStage.Invitation?Advance(progress,id,stage,ExpeditionStage.FirstClue,nearby)
            :stage==ExpeditionStage.Consult&&Advance(progress,id,stage,ExpeditionStage.SecondClue,nearby);
    }
    public static int Leg(ExpeditionStage stage)=>stage==ExpeditionStage.FirstDig?1:stage==ExpeditionStage.SecondDig?2:0;
    public static bool ChooseLocation(ExpeditionProgress progress,string id,string location)
    {
        var story=All.FirstOrDefault(s=>s.Id==id);var stage=Stage(progress,id);
        return story!=null&&(stage==ExpeditionStage.FirstClue?Advance(progress,id,stage,ExpeditionStage.FirstDig,location==story.First)
            :stage==ExpeditionStage.SecondClue&&Advance(progress,id,stage,ExpeditionStage.SecondDig,location==story.Second));
    }
    public static bool ValidSite(DigSite site)
    {
        var story=All.FirstOrDefault(s=>site.EventKind==s.Kind(1)||site.EventKind==s.Kind(2));
        if(story==null)return false;int leg=site.EventKind==story.Kind(1)?1:2;
        return site.EventId==story.Key(leg)&&site.Culture==story.Culture&&site.Location==(leg==1?story.First:story.Second)
            &&site.Expires==int.MaxValue&&site.Points.Count==2&&site.Points.All(p=>p.X>=0&&p.Y>=0)&&site.Points.Select(p=>(p.X,p.Y)).Distinct().Count()==2;
    }
    public static bool CompleteSite(ExpeditionProgress progress,DigSite site)
    {
        if(!ValidSite(site)||site.Points.Any(p=>!p.Dug))return false;
        var story=All.First(s=>site.EventKind==s.Kind(1)||site.EventKind==s.Kind(2));
        return site.EventKind==story.Kind(1)?Advance(progress,story.Id,ExpeditionStage.FirstDig,ExpeditionStage.Consult,true)
            :Advance(progress,story.Id,ExpeditionStage.SecondDig,ExpeditionStage.Meeting,true);
    }
    public static bool Observe(ExpeditionProgress progress,string id,string location,int answer)
    {
        var story=All.FirstOrDefault(s=>s.Id==id);
        return story!=null&&Advance(progress,id,ExpeditionStage.Meeting,ExpeditionStage.Restoration,location==story.Second&&answer==story.Observation);
    }
    public static bool Place(ExpeditionProgress progress,string id,int slot,int piece)
    {
        if(Stage(progress,id)!=ExpeditionStage.Restoration||slot is <0 or >3||piece is <0 or >3)return false;
        var layout=progress.Stories[id].Assembly;int other=Array.IndexOf(layout,piece),old=layout[slot];
        if(other>=0&&other!=slot)layout[other]=old;
        layout[slot]=piece;return true;
    }
    public static bool Assemble(ExpeditionProgress progress,string id)
    {
        var story=All.FirstOrDefault(s=>s.Id==id);
        return story!=null&&Advance(progress,id,ExpeditionStage.Restoration,ExpeditionStage.Interpretation,progress.Stories.TryGetValue(id,out var r)&&r.Assembly.SequenceEqual(story.Layout));
    }
    public static bool Interpret(ExpeditionProgress progress,string id,int purpose)
    {
        var story=All.FirstOrDefault(s=>s.Id==id);
        return story!=null&&Advance(progress,id,ExpeditionStage.Interpretation,ExpeditionStage.Finale,purpose==story.Purpose);
    }
    public static bool Finish(ArchaeologyState state,string id,bool nearby)
    {
        if(!Advance(state.Expeditions,id,ExpeditionStage.Finale,ExpeditionStage.Reward,nearby))return false;
        state.Experience+=Mastery.HistoryReward(state,250);state.Museum.Marks+=5;return true;
    }
    public static bool Claim(ExpeditionProgress progress,string id,bool space)=>Advance(progress,id,ExpeditionStage.Reward,ExpeditionStage.Complete,space);
    public static int RevealHint(ExpeditionProgress progress,string id)
    {
        var stage=Stage(progress,id);
        if(!HintStages.Contains(stage))return 0;
        var hints=progress.Stories[id].Hints;int level=Math.Min(2,hints.GetValueOrDefault((int)stage)+1);hints[(int)stage]=level;return level;
    }
    public static bool IsValid(ExpeditionProgress? progress)=>progress?.Stories!=null&&progress.Stories.All(p=>All.Any(s=>s.Id==p.Key)&&p.Value!=null
        &&Enum.IsDefined(typeof(ExpeditionStage),p.Value.Stage)&&p.Value.Hints!=null&&p.Value.Hints.All(h=>HintStages.Contains((ExpeditionStage)h.Key)&&h.Value is >=1 and <=2&&h.Key<=(int)p.Value.Stage)
        &&p.Value.Assembly is {Length:4} a&&a.All(i=>i is >=-1 and <=3)&&a.Where(i=>i>=0).Distinct().Count()==a.Count(i=>i>=0)
        &&(p.Value.Stage>=ExpeditionStage.Restoration||a.All(i=>i==-1))&&(p.Value.Stage<ExpeditionStage.Interpretation||a.SequenceEqual(All.First(s=>s.Id==p.Key).Layout)));
}
