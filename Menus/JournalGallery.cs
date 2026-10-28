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
        var collections = MuseumCollections.All.Where(p=>FrontierResearch.CollectionVisible(mod.State,p)).ToList();
        if (!collections.Any(p => p.Id == selected)) selected = collections[0].Id;
        Text(b, mod.T("gallery.title"), left.X + 12, left.Y + 8, Ink, fontScale * 1.08f);
        var layout=CollectionListLayout.Create(left.Height);
        int page=collections.FindIndex(p=>p.Id==selected)/layout.PageSize;
        int pages=(collections.Count+layout.PageSize-1)/layout.PageSize;
        for (int i = page*layout.PageSize; i < Math.Min(collections.Count,(page+1)*layout.PageSize); i++)
        {
            var collection = collections[i];
            var rect = new Rectangle(left.X + 6, left.Y + layout.FirstRowY + (i%layout.PageSize)*layout.RowHeight, left.Width - 12, layout.RowHeight - 4);
            Button(b, "gallery:select:" + collection.Id, "", rect, active: selected == collection.Id);
            string title = mod.T("gallery.collection." + collection.Id);
            float fit = Math.Min(fontScale, (rect.Width - 20) / Math.Max(1, Game1.smallFont.MeasureString(title).X));
            Text(b, title, rect.X + 10, rect.Y + 5, Ink, fit);
            string status = mod.State.CollectionRewards.Contains(collection.Id) ? mod.T("gallery.claimed")
                : mod.T("gallery.collection-progress", new { count = MuseumCollections.Count(mod.State, collection) });
            Text(b, status, rect.X + 10, rect.Y + rect.Height - 24, Muted, fontScale * .79f);
        }
        if(pages>1)
        {
            int bw=Math.Min(100,(left.Width-100)/2), y=left.Y+layout.PageControlsY;
            Button(b,"gallery:prev",mod.T("unusual.page-prev"),new Rectangle(left.X+6,y,bw,28),page>0);
            Button(b,"gallery:next",mod.T("unusual.page-next"),new Rectangle(left.Right-6-bw,y,bw,28),page+1<pages);
            Center(b,mod.T("unusual.page",new{page=page+1,pages}),new Rectangle(left.X+bw+8,y,left.Width-2*bw-16,28),Muted,fontScale*.7f);
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
            string name=JournalCatalog.IsKnown(mod.State,artifact)?mod.ArtifactName(artifact):mod.T("journal.unknown");
            float fit = Math.Min(fontScale, (w - 48) / Math.Max(1, Game1.smallFont.MeasureString(name).X));
            Text(b, name, x + 43, y + 2, found ? new Color(71, 119, 62) : Muted, fit);
            Text(b, mod.T(found ? "gallery.found" : "gallery.missing"), x + 43, y + requirementH - 22, Muted, fontScale * .73f);
        }
        int rewardY = right.Y + 46 + 3 * requirementH;
        var data = ItemRegistry.GetDataOrErrorItem("(F)" + ModEntry.ItemId(set.Reward));
        b.Draw(data.GetTexture(), new Vector2(x, rewardY + 2), data.GetSourceRect(), Color.White, 0, Vector2.Zero, 2, SpriteEffects.None, 1);
        Paragraph(b, mod.T("gallery.reward-label", new { name = mod.T("gallery.reward." + set.Reward) }), new Rectangle(x + 76, rewardY + 2, w - 76, 78));
        // Full collection notes remain readable by scrolling on smaller windows.
        int top = rewardY + 82, bottom = right.Bottom - 54;
        var lines = Wrap(mod.T("gallery.collection-note"), w).ToList();
        int lineH = ReadingLineHeight;
        int visible = Math.Max(0, (bottom - top) / lineH);
        maxScroll = Math.Max(0, lines.Count - visible);
        scroll = Math.Clamp(scroll, 0, maxScroll);
        for (int i = scroll; i < Math.Min(lines.Count, scroll + visible); i++) ReadingText(b, lines[i], x, top + (i - scroll) * lineH, Muted);
        bool claimed = mod.State.CollectionRewards.Contains(set.Id);
        Button(b, "gallery:reward:" + set.Id, mod.T(claimed ? "gallery.claimed" : "gallery.claim"),
            new Rectangle(x, right.Bottom - 44, w, 38), MuseumCollections.CanClaim(mod.State, set.Id, true));
    }

    private void TurnGalleryPage(int direction)
    {
        var collections=MuseumCollections.All.Where(p=>FrontierResearch.CollectionVisible(mod.State,p)).ToList();
        int size=CollectionListLayout.Create(left.Height).PageSize;
        int page=Math.Max(0,collections.FindIndex(p=>p.Id==selected))/size+direction;
        if(page>=0 && page*size<collections.Count)selected=collections[page*size].Id;
        scroll=0;
    }
}
