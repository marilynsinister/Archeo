using Archeo.Menus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace Archeo;

public sealed partial class ModEntry
{
    private bool TryGetCharacterMenuButtons(out GameMenu menu, out Rectangle journal, out Rectangle collections)
    {
        menu = (Game1.activeClickableMenu as GameMenu)!;
        journal = collections = Rectangle.Empty;
        if (!Context.IsWorldReady || states.Value == null || menu == null || menu.invisible
            || GameMenu.forcePreventClose || menu.GetChildMenu() != null || menu.tabs.Count == 0
            || menu.GetCurrentPage() is not (InventoryPage or SkillsPage)) return false;

        // Use the free end of the tab row, before the close button. Recalculate
        // from the current menu and UI viewport to follow resizing and UI scale.
        int x = menu.tabs.Max(p => p.bounds.Right) + 8;
        int right = Math.Min(Game1.uiViewport.Width - 8, menu.xPositionOnScreen + menu.width - 8);
        if (menu.upperRightCloseButton != null) right = Math.Min(right, menu.upperRightCloseButton.bounds.Left - 8);
        int size = Math.Min(48, (right - x - 4) / 2);
        if (size < 32 || x < 4 || Game1.uiViewport.Height < size + 8) return false;
        int y = Math.Clamp(menu.tabs.Min(p => p.bounds.Top) + 8, 4, Game1.uiViewport.Height - size - 4);
        journal = new Rectangle(x, y, size, size);
        collections = new Rectangle(journal.Right + 4, y, size, size);
        return true;
    }

    private void DrawCharacterMenuButtons(object? sender, RenderedActiveMenuEventArgs e)
    {
        if (!TryGetCharacterMenuButtons(out var menu, out var journal, out var collections)) return;
        int x = Game1.getMouseX(true), y = Game1.getMouseY(true);
        bool enabled = menu.readyToClose();
        DrawCharacterMenuButton(e.SpriteBatch, journal, Atlas, new Rectangle(80, 32, 16, 16), enabled, journal.Contains(x, y));
        bool galleryOpen=Core.Journey.IsOpen(State,"gallery");
        if(galleryOpen) DrawCharacterMenuButton(e.SpriteBatch, collections, GalleryTexture, new Rectangle(0, 0, 32, 32), enabled, collections.Contains(x, y));
        string? key = journal.Contains(x, y) ? "menu.journal" : galleryOpen && collections.Contains(x, y) ? "menu.collections" : null;
        if (key == null) return;
        IClickableMenu.drawHoverText(e.SpriteBatch, T(key) + "\n" + T(key + "-help"), Game1.smallFont);
        if (!Game1.options.hardwareCursor) menu.drawMouse(e.SpriteBatch);
    }

    private static void DrawCharacterMenuButton(SpriteBatch b, Rectangle bounds, Texture2D texture, Rectangle source, bool enabled, bool hovered)
    {
        b.Draw(Game1.staminaRect, bounds, new Color(130, 94, 58));
        var fill = new Rectangle(bounds.X + 2, bounds.Y + 2, bounds.Width - 4, bounds.Height - 4);
        b.Draw(Game1.staminaRect, fill, !enabled ? new Color(209, 195, 166) : hovered ? new Color(238, 207, 148) : new Color(230, 208, 163));
        int size = bounds.Width - 16;
        b.Draw(texture, new Rectangle(bounds.X + 8, bounds.Y + 8, size, size), source, enabled ? Color.White : Color.White * .5f);
    }

    private bool TryHandleCharacterMenuButton(ButtonPressedEventArgs e)
    {
        if (e.Button != SButton.MouseLeft || !TryGetCharacterMenuButtons(out var menu, out var journal, out var collections)) return false;
        int x = Game1.getMouseX(true), y = Game1.getMouseY(true);
        string? tab = journal.Contains(x, y) ? "overview" : Core.Journey.IsOpen(State,"gallery") && collections.Contains(x, y) ? "gallery" : null;
        if (tab == null) return false;
        Helper.Input.Suppress(e.Button);
        // InventoryPage refuses to close while an item is held by the cursor.
        if (!menu.readyToClose()) return true;
        int returnTab = menu.currentTab;
        var book = new JournalMenu(this, tab);
        book.exitFunction = () =>
        {
            if (Context.IsWorldReady && states.Value != null && Game1.activeClickableMenu == null)
                Game1.activeClickableMenu = new GameMenu(returnTab);
        };
        Game1.playSound("bigSelect");
        Game1.activeClickableMenu = book;
        return true;
    }
}
