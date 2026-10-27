namespace Archeo.Core;

public enum ShipwreckStage { Undiscovered, Buckle, PortRecord, AwaitBottle, Letter, Cache, Recovered, Complete }

public sealed class ShipwreckState
{
    public ShipwreckStage Stage { get; set; }
    public int LastObservedDay { get; set; } = -1;
    public bool Raining { get; set; }
    public int BottleUntil { get; set; } = -1;
}

/// <summary>A complete, replay-safe story; weather windows start only from days observed with the mod.</summary>
public static class Shipwreck
{
    public const string BellId = "ship_bell";
    public const int BellEnergy = 35;
    public const int BottleLifetime = 3;
    public static readonly string[] Kinds = { "ship_bottle", "ship_cache" };

    public static bool Discover(ShipwreckState state, IReadOnlyDictionary<string, int> restored)
    {
        if (state.Stage != ShipwreckStage.Undiscovered || !restored.TryGetValue("sailor_buckle", out int count) || count < 1) return false;
        state.Stage = ShipwreckStage.Buckle;
        return true;
    }

    public static bool Study(ShipwreckState state, bool inMuseum)
    {
        if (!inMuseum || state.Stage != ShipwreckStage.Buckle) return false;
        state.Stage = ShipwreckStage.PortRecord;
        return true;
    }

    public static bool Consult(ShipwreckState state, bool nearWilly)
    {
        if (!nearWilly || state.Stage != ShipwreckStage.PortRecord) return false;
        state.Stage = ShipwreckStage.AwaitBottle;
        return true;
    }

    public static void Observe(ShipwreckState state, int day, bool raining)
    {
        if (state.LastObservedDay == day) { state.Raining |= raining; return; }
        if (state.Stage == ShipwreckStage.AwaitBottle && state.LastObservedDay >= 0 && state.LastObservedDay == day - 1 && state.Raining)
            state.BottleUntil = day + BottleLifetime - 1;
        state.LastObservedDay = day;
        state.Raining = raining;
    }

    public static string? SiteKind(ShipwreckState state, int day)
        => state.Stage == ShipwreckStage.Cache ? "ship_cache"
            : state.Stage == ShipwreckStage.AwaitBottle && state.BottleUntil >= day ? "ship_bottle" : null;

    public static bool CompleteSite(ShipwreckState state, DigSite site, int day)
    {
        if (site.Location != "Beach" || site.Culture != "sea" || site.Expires < day || site.Points.Count == 0
            || site.Points.Any(p => !p.Dug)) return false;
        if (state.Stage == ShipwreckStage.AwaitBottle && site.EventKind == "ship_bottle" && site.Points.Count == 1 && site.EventId.StartsWith("ship:bottle:", StringComparison.Ordinal)
            && state.BottleUntil >= day)
        {
            state.Stage = ShipwreckStage.Letter;
            state.BottleUntil = -1;
            return true;
        }
        if (state.Stage == ShipwreckStage.Cache && site.EventKind == "ship_cache" && site.Points.Count == 3 && site.EventId == "ship:cache")
        {
            state.Stage = ShipwreckStage.Recovered;
            return true;
        }
        return false;
    }

    public static bool Decode(ShipwreckState state, bool nearWilly)
    {
        if (!nearWilly || state.Stage != ShipwreckStage.Letter) return false;
        state.Stage = ShipwreckStage.Cache;
        return true;
    }

    public static bool RecordClaim(ShipwreckState state, bool inventorySpace)
    {
        if (!inventorySpace || state.Stage != ShipwreckStage.Recovered) return false;
        state.Stage = ShipwreckStage.Complete;
        return true;
    }

    public static bool CanRing(int lastUse, int day, float energy, int maxEnergy)
        => lastUse != day && energy < maxEnergy;

    public static bool IsValid(ShipwreckState? state)
        => state != null && Enum.IsDefined(typeof(ShipwreckStage), state.Stage) && state.LastObservedDay >= -1 && state.BottleUntil >= -1
            && (state.BottleUntil == -1 || state.Stage == ShipwreckStage.AwaitBottle && state.LastObservedDay >= 0);
}
