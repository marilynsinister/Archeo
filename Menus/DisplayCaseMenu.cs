using Archeo.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Network;
using StardewValley.Objects;

namespace Archeo.Menus;

/// <summary>Click-to-transfer keeps every item in a real inventory; nothing is left on a menu cursor.</summary>
internal sealed class DisplayCaseMenu : IClickableMenu
{
    private readonly ModEntry mod;
    private readonly Furniture furniture;
    private readonly Chest storage;
    private readonly NetMutex mutex;
    private readonly List<Rectangle> exhibits = new();
    private readonly List<Rectangle> backpack = new();
    private Rectangle caption;
    private int cell;
    private float scale;
    private bool released;
    private string hover = "";
    private static readonly Color Ink = new(71, 47, 35);

    internal DisplayCaseMenu(ModEntry mod, Furniture furniture, Chest storage, NetMutex mutex) : base(0, 0, 100, 100, true)
    {
        this.mod = mod; this.furniture = furniture; this.storage = storage; this.mutex = mutex;
        Layout();
    }

    private void Layout()
    {
        width = Math.Min(900, Game1.uiViewport.Width - 32);
        height = Math.Min(690, Game1.uiViewport.Height - 32);
        xPositionOnScreen = (Game1.uiViewport.Width - width) / 2;
        yPositionOnScreen = (Game1.uiViewport.Height - height) / 2;
        scale = width < 800 || height < 600 ? .7f : .85f;
        cell = Math.Min(64, Math.Min((width - 64) / 12, (height - 296) / 3));
        exhibits.Clear(); backpack.Clear();
        // Extra slots are recovery-only if a future version or another mod adds items to the contained chest.
        int count = Math.Max(MuseumCollections.CaseCapacity, storage.Items.Count);
        int columnW = (width - 64) / count;
        for (int i = 0; i < count; i++) exhibits.Add(new Rectangle(xPositionOnScreen + 32 + columnW * i + columnW / 2 - 28, yPositionOnScreen + 100, 56, 56));
        int packX = xPositionOnScreen + (width - cell * 12) / 2;
        int packY = yPositionOnScreen + height - 40 - cell * 3;
        for (int i = 0; i < Math.Min(36, Game1.player.MaxItems); i++)
            backpack.Add(new Rectangle(packX + i % 12 * cell, packY + i / 12 * cell, cell, cell));
        caption = new Rectangle(xPositionOnScreen + 32, yPositionOnScreen + 226, width - 64, 32);
        initializeUpperRightCloseButton();
    }

    private bool CanTransfer => !released && mutex.IsLockHeld() && furniture.Location == Game1.currentLocation
        && Game1.currentLocation.furniture.Contains(furniture) && furniture.heldObject.Value == storage;

    public override void update(GameTime time)
    {
        base.update(time);
        if (!CanTransfer) exitThisMenu();
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds) => Layout();

    protected override void cleanupBeforeExit()
    {
        Release();
        base.cleanupBeforeExit();
    }

    internal void Release()
    {
        if (!released)
        {
            if (mutex.IsLockHeld())
            {
                if (!storage.Items.Any(p => p != null) && furniture.heldObject.Value == storage)
                    furniture.heldObject.Value = null;
                mutex.ReleaseLock();
            }
            released = true;
        }
    }

    private void Deposit(int index)
    {
        if (!CanTransfer || index >= Game1.player.Items.Count) return;
        var item = Game1.player.Items[index];
        if (item == null) return;
        if (!mod.IsExhibit(item)) { mod.Notify(mod.T("gallery.artifacts-only")); return; }
        int slot = Enumerable.Range(0, MuseumCollections.CaseCapacity).FirstOrDefault(i => i >= storage.Items.Count || storage.Items[i] == null, -1);
        if (slot < 0) { mod.Notify(mod.T("gallery.full")); return; }
        var exhibit = item.getOne();
        exhibit.Stack = 1; // getOne preserves quality and item modData.
        while (storage.Items.Count <= slot) storage.Items.Add(null!);
        storage.Items[slot] = exhibit;
        if (item.Stack == 1) Game1.player.Items[index] = null!; else item.Stack--;
        Game1.playSound("dwop");
    }

    private void Retrieve(int index)
    {
        if (!CanTransfer || index >= storage.Items.Count || storage.Items[index] is not Item item) return;
        if (!Game1.player.couldInventoryAcceptThisItem(item)) { mod.Notify(mod.T("message.inventory-full")); return; }
        if (!Game1.player.addItemToInventoryBool(item)) return;
        storage.Items[index] = null!;
        Game1.playSound("dwop");
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (upperRightCloseButton?.containsPoint(x, y) == true) { exitThisMenu(); return; }
        if (!CanTransfer) return;
        int index = exhibits.FindIndex(p => p.Contains(x, y));
        if (index >= 0) { Retrieve(index); return; }
        index = backpack.FindIndex(p => p.Contains(x, y));
        if (index >= 0) { Deposit(index); return; }
        if (caption.Contains(x, y))
        {
            string[] options = { "general", "valley", "dwarf", "sea" };
            string current = furniture.modData.TryGetValue(ModEntry.CaptionKey, out string? value) ? value : "general";
            furniture.modData[ModEntry.CaptionKey] = options[(Array.IndexOf(options, current) + 1) % options.Length];
            Game1.playSound("smallSelect");
        }
    }

    public override void receiveKeyPress(Keys key)
    {
        if (key == Keys.Escape || Game1.options.menuButton.Any(p => p.key == key)) exitThisMenu();
    }

    private void Text(SpriteBatch b, string text, int x, int y, int maxWidth, Color? color = null)
    {
        float fit = Math.Min(scale, maxWidth / Math.Max(1, Game1.smallFont.MeasureString(text).X));
        b.DrawString(Game1.smallFont, text, new Vector2(x, y), color ?? Ink, 0, Vector2.Zero, fit, SpriteEffects.None, 1);
    }

    private static void Slot(SpriteBatch b, Rectangle area)
    {
        b.Draw(Game1.staminaRect, area, new Color(102, 68, 48));
        b.Draw(Game1.staminaRect, new Rectangle(area.X + 2, area.Y + 2, area.Width - 4, area.Height - 4), new Color(226, 204, 162));
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * .65f);
        drawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);
        Text(b, mod.T("gallery.case-name"), xPositionOnScreen + 32, yPositionOnScreen + 25, width - 80);
        Text(b, mod.T("gallery.transfer-hint"), xPositionOnScreen + 32, yPositionOnScreen + 64, width - 64);
        for (int i = 0; i < exhibits.Count; i++)
        {
            var rect = exhibits[i];
            Slot(b, rect);
            Item? item = i < storage.Items.Count ? storage.Items[i] : null;
            item?.drawInMenu(b, new Vector2(rect.X + 4, rect.Y + 4), .75f, 1, 1, StackDrawType.Hide, Color.White, false);
            int labelW = (width - 64) / exhibits.Count - 6;
            Text(b, item?.DisplayName ?? mod.T("gallery.empty-slot"), rect.Center.X - labelW / 2, rect.Bottom + 8, labelW);
        }
        b.Draw(Game1.staminaRect, caption, new Color(220, 193, 145));
        Text(b, mod.T("gallery.change-caption", new { name = mod.GalleryCaption(furniture) }), caption.X + 8, caption.Y + 3, caption.Width - 16);
        for (int i = 0; i < backpack.Count; i++)
        {
            var rect = backpack[i]; Slot(b, rect);
            if (i >= Game1.player.Items.Count || Game1.player.Items[i] is not Item item) continue;
            bool eligible = mod.IsExhibit(item);
            int iconSize = Math.Max(1, cell / 16) * 16;
            float itemScale = iconSize / 64f;
            item.drawInMenu(b, new Vector2(rect.X + (cell - iconSize) / 2, rect.Y + (cell - iconSize) / 2), itemScale, eligible ? 1 : .35f, 1, StackDrawType.Draw, Color.White, false);
        }
        upperRightCloseButton?.draw(b);
        drawMouse(b);
        if (hover.Length > 0) drawHoverText(b, hover, Game1.smallFont);
    }

    public override void performHoverAction(int x, int y)
    {
        hover = "";
        int exhibit = exhibits.FindIndex(p => p.Contains(x, y));
        if (exhibit >= 0 && exhibit < storage.Items.Count && storage.Items[exhibit] is Item shown)
            hover = shown.DisplayName;
        int index = backpack.FindIndex(p => p.Contains(x, y));
        if (index >= 0 && index < Game1.player.Items.Count && Game1.player.Items[index] is Item item)
            hover = item.DisplayName + (mod.IsExhibit(item) ? "" : "\n" + mod.T("gallery.artifacts-only"));
    }
}
