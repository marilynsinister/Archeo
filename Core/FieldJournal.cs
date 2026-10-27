namespace Archeo.Core;

public enum ObjectiveKind { Reward, Dig, Talk, Restore, Repair, Part, Wait }

public sealed class FieldObjective
{
    public string Id { get; init; } = "";
    public ObjectiveKind Kind { get; init; }
    public string Tab { get; init; } = "overview";
    public string Selection { get; init; } = "";
    public string Topic { get; init; } = "";
    public string Artifact { get; init; } = "";
    public string Culture { get; init; } = "";
    public string Location { get; init; } = "";
    public string Npc { get; init; } = "";
    public string DetailKey { get; init; } = "";
    public int Remaining { get; init; }
    public int Expires { get; init; } = int.MaxValue;
    public int Starts { get; init; }
    public bool UseKeystone { get; init; }
    public int Cost { get; init; }
}

/// <summary>A read-only view of actionable progress. Building the journal never rolls projects or grants rewards.</summary>
public static class FieldJournal
{
    public static List<FieldObjective> Build(ArchaeologyState state, IReadOnlyList<ArtifactDefinition> catalog, int day,
        bool weatherEnabled = true, bool natureEnabled = true)
    {
        var goals = new List<FieldObjective>();
        var active = state.Sites.Where(p => p.Expires >= day && p.Points.Any(n => !n.Dug)).ToList();
        foreach (var site in active)
        {
            var resident = ResidentStories.All.FirstOrDefault(p => p.SiteKind == site.EventKind);
            string tab = resident != null ? "residents" : Shipwreck.Kinds.Contains(site.EventKind) ? "shipwreck"
                : NaturalEvents.Kinds.Contains(site.EventKind) ? "nature" : WeatherExpeditions.Kinds.Contains(site.EventKind) ? "weather"
                : site.EventKind == "stars" ? "overview" : site.Culture;
            goals.Add(new FieldObjective { Id = "dig:"+site.Key, Kind = ObjectiveKind.Dig, Tab = tab,
                Selection = resident?.Id ?? (tab == "nature" ? site.EventKind : ""), Topic = site.EventKind,
                Culture = site.Culture, Location = site.Location, Remaining = site.Points.Count(p => !p.Dug), Expires = site.Expires,
                DetailKey = site.Expires == int.MaxValue ? "field.detail.permanent-dig" : "field.detail.dig" });
        }
        foreach (string id in state.Weather.PendingRewards.Concat(state.Nature.PendingRewards).OrderBy(p => p,StringComparer.Ordinal))
        {
            var artifact = catalog.FirstOrDefault(p => p.Id == id);
            if (artifact == null) continue;
            bool nature = NaturalEvents.Kinds.Contains(artifact.Source);
            goals.Add(new FieldObjective { Id = "reward:"+id, Kind = ObjectiveKind.Reward, Tab = nature ? "nature" : "weather",
                Selection = nature ? artifact.Source : "", Artifact = id, Culture = artifact.Culture, DetailKey = "field.detail.reward" });
        }
        foreach (var collection in MuseumCollections.All.Where(p => MuseumCollections.CanClaim(state,p.Id,true)))
            goals.Add(new FieldObjective { Id = "collection:"+collection.Id, Kind = ObjectiveKind.Reward, Tab = "gallery",
                Selection = collection.Id, Topic = "collection", Artifact = collection.Reward, DetailKey = "field.detail.collection" });
        if (state.Shipwreck.Stage == ShipwreckStage.Recovered)
            goals.Add(new FieldObjective { Id = "reward:ship-bell", Kind = ObjectiveKind.Reward, Tab = "shipwreck",
                Artifact = Shipwreck.BellId, Topic = "bell", DetailKey = "field.detail.reward" });

        foreach (string culture in Research.Cultures)
        {
            if (!state.Cultures.TryGetValue(culture,out var progress)) continue;
            var project = catalog.FirstOrDefault(p => p.Id == progress.Project && p.Culture == culture && p.Source == "research");
            if (project == null) continue;
            bool useKey = progress.Fragments < project.Cost && progress.Keystones > 0;
            int cost = Research.Required(project,progress,useKey);
            if (progress.Fragments < cost) continue;
            goals.Add(new FieldObjective { Id = "restore:"+culture+":"+project.Id, Kind = ObjectiveKind.Restore, Tab = culture,
                Selection = project.Id, Artifact = project.Id, Culture = culture, Cost = cost, UseKeystone = useKey,
                DetailKey = useKey ? "field.detail.restore-key" : "field.detail.restore" });
        }

        void Talk(string id,string tab,string selection,string npc,string location,string detail,string artifact = "")
            => goals.Add(new FieldObjective { Id = id, Kind = ObjectiveKind.Talk, Tab = tab, Selection = selection,
                Npc = npc, Location = location, DetailKey = detail, Artifact = artifact });
        switch (state.Workshop.Stage)
        {
            case WorkshopStage.Blueprint: Talk("workshop:study","workshop","","Gunther","ArchaeologyHouse","workshop.text.1"); break;
            case WorkshopStage.MuseumNotes: Talk("workshop:consult","workshop","","Robin","","workshop.text.2"); break;
            case WorkshopStage.Components:
                foreach (string part in Workshop.Parts.Where(p => !state.Workshop.Parts.Contains(p)))
                    goals.Add(new FieldObjective { Id = "workshop:part:"+part, Kind = ObjectiveKind.Part, Tab = "workshop", Topic = part,
                        Culture = part == "spring" ? "dwarf" : part == "lens" ? "sea" : "valley", DetailKey = "field.detail.part" });
                break;
            case WorkshopStage.Ready:
                goals.Add(new FieldObjective { Id = "workshop:repair", Kind = ObjectiveKind.Repair, Tab = "workshop", Location = "Farm", DetailKey = "field.detail.repair" });
                break;
        }
        switch (state.Shipwreck.Stage)
        {
            case ShipwreckStage.Buckle: Talk("ship:study","shipwreck","","Gunther","ArchaeologyHouse","shipwreck.text.1"); break;
            case ShipwreckStage.PortRecord: Talk("ship:consult","shipwreck","","Willy","","shipwreck.text.2"); break;
            case ShipwreckStage.Letter: Talk("ship:decode","shipwreck","","Willy","","shipwreck.text.4"); break;
            case ShipwreckStage.AwaitBottle:
                if (!active.Any(p => p.EventKind == "ship_bottle"))
                    goals.Add(new FieldObjective { Id = "ship:wait-bottle", Kind = ObjectiveKind.Wait, Tab = "shipwreck", Topic = "ship",
                        Location = "Beach", DetailKey = state.Shipwreck.BottleUntil >= day ? "field.detail.queued" : "shipwreck.text.3",
                        Expires = state.Shipwreck.BottleUntil >= day ? state.Shipwreck.BottleUntil : int.MaxValue });
                break;
            case ShipwreckStage.Cache:
                if (!active.Any(p => p.EventKind == "ship_cache"))
                    goals.Add(new FieldObjective { Id = "ship:wait-cache", Kind = ObjectiveKind.Wait, Tab = "shipwreck", Topic = "ship", Location = "Beach", DetailKey = "field.detail.queued" });
                break;
        }
        foreach (var story in ResidentStories.All)
        {
            switch (ResidentStories.Stage(state.Residents,story.Id))
            {
                case ResidentStage.Clue: Talk("resident:"+story.Id+":start","residents",story.Id,story.Npc,"","residents.stage.1",story.Trigger); break;
                case ResidentStage.Recovered: Talk("resident:"+story.Id+":share","residents",story.Id,story.Npc,"","residents.stage.3",story.Trigger); break;
                case ResidentStage.Searching:
                    if (!active.Any(p => p.EventKind == story.SiteKind))
                        goals.Add(new FieldObjective { Id = "resident:"+story.Id+":wait", Kind = ObjectiveKind.Wait, Tab = "residents", Selection = story.Id,
                            Topic = story.SiteKind, Location = story.Location, DetailKey = "field.detail.queued" });
                    break;
                case ResidentStage.Reward:
                    goals.Add(new FieldObjective { Id = "reward:"+story.Artifact, Kind = ObjectiveKind.Reward, Tab = "residents", Selection = story.Id,
                        Artifact = story.Artifact, Culture = story.Culture, DetailKey = "field.detail.reward" });
                    break;
            }
        }
        if (state.Weather.Lead is { } weather && weather.Expires >= day && !active.Any(p => WeatherExpeditions.Kinds.Contains(p.EventKind)))
            goals.Add(new FieldObjective { Id = "lead:"+weather.Key, Kind = ObjectiveKind.Wait, Tab = "weather", Topic = weather.Kind,
                Expires = weather.Expires, Starts = weather.Day, DetailKey = weatherEnabled ? "field.detail.queued" : "field.detail.disabled" });
        foreach (var lead in state.Nature.Leads.Where(p => p.Expires >= day))
            goals.Add(new FieldObjective { Id = "lead:"+lead.Key, Kind = ObjectiveKind.Wait, Tab = "nature", Selection = lead.Kind, Topic = lead.Kind,
                Expires = lead.Expires, Starts = lead.Day, DetailKey = !natureEnabled ? "field.detail.disabled" : lead.Day > day ? "field.detail.tomorrow" : "field.detail.queued" });
        if (state.Stars.Lead is { } stars && stars.Expires >= day && !active.Any(p => p.EventKind == "stars"))
            goals.Add(new FieldObjective { Id = "lead:"+stars.Key, Kind = ObjectiveKind.Wait, Tab = "overview", Topic = "stars", Location = stars.Location,
                Culture = stars.Culture, Starts = stars.Day, Expires = stars.Expires, DetailKey = stars.Day > day ? "field.detail.tomorrow" : "field.detail.queued" });

        return goals.OrderBy(Priority).ThenBy(p => p.Expires).ThenBy(p => p.Id,StringComparer.Ordinal).ToList();
    }

    private static int Priority(FieldObjective goal) => goal.Kind switch
    {
        ObjectiveKind.Reward => 0,
        ObjectiveKind.Dig when goal.Expires != int.MaxValue => 1,
        ObjectiveKind.Talk or ObjectiveKind.Restore or ObjectiveKind.Repair => 2,
        ObjectiveKind.Dig => 3,
        ObjectiveKind.Part => 4,
        _ => 5
    };

    public static FieldObjective? Tracked(ArchaeologyState state,IReadOnlyList<FieldObjective> goals)
        => goals.FirstOrDefault(p => p.Id == state.TrackedObjective);

    public static void PruneTracking(ArchaeologyState state,IReadOnlyList<FieldObjective> goals)
    {
        if (state.TrackedObjective.Length == 0 || Tracked(state,goals) != null) return;
        string old = state.TrackedObjective;
        // A placed cache is the same objective as its lead, even though its available actions changed.
        string? replacement = old.StartsWith("lead:",StringComparison.Ordinal) ? "dig:"+old[5..] : null;
        var resident = ResidentStories.All.FirstOrDefault(p => old == "resident:"+p.Id+":wait");
        if (resident != null) replacement = "dig:"+resident.SiteKey;
        if (old == "ship:wait-cache") replacement = "dig:ship:cache";
        if (old == "ship:wait-bottle") replacement = goals.FirstOrDefault(p => p.Kind == ObjectiveKind.Dig && p.Topic == "ship_bottle")?.Id;
        state.TrackedObjective = goals.FirstOrDefault(p => p.Id == replacement)?.Id ?? "";
    }
}
