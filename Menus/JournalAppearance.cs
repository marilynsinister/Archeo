using Archeo.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace Archeo.Menus;

internal sealed partial class JournalMenu
{
    private static readonly Color Leather = new(43, 70, 60);
    private static readonly Color LeatherDark = new(25, 43, 38);
    private static readonly Color LeatherLight = new(73, 99, 77);
    private static readonly Color Brass = new(185, 144, 76);
    private static readonly Color BrassLight = new(234, 202, 135);
    private static readonly Color PageEdge = new(203, 178, 132);

    private static void Fill(SpriteBatch b, int x, int y, int w, int h, Color color)
    {
        if (w > 0 && h > 0) b.Draw(Game1.staminaRect, new Rectangle(x, y, w, h), color);
    }

    // Stepped corners and whole-pixel highlights stay crisp at Stardew's UI scale.
    private static void PixelPanel(SpriteBatch b, Rectangle r, Color border, Color fill, int corner = 3)
    {
        Fill(b, r.X + corner, r.Y, r.Width - corner * 2, r.Height, border);
        Fill(b, r.X, r.Y + corner, r.Width, r.Height - corner * 2, border);
        Fill(b, r.X + 2, r.Y + corner, r.Width - 4, r.Height - corner * 2, fill);
        Fill(b, r.X + corner, r.Y + 2, r.Width - corner * 2, r.Height - 4, fill);
    }

    private static void Diamond(SpriteBatch b, int x, int y, Color color)
    {
        Fill(b, x - 1, y - 3, 2, 6, color);
        Fill(b, x - 3, y - 1, 6, 2, color);
    }

    private static void DrawPageRule(SpriteBatch b, int x, int y, int w)
    {
        Fill(b, x, y, w, 1, PageEdge);
        Diamond(b, x + w / 2, y, Brass);
    }

    private void DrawBook(SpriteBatch b)
    {
        int x = xPositionOnScreen, y = yPositionOnScreen;
        b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), new Color(13, 23, 20) * .78f);
        PixelPanel(b, new Rectangle(x + 7, y + 14, width, height), Color.Black * .12f, Color.Black * .12f, 10);
        PixelPanel(b, new Rectangle(x + 3, y + 8, width, height), Color.Black * .22f, Color.Black * .22f, 8);
        PixelPanel(b, new Rectangle(x, y, width, height), LeatherDark, Leather, 8);
        Fill(b, x + 10, y + 4, width - 20, 2, LeatherLight);
        Fill(b, x + 4, y + 10, 2, height - 20, LeatherLight);
        Fill(b, x + width - 6, y + 10, 2, height - 20, LeatherDark);
        Fill(b, x + 10, y + height - 6, width - 20, 2, LeatherDark);
        // Small dashes follow the cover like a saddle-stitched field notebook.
        for (int sx = x + 38; sx < x + width - 38; sx += 12)
        {
            Fill(b, sx, y + 12, 5, 1, LeatherLight);
            Fill(b, sx, y + height - 12, 5, 1, LeatherLight);
        }
        for (int sy = y + 40; sy < y + height - 36; sy += 12)
        {
            Fill(b, x + 11, sy, 1, 5, LeatherLight);
            Fill(b, x + width - 12, sy, 1, 5, LeatherLight);
        }

        int pageX = x + 20, pageY = y + 92, pageW = width - 40, pageH = height - 108;
        Fill(b, pageX + 3, pageY + 6, pageW, pageH + 2, new Color(132, 111, 80));
        Fill(b, pageX, pageY + 2, pageW, pageH + 4, PageEdge);
        for (int i = 0; i < 3; i++)
            Fill(b, pageX + 3, pageY + pageH + i * 2, pageW - 6, 1, new Color(237, 219, 181));
        Fill(b, pageX, pageY, pageW, pageH, Paper);
        Fill(b, pageX, pageY, 3, pageH, new Color(223, 201, 161));
        Fill(b, pageX + 3, pageY, 3, pageH, new Color(240, 224, 188));
        Fill(b, pageX + pageW - 5, pageY, 5, pageH, new Color(236, 219, 180));

        int gutter = right.X - 12;
        Fill(b, left.X, left.Y - 5, left.Width, left.Height + 10, new Color(244, 232, 201));
        for (int i = 0; i < 8; i++)
        {
            Fill(b, gutter - 16 + i * 2, y + 142, 2, height - 194, Color.Lerp(Paper, PageEdge, (i + 1) / 10f));
            Fill(b, gutter + i * 2, y + 142, 2, height - 194, Color.Lerp(PageEdge, Paper, i / 7f));
        }
        Fill(b, gutter - 1, y + 142, 2, height - 194, new Color(169, 137, 94));
        for (int sy = left.Y + 18; sy < left.Bottom; sy += 46)
        {
            Fill(b, gutter - 4, sy - 1, 9, 4, new Color(171, 138, 94));
            Fill(b, gutter - 5, sy, 9, 1, new Color(248, 229, 190));
        }

        // A quiet margin ornament leaves all text and list rows on clean paper.
        DrawPageRule(b, left.X + 8, left.Y - 8, left.Width - 16);
        DrawPageRule(b, right.X + 8, right.Y - 8, right.Width - 16);
        Fill(b, x + 34, y + height - 52, width - 68, 1, PageEdge * .65f);
        DrawCoverCorner(b, x + 7, y + 7, 1, 1);
        DrawCoverCorner(b, x + width - 8, y + 7, -1, 1);
        DrawCoverCorner(b, x + 7, y + height - 8, 1, -1);
        DrawCoverCorner(b, x + width - 8, y + height - 8, -1, -1);

        // Folded paper at the outer corner, outside the content and footer.
        for (int i = 0; i < 16; i += 2)
            Fill(b, pageX + pageW - 17 + i, pageY + pageH - 16, 2, i, new Color(218, 195, 153));
    }

    private static void DrawCoverCorner(SpriteBatch b, int x, int y, int dx, int dy)
    {
        for (int i = 0; i < 4; i++)
        {
            int length = 25 - i * 5;
            Fill(b, dx > 0 ? x : x - length + 2, y + dy * i * 2, length, 2, i == 0 ? BrassLight : Brass);
            Fill(b, x + dx * i * 2, dy > 0 ? y : y - length + 2, 2, length, i == 0 ? BrassLight : Brass);
        }
        Fill(b, x + dx * 7, y + dy * 7, 3, 3, LeatherDark);
        Fill(b, x + dx * 7, y + dy * 7, 2, 2, BrassLight);
    }

    private void DrawBookHeading(SpriteBatch b)
    {
        int x = xPositionOnScreen, y = yPositionOnScreen;
        var seal = new Rectangle(x + 32, y + 27, 48, 48);
        PixelPanel(b, seal, Brass, LeatherDark, 8);
        b.Draw(mod.Atlas, new Vector2(seal.X + 4, seal.Y + 4), Sprite(21), Color.White, 0, Vector2.Zero, 2.5f, SpriteEffects.None, 1);
        string title = mod.T("journey.book-title");
        float titleScale = Math.Min(1.22f, (width - 150f) / Math.Max(1, Game1.smallFont.MeasureString(title).X));
        Text(b, title, x + 94, y + 25, Paper, titleScale);
        string counter = mod.T("journey.book-counter", new { count = mod.State.Restored.Count, level = Research.Level(mod.State.Experience) });
        Text(b, counter, x + 95, y + 60, BrassLight, Math.Min(.8f, (width - 160f) / Math.Max(1, Game1.smallFont.MeasureString(counter).X)));
        // A pressed botanical sprig, drawn in the same pixel grid as the cover.
        if (width >= 1050 && TabSection(tab) == tab)
        {
            int sx = x + width - 94, sy = y + 36;
            Fill(b, sx + 15, sy, 2, 36, Brass);
            for (int i = 0; i < 3; i++)
            {
                Fill(b, sx + 5, sy + 6 + i * 10, 10, 3, Brass);
                Fill(b, sx + 1, sy + 3 + i * 10, 7, 3, BrassLight);
                Fill(b, sx + 17, sy + 2 + i * 10, 10, 3, Brass);
                Fill(b, sx + 24, sy - 1 + i * 10, 6, 3, BrassLight);
            }
        }
    }

    private static void DrawButtonSurface(SpriteBatch b, Rectangle r, bool enabled, bool active, bool hovered, bool indexTab)
    {
        Color border = !enabled ? new Color(191, 178, 151) : active ? LeatherLight : hovered ? Brass : new Color(185, 163, 124);
        Color fill = !enabled ? new Color(231, 222, 200) : active ? new Color(218, 227, 199) : hovered ? new Color(251, 237, 202) : new Color(242, 229, 199);
        if (indexTab && active) { border = LeatherDark; fill = Leather; }
        Fill(b, r.X + 3, r.Bottom, r.Width - 6, 2, new Color(153, 126, 86) * .22f);
        PixelPanel(b, r, border, fill);
        Fill(b, r.X + 4, r.Y + 2, r.Width - 8, 1, indexTab && active ? LeatherLight : Color.White * .35f);
        if (active)
        {
            if (indexTab)
            {
                Fill(b, r.X + 8, r.Bottom - 4, r.Width - 16, 2, Brass);
                Diamond(b, r.Center.X, r.Bottom - 3, BrassLight);
            }
            else Fill(b, r.X + 2, r.Y + 5, 3, r.Height - 10, LeatherLight);
        }
    }

    private static void DrawSpecimenMount(SpriteBatch b, Rectangle r, Color accent)
    {
        PixelPanel(b, r, PageEdge, new Color(237, 222, 187), 6);
        Fill(b, r.X + 6, r.Y + 5, r.Width - 12, 2, new Color(255, 247, 222));
        Fill(b, r.X + 6, r.Bottom - 7, r.Width - 12, 2, new Color(213, 191, 149));
        Diamond(b, r.Center.X, r.Bottom - 4, accent);
    }

    private void DrawBookCloseButton(SpriteBatch b)
    {
        if (upperRightCloseButton == null) return;
        Rectangle r = upperRightCloseButton.bounds;
        bool hovered = r.Contains(Game1.getMouseX(), Game1.getMouseY());
        PixelPanel(b, r, LeatherDark, hovered ? LeatherLight : Leather, 4);
        PixelPanel(b, new Rectangle(r.X + 3, r.Y + 3, r.Width - 6, r.Height - 6), Brass, hovered ? LeatherLight : Leather, 3);
        for (int i = -4; i <= 4; i++)
        {
            Fill(b, r.Center.X + i * 2 - 1, r.Center.Y + i * 2 - 1, 3, 3, BrassLight);
            Fill(b, r.Center.X + i * 2 - 1, r.Center.Y - i * 2 - 1, 3, 3, BrassLight);
        }
    }
}
