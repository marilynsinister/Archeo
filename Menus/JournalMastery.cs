using Archeo.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Archeo.Menus;

internal sealed partial class JournalMenu
{
    private int masteryLevel = 5;
    private string masteryTrack = "search";

    private void DrawMastery(SpriteBatch b)
    {
        Button(b, "mastery:level:5", mod.T("mastery.level", new { level = 5 }), new Rectangle(left.X + 8, left.Y + 8, left.Width / 2 - 12, 34), active: masteryLevel == 5);
        Button(b, "mastery:level:10", mod.T("mastery.level", new { level = 10 }), new Rectangle(left.X + left.Width / 2 + 4, left.Y + 8, left.Width / 2 - 12, 34), active: masteryLevel == 10);
        string choice = masteryLevel == 5 ? mod.State.Mastery.Level5 : mod.State.Mastery.Level10;
        int y = left.Y + 62;
        foreach (string track in Mastery.Tracks)
        {
            Button(b, "mastery:track:" + track, (choice == track ? "* " : "") + mod.T("mastery.track." + track), new Rectangle(left.X + 8, y, left.Width - 16, 42), active: masteryTrack == track);
            y += 54;
        }
        Paragraph(b, mod.T("mastery.current", new { first = mod.T(mod.State.Mastery.Level5 == "" ? "mastery.none" : "mastery.track." + mod.State.Mastery.Level5), second = mod.T(mod.State.Mastery.Level10 == "" ? "mastery.none" : "mastery.track." + mod.State.Mastery.Level10) }), new Rectangle(left.X + 12, y + 8, left.Width - 24, left.Bottom - y - 16), Muted);
        bool unlocked = Research.Level(mod.State.Experience) >= masteryLevel && (masteryLevel == 5 || mod.State.Mastery.Level5 != "");
        string text = mod.T("mastery.intro") + "\n\n" + mod.T("mastery.description." + masteryTrack) + "\n\n"
            + mod.T(choice != "" ? "mastery.already-chosen" : unlocked ? "mastery.available" : "mastery.locked", new { level = masteryLevel });
        DrawPageText(b, mod.T("mastery.track." + masteryTrack), text, right.Bottom - 62);
        Button(b, "mastery:choose", mod.T("mastery.choose"), new Rectangle(right.X + 12, right.Bottom - 46, right.Width - 24, 38), unlocked && choice == "");
    }

    private void HandleMasteryClick(string id)
    {
        if (id.StartsWith("mastery:level:")) masteryLevel = int.Parse(id[14..]);
        else if (id.StartsWith("mastery:track:")) masteryTrack = id[14..];
        else if (id == "mastery:choose") mod.ChooseMastery(masteryLevel, masteryTrack);
        scroll = 0;
    }

    private void DrawPageText(SpriteBatch b, string title, string text, int bottom)
    {
        int titleHeight = Wrap(title, right.Width - 24, fontScale).Count() * (StardewValley.Game1.smallFont.LineSpacing + 4);
        Paragraph(b, title, new Rectangle(right.X + 12, right.Y + 8, right.Width - 24, titleHeight));
        int top = right.Y + 22 + titleHeight, lineH = ReadingLineHeight;
        var lines = Wrap(text, right.Width - 24).ToList();
        int visible = Math.Max(1, (bottom - top) / lineH);
        maxScroll = Math.Max(0, lines.Count - visible);
        scroll = Math.Clamp(scroll, 0, maxScroll);
        for (int i = scroll; i < Math.Min(lines.Count, scroll + visible); i++) ReadingText(b, lines[i], right.X + 12, top + (i - scroll) * lineH);
        if (maxScroll > 0) Text(b, (scroll > 0 ? "^ " : "") + (scroll < maxScroll ? "v" : ""), right.Right - 24, bottom - 16, Muted, .65f);
    }
}
