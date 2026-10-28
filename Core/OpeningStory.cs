namespace Archeo.Core;

public enum OpeningStage { Uninitialized, Invitation, Searching, Restored, Complete, Legacy }

public sealed class OpeningProgress
{
    public OpeningStage Stage { get; set; }
}

/// <summary>The first cup is a self-contained story; existing researchers keep their discoveries.</summary>
public static class OpeningStory
{
    public const string Artifact = "settler_cup";
    public const string SiteKey = "opening:cup";
    public const int Finds = 4;
    public static bool Active(ArchaeologyState state) => state.Opening.Stage is OpeningStage.Searching or OpeningStage.Restored;
    public static bool Introduced(ArchaeologyState state) => Active(state) || state.Opening.Stage == OpeningStage.Complete;

    public static void Initialize(ArchaeologyState state, bool existingResearch)
    {
        if (state.Opening.Stage == OpeningStage.Uninitialized)
            state.Opening.Stage = existingResearch ? OpeningStage.Legacy : OpeningStage.Invitation;
    }

    public static void Begin(ArchaeologyState state)
    {
        if (state.Journey.Started && state.Opening.Stage == OpeningStage.Invitation)
            state.Opening.Stage = OpeningStage.Searching;
    }

    public static bool CanRestore(ArchaeologyState state, string culture)
        => !Active(state) || state.Opening.Stage == OpeningStage.Searching && culture == "valley";

    public static void Restored(ArchaeologyState state, string artifact)
    {
        if (state.Opening.Stage == OpeningStage.Searching && artifact == Artifact)
            state.Opening.Stage = OpeningStage.Restored;
    }

    public static bool Conclude(ArchaeologyState state, bool inMuseum)
    {
        // The recorded restoration is enough: a sold, donated or displayed cup never blocks the story.
        if (!inMuseum || state.Opening.Stage != OpeningStage.Restored || !state.Restored.ContainsKey(Artifact)) return false;
        state.Opening.Stage = OpeningStage.Complete;
        return true;
    }

    public static List<FieldObjective> Objectives(ArchaeologyState state, IReadOnlyList<ArtifactDefinition> catalog, int day)
    {
        if (state.Opening.Stage == OpeningStage.Restored)
            return new() { new() { Id = "opening:talk", Kind = ObjectiveKind.Talk, Tab = "opening", Topic = "opening", Npc = "Gunther",
                Location = "ArchaeologyHouse", Artifact = Artifact, DetailKey = "opening.goal.talk" } };
        var artifact = catalog.Single(p => p.Id == Artifact);
        if (state.Cultures.TryGetValue("valley", out var progress))
        {
            int cost = Research.Required(artifact, progress, false, state);
            if (progress.Fragments >= cost)
                return new() { new() { Id = "opening:restore", Kind = ObjectiveKind.Restore, Tab = "valley", Topic = "opening", Culture = "valley",
                    Selection = Artifact, Artifact = Artifact, Cost = cost, DetailKey = "opening.goal.restore" } };
        }
        var site = state.Sites.FirstOrDefault(p => p.Key == SiteKey && p.Expires >= day && p.Points.Any(n => !n.Dug));
        if (site != null)
            return new() { new() { Id = "dig:" + SiteKey, Kind = ObjectiveKind.Dig, Tab = "opening", Topic = "opening", Culture = "valley",
                Location = site.Location, Remaining = site.Points.Count(p => !p.Dug), DetailKey = "opening.goal.dig" } };
        return new() { new() { Id = "opening:wait", Kind = ObjectiveKind.Wait, Tab = "opening", Topic = "opening", DetailKey = "opening.goal.wait" } };
    }

    public static bool IsValid(OpeningProgress? progress)
        => progress != null && Enum.IsDefined(typeof(OpeningStage), progress.Stage);
}
