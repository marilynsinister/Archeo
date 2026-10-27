using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace Archeo;

public sealed class ModConfig
{
    public KeybindList JournalKey { get; set; } = new(SButton.O);
    public KeybindList SurveyKey { get; set; } = new(SButton.K);
    public bool ShowHud { get; set; } = true;
    public bool ShowTrackedObjective { get; set; } = true;
    public bool EnableVanillaSpotFragments { get; set; } = true;
    public int SiteLifetimeDays { get; set; } = 3;
    public int FindsPerSite { get; set; } = 4;
    public float FragmentMultiplier { get; set; } = 1f;
    public bool EnableWeatherEvents { get; set; } = true;
    public int WeatherSiteLifetimeDays { get; set; } = 3;
    public int WeatherEventCooldownDays { get; set; } = 4;
    public bool EnableNatureEvents { get; set; } = true;
    public int NatureSiteLifetimeDays { get; set; } = 5;
}
