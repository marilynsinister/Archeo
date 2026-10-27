namespace Archeo.Core;

public sealed class MasteryProgress
{
    public string Level5 { get; set; } = "";
    public string Level10 { get; set; } = "";
}

/// <summary>Two permanent, independently chosen ranks. Bonuses apply equally to every culture.</summary>
public static class Mastery
{
    public static readonly string[] Tracks = { "search", "restoration", "history" };
    public static bool IsValid(MasteryProgress? progress, int experience)
        => progress != null && (progress.Level5 == "" || Tracks.Contains(progress.Level5) && Research.Level(experience) >= 5)
            && (progress.Level10 == "" || Tracks.Contains(progress.Level10) && Research.Level(experience) >= 10 && progress.Level5 != "");

    public static bool Choose(ArchaeologyState state, int level, string track)
    {
        if (!Tracks.Contains(track) || Research.Level(state.Experience) < level) return false;
        if (level == 5 && state.Mastery.Level5 == "") state.Mastery.Level5 = track;
        else if (level == 10 && state.Mastery.Level5 != "" && state.Mastery.Level10 == "") state.Mastery.Level10 = track;
        else return false;
        return true;
    }

    public static int Rank(ArchaeologyState state, string track)
        => (state.Mastery.Level5 == track ? 1 : 0) + (state.Mastery.Level10 == track ? 1 : 0);
    public static int FragmentBonus(ArchaeologyState state) => Rank(state, "search");
    public static float RevealRadius(ArchaeologyState state) => 2.5f + .75f * Rank(state, "search");
    public static int RestorationCost(ArchaeologyState? state, int cost)
        => Math.Max(1, cost - cost * (state == null ? 0 : Rank(state, "restoration")) / 10);
    public static int HistoryReward(ArchaeologyState state, int amount)
        => amount + amount * Rank(state, "history") / 4;
}
