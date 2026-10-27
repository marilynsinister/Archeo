using System.Text.Json;
using Archeo.Core;

internal static class MuseumChecks
{
    internal static void Run(Action<bool,string> check,List<ArtifactDefinition> catalog)
    {
        ArchaeologyState Explorer()
        {
            var s=new ArchaeologyState{Journey=new(){Initialized=true,Started=true,Chapters=new(){"valley","dwarf","sea","calico","island","gallery"}}};
            s.Donated.Add("settler_cup");foreach(var a in catalog)s.Restored[a.Id]=2;Journey.Refresh(s,catalog);return s;
        }
        var fresh=new ArchaeologyState();
        check(!LivingMuseum.EnsureOrder(fresh,catalog,2,7),"museum commissions never skip the introduction");
        var early=Explorer();early.Donated.Clear();
        check(!LivingMuseum.EnsureOrder(early,catalog,2,7),"a first museum donation is needed for the friends circle");
        var state=Explorer();
        check(Journey.IsOpen(state,"museum")&&LivingMuseum.EnsureOrder(state,catalog,2,7),"donation and collection progress unlock living museum");
        string offer=JsonSerializer.Serialize(state.Museum.Order);
        check(!LivingMuseum.EnsureOrder(state,catalog,7,99)&&JsonSerializer.Serialize(state.Museum.Order)==offer,"new seed and reopening cannot reroll the current week's offer");
        var requirements=LivingMuseum.OrderItems(state.Museum.Order!.Theme,catalog);
        check(requirements.Length==2&&requirements.All(id=>state.Restored.ContainsKey(id)&&catalog.Single(a=>a.Id==id).Rarity==0),"orders use two already known common finds");
        check(!LivingMuseum.RecordDelivery(state,catalog,2,false,_=>1)&&!LivingMuseum.RecordDelivery(state,catalog,2,true,id=>id==requirements[0]?1:0),"delivery rejects wrong location and missing items without awarding");
        check(state.Museum.Marks==0&&!state.Museum.Order.Completed,"failed delivery leaves its ledger unchanged");
        check(LivingMuseum.RecordDelivery(state,catalog,2,true,_=>1)&&state.Museum.Marks==3&&state.Museum.OrdersCompleted==1,"one completed commission awards exactly three marks");
        check(!LivingMuseum.RecordDelivery(state,catalog,2,true,_=>1)&&!LivingMuseum.EnsureOrder(state,catalog,7,99),"commission rewards cannot repeat during their week");
        check(LivingMuseum.EnsureOrder(state,catalog,8,7)&&!state.Museum.Order!.Completed&&state.Museum.AchievementClaims.Count==0,"new week replaces only the repeatable commission");
        check(!LivingMuseum.RecordDelivery(state,catalog,15,true,_=>99),"expired commission cannot be handed in before refresh");
        var locked=Explorer();locked.Journey.Chapters.Remove("calico");locked.Journey.Chapters.Remove("island");
        for(int seed=0;seed<50;seed++){locked.Museum.Order=null;LivingMuseum.EnsureOrder(locked,catalog,2,seed);check(!FrontierResearch.Cultures.Contains(locked.Museum.Order!.Theme),"weekly selection does not disclose a locked route");}
        foreach(string culture in Research.Cultures)
        {
            var s=Explorer();s.Restored.Clear();foreach(string id in LivingMuseum.OrderItems(culture,catalog))s.Restored[id]=1;
            s.Museum.Order=null;LivingMuseum.EnsureOrder(s,catalog,2,4);
            check(s.Museum.Order?.Theme==culture,"every culture can receive a themed commission: "+culture);
        }
        var common=catalog.Single(a=>a.Id=="settler_cup");
        var one=Explorer();one.Restored[common.Id]=1;
        check(!LivingMuseum.RecordExchange(one,common,3)&&!LivingMuseum.RecordExchange(state,common,1),"exchange requires a repeat restoration and leaves a physical copy");
        foreach(var artifact in catalog.Where(a=>a.Rarity>0||a.Source!="research"||a.Effect!=""))
            check(!LivingMuseum.RecordExchange(state,artifact,10),"valuable relic cannot enter duplicate exchange: "+artifact.Id);
        int marks=state.Museum.Marks;
        check(LivingMuseum.RecordExchange(state,common,2)&&state.Museum.Marks==marks+1&&state.Restored[common.Id]==2,"duplicate exchange awards one mark without erasing discovery credit");
        var supplies=Explorer();supplies.Museum.Marks=1;
        check(!LivingMuseum.BuySupplies(supplies,"valley")&&supplies.Museum.Marks==1,"insufficient marks do not partially spend");
        supplies.Museum.Marks=4;supplies.Journey.Chapters.Remove("island");
        check(!LivingMuseum.BuySupplies(supplies,"island")&&!LivingMuseum.BuySupplies(supplies,"invalid")&&supplies.Museum.Marks==4,"supply exchange cannot open a locked or invalid culture");
        check(LivingMuseum.BuySupplies(supplies,"calico")&&supplies.Cultures["calico"].Fragments==12&&supplies.Museum.Marks==2,"two marks buy exactly twelve fragments in the selected field");
        var set=MuseumCollections.All[0];
        check(LivingMuseum.MatchesDisplay(set.Id,set.Artifacts)&&!LivingMuseum.MatchesDisplay(set.Id,new[]{set.Artifacts[0],set.Artifacts[0],set.Artifacts[1]}),"exhibition needs all three distinct collection objects");
        check(!LivingMuseum.MatchesDisplay(set.Id,new[]{"other"}.Concat(set.Artifacts)),"extra recovery slots cannot stand in for the first three exhibit positions");
        var show=Explorer();
        check(!LivingMuseum.Start(show,"valley",3,false)&&!LivingMuseum.Start(show,"invalid",3,true),"starting an exhibition requires a real matching case");
        check(LivingMuseum.Start(show,"valley",3,true)&&!LivingMuseum.Start(show,"sea",3,true),"only one exhibition may run at a time");
        check(!LivingMuseum.ObserveDisplay(show.Museum,2,true),"exhibition does not count days before its opening");
        check(LivingMuseum.ObserveDisplay(show.Museum,3,true)&&!LivingMuseum.ObserveDisplay(show.Museum,3,true)&&show.Museum.Exhibition!.DisplayDays==1,"each display evening counts once across repeated events and reloads");
        check(!LivingMuseum.ObserveDisplay(show.Museum,4,false)&&show.Museum.Exhibition!.DisplayDays==1,"missing exhibit pauses progress without taking objects");
        check(!LivingMuseum.ClaimExhibition(show),"unfinished exhibition grants no reward");
        check(!LivingMuseum.React(show.Museum,"Penny",4,false)&&!LivingMuseum.React(show.Museum,"Willy",4,true),"visitor reaction requires nearby displayed collection and matching interests");
        check(LivingMuseum.React(show.Museum,"Penny",4,true)&&!LivingMuseum.React(show.Museum,"Penny",4,true),"each visitor reacts only once per exhibition");
        LivingMuseum.ObserveDisplay(show.Museum,5,true);LivingMuseum.ObserveDisplay(show.Museum,6,true);
        check(show.Museum.Exhibition!.Ready&&!LivingMuseum.ObserveDisplay(show.Museum,7,true),"three full evenings complete the display without repeated accrual");
        var copy=JsonSerializer.Deserialize<ArchaeologyState>(JsonSerializer.Serialize(show))!;Research.ValidateState(copy);
        check(copy.Museum.Exhibition!.Ready&&copy.Museum.Exhibition.Reactions.Contains("Penny")&&!LivingMuseum.React(copy.Museum,"Penny",6,true),"exhibition progress and visitor ledger survive reload");
        check(LivingMuseum.ClaimExhibition(copy)&&copy.Museum.Marks==5&&copy.Experience==150&&!LivingMuseum.ClaimExhibition(copy),"completed exhibition awards five marks and 150 XP exactly once");
        check(!LivingMuseum.Start(copy,"sea",9,true)&&LivingMuseum.Start(copy,"sea",10,true),"exhibitions have a seven-day opening cadence");
        var expired=Explorer();LivingMuseum.Start(expired,"valley",1,true);LivingMuseum.ObserveDisplay(expired.Museum,1,true);
        check(!LivingMuseum.ObserveDisplay(expired.Museum,8,true)&&!LivingMuseum.ClaimExhibition(expired)&&LivingMuseum.Start(expired,"valley",8,true),"failed exhibition expires without rewards and can be replaced next week");
        check(!LivingMuseum.React(expired.Museum,"Penny",7,true),"future exhibition cannot receive early visitor credit");
        var earned=Explorer();earned.Museum.OrdersCompleted=1;earned.Museum.ExhibitionsCompleted=3;
        foreach(var achievement in LivingMuseum.Achievements)
        {
            check(!LivingMuseum.ClaimAchievement(earned.Museum,achievement.Id,false)&&!earned.Museum.AchievementClaims.Contains(achievement.Id),"full backpack preserves keepsake: "+achievement.Id);
            check(LivingMuseum.ClaimAchievement(earned.Museum,achievement.Id,true)&&!LivingMuseum.ClaimAchievement(earned.Museum,achievement.Id,true),"each keepsake is claimed once independently: "+achievement.Id);
        }
        var old=JsonSerializer.Deserialize<ArchaeologyState>("{\"Version\":1,\"Restored\":{\"settler_cup\":3},\"CollectionRewards\":[\"valley\"]}")!;Research.ValidateState(old);
        check(old.Museum.Marks==0&&old.Museum.AchievementClaims.Count==0&&old.Restored["settler_cup"]==3&&old.CollectionRewards.Contains("valley"),"1.2 saves gain empty museum records without changing collections");
        foreach(var invalid in new MuseumProgress?[]{null,new(){Marks=-1},new(){AchievementClaims=new(){"curator"}},new(){Order=new(){Theme="invalid"}},new(){Exhibition=new(){Theme="valley",Started=1,Ends=7,DisplayDays=4}}})
            check(!LivingMuseum.IsValid(invalid),"damaged museum data is rejected without resetting progress");
        var goals=Explorer();goals.Museum.OrdersCompleted=1;LivingMuseum.EnsureOrder(goals,catalog,1,2);LivingMuseum.Start(goals,"valley",1,true);
        var notes=FieldJournal.Build(goals,catalog,1);string before=JsonSerializer.Serialize(goals);
        check(notes.Any(g=>g.Topic=="museum_order")&&notes.Any(g=>g.Topic=="museum_show")&&notes.Any(g=>g.Topic=="museum_award"),"commissions, exhibitions and keepsakes join field objectives");
        FieldJournal.Build(goals,catalog,1);check(JsonSerializer.Serialize(goals)==before,"reading museum field objectives never advances or rerolls them");
        check(new ArchaeologyState().Museum!=state.Museum&&LivingMuseum.IsValid(earned.Museum),"museum ledgers remain independent per player");
    }
}
