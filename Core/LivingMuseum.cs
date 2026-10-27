namespace Archeo.Core;

public sealed class MuseumOrder
{
    public int Week { get; set; }
    public string Theme { get; set; } = "";
    public bool Completed { get; set; }
}

public sealed class MuseumExhibition
{
    public string Theme { get; set; } = "";
    public int Started { get; set; }
    public int Ends { get; set; }
    public int LastObserved { get; set; } = -1;
    public int DisplayDays { get; set; }
    public bool Claimed { get; set; }
    public HashSet<string> Reactions { get; set; } = new();
    public bool Ready => DisplayDays >= LivingMuseum.RequiredDays;
}

public sealed class MuseumProgress
{
    public int Marks { get; set; }
    public int OrdersCompleted { get; set; }
    public int ExhibitionsCompleted { get; set; }
    public int NextExhibitionDay { get; set; }
    public MuseumOrder? Order { get; set; }
    public MuseumExhibition? Exhibition { get; set; }
    public HashSet<string> AchievementClaims { get; set; } = new();
}

public sealed record MuseumAchievement(string Id,string Reward);

/// <summary>Personal museum records; furniture contents remain ordinary game inventories.</summary>
public static class LivingMuseum
{
    public const int RequiredDays=3;
    public static readonly MuseumAchievement[] Achievements={new("patron","scholar_case"),new("exhibitor","museum_banner"),new("curator","curator_case")};
    public static int Week(int day)=>Math.Max(0,(Math.Max(1,day)-1)/7);
    public static int WeekEnd(int day)=>(Week(day)+1)*7;
    public static string[] OrderItems(string theme,IReadOnlyList<ArtifactDefinition> catalog)
        => catalog.Where(a=>a.Culture==theme&&a.Source=="research"&&a.Rarity==0&&a.Effect=="").OrderBy(a=>a.Cost).ThenBy(a=>a.Id,StringComparer.Ordinal).Take(2).Select(a=>a.Id).ToArray();
    public static bool Unlocked(ArchaeologyState state)=>Journey.IsOpen(state,"gallery")&&state.Donated.Count>0;
    public static bool EnsureOrder(ArchaeologyState state,IReadOnlyList<ArtifactDefinition> catalog,int day,int seed)
    {
        if(!Unlocked(state)||state.Museum.Order?.Week==Week(day))return false;
        var themes=Research.Cultures.Where(c=>Journey.IsOpen(state,c)).Where(c=>{var items=OrderItems(c,catalog);return items.Length==2&&items.All(state.Restored.ContainsKey);}).ToList();
        if(themes.Count==0)return false;
        state.Museum.Order=new(){Week=Week(day),Theme=themes[new Random(seed).Next(themes.Count)]};
        return true;
    }
    public static bool CanDeliver(ArchaeologyState state,IReadOnlyList<ArtifactDefinition> catalog,int day,bool museum,Func<string,int> inventory)
        => Unlocked(state)&&museum&&state.Museum.Order is {Completed:false} order&&order.Week==Week(day)
            && OrderItems(order.Theme,catalog) is {Length:2} ids&&ids.All(id=>inventory(id)>=1);
    public static bool RecordDelivery(ArchaeologyState state,IReadOnlyList<ArtifactDefinition> catalog,int day,bool museum,Func<string,int> inventory)
    {
        if(!CanDeliver(state,catalog,day,museum,inventory))return false;
        state.Museum.Order!.Completed=true;state.Museum.OrdersCompleted++;state.Museum.Marks+=3;
        return true;
    }
    public static bool CanExchange(ArchaeologyState state,ArtifactDefinition artifact,int physicalCount)
        => Unlocked(state)&&artifact.Source=="research"&&artifact.Rarity==0&&artifact.Effect==""
            && state.Restored.GetValueOrDefault(artifact.Id)>=2&&physicalCount>=2;
    public static bool RecordExchange(ArchaeologyState state,ArtifactDefinition artifact,int physicalCount)
    {
        if(!CanExchange(state,artifact,physicalCount))return false;
        state.Museum.Marks++;return true;
    }
    public static bool BuySupplies(ArchaeologyState state,string culture)
    {
        if(!Unlocked(state)||!Journey.IsOpen(state,culture)||!Research.Cultures.Contains(culture)||state.Museum.Marks<2)return false;
        state.Museum.Marks-=2;
        if(!state.Cultures.TryGetValue(culture,out var progress))state.Cultures[culture]=progress=new();
        progress.Fragments+=12;return true;
    }
    public static bool MatchesDisplay(string theme,IEnumerable<string> items)
    {
        var collection=MuseumCollections.All.FirstOrDefault(c=>c.Id==theme);
        var displayed=items.Take(MuseumCollections.CaseCapacity).ToHashSet(StringComparer.Ordinal);
        return collection!=null&&collection.Artifacts.All(displayed.Contains);
    }
    public static bool CanStart(ArchaeologyState state,string theme,int day,bool displayed)
        => Unlocked(state)&&displayed&&day>=state.Museum.NextExhibitionDay&&MuseumCollections.All.Any(c=>c.Id==theme&&c.Artifacts.All(state.Restored.ContainsKey))
            && (state.Museum.Exhibition==null || state.Museum.Exhibition.Claimed || !state.Museum.Exhibition.Ready&&state.Museum.Exhibition.Ends<day);
    public static bool Start(ArchaeologyState state,string theme,int day,bool displayed)
    {
        if(!CanStart(state,theme,day,displayed))return false;
        state.Museum.Exhibition=new(){Theme=theme,Started=day,Ends=day+6};state.Museum.NextExhibitionDay=day+7;return true;
    }
    public static bool ObserveDisplay(MuseumProgress museum,int day,bool displayed)
    {
        var show=museum.Exhibition;
        if(show==null||show.Claimed||show.Ready||day<show.Started||day>show.Ends||day<=show.LastObserved)return false;
        show.LastObserved=day;if(displayed)show.DisplayDays++;return displayed;
    }
    public static bool ClaimExhibition(ArchaeologyState state)
    {
        if(state.Museum.Exhibition is not {Ready:true,Claimed:false} show)return false;
        show.Claimed=true;state.Museum.ExhibitionsCompleted++;state.Museum.Marks+=5;state.Experience+=150;return true;
    }
    public static string[] Visitors(string theme)=>theme switch{
        "valley"=>new[]{"Penny","Evelyn"},"dwarf"=>new[]{"Maru","Clint"},"sea"=>new[]{"Willy","Elliott"},"calico"=>new[]{"Sandy","Emily"},"island"=>new[]{"Leo","Willy"},_=>Array.Empty<string>()};
    public static bool React(MuseumProgress museum,string npc,int day,bool nearbyDisplay)
    {
        var show=museum.Exhibition;
        return show!=null&&!show.Claimed&&day>=show.Started&&day<=show.Ends&&nearbyDisplay&&Visitors(show.Theme).Contains(npc)&&show.Reactions.Add(npc);
    }
    public static bool AchievementReady(MuseumProgress museum,string id)=>id switch{
        "patron"=>museum.OrdersCompleted>=1,"exhibitor"=>museum.ExhibitionsCompleted>=1,"curator"=>museum.ExhibitionsCompleted>=3,_=>false};
    public static bool ClaimAchievement(MuseumProgress museum,string id,bool space)
        => space&&AchievementReady(museum,id)&&museum.AchievementClaims.Add(id);
    public static bool IsValid(MuseumProgress? museum)
        => museum!=null&&museum.Marks>=0&&museum.OrdersCompleted>=0&&museum.ExhibitionsCompleted>=0&&museum.NextExhibitionDay>=0
            && museum.AchievementClaims!=null&&museum.AchievementClaims.All(id=>Achievements.Any(a=>a.Id==id)&&AchievementReady(museum,id))
            && (museum.Order==null||museum.Order.Week>=0&&Research.Cultures.Contains(museum.Order.Theme))
            && (museum.Exhibition==null||museum.Exhibition is {} s&&Research.Cultures.Contains(s.Theme)&&s.Started>=0&&s.Ends==s.Started+6
                &&s.LastObserved>=-1&&s.LastObserved<=s.Ends&&s.DisplayDays is >=0 and <=RequiredDays&&(!s.Claimed||s.Ready)
                &&s.Reactions!=null&&s.Reactions.All(Visitors(s.Theme).Contains));
}
