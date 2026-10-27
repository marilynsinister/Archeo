using System.Text.Json;
using Archeo.Core;

internal static class MasteryJournalChecks
{
    internal static void Run(Action<bool, string> check, List<ArtifactDefinition> catalog, Research research)
    {
        var state = new ArchaeologyState();
        check(!Mastery.Choose(state, 5, "search") && !Mastery.Choose(state, 10, "history"), "specialties are locked below their levels");
        state.Experience = 1279;
        check(Research.Level(state.Experience) == 4 && !Mastery.Choose(state, 5, "search"), "level five threshold is enforced exactly");
        state.Experience++;
        check(Mastery.Choose(state, 5, "search") && !Mastery.Choose(state, 5, "restoration"), "level five choice is permanent");
        check(Mastery.FragmentBonus(state) == 1 && Mastery.RevealRadius(state) == 3.25f, "first search rank improves fragments and reveal radius");
        state.Experience = 6479;
        check(!Mastery.Choose(state, 10, "search"), "level ten choice cannot be taken early");
        state.Experience++;
        check(Mastery.Choose(state, 10, "search") && Mastery.Rank(state, "search") == 2 && Mastery.RevealRadius(state) == 4 && Mastery.FragmentBonus(state) == 2, "same specialty can be strengthened at level ten");
        foreach (string first in Mastery.Tracks)
        foreach (string second in Mastery.Tracks)
        {
            state = new ArchaeologyState { Experience = 6480 };
            check(!Mastery.Choose(state, 10, second), "old high level saves must choose level five first");
            check(Mastery.Choose(state, 5, first) && Mastery.Choose(state, 10, second), "every focused and mixed build is available");
            Research.ValidateState(state);
            var copy = JsonSerializer.Deserialize<ArchaeologyState>(JsonSerializer.Serialize(state))!;
            check(copy.Mastery.Level5 == first && copy.Mastery.Level10 == second && !Mastery.Choose(copy, 10, "search"), "both permanent choices survive serialization");
            check(Mastery.HistoryReward(state, 100) == 100 + 25 * Mastery.Rank(state, "history"), "story friendship and experience scale per rank");
            foreach (string culture in Research.Cultures)
            {
                var progress = research.Progress(state, culture);
                var artifact = research.Project(state, culture, new Random(7));
                int cost = 20 - 2 * Mastery.Rank(state, "restoration");
                check(Research.Required(artifact, progress, false, state) == cost, "restoration discount treats all cultures equally");
                progress.Fragments = cost - 1;
                check(!research.Restore(state, culture, false, new Random(8), out _) && progress.Fragments == cost - 1, "discounted restoration still checks funds atomically");
                progress.Fragments = cost;
                check(FieldJournal.Build(state, catalog, 1).Any(p => p.Kind == ObjectiveKind.Restore && p.Culture == culture && p.Cost == cost && !p.UseKeystone), "objectives agree with discounted price without wasting a keystone");
                check(research.Restore(state, culture, false, new Random(8), out _) && progress.Fragments == 0, "restore spends exactly the shown discounted cost");
                progress.Project = artifact.Id; progress.Keystones = 1; progress.Fragments = cost - 12;
                check(FieldJournal.Build(state, catalog, 1).Any(p => p.Kind == ObjectiveKind.Restore && p.Culture == culture && p.UseKeystone && p.Cost == cost - 12), "objective combines specialty discount and keystone");
                check(research.Restore(state, culture, true, new Random(9), out _) && progress.Fragments == 0 && progress.Keystones == 0, "keystone applies after specialty discount");
            }
        }
        state = new ArchaeologyState { Experience = 6480 };
        check(!Mastery.Choose(state, 0, "search") && !Mastery.Choose(state, 6, "search") && !Mastery.Choose(state, 5, "invalid"), "invalid specialty choices never change progress");
        foreach (var bad in new[] { new MasteryProgress { Level5 = "invalid" }, new MasteryProgress { Level10 = "history" }, new MasteryProgress { Level5 = "search", Level10 = null! } })
            check(!Mastery.IsValid(bad, 6480), "malformed specialty data is rejected for preservation");
        check(!Mastery.IsValid(new MasteryProgress { Level5 = "history" }, 1279) && !Mastery.IsValid(null, 6480), "specialty validation respects levels and missing data");
        var legacy = JsonSerializer.Deserialize<ArchaeologyState>("{\"Version\":1,\"Experience\":6480,\"Restored\":{\"copper_brooch\":2},\"TrackedObjective\":\"saved-goal\"}")!;
        Research.ValidateState(legacy);
        check(legacy.Mastery.Level5 == "" && legacy.Mastery.Level10 == "" && legacy.Restored["copper_brooch"] == 2 && legacy.TrackedObjective == "saved-goal", "1.0 saves gain empty choices without losing collections or tracking");
        var independent = new ArchaeologyState { Experience = 6480 };
        Mastery.Choose(legacy, 5, "history");
        check(independent.Mastery.Level5 == "" && Mastery.HistoryReward(legacy, 45) == 56, "players have separate specialties and fractional bonuses round down");

        state = new ArchaeologyState();
        var projectItem = research.Project(state, "valley", new Random(1));
        var found = catalog.First(p => p.Id != projectItem.Id && p.Culture == "valley");
        state.Restored[found.Id] = 1;
        var pending = catalog.First(p => p.Source == "rain");
        state.Weather.PendingRewards.Add(pending.Id);
        string before = JsonSerializer.Serialize(state);
        string Visible(ArtifactDefinition p) => JournalCatalog.IsKnown(state, p) ? "  " + p.Id + "  café Ёлка " : "Unknown antiquity";
        check(JournalCatalog.Filter(catalog, state, new(), Visible).Count == catalog.Count, "unfiltered catalog includes all cultures and sources");
        check(JournalCatalog.Filter(catalog, state, new() { Query = "  CAFE елка  " }, Visible).Count == 3, "catalog search ignores case, whitespace, accents and Russian yo");
        var unknown = catalog.First(p => !JournalCatalog.IsKnown(state, p));
        check(JournalCatalog.Filter(catalog, state, new() { Query = unknown.Id }, Visible).Count == 0, "search does not expose hidden artifact names");
        check(JournalCatalog.Filter(catalog, state, new() { Discovery = "found" }, Visible).Single().Id == found.Id, "found filter means restored including donated items");
        state.Donated.Add(found.Id);
        check(JournalCatalog.Filter(catalog, state, new() { Discovery = "found" }, Visible).Single().Id == found.Id, "museum donation keeps artifact in found filter");
        check(JournalCatalog.Filter(catalog, state, new() { Discovery = "unknown" }, Visible).All(p => p.Id != projectItem.Id && p.Id != pending.Id && p.Id != found.Id), "undiscovered filter excludes known projects and queued rewards");
        foreach (string culture in Research.Cultures)
        foreach (int rarity in Enumerable.Range(0, 5))
        foreach (string source in JournalCatalog.Sources)
        {
            var items = JournalCatalog.Filter(catalog, state, new() { Culture = culture, Rarity = rarity, Source = source }, Visible);
            check(items.All(p => p.Culture == culture && p.Rarity == rarity && (source == "" || JournalCatalog.SourceGroup(p) == source)), "culture, rarity and source filters intersect");
        }
        check(JournalCatalog.Filter(catalog, state, new() { Source = "weather" }, Visible).Count == 2 && JournalCatalog.Filter(catalog, state, new() { Source = "nature" }, Visible).Count == 3 && JournalCatalog.Filter(catalog, state, new() { Source = "residents" }, Visible).Count == 3, "event and resident source groups cover every unique reward");
        check(JournalCatalog.Filter(catalog, state, new() { Query = "no matching name" }, Visible).Count == 0, "empty catalog results are supported");
        state.Donated.Remove(found.Id);
        check(JsonSerializer.Serialize(state) == before, "catalog search and filtering never mutate progression");

        var controls = new List<JournalControl> { new("left", 0, 100, 100, 40), new("right", 200, 100, 100, 40), new("below", 0, 150, 100, 40), new("tab", 0, 0, 100, 30) };
        check(JournalNavigation.Move(controls, "left", 1, 0) == "right" && JournalNavigation.Move(controls, "right", -1, 0) == "left", "controller can cross between list and actions");
        check(JournalNavigation.Move(controls, "left", 0, 1) == "below" && JournalNavigation.Move(controls, "below", 0, -1) == "left", "controller follows aligned vertical buttons");
        check(JournalNavigation.Move(controls, "left", 0, -1) == "tab", "controller can return to tab controls");
        check(JournalNavigation.Move(controls, "right", 1, 0) == "right", "navigation at an edge keeps its focus");
        check(JournalNavigation.Move(Array.Empty<JournalControl>(), "missing", 0, 1) == "" && JournalNavigation.Move(controls, "missing", 1, 0) == "left", "controller handles empty pages and stale focus");
        foreach (var relic in catalog.Where(p => p.Effect is not ("" or "lamp")))
        {
            check(UnusualRelics.DaysUntilReady(-1, 50, relic.Effect) == 0, "every active relic starts charged");
            check(UnusualRelics.DaysUntilReady(50, 50, relic.Effect) == (relic.Effect == "garden" ? 7 : 1), "all relic readiness badges use daily or weekly charge rules");
            check(UnusualRelics.DaysUntilReady(50, relic.Effect == "garden" ? 57 : 51, relic.Effect) == 0, "all relic badges become ready at their correct boundary");
        }
    }
}
