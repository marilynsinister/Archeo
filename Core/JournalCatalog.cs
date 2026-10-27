using System.Globalization;
using System.Text;

namespace Archeo.Core;

public sealed class CatalogFilter
{
    public string Query { get; set; } = "";
    public string Culture { get; set; } = "";
    public int Rarity { get; set; } = -1;
    public string Source { get; set; } = "";
    public string Discovery { get; set; } = "";
}

public static class JournalCatalog
{
    public static readonly string[] Sources = { "", "research", "weather", "nature", "residents" };
    public static readonly string[] Discoveries = { "", "found", "unknown" };
    public static bool IsKnown(ArchaeologyState state, ArtifactDefinition artifact)
        => state.Restored.ContainsKey(artifact.Id) || state.Cultures.Values.Any(p => p.Project == artifact.Id)
            || state.Weather.PendingRewards.Contains(artifact.Id) || state.Nature.PendingRewards.Contains(artifact.Id)
            || ResidentStories.PendingArtifact(state.Residents, artifact.Id);
    public static string SourceGroup(ArtifactDefinition artifact)
        => artifact.Source == "research" ? "research" : WeatherExpeditions.Kinds.Contains(artifact.Source) ? "weather"
            : NaturalEvents.Kinds.Contains(artifact.Source) ? "nature" : "residents";

    public static List<ArtifactDefinition> Filter(IEnumerable<ArtifactDefinition> artifacts, ArchaeologyState state,
        CatalogFilter filter, Func<ArtifactDefinition, string> visibleText)
    {
        string query = Normalize(filter.Query.Trim());
        return artifacts.Where(p => (filter.Culture == "" || p.Culture == filter.Culture)
                && (filter.Rarity < 0 || p.Rarity == filter.Rarity)
                && (filter.Source == "" || SourceGroup(p) == filter.Source)
                && (filter.Discovery == "" || (filter.Discovery == "found" ? state.Restored.ContainsKey(p.Id) : filter.Discovery == "projects" ? state.Cultures.Values.Any(c=>c.Project==p.Id) : !IsKnown(state, p)))
                && (query == "" || Normalize(visibleText(p)).Contains(query, StringComparison.Ordinal)))
            .OrderBy(p => Array.IndexOf(Research.Cultures, p.Culture)).ThenBy(p => p.Rarity).ThenBy(p => p.Id, StringComparer.Ordinal).ToList();
    }

    private static string Normalize(string text)
        => new string(text.Normalize(NormalizationForm.FormD).Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray()).ToUpperInvariant();
}
