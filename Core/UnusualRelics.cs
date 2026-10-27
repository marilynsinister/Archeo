namespace Archeo.Core;

public readonly record struct GridPoint(int X, int Y);

public sealed class StarLead
{
    public int Day { get; set; }
    public int Expires { get; set; }
    public string Culture { get; set; } = "";
    public string Location { get; set; } = "";
    [System.Text.Json.Serialization.JsonIgnore]
    public string Key => "stars:" + Day;
}

public sealed class StarResearch
{
    public StarLead? Lead { get; set; }
    public HashSet<string> Completed { get; set; } = new();
}

/// <summary>Cooldowns, stable site extension and overnight research reservations, without game dependencies.</summary>
public static class UnusualRelics
{
    public static readonly string[] Effects = { "past", "garden", "hourglass", "stars" };
    public const int StarLifetime = 3;
    public static int Cooldown(string effect) => effect == "garden" ? 7 : 1;
    public static int DaysUntilReady(int lastUse, int day, string effect)
        => lastUse < 0 ? 0 : (int)Math.Clamp((long)lastUse + Cooldown(effect) - day, 0, int.MaxValue);

    public static bool CanGrowFruit(int daysUntilMature, bool stump, int lightningDays, bool inSeason, int fruitCount)
        => daysUntilMature <= 0 && !stump && lightningDays <= 0 && inSeason && fruitCount is >= 0 and < 3;

    public static bool CanExtend(DigSite site, int day)
        => !site.TimeExtended && site.Expires >= day && site.Expires < int.MaxValue
            && !Shipwreck.Kinds.Contains(site.EventKind) && site.Points.Any(p => !p.Dug);

    public static bool Extend(DigSite site, int day)
    {
        if (!CanExtend(site, day)) return false;
        // Normal site keys used to depend on Expires. Pin the original key before changing it,
        // keeping current survey signals and recorded targets attached to the same excavation.
        if (site.EventId.Length == 0) site.EventId = site.Key;
        site.Expires++;
        site.TimeExtended = true;
        return true;
    }

    public static void ExpireLead(StarResearch stars, int day)
    { if (stars.Lead?.Expires < day) stars.Lead = null; }

    public static bool ReserveStars(ArchaeologyState state, int day, int time, bool outdoors, string culture, string location)
    {
        ExpireLead(state.Stars, day);
        if (time < 2000 || !outdoors || !Research.Cultures.Contains(culture) || string.IsNullOrWhiteSpace(location)
            || state.Stars.Lead != null || state.Sites.Any(s => s.EventKind == "stars" && s.Expires >= day && s.Points.Any(p => !p.Dug))) return false;
        state.Stars.Lead = new StarLead { Day = day + 1, Expires = day + StarLifetime, Culture = culture, Location = location };
        return true;
    }

    public static bool AttachStars(StarResearch stars, StarLead lead, int day)
    {
        if (stars.Lead?.Key != lead.Key || day < lead.Day || day > lead.Expires) return false;
        stars.Lead = null;
        return true;
    }

    public static bool CompleteStars(ArchaeologyState state, DigSite site)
        => site.EventKind == "stars" && site.EventId.StartsWith("stars:", StringComparison.Ordinal)
            && site.Points.Count == 2 && site.Points.All(p => p.Dug) && state.Stars.Completed.Add(site.Key);

    public static bool IsValid(StarResearch? stars)
        => stars != null && stars.Completed != null && stars.Completed.All(k => k.StartsWith("stars:", StringComparison.Ordinal))
            && (stars.Lead == null || stars.Lead.Day >= 0 && stars.Lead.Expires >= stars.Lead.Day
                && Research.Cultures.Contains(stars.Lead.Culture) && !string.IsNullOrWhiteSpace(stars.Lead.Location));

    /// <summary>Four-directional route through actual walkable cells; never draws a straight line across blocked terrain.</summary>
    public static List<GridPoint> FindTrail(GridPoint start, GridPoint goal, Func<GridPoint, bool> walkable, int limit = 6000)
    {
        if (limit < 1 || !walkable(goal)) return new();
        var previous = new Dictionary<GridPoint, GridPoint> { [start] = start };
        var queue = new Queue<GridPoint>();
        queue.Enqueue(start);
        GridPoint[] directions = { new(0,1), new(1,0), new(0,-1), new(-1,0) };
        while (queue.Count > 0)
        {
            var at = queue.Dequeue();
            if (at == goal)
            {
                var path = new List<GridPoint> { at };
                while (at != start) { at = previous[at]; path.Add(at); }
                path.Reverse();
                return path;
            }
            foreach (var offset in directions)
            {
                var next = new GridPoint(at.X + offset.X, at.Y + offset.Y);
                if (previous.ContainsKey(next) || !walkable(next) || previous.Count >= limit) continue;
                previous[next] = at;
                queue.Enqueue(next);
            }
        }
        return new();
    }
}
