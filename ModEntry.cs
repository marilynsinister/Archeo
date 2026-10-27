using System.Text.Json;
using Archeo.Core;
using Archeo.Menus;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.GameData.Objects;
using StardewValley.GameData.BigCraftables;
using StardewValley.Tools;
using SObject = StardewValley.Object;

namespace Archeo;

public sealed partial class ModEntry : Mod
{
    public const string Id = "Archeo.SecretsOfTheValley";
    private const string StateKey = Id + "/Progress";
    private readonly PerScreen<ArchaeologyState?> states = new(() => null);
    private readonly PerScreen<SurveySignal> signals = new(() => new SurveySignal());
    private readonly PerScreen<double> nextSurvey = new(() => 0);
    private readonly HashSet<long> invalidStates = new();
    internal Research Research { get; private set; } = null!;
    internal ModConfig Config { get; private set; } = null!;
    internal Texture2D Atlas { get; private set; } = null!;
    internal ArchaeologyState State => states.Value ?? throw new InvalidOperationException("No loaded archaeology state.");
    internal Random RandomFor(string salt) => new(Core.Research.StableSeed((long)Game1.uniqueIDForThisGame, Game1.player.UniqueMultiplayerID, Today, salt));
    internal static int Today => (int)Game1.stats.DaysPlayed;
    internal string T(string key, object? tokens = null) => Helper.Translation.Get(key, tokens).ToString();
    internal static string ItemId(string artifact) => Id + "_" + artifact;
    internal static string QualifiedId(ArtifactDefinition artifact) => (artifact.Effect == "lamp" ? "(BC)" : "(O)") + ItemId(artifact.Id);
    internal string ArtifactName(ArtifactDefinition artifact) => T("artifact." + artifact.Id + ".name");
    internal string CultureName(string culture) => T("culture." + culture);
    internal static Color RarityColor(int rarity) => rarity switch
    {
        1 => new(71, 119, 62), 2 => new(49, 104, 162), 3 => new(133, 75, 168), 4 => new(170, 112, 24), _ => new(113, 101, 87)
    };

    public override void Entry(IModHelper helper)
    {
        Config = helper.ReadConfig<ModConfig>();
        Config.Normalize();
        helper.WriteConfig(Config);
        Research = new Research(helper.Data.ReadJsonFile<List<ArtifactDefinition>>("assets/artifacts.json") ?? throw new InvalidDataException("Missing artifact catalog."));
        helper.Events.Content.AssetRequested += OnAssetRequested;
        helper.Events.GameLoop.GameLaunched += (_, _) =>
        {
            Atlas = helper.ModContent.Load<Texture2D>("assets/artifacts.png");
            WorkshopAtlas = helper.ModContent.Load<Texture2D>("assets/workshop.png");
            MoleTexture = helper.ModContent.Load<Texture2D>("assets/mole.png");
            GalleryTexture = helper.ModContent.Load<Texture2D>("assets/gallery_case.png");
            ShipwreckAtlas = helper.ModContent.Load<Texture2D>("assets/shipwreck.png");
            PastTexture = helper.ModContent.Load<Texture2D>("assets/past_tracks.png");
        };
        helper.Events.GameLoop.SaveLoaded += (_, _) => { LoadState(); InitializeGallery(); InitializeUnusualRelics(); };
        helper.Events.GameLoop.DayStarted += (_, _) => { if (states.Value != null) { RefreshSites(RefreshFrontiers()); RefreshWeatherSites(); RefreshShipwreckSites(); RefreshResidentSites(); RefreshStarSites(); RefreshNatureSites(); RefreshMuseum(); SendIntroduction(); SaveState(); } };
        helper.Events.GameLoop.DayEnding += (_, _) => { if (states.Value != null) { RememberWeather(); RememberShipwreckWeather(); RememberNature(); ObserveMuseumDisplays(); SaveState(); } };
        helper.Events.GameLoop.Saving += (_, _) => SaveState();
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => { states.ResetAllScreens(); signals.ResetAllScreens(); digFeedback.ResetAllScreens(); companions.ResetAllScreens(); resonance.ResetAllScreens(); bellPulses.ResetAllScreens(); ResetUnusualRelics(); invalidStates.Clear(); galleryRequestPending = false; };
        helper.Events.GameLoop.UpdateTicked += UpdateCompanion;
        helper.Events.GameLoop.UpdateTicked += UpdateFieldSearch;
        helper.Events.Input.ButtonsChanged += OnButtonsChanged;
        helper.Events.Input.ButtonPressed += OnButtonPressed;
        helper.Events.Player.Warped += OnFrontierWarp;
        helper.Events.Display.RenderedWorld += OnRenderedWorld;
        helper.Events.Display.RenderedHud += OnRenderedHud;
        helper.Events.Display.RenderedHud += DrawGalleryHover;
        helper.Events.Display.RenderedActiveMenu += DrawCharacterMenuButtons;
        helper.Events.Display.MenuChanged += (_, e) => { if (e.OldMenu is DisplayCaseMenu gallery) gallery.Release(); };
        helper.ConsoleCommands.Add("archeo", "Archaeology tools: archeo status | journal | goals | catalog | mastery | settings | survey | workshop | weather | collections | shipwreck | nature | residents | validate", OnCommand);
        try
        {
            var harmony = new Harmony(Id);
            DigPatches.Mod = this;
            harmony.Patch(AccessTools.Method(typeof(GameLocation), nameof(GameLocation.digUpArtifactSpot), new[] { typeof(int), typeof(int), typeof(Farmer) }), postfix: new HarmonyMethod(typeof(DigPatches), nameof(DigPatches.ArtifactSpot)));
            harmony.Patch(AccessTools.Method(typeof(Hoe), nameof(Hoe.DoFunction)), prefix: new HarmonyMethod(typeof(DigPatches), nameof(DigPatches.BeforeHoe)), postfix: new HarmonyMethod(typeof(DigPatches), nameof(DigPatches.HoeUsed)));
        }
        catch (Exception ex)
        {
            Monitor.Log($"Could not install excavation hooks: {ex}", LogLevel.Error);
        }
        try { InstallGalleryPatches(new Harmony(Id + ".Gallery")); }
        catch (Exception ex) { Monitor.Log($"Could not install museum hooks: {ex}", LogLevel.Error); }
        try { InstallShipwreckPatch(new Harmony(Id + ".Shipwreck")); }
        catch (Exception ex) { Monitor.Log($"Could not install ship bell interaction: {ex}", LogLevel.Error); }
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
            e.Edit(asset =>
            {
                var data = asset.AsDictionary<string, ObjectData>().Data;
                string texture = Helper.ModContent.GetInternalAssetName("assets/artifacts.png").Name;
                foreach (var artifact in Research.Artifacts.Where(p => p.Effect != "lamp"))
                    data[ItemId(artifact.Id)] = new ObjectData
                    {
                        Name = ItemId(artifact.Id), DisplayName = ArtifactName(artifact),
                        Description = T("rarity." + artifact.Rarity) + " · " + CultureName(artifact.Culture) + "\n" + T("artifact." + artifact.Id + ".description"),
                        Type = "Basic", Category = SObject.CraftingCategory, Price = artifact.Price,
                        Texture = texture, SpriteIndex = artifact.Sprite, Edibility = -300,
                        CanBeGivenAsGift = false, ExcludeFromShippingCollection = true, ExcludeFromRandomSale = true,
                        ContextTags = new List<string> { "archeo_artifact", "archeo_" + artifact.Culture }
                    };
                data[ItemId("journal")] = new ObjectData
                {
                    Name = ItemId("journal"), DisplayName = T("item.journal.name"), Description = T("item.journal.description"),
                    Type = "Basic", Category = SObject.CraftingCategory, Texture = texture, SpriteIndex = 21,
                    Price = 0, Edibility = -300, CanBeGivenAsGift = false, CanBeTrashed = false,
                    ExcludeFromShippingCollection = true, ExcludeFromRandomSale = true
                };
                data[ItemId("surveyor")] = new ObjectData
                {
                    Name = ItemId("surveyor"), DisplayName = T("item.surveyor.name"), Description = T("item.surveyor.description"),
                    Type = "Basic", Category = SObject.CraftingCategory, Texture = texture, SpriteIndex = 22,
                    Price = 0, Edibility = -300, CanBeGivenAsGift = false, CanBeTrashed = false,
                    ExcludeFromShippingCollection = true, ExcludeFromRandomSale = true
                };
            });
        if (e.NameWithoutLocale.IsEquivalentTo("Data/Furniture"))
            e.Edit(asset =>
            {
                var data = asset.AsDictionary<string, string>().Data;
                data[ItemId("display_case")] = $"{ItemId("display_case")}/table/1 2/1 1/1/0/2/{T("workshop.case.name")}/0/{FurnitureTexture("assets/display_case.png")}/true";
                RegisterGalleryFurniture(data);
            });
        if (e.NameWithoutLocale.IsEquivalentTo("Data/BigCraftables"))
            e.Edit(asset =>
            {
                var data = asset.AsDictionary<string, BigCraftableData>().Data;
                foreach (var artifact in Research.Artifacts.Where(p => p.Effect == "lamp"))
                    data[ItemId(artifact.Id)] = new BigCraftableData
                    {
                        Name = ItemId(artifact.Id), DisplayName = ArtifactName(artifact),
                        Description = T("rarity." + artifact.Rarity) + "\n" + T("artifact." + artifact.Id + ".description"),
                        Price = artifact.Price, Fragility = 0, CanBePlacedIndoors = true, CanBePlacedOutdoors = true,
                        IsLamp = true, Texture = Helper.ModContent.GetInternalAssetName("assets/lamps.png").Name,
                        SpriteIndex = artifact.Id == "keeper_lantern" ? 0 : 1
                    };
                data[ItemId(Shipwreck.BellId)] = new BigCraftableData
                {
                    Name = ItemId(Shipwreck.BellId), DisplayName = T("shipwreck.bell-name"), Description = T("shipwreck.bell-description"),
                    Price = 0, Fragility = 0, CanBePlacedIndoors = true, CanBePlacedOutdoors = true,
                    Texture = Helper.ModContent.GetInternalAssetName("assets/ship_bell.png").Name, SpriteIndex = 0
                };
            });
        if (e.NameWithoutLocale.IsEquivalentTo("Data/mail"))
            e.Edit(asset => asset.AsDictionary<string, string>().Data[Id + "_Welcome"] = T("mail.welcome") + "[#]" + T("mail.title"));
    }

    private void LoadState()
    {
        if (Game1.player.modData.TryGetValue(StateKey, out string? json))
        {
            try
            {
                var loaded = JsonSerializer.Deserialize<ArchaeologyState>(json) ?? throw new JsonException("Empty state.");
                Core.Research.ValidateState(loaded);
                states.Value = loaded;
            }
            catch (Exception ex)
            {
                invalidStates.Add(Game1.player.UniqueMultiplayerID);
                Monitor.Log($"Archaeology progress could not be read; the stored data will be preserved. {ex}", LogLevel.Error);
                return;
            }
        }
        else states.Value = new ArchaeologyState();
        Core.Journey.Initialize(State, Research.Artifacts, Game1.player.Items.Any(p => p?.ItemId == ItemId("surveyor")));
        bool frontierOpened=RefreshFrontiers();
        foreach (string culture in Core.Research.Cultures.Where(c => Core.Journey.IsOpen(State, c))) Research.Project(State, culture, RandomFor("project:" + culture));
        RefreshSites(frontierOpened);
        RefreshWeatherSites();
        RefreshShipwreckSites();
        RefreshResidentSites();
        RefreshStarSites();
        RefreshNatureSites();
        RefreshMuseum();
        SendIntroduction();
        SaveState();
    }

    internal void SaveState()
    {
        if (Context.IsWorldReady && states.Value != null && !invalidStates.Contains(Game1.player.UniqueMultiplayerID))
        {
            Core.Journey.Refresh(State, Research.Artifacts);
            FieldJournal.PruneTracking(State,FieldObjectives());
            Game1.player.modData[StateKey] = JsonSerializer.Serialize(State);
        }
    }

    private void SendIntroduction()
    {
        if (State.Introduced) return;
        if (!Game1.player.mailReceived.Contains(Id + "_Welcome") && !Game1.player.mailbox.Contains(Id + "_Welcome"))
            Game1.player.mailbox.Add(Id + "_Welcome");
        State.Introduced = true;
        Notify(T("message.welcome", new { journal = Config.JournalKey.ToString(), survey = Config.SurveyKey.ToString() }));
    }

    private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
    {
        if (!Context.IsWorldReady || states.Value == null || !Context.IsPlayerFree) return;
        if (Config.JournalKey.JustPressed())
        {
            Helper.Input.SuppressActiveKeybinds(Config.JournalKey);
            OpenJournal();
        }
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (TryHandleCharacterMenuButton(e)) return;
        if (!Context.IsWorldReady || states.Value == null || !Context.IsPlayerFree || !e.Button.IsActionButton()) return;
        if (TryIntroInteraction(e.Cursor.GrabTile)) { Helper.Input.Suppress(e.Button); return; }
        if (TryGalleryInteraction(e.Cursor.GrabTile, e.Cursor.AbsolutePixels)) { Helper.Input.Suppress(e.Button); return; }
        if (TryShipBellInteraction(e.Cursor.GrabTile)) { Helper.Input.Suppress(e.Button); return; }
        if (TryShipwreckInteraction(e.Cursor.GrabTile)) { Helper.Input.Suppress(e.Button); return; }
        if (TryWorkshopInteraction(e.Cursor.GrabTile)) { Helper.Input.Suppress(e.Button); return; }
        if(TryMuseumReaction(e.Cursor.GrabTile)){Helper.Input.Suppress(e.Button);return;}
        var item = Game1.player.CurrentItem;
        if (item == null) return;
        if (item.ItemId == ItemId("journal")) { Helper.Input.Suppress(e.Button); OpenJournal(); }
        else if (item.ItemId == ItemId("surveyor")) { Helper.Input.Suppress(e.Button); Survey(); }
        else
        {
            var artifact = Research.Artifacts.FirstOrDefault(p => ItemId(p.Id) == item.ItemId && p.Effect != "" && p.Effect != "lamp");
            if (artifact != null) { Helper.Input.Suppress(e.Button); UseRelic(artifact); }
        }
    }

    internal void OpenJournal() => Game1.activeClickableMenu = new JournalMenu(this);
    internal void Notify(string message) => Game1.addHUDMessage(new HUDMessage(message, HUDMessage.newQuest_type));

    internal void Restore(string culture, bool keystone)
    {
        var artifact = Research.Project(State, culture, RandomFor("project:" + culture));
        var item = ItemRegistry.Create(QualifiedId(artifact));
        if (!Game1.player.couldInventoryAcceptThisItem(item)) { Notify(T("message.inventory-full")); return; }
        if (!Research.Restore(State, culture, keystone, RandomFor("solve:" + culture + ":" + Research.Progress(State, culture).Solves), out artifact)) return;
        Game1.player.addItemToInventoryBool(item);
        DiscoverShipwreck();
        DiscoverResidents();
        Core.Journey.Refresh(State, Research.Artifacts);
        RefreshSites(true);
        RefreshMuseum();
        SaveState();
        Game1.playSound("reward");
        Notify(T("message.restored", new { name = ArtifactName(artifact) }));
    }

    internal void Donate(ArtifactDefinition artifact)
    {
        if (Game1.currentLocation.Name != "ArchaeologyHouse" || !State.Restored.ContainsKey(artifact.Id) || State.Donated.Contains(artifact.Id)) return;
        var item = Game1.player.Items.FirstOrDefault(p => p?.QualifiedItemId == QualifiedId(artifact));
        if (item == null) { Notify(T("message.bring-artifact")); return; }
        if (item.Stack > 1) item.Stack--; else Game1.player.removeItemFromInventory(item);
        State.Donated.Add(artifact.Id);
        RefreshMuseum();
        Game1.player.Money += Math.Max(100, artifact.Price / 2);
        Research.Progress(State, artifact.Culture).Keystones++;
        SaveState();
        Game1.playSound("newArtifact");
        Notify(T("message.donated", new { name = ArtifactName(artifact) }));
    }

    internal void ClaimTools()
    {
        ClaimFieldKit();
    }

    private void OnCommand(string command, string[] args)
    {
        if (args.FirstOrDefault() == "validate")
        {
            foreach (var artifact in Research.Artifacts)
            {
                var item = ItemRegistry.Create(QualifiedId(artifact));
                if (ItemRegistry.GetDataOrErrorItem(item.QualifiedItemId).IsErrorItem) throw new InvalidDataException("Invalid registered artifact: " + artifact.Id);
                if (!Helper.Translation.Get("artifact." + artifact.Id + ".lore").HasValue()) throw new InvalidDataException("Missing lore: " + artifact.Id);
            }
            foreach (string id in MuseumCollections.All.Select(p => p.Reward).Concat(LivingMuseum.Achievements.Select(a=>a.Reward)).Concat(new[] { "display_case", "gallery_case" }))
            {
                var furniture = ItemRegistry.Create("(F)" + ItemId(id));
                if (ItemRegistry.GetDataOrErrorItem(furniture.QualifiedItemId).IsErrorItem) throw new InvalidDataException("Invalid furniture registration: " + id);
                string[] fields = Game1.content.Load<Dictionary<string, string>>("Data/Furniture")[ItemId(id)].Split('/');
                if (fields.Length != 11) throw new InvalidDataException("Invalid slash-delimited furniture data: " + id);
                _ = Game1.content.Load<Texture2D>(fields[9]);
            }
            if (WorkshopAtlas.Width != 96 || WorkshopAtlas.Height != 16 || MoleTexture.Width != 64 || MoleTexture.Height != 64)
                throw new InvalidDataException("Invalid workshop spritesheet dimensions.");
            if (GalleryTexture.Width != 32 || GalleryTexture.Height != 32 || !galleryHooksReady)
                throw new InvalidDataException("Gallery graphics or interaction hooks are unavailable.");
            var bell = ItemRegistry.Create("(BC)" + ItemId(Shipwreck.BellId));
            if (ItemRegistry.GetDataOrErrorItem(bell.QualifiedItemId).IsErrorItem || !bellHookReady || ShipwreckAtlas.Width != 64 || ShipwreckAtlas.Height != 16)
                throw new InvalidDataException("Ship bell registration, story sprites or interaction hook are unavailable.");
            if (PastTexture.Width != 32 || PastTexture.Height != 16 || Atlas.Width != 128 || Atlas.Height != 96)
                throw new InvalidDataException("Invalid relic or spectral effect atlas dimensions.");
            Monitor.Log($"PASS: {Research.Artifacts.Count} artifacts, 10 furniture registrations, ship bell, interaction hooks, sprites, and localized lore.", LogLevel.Info);
            return;
        }
        if (!Context.IsWorldReady || states.Value == null) { Monitor.Log("Load a save first.", LogLevel.Info); return; }
        switch (args.FirstOrDefault())
        {
            case "journal": OpenJournal(); break;
            case "goals": OpenJournal(); break;
            case "catalog": Game1.activeClickableMenu = new JournalMenu(this, "catalog"); break;
            case "mastery": Game1.activeClickableMenu = new JournalMenu(this, "mastery"); break;
            case "settings": Game1.activeClickableMenu = new JournalMenu(this, "settings"); break;
            case "survey": Survey(); break;
            case "workshop": Game1.activeClickableMenu = new JournalMenu(this, "workshop"); break;
            case "weather": Game1.activeClickableMenu = new JournalMenu(this, "weather"); break;
            case "collections": Game1.activeClickableMenu = new JournalMenu(this, "gallery"); break;
            case "museum": Game1.activeClickableMenu = new JournalMenu(this,"museum");break;
            case "shipwreck": Game1.activeClickableMenu = new JournalMenu(this, "shipwreck"); break;
            case "nature": Game1.activeClickableMenu = new JournalMenu(this, "nature"); break;
            case "residents": Game1.activeClickableMenu = new JournalMenu(this, "residents"); break;
            default: Monitor.Log($"Level {Core.Research.Level(State.Experience)}; finds {State.Finds}; restored {State.Restored.Count}/{Research.Artifacts.Count}; active sites {State.Sites.Count}. Journal: {Config.JournalKey}; Survey: {Config.SurveyKey}", LogLevel.Info); break;
        }
    }
}

internal sealed class SurveySignal
{
    public string Location { get; set; } = "";
    public Vector2 Target { get; set; }
    public double Until { get; set; }
    public bool Revealed { get; set; }
    public string SiteKey { get; set; } = "";
    public Vector2 Waypoint { get; set; }
    public Vector2 LastPlayerTile { get; set; } = new(-999, -999);
    public double NextRouteCheck { get; set; }
    public double PulseUntil { get; set; }
}

internal static class DigPatches
{
    internal static ModEntry Mod = null!;
    internal static void ArtifactSpot(GameLocation __instance, int __0, int __1, Farmer __2)
    {
        try { Mod.OnVanillaDig(__instance, __0, __1, __2); }
        catch (Exception ex) { Mod.Monitor.Log($"Artifact spot bonus failed: {ex}", LogLevel.Error); }
    }
    private static readonly System.Reflection.MethodInfo AffectedTiles = AccessTools.Method(typeof(Tool), "tilesAffected");
    internal static void BeforeHoe(Hoe __instance, int __1, int __2, Farmer __4, out List<Vector2> __state)
    {
        __state = new();
        try { __state = (List<Vector2>)AffectedTiles.Invoke(__instance, new object[] { new Vector2(__1 / 64, __2 / 64), __4.toolPower.Value, __4 })!; }
        catch (Exception ex) { Mod.Monitor.Log($"Could not inspect hoe area: {ex}", LogLevel.Error); }
    }
    internal static void HoeUsed(GameLocation __0, Farmer __4, List<Vector2> __state)
    {
        try { foreach (var tile in __state) Mod.OnHoeDig(__0, (int)tile.X, (int)tile.Y, __4); }
        catch (Exception ex) { Mod.Monitor.Log($"Excavation failed: {ex}", LogLevel.Error); }
    }
}
