using Archeo.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace Archeo.Menus;

internal sealed partial class JournalMenu
{
    private void DrawGallery(SpriteBatch b)
    {
        buttons.Remove("tools");
        var collections = MuseumCollections.All;
        if (!collections.Any(p => p.Id == selected)) selected = collections[0].Id;
        Text(b, mod.T("gallery.title"), left.X + 12, left.Y + 8, Ink, fontScale * 1.08f);
        int rowH = Math.Min(84, (left.Height - 160) / 3);
        for (int i = 0; i < collections.Length; i++)
        {
            var collection = collections[i];
            var rect = new Rectangle(left.X + 6, left.Y + 48 + i * rowH, left.Width - 12, rowH - 4);
            Button(b, "gallery:select:" + collection.Id, "", rect, active: selected == collection.Id);
            string title = mod.T("gallery.collection." + collection.Id);
            float fit = Math.Min(fontScale, (rect.Width - 20) / Math.Max(1, Game1.smallFont.MeasureString(title).X));
            Text(b, title, rect.X + 10, rect.Y + 5, Ink, fit);
            string status = mod.State.CollectionRewards.Contains(collection.Id) ? mod.T("gallery.claimed")
                : mod.T("gallery.collection-progress", new { count = MuseumCollections.Count(mod.State, collection) });
            Text(b, status, rect.X + 10, rect.Y + rect.Height - 24, Muted, fontScale * .79f);
        }
        int craftY = left.Bottom - 45;
        Paragraph(b, mod.T(mod.State.Workshop.Stage == WorkshopStage.Complete ? "gallery.cost" : "gallery.unlock",
            new { wood = mod.MaterialCount("388"), iron = mod.MaterialCount("335"), quartz = mod.MaterialCount("338") }),
            new Rectangle(left.X + 12, craftY - 70, left.Width - 24, 66), Muted);
        Button(b, "gallery:craft", mod.T("gallery.craft"), new Rectangle(left.X + 12, craftY, left.Width - 24, 38), mod.CanCraftGallery);

        var set = collections.First(p => p.Id == selected);
        int x = right.X + 12, w = right.Width - 24;
        Text(b, mod.T("gallery.collection." + set.Id), x, right.Y + 8, Ink, Math.Min(fontScale, w / Math.Max(1, Game1.smallFont.MeasureString(mod.T("gallery.collection." + set.Id)).X)));
        int requirementH = Math.Clamp((right.Height - 238) / 3, 36, 60);
        for (int i = 0; i < set.Artifacts.Length; i++)
        {
            var artifact = mod.Research.Artifacts.First(p => p.Id == set.Artifacts[i]);
            bool found = mod.State.Restored.ContainsKey(artifact.Id);
            int y = right.Y + 42 + i * requirementH;
            b.Draw(mod.Atlas, new Vector2(x, y + 3), Sprite(artifact.Sprite), found ? Color.White : new Color(83, 66, 53), 0, Vector2.Zero, 2, SpriteEffects.None, 1);
            float fit = Math.Min(fontScale, (w - 48) / Math.Max(1, Game1.smallFont.MeasureString(mod.ArtifactName(artifact)).X));
            Text(b, mod.ArtifactName(artifact), x + 43, y + 2, found ? new Color(71, 119, 62) : Muted, fit);
            Text(b, mod.T(found ? "gallery.found" : "gallery.missing"), x + 43, y + requirementH - 22, Muted, fontScale * .73f);
        }
        int rewardY = right.Y + 46 + 3 * requirementH;
        var data = ItemRegistry.GetDataOrErrorItem("(F)" + ModEntry.ItemId(set.Reward));
        b.Draw(data.GetTexture(), new Vector2(x, rewardY + 2), data.GetSourceRect(), Color.White, 0, Vector2.Zero, 2, SpriteEffects.None, 1);
        Paragraph(b, mod.T("gallery.reward-label", new { name = mod.T("gallery.reward." + set.Reward) }), new Rectangle(x + 76, rewardY + 2, w - 76, 78));
        // Full collection notes remain readable by scrolling on smaller windows.
        int top = rewardY + 82, bottom = right.Bottom - 54;
        var lines = Wrap(mod.T("gallery.collection-note"), w).ToList();
        int lineH = (int)(Game1.smallFont.LineSpacing * fontScale);
        int visible = Math.Max(0, (bottom - top) / lineH);
        maxScroll = Math.Max(0, lines.Count - visible);
        scroll = Math.Clamp(scroll, 0, maxScroll);
        for (int i = scroll; i < Math.Min(lines.Count, scroll + visible); i++) Text(b, lines[i], x, top + (i - scroll) * lineH, Muted);
        bool claimed = mod.State.CollectionRewards.Contains(set.Id);
        Button(b, "gallery:reward:" + set.Id, mod.T(claimed ? "gallery.claimed" : "gallery.claim"),
            new Rectangle(x, right.Bottom - 44, w, 38), MuseumCollections.CanClaim(mod.State, set.Id, true));
    }
}
