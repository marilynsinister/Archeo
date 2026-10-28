using Archeo.Core;
using Archeo.Menus;
using Microsoft.Xna.Framework;
using StardewValley;

namespace Archeo.Smoke;

public sealed partial class SmokeEntry
{
    private void CaptureLearning(ModEntry mod, string stage)
    {
        var viewport = Game1.uiViewport;
        Game1.uiViewport = new xTile.Dimensions.Rectangle(0, 0, 900, 640);
        var menu = new JournalMenu(mod);
        Capture(menu, "learn-next-" + stage + ".png", 900, 640);
        Click(menu, "field:guide");
        Capture(menu, "learn-guide-" + stage + ".png", 900, 640);
        Check(Field<string>(menu, "tab") == "help", "overview opens the contextual lesson archive: " + stage);
        Check(Field<string>(menu, "learningSelection") == FieldLearning.Current(mod.State, mod.Research.Artifacts), "archive selects the actual next learning step: " + stage);
        Game1.uiViewport = viewport;
        if (stage is "kit" or "fragments") CaptureOpeningLayouts(mod,stage);
    }

    private void CheckLearningLayouts(ModEntry mod)
    {
        var viewport = Game1.uiViewport;
        var language = LocalizedContentManager.CurrentLanguageCode;
        foreach (var code in new[] { LocalizedContentManager.LanguageCode.ru, LocalizedContentManager.LanguageCode.en })
        foreach (var size in new[] { new Point(900, 640), new Point(1280, 720) })
        foreach (float scale in new[] { 1f, 1.3f })
        {
            LocalizedContentManager.CurrentLanguageCode = code;
            Game1.uiViewport = new xTile.Dimensions.Rectangle(0, 0, size.X, size.Y);
            mod.Config.JournalTextScale = scale;
            var menu = new JournalMenu(mod, "help");
            Capture(menu, $"learn-archive-{code}-{size.X}-{scale:0.0}.png", size.X, size.Y);
            Check(Field<float>(menu, "fontScale") == 1f, "small windows keep native journal font size");
            int max = Field<int>(menu, "maxScroll");
            for (int i = 0; i < max + 1; i++) menu.receiveScrollWheelAction(-1);
            Check(Field<int>(menu, "scroll") == max, "enlarged lesson reaches its final line");
            Capture(menu, $"learn-bottom-{code}-{size.X}-{scale:0.0}.png", size.X, size.Y);
            var settings = new JournalMenu(mod, "settings");
            Capture(settings, $"learn-settings-{code}-{size.X}-{scale:0.0}.png", size.X, size.Y);
            Click(settings, "settings:select:text-size");
            Capture(settings, $"learn-size-{code}-{size.X}-{scale:0.0}.png", size.X, size.Y);
            Check(ButtonsOf(settings).ContainsKey("settings:plus"), "reading size remains editable at both resolutions");
            var catalog = new JournalMenu(mod, "catalog");
            Set(catalog, "selected", "brass_gear");
            Capture(catalog, $"learn-long-title-{code}-{size.X}-{scale:0.0}.png", size.X, size.Y);
            var equipment = new JournalMenu(mod, "equipment");
            Capture(equipment, $"learn-equipment-{code}-{size.X}-{scale:0.0}.png", size.X, size.Y);
        }
        mod.Config.JournalTextScale = 1;
        Game1.uiViewport = viewport;
        LocalizedContentManager.CurrentLanguageCode = language;
    }
}
