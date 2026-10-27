using Archeo.Core;
using StardewValley;

namespace Archeo;

public sealed partial class ModEntry
{
    // Furniture uses slash-delimited legacy data; asset separators must use backslashes.
    private string FurnitureTexture(string path) => Helper.ModContent.GetInternalAssetName(path).Name.Replace('/', '\\');

    internal string ArtifactHint(ArtifactDefinition artifact)
    {
        if (artifact.Source != "research")
            return T((ResidentStories.Sources.Contains(artifact.Source) ? "residents.artifact-hint." : NaturalEvents.Kinds.Contains(artifact.Source) ? "nature.artifact-hint." : "weather.artifact-hint.") + artifact.Source);
        string places = string.Join(", ", SiteLocations[artifact.Culture].Select(LocationName));
        string text = T("catalog.hint.research", new { culture = CultureName(artifact.Culture), locations = places });
        var sites = State.Sites.Where(p => p.Culture == artifact.Culture && p.Expires >= Today && p.Points.Any(n => !n.Dug)).Select(p => LocationName(p.Location)).Distinct().ToList();
        if (sites.Count > 0) text += "\n\n" + T("catalog.hint.active", new { locations = string.Join(", ", sites) });
        return text + "\n\n" + T("catalog.hint.keystone");
    }

    internal string RelicStatus(ArtifactDefinition artifact)
    {
        int days = RelicDaysUntilReady(artifact);
        return T(days == 0 ? "catalog.ready" : "catalog.cooldown", new { days });
    }

    internal void ChooseMastery(int level, string track)
    {
        if (!Mastery.Choose(State, level, track)) return;
        SaveState();
        Game1.playSound("reward");
        Notify(T("mastery.chosen", new { level, name = T("mastery.track." + track) }));
    }

    internal void SaveSettings()
    {
        Config.Normalize();
        Helper.WriteConfig(Config);
    }
}
