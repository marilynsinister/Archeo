using System.Text.Json;
using Archeo.Core;

internal static class LearningChecks
{
    internal static void Run(Action<bool, string> check, List<ArtifactDefinition> catalog, Research research)
    {
        var state = new ArchaeologyState();
        Journey.Initialize(state, catalog);
        FieldLearning.Initialize(state, catalog);
        check(FieldLearning.Current(state, catalog) == "welcome" && FieldLearning.Available(state, catalog).SequenceEqual(new[] { "welcome" }), "invitation teaches only the museum visit");
        state.Journey.Started = true;
        Journey.Refresh(state, catalog);
        research.Project(state, "valley", new Random(1));
        check(FieldLearning.Current(state, catalog) == "search" && FieldLearning.Available(state, catalog).SequenceEqual(new[] { "welcome", "search" }), "kit introduces the instrument without revealing future mechanics");
        state.Learning.Events.Add("survey");
        check(FieldLearning.Current(state, catalog) == "signal" && !FieldLearning.Available(state, catalog).Contains("fragments"), "first signal teaches following the arrow before collecting anything");
        state.Learning.Events.Add("marked");
        check(FieldLearning.Current(state, catalog) == "dig", "permanent marker teaches switching to the hoe");
        state.Finds = 1;
        var progress = research.Progress(state, "valley");
        progress.Fragments = 7;
        FieldLearning.Observe(state);
        check(FieldLearning.Current(state, catalog) == "fragments" && !FieldLearning.Available(state, catalog).Contains("restore"), "first fragments explain storage before restoration is affordable");
        check(FieldLearning.TakeNotice(state, catalog) == "fragments" && FieldLearning.TakeNotice(state, catalog) == null, "fast excavation announces the current step once without stale search instructions");
        progress.Keystones = 1;
        progress.Fragments = 8;
        FieldLearning.Observe(state);
        check(FieldLearning.Available(state, catalog).Contains("keystones") && FieldLearning.Current(state, catalog) == "restore", "a real ancient part teaches the discount and affordable restoration");
        state.Learning.Announced.UnionWith(FieldLearning.Available(state, catalog));
        progress.Fragments = 0;
        progress.Keystones = 0;
        check(FieldLearning.Available(state, catalog).Contains("keystones") && FieldLearning.Available(state, catalog).Contains("restore"), "explained mechanics stay readable after their materials are spent");
        progress.Fragments = 100;
        research.Restore(state, "valley", false, new Random(2), out var artifact);
        Journey.Refresh(state, catalog);
        FieldLearning.Observe(state);
        check(FieldLearning.Current(state, catalog) == "homecoming" && !FieldLearning.Available(state, catalog).Contains("chapters"), "restoring the first cup teaches returning to Gunther before new systems");
        check(FieldLearning.TakeNotice(state, catalog) == "homecoming", "the first story's next action takes priority after restoration");
        state.Donated.Add(artifact.Id);
        check(FieldLearning.Current(state,catalog)=="homecoming","donating the cup does not skip the concluding story");
        OpeningStory.Conclude(state,true); Journey.Refresh(state,catalog);
        check(FieldLearning.Current(state, catalog) == "chapters", "donation moves the introductory next step to independent exploration");
        check(!FieldLearning.Available(state, catalog).Contains("weather") && !FieldLearning.Available(state, catalog).Contains("expeditions"), "lesson archive hides undiscovered stories");
        state.Journey.Chapters.Add("weather");
        check(FieldLearning.Available(state, catalog).Contains("weather"), "weather lesson follows the actual chapter discovery");
        var before = JsonSerializer.Serialize(state);
        FieldLearning.Available(state, catalog);
        FieldLearning.Current(state, catalog);
        check(JsonSerializer.Serialize(state) == before, "reading lessons does not spend fragments, roll projects, or advance stories");
        var copy = JsonSerializer.Deserialize<ArchaeologyState>(before)!;
        Research.ValidateState(copy);
        check(copy.Learning.Events.SetEquals(state.Learning.Events) && copy.Learning.Announced.SetEquals(state.Learning.Announced), "learned steps and one-time notices survive reload");
        var legacy = JsonSerializer.Deserialize<ArchaeologyState>("{\"Finds\":12,\"Cultures\":{\"valley\":{\"Fragments\":10}},\"Restored\":{\"clay_bead\":1}}")!;
        Journey.Initialize(legacy, catalog);
        FieldLearning.Initialize(legacy, catalog);
        check(FieldLearning.Available(legacy, catalog).All(legacy.Learning.Announced.Contains), "existing players receive no backlog of beginner notifications");
        var announced = legacy.Learning.Announced.ToHashSet();
        legacy.Journey.Chapters.Add("weather");
        FieldLearning.Initialize(legacy, catalog);
        check(legacy.Learning.Announced.SetEquals(announced) && !legacy.Learning.Announced.Contains("weather"), "reloading does not silence lessons discovered later");
        check(FieldLearning.TakeNotice(legacy, catalog) == "weather" && FieldLearning.TakeNotice(legacy, catalog) == null, "new chapter notification is delivered once on a migrated save");
        foreach (var invalid in new LearningProgress?[] { null, new() { Events = null! }, new() { Announced = null! }, new() { Events = new() { "bad" } }, new() { Announced = new() { "bad" } } })
            check(!FieldLearning.IsValid(invalid), "malformed lesson progress is rejected for data preservation");
    }
}
