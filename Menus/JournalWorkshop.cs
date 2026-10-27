using Archeo.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Archeo.Menus;

internal sealed partial class JournalMenu
{
    private void WorkshopIcon(SpriteBatch b, int index, Vector2 at, int scale, bool known = true)
        => b.Draw(mod.WorkshopAtlas, at, new Rectangle(index * 16, 0, 16, 16), known ? Color.White : new Color(83, 66, 53), 0, Vector2.Zero, scale, SpriteEffects.None, 1);

    private void DrawWorkshop(SpriteBatch b)
    {
        buttons.Remove("tools");
        var story = mod.State.Workshop;
        int stage = (int)story.Stage;
        Text(b, mod.T("workshop.title"), left.X + 12, left.Y + 8, Ink, fontScale * 1.04f);
        int rowH = Math.Min(60, (left.Height - 92) / 5);
        for (int i = 0; i < 5; i++)
        {
            int y = left.Y + 53 + i * rowH;
            bool complete = stage > i;
            bool current = stage == i;
            if (current) b.Draw(StardewValley.Game1.staminaRect, new Rectangle(left.X + 6, y - 3, left.Width - 12, rowH - 3), new Color(220, 193, 145));
            WorkshopIcon(b, i == 0 ? 0 : i == 3 ? 1 : i == 4 ? 4 : 5, new Vector2(left.X + 12, y + 2), 2, complete || current);
            float titleScale = Math.Min(fontScale, (left.Width - 64f) / Math.Max(1, StardewValley.Game1.smallFont.MeasureString(mod.T("workshop.step." + i)).X));
            Text(b, mod.T("workshop.step." + i), left.X + 55, y + 2, complete ? new Color(71, 119, 62) : current ? Ink : Muted, titleScale);
            Text(b, mod.T(complete ? "workshop.done" : current ? "workshop.current" : "workshop.locked"), left.X + 55, y + (int)(27 * fontScale), Muted, fontScale * .73f);
        }
        Text(b, mod.T("workshop.archive"), left.X + 12, left.Bottom - 26, Muted, fontScale * .77f);

        int x = right.X + 12, w = right.Width - 24;
        WorkshopIcon(b, stage == 5 ? 4 : 0, new Vector2(x, right.Y + 4), 4);
        Paragraph(b, mod.T("workshop.heading." + stage), new Rectangle(x + 80, right.Y + 8, w - 80, 76), Ink);
        int textTop = right.Y + 98;
        int bottom = right.Bottom - (stage == 5 ? 146 : stage is 1 or 2 or 4 ? 85 : 12);
        if (stage is 3 or 4)
        {
            int partW = w / 3;
            for (int i = 0; i < 3; i++)
            {
                string part = Workshop.Parts[i];
                WorkshopIcon(b, i + 1, new Vector2(x + i * partW + partW / 2 - 16, textTop), 2, story.Parts.Contains(part));
                Center(b, mod.T("workshop.short." + part), new Rectangle(x + i * partW, textTop + 35, partW, 24), story.Parts.Contains(part) ? new Color(71, 119, 62) : Muted, fontScale * .8f);
            }
            textTop += 70;
        }
        string content = mod.T("workshop.text." + stage);
        if (story.Stage == WorkshopStage.Components)
            foreach (string part in Workshop.Parts.Where(p => !story.Parts.Contains(p)))
                content += "\n\n" + mod.T("catalog.part." + part);
        var lines = Wrap(content, w).ToList();
        int lineH = (int)(StardewValley.Game1.smallFont.LineSpacing * fontScale);
        int visible = Math.Max(1, (bottom - textTop) / lineH);
        maxScroll = Math.Max(0, lines.Count - visible);
        scroll = Math.Clamp(scroll, 0, maxScroll);
        for (int i = scroll; i < Math.Min(lines.Count, scroll + visible); i++) Text(b, lines[i], x, textTop + (i - scroll) * lineH);
        if (maxScroll > scroll) Text(b, "v", right.Right - 24, bottom - 12, Muted);
        string? action = stage switch { 1 => "study", 2 => "consult", 4 => "repair", _ => null };
        if (action != null)
        {
            if (stage == 4) Text(b, mod.T("workshop.repair-cost", new { wood = mod.MaterialCount("388"), copper = mod.MaterialCount("334") }), x, right.Bottom - 70, Muted, fontScale * .8f);
            Button(b, "story:" + action, mod.T("workshop.action." + action), new Rectangle(x, right.Bottom - 44, w, 38), mod.CanWorkshopAction(action));
        }
        if (stage == 5)
        {
            Button(b, "story:companion", mod.T(story.CompanionEnabled ? "workshop.action.dismiss" : "workshop.action.summon"), new Rectangle(x, right.Bottom - 130, w, 38));
            Text(b, mod.T("workshop.case-cost", new { wood = mod.MaterialCount("388"), iron = mod.MaterialCount("335") }), x, right.Bottom - 76, Muted, fontScale * .8f);
            Button(b, "story:case", mod.T("workshop.action.case"), new Rectangle(x, right.Bottom - 44, w, 38), mod.CanWorkshopAction("case"));
        }
    }
}
