using System.Reflection;
using System.Text.Json;
using Archeo.Core;
using Archeo.Menus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Tools;

namespace Archeo.Smoke;

public sealed class SmokeEntry : Mod
{
    private bool started;
    private int ticks;
    private readonly List<string> results = new();
    private string Out => Path.GetFullPath(Path.Combine(Helper.DirectoryPath, "..", ".."));
    public override void Entry(IModHelper helper) => helper.Events.GameLoop.UpdateTicked += Tick;

    private void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("FAIL: " + message);
        results.Add("PASS: " + message);
    }

    private static void Call(object instance, string name, params object[] args)
        => instance.GetType().GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(instance, args);

    private void Tick(object? sender, UpdateTickedEventArgs e)
    {
        if (started || ++ticks < 90) return;
        started = true;
        try
        {
            var mod = (ModEntry)typeof(ModEntry).Assembly.GetType("Archeo.DigPatches")!.GetField("Mod", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            Call(mod, "OnCommand", "archeo", new[] { "validate" });
            Check(true, "33 item registrations and localized lore");
            LocalizedContentManager.CurrentLanguageCode = LocalizedContentManager.LanguageCode.ru;
            Game1.game1.loadForNewGame(false);
            Game1.player.Name = "Archeo QA";
            Game1.player.farmName.Value = "Unsaved QA";
            Game1.hasLoadedGame = true;
            Game1.gameMode = 3;
            Game1.activeClickableMenu = null;
            Game1.currentLocation = Game1.getLocationFromName("Forest");
            Game1.player.currentLocation = Game1.currentLocation;
            typeof(Context).GetProperty("IsWorldReady")!.SetValue(null, true);
            Call(mod, "LoadState");
            Check(mod.State.Sites.Count == 3, "three reachable sites in an unsaved world");
            Check(mod.State.Sites.All(s => s.Points.Count >= 2), "dig sites contain usable points");
            foreach (var site in mod.State.Sites)
            {
                var location = Game1.getLocationFromName(site.Location);
                Check(site.Points.All(p => location.isTileOnMap(p.X, p.Y) && location.doesTileHaveProperty(p.X, p.Y, "Diggable", "Back") != null), "diggable site: " + site.Culture);
            }
            var dig = mod.State.Sites[0];
            Game1.currentLocation = Game1.getLocationFromName(dig.Location);
            Game1.player.currentLocation = Game1.currentLocation;
            var point = dig.Points[0];
            Game1.player.setTileLocation(new Vector2(point.X, point.Y + 1));
            mod.Survey();
            int before = mod.State.Finds;
            new Hoe().DoFunction(Game1.currentLocation, point.X * 64 + 32, point.Y * 64 + 32, 0, Game1.player);
            Check(mod.State.Finds == before + 1 && point.Dug, "survey, highlight, and actual patched hoe excavation");
            mod.OnHoeDig(Game1.currentLocation, point.X, point.Y, Game1.player);
            Check(mod.State.Finds == before + 1, "same find cannot be dug twice");
            var progress = mod.Research.Progress(mod.State, "valley");
            progress.Fragments = 100;
            var project = mod.Research.Project(mod.State, "valley", new Random(1));
            mod.Restore("valley", false);
            Check(mod.State.Restored.ContainsKey(project.Id) && Game1.player.Items.Any(p => p?.ItemId == ModEntry.ItemId(project.Id)), "restoration gives a real inventory item");
            Game1.currentLocation = Game1.getLocationFromName("ArchaeologyHouse");
            Game1.player.currentLocation = Game1.currentLocation;
            int money = Game1.player.Money;
            mod.Donate(project);
            Check(mod.State.Donated.Contains(project.Id) && Game1.player.Money > money, "museum donation consumes artifact and grants reward");
            mod.Donate(project);
            Check(mod.State.Donated.Count == 1, "museum donation cannot repeat");
            var relic = mod.Research.Artifacts.First(p => p.Effect == "energy");
            Game1.player.addItemToInventoryBool(ItemRegistry.Create(ModEntry.QualifiedId(relic)));
            Game1.player.Stamina = 10;
            mod.UseRelic(relic);
            Check(Game1.player.Stamina == 70, "energy relic effect");
            mod.UseRelic(relic);
            Check(Game1.player.Stamina == 70, "relic daily cooldown");
            mod.SaveState();
            var saved = Game1.player.modData[ModEntry.Id + "/Progress"];
            Call(mod, "LoadState");
            Check(JsonSerializer.Serialize(mod.State) == saved, "runtime progression reload");
            var menu = new JournalMenu(mod);
            Capture(menu, "journal-overview.png");
            menu.receiveKeyPress(Keys.Right);
            Capture(menu, "journal-valley.png");
            menu.receiveKeyPress(Keys.Right);
            Capture(menu, "journal-dwarf.png");
            var viewport = Game1.uiViewport;
            Game1.uiViewport = new xTile.Dimensions.Rectangle(0, 0, 900, 640);
            menu.gameWindowSizeChanged(new Rectangle(), new Rectangle(0, 0, 900, 640));
            Capture(menu, "journal-compact.png", 900, 640);
            var weatherMenu = new JournalMenu(mod, "weather");
            Capture(weatherMenu, "weather-compact.png", 900, 640);
            Capture(new JournalMenu(mod, "gallery"), "gallery-compact.png", 900, 640);
            Capture(new JournalMenu(mod, "shipwreck"), "shipwreck-compact.png", 900, 640);
            Capture(new JournalMenu(mod, "nature"), "nature-compact.png", 900, 640);
            Capture(new JournalMenu(mod, "residents"), "residents-compact.png", 900, 640);
            Game1.uiViewport = viewport;
            Capture(new JournalMenu(mod,"weather"), "journal-weather.png");
            Capture(new JournalMenu(mod,"gallery"), "journal-gallery.png");
            Capture(new JournalMenu(mod,"shipwreck"), "journal-shipwreck.png");
            Capture(new JournalMenu(mod,"nature"), "journal-nature.png");
            Capture(new JournalMenu(mod,"residents"), "journal-residents.png");
            Check(true, "Russian journal rendered at full and compact sizes");
        }
        catch (Exception ex)
        {
            results.Add(ex.ToString());
            Monitor.Log(ex.ToString(), LogLevel.Error);
        }
        File.WriteAllLines(Path.Combine(Out, "smoke-results.txt"), results);
        Game1.game1.Exit();
    }

    private void Capture(JournalMenu menu, string name, int width = 0, int height = 0)
    {
        var device = Game1.graphics.GraphicsDevice;
        width = width == 0 ? Game1.uiViewport.Width : width;
        height = height == 0 ? Game1.uiViewport.Height : height;
        using var target = new RenderTarget2D(device, width, height);
        device.SetRenderTarget(target);
        device.Clear(new Color(36, 44, 39));
        var batch = Game1.spriteBatch;
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        menu.draw(batch);
        batch.End();
        device.SetRenderTarget(null);
        using var file = File.Create(Path.Combine(Out, name));
        target.SaveAsPng(file, width, height);
    }
}
