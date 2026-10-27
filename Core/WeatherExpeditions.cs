namespace Archeo.Core;

public sealed class WeatherLead
{
    public string Kind { get; set; } = "";
    public int Day { get; set; }
    public int Expires { get; set; }
    public string Key => $"weather:{Kind}:{Day}";
}

public sealed class WeatherProgress
{
    public int LastObservedDay { get; set; } = -1;
    public bool Raining { get; set; }
    public bool Storm { get; set; }
    public int LastEventDay { get; set; } = -100;
    public WeatherLead? Lead { get; set; }
    public Dictionary<string, int> Completed { get; set; } = new();
    public HashSet<string> CompletedSites { get; set; } = new();
    public HashSet<string> PendingRewards { get; set; } = new();
}

/// <summary>Weather history and reward transactions, independent of the game process.</summary>
public static class WeatherExpeditions
{
    public static readonly string[] Kinds = { "rain", "storm" };
    public static string Artifact(string kind) => kind switch
    {
        "rain" => "rain_pitcher", "storm" => "storm_fork", _ => throw new ArgumentException("Unknown weather event.")
    };
    public static string Culture(string kind) => kind == "rain" ? "valley" : kind == "storm" ? "dwarf" : throw new ArgumentException("Unknown weather event.");

    public static void Observe(WeatherProgress state, int day, bool raining, bool storm, bool enabled, bool activeEvent, int lifetime, int cooldown)
    {
        if (state.Lead?.Expires < day) state.Lead = null;
        if (state.LastObservedDay == day)
        {
            // Midday weather changes can add evidence without erasing an observed storm.
            state.Raining |= raining || storm;
            state.Storm |= storm;
            return;
        }
        if (state.LastObservedDay == day - 1 && state.LastObservedDay >= 0 && enabled && !activeEvent
            && state.Lead == null && day - state.LastEventDay >= cooldown)
        {
            string kind = state.Storm ? "storm" : state.Raining ? "rain" : "";
            if (kind.Length > 0) state.Lead = new WeatherLead { Kind = kind, Day = day, Expires = day + lifetime - 1 };
        }
        // A new install or a gap in play records current weather without inventing history.
        state.LastObservedDay = day;
        state.Raining = raining || storm;
        state.Storm = storm;
    }

    public static bool Attach(WeatherProgress state, WeatherLead lead, int day)
    {
        if (state.Lead?.Key != lead.Key || day < lead.Day || day > lead.Expires) return false;
        state.LastEventDay = day;
        state.Lead = null;
        return true;
    }

    public static bool Complete(ArchaeologyState state, DigSite site)
    {
        if (!Kinds.Contains(site.EventKind) || site.EventId.Length == 0 || !site.Points.Any() || site.Points.Any(p => !p.Dug)
            || !state.Weather.CompletedSites.Add(site.Key)) return false;
        string kind = site.EventKind;
        state.Weather.Completed[kind] = state.Weather.Completed.GetValueOrDefault(kind) + 1;
        string artifact = Artifact(kind);
        if (!state.Restored.ContainsKey(artifact)) state.Weather.PendingRewards.Add(artifact);
        return true;
    }

    public static bool RecordClaim(ArchaeologyState state, string artifact)
    {
        if (!state.Weather.PendingRewards.Remove(artifact)) return false;
        state.Restored[artifact] = state.Restored.GetValueOrDefault(artifact) + 1;
        return true;
    }

    public static bool IsValid(WeatherProgress? state)
        => state != null && state.LastObservedDay >= -1 && state.Completed != null && state.CompletedSites != null && state.PendingRewards != null
            && state.Completed.All(p => Kinds.Contains(p.Key) && p.Value >= 0)
            && state.PendingRewards.All(id => Kinds.Any(k => Artifact(k) == id))
            && (state.Lead == null || Kinds.Contains(state.Lead.Kind) && state.Lead.Day >= 0 && state.Lead.Expires >= state.Lead.Day);
}
