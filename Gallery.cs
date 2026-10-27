using Archeo.Core;
using Archeo.Menus;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Network;
using StardewValley.Objects;
using SObject = StardewValley.Object;

namespace Archeo;

public sealed partial class ModEntry
{
    private const string GalleryMutexKey = Id + "/MuseumCases";
    internal const string CaptionKey = Id + "/Caption";
    internal Texture2D GalleryTexture { get; private set; } = null!;
    private bool galleryHooksReady;
    private bool galleryRequestPending;
    internal static bool IsGallery(Furniture furniture) => furniture.ItemId == ItemId("gallery_case");
    internal NetMutex? GalleryMutex => Game1.player.team.globalInventoryMutexes.TryGetValue(GalleryMutexKey, out var mutex) ? mutex : null;

    private void InitializeGallery()
    {
        // The team updates this vanilla mutex, including releasing locks after a disconnect.
        // Only the host creates it; clients wait for the same replicated instance.
        if (Context.IsMainPlayer) Game1.player.team.GetOrCreateGlobalInventoryMutex(GalleryMutexKey);
    }

    internal bool IsExhibit(Item item)
        => item is SObject obj && !obj.bigCraftable.Value && (obj.Type == "Arch"
            || Research.Artifacts.Any(p => p.Effect != "lamp" && QualifiedId(p) == item.QualifiedItemId));

    internal string GalleryCaption(Furniture furniture)
    {
        string caption = furniture.modData.TryGetValue(CaptionKey, out string? value) ? value : "general";
        return T("gallery.caption." + (MuseumCollections.All.Any(p => p.Id == caption) ? caption : "general"));
    }

    private bool TryGalleryInteraction(Vector2 tile, Vector2 cursor)
    {
        var furniture = Game1.currentLocation.furniture.FirstOrDefault(p => IsGallery(p)
            && Vector2.Distance(Game1.player.Tile, p.TileLocation) <= 3
            && (p.GetBoundingBox().Contains(tile * 64 + new Vector2(32))
                || new Rectangle(p.GetBoundingBox().X, p.GetBoundingBox().Bottom - 128, 128, 128).Contains(cursor)));
        if (furniture == null) return false;
        OpenGallery(furniture, Game1.player);
        return true;
    }

    internal void OpenGallery(Furniture furniture, Farmer who)
    {
        if (!who.IsLocalPlayer || Game1.activeClickableMenu != null || states.Value == null) return;
        var mutex = GalleryMutex;
        if (galleryRequestPending || mutex == null || mutex.IsLocked()) { Notify(T("gallery.busy")); return; }
        var location = Game1.currentLocation;
        galleryRequestPending = true;
        mutex.RequestLock(() =>
        {
            galleryRequestPending = false;
            if (!Context.IsWorldReady || Game1.currentLocation != location || Game1.activeClickableMenu != null
                || !location.furniture.Contains(furniture) || Vector2.Distance(who.Tile, furniture.TileLocation) > 4)
            { mutex.ReleaseLock(); return; }
            // Both the furniture and its contained chest are vanilla save types. Never overwrite an unexpected held item.
            if (furniture.heldObject.Value != null && furniture.heldObject.Value is not Chest)
            { mutex.ReleaseLock(); Notify(T("gallery.unexpected")); return; }
            var storage = furniture.heldObject.Value as Chest ?? new Chest(true);
            furniture.heldObject.Value = storage;
            Game1.activeClickableMenu = new DisplayCaseMenu(this, furniture, storage, mutex);
        }, () => { galleryRequestPending = false; Notify(T("gallery.busy")); });
    }

    internal bool CanCraftGallery => galleryHooksReady && MuseumCollections.CanCraftCase(State.Workshop, MaterialCount("388"), MaterialCount("335"), MaterialCount("338"), true);

    internal void CraftGallery()
    {
        if (!CanCraftGallery) { Notify(T("gallery.requirements")); return; }
        var item = ItemRegistry.Create("(F)" + ItemId("gallery_case"));
        if (!Game1.player.couldInventoryAcceptThisItem(item)) { Notify(T("message.inventory-full")); return; }
        if (!Game1.player.addItemToInventoryBool(item)) return;
        ConsumeMaterial("388", MuseumCollections.CaseWood);
        ConsumeMaterial("335", MuseumCollections.CaseIron);
        ConsumeMaterial("338", MuseumCollections.CaseQuartz);
        Game1.playSound("crafting");
        Notify(T("gallery.crafted"));
    }

    internal void ClaimCollection(string id)
    {
        if (!MuseumCollections.CanClaim(State, id, true)) return;
        var collection = MuseumCollections.All.First(p => p.Id == id);
        var item = ItemRegistry.Create("(F)" + ItemId(collection.Reward));
        if (!Game1.player.couldInventoryAcceptThisItem(item)) { Notify(T("message.inventory-full")); return; }
        if (!Game1.player.addItemToInventoryBool(item)) return;
        MuseumCollections.RecordClaim(State, id, true);
        SaveState();
        Game1.playSound("reward");
        Notify(T("gallery.reward-received", new { name = T("gallery.reward." + collection.Reward) }));
    }

    private void RegisterGalleryFurniture(IDictionary<string, string> data)
    {
        foreach (var entry in new[] {
            ("gallery_case", "other", "2 2", "2 1", 2, "gallery.case-name"),
            ("ancient_map", "painting", "2 2", "2 2", 0, "gallery.reward.ancient_map"),
            ("mechanical_clock", "other", "1 2", "1 1", 2, "gallery.reward.mechanical_clock"),
            ("sea_mosaic", "painting", "2 2", "2 2", 0, "gallery.reward.sea_mosaic")
        })
            data[ItemId(entry.Item1)] = $"{ItemId(entry.Item1)}/{entry.Item2}/{entry.Item3}/{entry.Item4}/1/0/{entry.Item5}/{T(entry.Item6)}/0/{Helper.ModContent.GetInternalAssetName("assets/" + entry.Item1 + ".png").Name}/true";
    }

    private void InstallGalleryPatches(Harmony harmony)
    {
        GalleryPatches.Mod = this;
        // Patch the vanilla click-to-remove-held-object path before allowing any new cases to be built.
        harmony.Patch(AccessTools.Method(typeof(Furniture), nameof(Furniture.clicked)), prefix: new HarmonyMethod(typeof(GalleryPatches), nameof(GalleryPatches.Clicked)));
        harmony.Patch(AccessTools.Method(typeof(Furniture), nameof(Furniture.checkForAction)), prefix: new HarmonyMethod(typeof(GalleryPatches), nameof(GalleryPatches.Action)));
        harmony.Patch(AccessTools.Method(typeof(Furniture), nameof(Furniture.canBeRemoved)), prefix: new HarmonyMethod(typeof(GalleryPatches), nameof(GalleryPatches.Remove)));
        harmony.Patch(AccessTools.Method(typeof(SObject), nameof(SObject.performToolAction), new[] { typeof(Tool) }), prefix: new HarmonyMethod(typeof(GalleryPatches), nameof(GalleryPatches.Tool)));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(Furniture), nameof(Furniture.draw), new[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float) }),
            prefix: new HarmonyMethod(typeof(GalleryPatches), nameof(GalleryPatches.Draw)));
        galleryHooksReady = true;
    }

    private void DrawGalleryHover(object? sender, RenderedHudEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsPlayerFree) return;
        Vector2 cursor = Helper.Input.GetCursorPosition().AbsolutePixels;
        var furniture = Game1.currentLocation.furniture.FirstOrDefault(p => IsGallery(p)
            && new Rectangle(p.GetBoundingBox().X, p.GetBoundingBox().Bottom - 128, 128, 128).Contains(cursor)
            && Vector2.Distance(Game1.player.Tile, p.TileLocation) <= 4);
        if (furniture == null) return;
        var labels = furniture.heldObject.Value is Chest storage
            ? storage.Items.Where(p => p != null).Select(p => p.DisplayName).ToArray() : Array.Empty<string>();
        string text = GalleryCaption(furniture) + "\n" + (labels.Length > 0 ? string.Join("\n", labels) : T("gallery.empty-slot"));
        StardewValley.Menus.IClickableMenu.drawHoverText(e.SpriteBatch, text, Game1.smallFont);
    }

    internal void DrawGallery(Furniture furniture, SpriteBatch b, float alpha)
    {
        var box = furniture.GetBoundingBox();
        Vector2 position = Game1.GlobalToLocal(Game1.viewport, new Vector2(box.X, box.Bottom - 128));
        float depth = Math.Clamp((box.Bottom - 8) / 10000f, 0, .999f);
        b.Draw(GalleryTexture, position, null, Color.White * alpha, 0, Vector2.Zero, 4, SpriteEffects.None, depth);
        if (furniture.heldObject.Value is not Chest storage) return;
        for (int i = 0; i < Math.Min(MuseumCollections.CaseCapacity, storage.Items.Count); i++)
        {
            var item = storage.Items[i];
            if (item == null) continue;
            var data = ItemRegistry.GetDataOrErrorItem(item.QualifiedItemId);
            b.Draw(data.GetTexture(), position + new Vector2(12 + i * 36, 44), data.GetSourceRect(), Color.White * alpha, 0, Vector2.Zero, 2, SpriteEffects.None, Math.Min(.9999f, depth + .00001f));
        }
    }
}

internal static class GalleryPatches
{
    internal static ModEntry Mod = null!;
    internal static bool Clicked(Furniture __instance, Farmer __0, ref bool __result)
    {
        if (!ModEntry.IsGallery(__instance) || __instance.heldObject.Value == null && Mod.GalleryMutex?.IsLocked() != true) return true;
        Game1.haltAfterCheck = false;
        __result = true;
        Mod.OpenGallery(__instance, __0);
        return false;
    }
    internal static bool Action(Furniture __instance, Farmer __0, bool __1, ref bool __result)
    {
        if (ModEntry.IsGallery(__instance))
        {
            __result = true;
            if (!__1) Mod.OpenGallery(__instance, __0);
            return false;
        }
        if (__instance.ItemId == ModEntry.ItemId("mechanical_clock"))
        {
            __result = true;
            if (!__1 && __0.IsLocalPlayer) Game1.drawObjectDialogue(Mod.T("gallery.clock-time", new { time = Game1.getTimeOfDayString(Game1.timeOfDay) }));
            return false;
        }
        return true;
    }
    internal static bool Remove(Furniture __instance, ref bool __result)
    {
        if (!ModEntry.IsGallery(__instance) || Mod.GalleryMutex?.IsLocked() != true) return true;
        __result = false;
        return false;
    }
    internal static bool Tool(SObject __instance, ref bool __result)
    {
        if (__instance is not Furniture furniture || !ModEntry.IsGallery(furniture)) return true;
        __result = false;
        return false;
    }
    internal static bool Draw(Furniture __instance, SpriteBatch __0, float __3)
    {
        if (!ModEntry.IsGallery(__instance)) return true;
        Mod.DrawGallery(__instance, __0, __3);
        return false;
    }
}
