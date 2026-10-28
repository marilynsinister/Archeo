using Archeo.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Archeo.Menus;

internal sealed partial class JournalMenu
{
    private string learningSelection = "";
    private int learningPage;
    private int LearningPageSize => Math.Max(1, (left.Height - 96) / 44);

    private void DrawLearning(SpriteBatch b)
    {
        var lessons = FieldLearning.Available(mod.State, mod.Research.Artifacts);
        string current = FieldLearning.Current(mod.State, mod.Research.Artifacts);
        if (!lessons.Contains(learningSelection))
        {
            learningSelection = current;
            learningPage = lessons.IndexOf(current) / LearningPageSize;
        }
        int pages = (lessons.Count + LearningPageSize - 1) / LearningPageSize;
        learningPage = Math.Clamp(learningPage, 0, pages - 1);
        Text(b, mod.T("learn.heading"), left.X + 12, left.Y + 8);
        int y = left.Y + 48;
        foreach (string lesson in lessons.Skip(learningPage * LearningPageSize).Take(LearningPageSize))
        {
            Button(b, "learn:select:" + lesson, (lesson == current ? "> " : "") + mod.T("learn.title." + lesson),
                new Rectangle(left.X + 8, y, left.Width - 16, 38), active: learningSelection == lesson);
            y += 44;
        }
        int half = (left.Width - 24) / 2;
        Button(b, "learn:prev", mod.T("unusual.page-prev"), new Rectangle(left.X + 8, left.Bottom - 38, half, 30), learningPage > 0);
        Button(b, "learn:next", mod.T("unusual.page-next"), new Rectangle(left.X + half + 16, left.Bottom - 38, half, 30), learningPage + 1 < pages);
        string text = mod.LearningText(learningSelection) + "\n\n" + mod.T("learn.archive-note");
        DrawPageText(b, mod.T("learn.title." + learningSelection), text, right.Bottom - 64);
        Button(b, "learn:continue", mod.T("learn.continue"), new Rectangle(right.X + 12, right.Bottom - 46, right.Width - 24, 38));
    }

    private void HandleLearningClick(string id)
    {
        if (id.StartsWith("learn:select:")) learningSelection = id[13..];
        else if (id == "learn:prev") learningPage--;
        else if (id == "learn:next") learningPage++;
        else if (id == "learn:continue")
        {
            string lesson = learningSelection;
            if (lesson == "welcome") SwitchTab("overview");
            else if (lesson == "restore")
            {
                var goal = mod.FieldObjectives().FirstOrDefault(p => p.Kind == ObjectiveKind.Restore);
                if (goal != null) OpenObjective(goal); else SwitchTab("catalog");
            }
            else if (lesson == "relics") { SwitchTab("catalog"); selected = mod.State.Restored.Keys.LastOrDefault() ?? ""; }
            else if (lesson == "chapters") SwitchTab("chapters");
            else if (Journey.IsOpen(mod.State, lesson)) SwitchTab(lesson);
            else { SwitchTab("overview"); overviewGuide = false; }
        }
        scroll = 0;
    }
}
