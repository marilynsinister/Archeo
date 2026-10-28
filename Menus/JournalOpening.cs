using Archeo.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Archeo.Menus;

internal sealed partial class JournalMenu
{
    private void DrawOpening(SpriteBatch b)
    {
        var stage = mod.State.Opening.Stage;
        b.Draw(mod.Atlas, new Vector2(left.Center.X - 40, left.Y + 28), Sprite(0), Color.White, 0, Vector2.Zero, 5, SpriteEffects.None, 1);
        Center(b, mod.T("opening.title"), new Rectangle(left.X + 8, left.Y + 132, left.Width - 16, 34));
        Paragraph(b, mod.T("opening.summary." + (int)stage), new Rectangle(left.X + 18, left.Y + 188, left.Width - 36, left.Height - 198), Muted);
        string text = stage == OpeningStage.Complete ? mod.T("opening.conversation").Replace("^", "\n\n")
            : stage == OpeningStage.Searching && mod.State.Learning.Events.Contains("fragments") ? mod.T("opening.text.fragments") : mod.T("opening.text." + (int)stage);
        DrawPageText(b, mod.T("opening.heading." + (int)stage), text, right.Bottom - 68);
        var button = new Rectangle(right.X + 12, right.Bottom - 46, right.Width - 24, 38);
        if (stage == OpeningStage.Restored)
            Button(b, "opening:talk", mod.T(mod.InMuseum ? "opening.talk" : "journey.visit-museum"), button, mod.InMuseum);
        else if (stage == OpeningStage.Searching)
            Button(b, "opening:next", mod.T("opening.project"), button);
        else
            Button(b, "opening:replay", mod.T("opening.replay"), button);
    }

    private void HandleOpeningClick(string id)
    {
        if (id == "opening:open") SwitchTab("opening");
        else if (id == "opening:talk") mod.ConcludeOpeningStory();
        else if (id == "opening:replay" && mod.State.Opening.Stage == OpeningStage.Complete)
            StardewValley.Game1.drawObjectDialogue(mod.T("opening.conversation"));
        else if (id == "opening:next") { SwitchTab("valley"); selected = OpeningStory.Artifact; }
    }
}
