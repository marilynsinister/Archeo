using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace Archeo;

public sealed class ModConfig
{
    public KeybindList JournalKey { get; set; } = new(new Keybind(SButton.O), new Keybind(SButton.ControllerBack));
    public KeybindList SurveyKey { get; set; } = new(SButton.K);
    public bool ShowHud { get; set; } = true;
    public bool ShowTrackedObjective { get; set; } = true;
    public float JournalTextScale { get; set; } = 1f;
    public bool EnableVanillaSpotFragments { get; set; } = true;
    public int SiteLifetimeDays { get; set; } = 3;
    public int FindsPerSite { get; set; } = 4;
    public float FragmentMultiplier { get; set; } = 1f;
    public bool EnableWeatherEvents { get; set; } = true;
    public int WeatherSiteLifetimeDays { get; set; } = 3;
    public int WeatherEventCooldownDays { get; set; } = 4;
    public bool EnableNatureEvents { get; set; } = true;
    public int NatureSiteLifetimeDays { get; set; } = 5;

    public void Normalize()
    {
        JournalKey ??= new(new Keybind(SButton.O), new Keybind(SButton.ControllerBack));
        SurveyKey ??= new(SButton.K);
        JournalTextScale = float.IsFinite(JournalTextScale) ? Math.Clamp(JournalTextScale, 1f, 1.3f) : 1f;
        SiteLifetimeDays = Math.Clamp(SiteLifetimeDays, 1, 14);
        FindsPerSite = Math.Clamp(FindsPerSite, 2, 8);
        FragmentMultiplier = float.IsFinite(FragmentMultiplier) ? Math.Clamp(FragmentMultiplier, .25f, 5f) : 1;
        WeatherSiteLifetimeDays = Math.Clamp(WeatherSiteLifetimeDays, 2, 7);
        WeatherEventCooldownDays = Math.Clamp(WeatherEventCooldownDays, 2, 14);
        NatureSiteLifetimeDays = Math.Clamp(NatureSiteLifetimeDays, 3, 14);
    }

    public void ResetDefaults()
    {
        JournalKey = new(new Keybind(SButton.O), new Keybind(SButton.ControllerBack)); SurveyKey = new(SButton.K);
        ShowHud = ShowTrackedObjective = EnableVanillaSpotFragments = EnableWeatherEvents = EnableNatureEvents = true;
        SiteLifetimeDays = WeatherSiteLifetimeDays = 3; FindsPerSite = WeatherEventCooldownDays = 4;
        FragmentMultiplier = JournalTextScale = 1; NatureSiteLifetimeDays = 5;
    }
}
