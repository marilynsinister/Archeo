using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace Archeo.Menus;

internal sealed partial class JournalMenu
{
    private static readonly string[] Settings = { "hud", "tracker", "vanilla", "weather", "nature", "lifetime", "finds", "fragments", "weather-life", "weather-cooldown", "nature-life", "journal-key" };
    private int settingsPage;
    private string settingSelection = "hud";
    private string bindingSetting = "";
    private int SettingsPageSize => Math.Max(1, (left.Height - 94) / 40);

    private string SettingValue(string key)
    {
        var c = mod.Config;
        string Toggle(bool value) => mod.T(value ? "settings.on" : "settings.off");
        return key switch
        {
            "hud" => Toggle(c.ShowHud), "tracker" => Toggle(c.ShowTrackedObjective), "vanilla" => Toggle(c.EnableVanillaSpotFragments),
            "weather" => Toggle(c.EnableWeatherEvents), "nature" => Toggle(c.EnableNatureEvents),
            "lifetime" => c.SiteLifetimeDays.ToString(), "finds" => c.FindsPerSite.ToString(), "fragments" => c.FragmentMultiplier.ToString("0.##"),
            "weather-life" => c.WeatherSiteLifetimeDays.ToString(), "weather-cooldown" => c.WeatherEventCooldownDays.ToString(), "nature-life" => c.NatureSiteLifetimeDays.ToString(),
            "journal-key" => c.JournalKey.ToString(), "survey-key" => c.SurveyKey.ToString(), _ => ""
        };
    }

    private void DrawSettings(SpriteBatch b)
    {
        int pages = (Settings.Length + SettingsPageSize - 1) / SettingsPageSize;
        settingsPage = Math.Clamp(settingsPage, 0, pages - 1);
        int y = left.Y + 8;
        foreach (string key in Settings.Skip(settingsPage * SettingsPageSize).Take(SettingsPageSize))
        {
            Button(b, "settings:select:" + key, mod.T("settings.name." + key) + ": " + SettingValue(key), new Rectangle(left.X + 6, y, left.Width - 12, 36), active: key == settingSelection);
            y += 40;
        }
        int half = (left.Width - 20) / 2;
        Button(b, "settings:prev", mod.T("unusual.page-prev"), new Rectangle(left.X + 6, left.Bottom - 78, half, 30), settingsPage > 0);
        Button(b, "settings:next", mod.T("unusual.page-next"), new Rectangle(left.X + half + 14, left.Bottom - 78, half, 30), settingsPage + 1 < pages);
        Button(b, "settings:reset", mod.T("settings.reset"), new Rectangle(left.X + 6, left.Bottom - 38, left.Width - 12, 32));
        string text = mod.T("settings.help." + settingSelection) + "\n\n" + mod.T("settings.current", new { value = SettingValue(settingSelection) }) + "\n\n" + mod.T("settings.saved");
        if (bindingSetting != "") text += "\n\n" + mod.T("settings.capture");
        DrawPageText(b, mod.T("settings.name." + settingSelection), text, right.Bottom - 65);
        if (settingSelection.EndsWith("-key"))
            Button(b, "settings:bind", mod.T(bindingSetting == "" ? "settings.bind" : "settings.cancel"), new Rectangle(right.X + 12, right.Bottom - 46, right.Width - 24, 38));
        else
        {
            int w = (right.Width - 32) / 2;
            Button(b, "settings:minus", "-", new Rectangle(right.X + 12, right.Bottom - 46, w, 38));
            Button(b, "settings:plus", "+", new Rectangle(right.X + 20 + w, right.Bottom - 46, w, 38));
        }
    }

    private void HandleSettingsClick(string id)
    {
        if (id.StartsWith("settings:select:")) { settingSelection = id[16..]; bindingSetting = ""; }
        else if (id == "settings:prev") settingsPage--;
        else if (id == "settings:next") settingsPage++;
        else if (id == "settings:bind") bindingSetting = bindingSetting == "" ? settingSelection : "";
        else if (id == "settings:reset") { mod.Config.ResetDefaults(); mod.SaveSettings(); bindingSetting = ""; }
        else if (id is "settings:minus" or "settings:plus")
        {
            int delta = id == "settings:minus" ? -1 : 1;
            var c = mod.Config;
            switch (settingSelection)
            {
                case "hud": c.ShowHud = !c.ShowHud; break;
                case "tracker": c.ShowTrackedObjective = !c.ShowTrackedObjective; break;
                case "vanilla": c.EnableVanillaSpotFragments = !c.EnableVanillaSpotFragments; break;
                case "weather": c.EnableWeatherEvents = !c.EnableWeatherEvents; break;
                case "nature": c.EnableNatureEvents = !c.EnableNatureEvents; break;
                case "lifetime": c.SiteLifetimeDays += delta; break;
                case "finds": c.FindsPerSite += delta; break;
                case "fragments": c.FragmentMultiplier += delta * .25f; break;
                case "weather-life": c.WeatherSiteLifetimeDays += delta; break;
                case "weather-cooldown": c.WeatherEventCooldownDays += delta; break;
                case "nature-life": c.NatureSiteLifetimeDays += delta; break;
            }
            mod.SaveSettings();
        }
        scroll = 0;
    }

    private void BindSetting(SButton button)
    {
        if (bindingSetting == "journal-key") mod.Config.JournalKey = new KeybindList(new Keybind(button), new Keybind(SButton.ControllerBack));
        else mod.Config.SurveyKey = new KeybindList(button);
        bindingSetting = ""; mod.SaveSettings();
    }

    private bool CaptureSettingKey(Keys key)
    {
        if (bindingSetting == "") return false;
        if (key == Keys.Escape) bindingSetting = "";
        else if (Enum.TryParse<SButton>(key.ToString(), out var button) && button != SButton.None) BindSetting(button);
        return true;
    }

    private bool CaptureSettingButton(Buttons button)
    {
        if (bindingSetting == "") return false;
        if (button is Buttons.B or Buttons.Back) bindingSetting = "";
        else if (Enum.TryParse<SButton>("Controller" + button, out var value)) BindSetting(value);
        return true;
    }
}
