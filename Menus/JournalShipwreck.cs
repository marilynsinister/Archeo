using Archeo.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace Archeo.Menus;

internal sealed partial class JournalMenu
{
    private void ShipwreckIcon(SpriteBatch b, int slot, Vector2 at, int scale, bool known = true)
        => b.Draw(mod.ShipwreckAtlas, at, new Rectangle(slot * 16, 0, 16, 16), known ? Color.White : new Color(83, 66, 53), 0, Vector2.Zero, scale, SpriteEffects.None, 1);

    private void DrawShipwreck(SpriteBatch b)
    {
        buttons.Remove("tools");
        var story = mod.State.Shipwreck;
        int stage = (int)story.Stage;
        Text(b, mod.T("shipwreck.title"), left.X + 12, left.Y + 8, Ink,
            Math.Min(fontScale, (left.Width - 24) / Math.Max(1, Game1.smallFont.MeasureString(mod.T("shipwreck.title")).X)));
        int[] thresholds = { 1, 2, 3, 4, 5, 7 };
        int[] icons = { -1, 0, 0, 1, 2, 3 };
        int rowH = Math.Min(62, (left.Height - 94) / 6);
        for (int i = 0; i < thresholds.Length; i++)
        {
            int y = left.Y + 44 + i * rowH;
            bool complete = stage >= thresholds[i];
            bool current = stage >= (i == 0 ? 0 : thresholds[i - 1]) && !complete;
            if (current) b.Draw(Game1.staminaRect, new Rectangle(left.X + 6, y, left.Width - 12, rowH - 3), new Color(220,193,145));
            if (icons[i] < 0)
            {
                var buckle = mod.Research.Artifacts.First(p => p.Id == "sailor_buckle");
                b.Draw(mod.Atlas, new Vector2(left.X + 12, y + 3), Sprite(buckle.Sprite), complete ? Color.White : new Color(83,66,53), 0, Vector2.Zero, 2, SpriteEffects.None, 1);
            }
            else ShipwreckIcon(b, icons[i], new Vector2(left.X + 12, y + 3), 2, complete || current);
            string label = mod.T("shipwreck.step." + i);
            float fit = Math.Min(fontScale, (left.Width - 66) / Math.Max(1, Game1.smallFont.MeasureString(label).X));
            Text(b, label, left.X + 55, y + 2, complete ? new Color(71,119,62) : current ? Ink : Muted, fit);
            Text(b, mod.T(complete ? "workshop.done" : current ? "workshop.current" : "workshop.locked"), left.X + 55, y + rowH - 23, Muted, fontScale * .72f);
        }
        if (stage >= (int)ShipwreckStage.Letter)
            Button(b, "ship:letter", mod.T(selected == "letter" ? "shipwreck.return" : "shipwreck.read-letter"), new Rectangle(left.X + 12, left.Bottom - 44, left.Width - 24, 38));
        else Text(b, mod.T("shipwreck.archive"), left.X + 12, left.Bottom - 28, Muted, fontScale * .76f);

        int x = right.X + 12, w = right.Width - 24;
        bool letter = selected == "letter" && stage >= (int)ShipwreckStage.Letter;
        int icon = letter ? 2 : stage >= 6 ? 3 : stage >= 4 ? 2 : stage == 3 ? 1 : 0;
        ShipwreckIcon(b, icon, new Vector2(x, right.Y + 4), 4);
        Paragraph(b, mod.T(letter ? "shipwreck.letter-title" : "shipwreck.heading." + stage), new Rectangle(x + 80, right.Y + 8, w - 80, 76));
        string content = letter ? mod.T("shipwreck.letter") : mod.T("shipwreck.text." + stage);
        if (!letter && stage is 3 or 5)
        {
            var site = mod.State.Sites.FirstOrDefault(p => Shipwreck.Kinds.Contains(p.EventKind) && p.Expires >= ModEntry.Today && p.Points.Any(n => !n.Dug));
            if (site != null)
                content += "\n\n" + mod.T(site.EventKind == "ship_bottle" ? "shipwreck.active-bottle" : "shipwreck.active-cache",
                    new { count = site.Points.Count(p => !p.Dug), days = site.Expires == int.MaxValue ? 0 : site.Expires - ModEntry.Today + 1, key = mod.Config.SurveyKey.ToString() });
            else if (stage == 3 && story.BottleUntil >= ModEntry.Today)
                content += "\n\n" + mod.T("shipwreck.no-safe-site");
            else if (stage == 5)
                content += "\n\n" + mod.T("shipwreck.no-safe-cache");
        }
        string? action = letter ? null : stage switch { 1 => "study", 2 => "consult", 4 => "decode", 6 => "claim", _ => null };
        int textTop = right.Y + 96;
        int bottom = right.Bottom - (action != null ? 58 : 12);
        var lines = Wrap(content, w).ToList();
        int lineH = (int)(Game1.smallFont.LineSpacing * fontScale);
        int visible = Math.Max(1, (bottom - textTop) / lineH);
        maxScroll = Math.Max(0, lines.Count - visible);
        scroll = Math.Clamp(scroll, 0, maxScroll);
        for (int i = scroll; i < Math.Min(lines.Count, scroll + visible); i++) Text(b, lines[i], x, textTop + (i - scroll) * lineH);
        if (maxScroll > scroll) Text(b, "↓", right.Right - 24, bottom - 16, Muted);
        if (action != null)
            Button(b, "ship:action:" + action, mod.T("shipwreck.action." + action), new Rectangle(x, right.Bottom - 44, w, 38), mod.CanShipwreckAction(action));
    }
}
