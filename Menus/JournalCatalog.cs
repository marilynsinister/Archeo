using Archeo.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace Archeo.Menus;

internal sealed partial class JournalMenu
{
    private readonly CatalogFilter filter = new();
    private TextBox? searchBox;
    private string lastQuery = "";
    private bool showCatalogFilters;

    private List<ArtifactDefinition> CatalogItems()
        => JournalCatalog.Filter(mod.Research.Artifacts.Where(p => Journey.VisibleArtifact(mod.State,p)), mod.State, filter, p =>
            (JournalCatalog.IsKnown(mod.State, p) ? mod.ArtifactName(p) : mod.T("journal.unknown")) + " "
            + mod.CultureName(p.Culture) + " " + mod.T("rarity." + p.Rarity) + " " + mod.T("catalog.source." + JournalCatalog.SourceGroup(p)));

    private int CatalogPageSize => Math.Max(1, (left.Height - (showCatalogFilters ? 182 : 100)) / 56);

    private void DrawCatalog(SpriteBatch b)
    {
        searchBox ??= new TextBox(Game1.content.Load<Texture2D>("LooseSprites\\textBox"), null, Game1.smallFont, Ink) { Text = filter.Query, limitWidth = true };
        searchBox.X = left.X + 6; searchBox.Y = left.Y + 2; searchBox.Width = left.Width - 12;
        if(showCatalogFilters)
        {
        buttons["catalog:search"] = new Rectangle(searchBox.X, searchBox.Y, searchBox.Width, 40);
        searchBox.Draw(b);
        if (searchBox.Text.Length == 0 && !searchBox.Selected)
            Text(b, mod.T("catalog.search"), left.X + 16, left.Y + 12, Muted, fontScale * .82f);
        filter.Query = searchBox.Text;
        if (lastQuery != filter.Query) { selected = ""; scroll = 0; lastQuery = filter.Query; }
        int half = (left.Width - 18) / 2;
        Button(b, "catalog:culture", filter.Culture == "" ? mod.T("catalog.culture") : mod.CultureName(filter.Culture), new Rectangle(left.X + 6, left.Y + 48, half, 30));
        Button(b, "catalog:rarity", filter.Rarity < 0 ? mod.T("catalog.rarity") : mod.T("rarity." + filter.Rarity), new Rectangle(left.X + half + 12, left.Y + 48, half, 30));
        Button(b, "catalog:source", mod.T("catalog.source." + filter.Source), new Rectangle(left.X + 6, left.Y + 82, half, 30));
        Button(b, "catalog:discovery", mod.T("catalog.discovery." + filter.Discovery), new Rectangle(left.X + half + 12, left.Y + 82, half, 30));
        Button(b,"catalog:filters",mod.T("journey.hide-filters"),new Rectangle(left.X+6,left.Y+115,130,25));
        Button(b, "catalog:reset", mod.T("catalog.reset"), new Rectangle(left.Right - 92, left.Y + 115, 86, 25));
        }
        else
        {
            Text(b,mod.T("journey.finds-title"),left.X+12,left.Y+10,Ink,fontScale*1.05f);
            Button(b,"catalog:filters",mod.T("journey.show-filters"),new Rectangle(left.Right-112,left.Y+6,104,30));
            filter.Query=searchBox.Text;
            if(lastQuery!=filter.Query){selected="";scroll=0;lastQuery=filter.Query;}
        }
        var artifacts = CatalogItems();
        if (artifacts.Count == 0)
        {
            selected = "";
            Paragraph(b, mod.T("catalog.empty"), new Rectangle(right.X + 16, right.Y + 20, right.Width - 32, right.Height - 40), Muted);
            return;
        }
        if (!artifacts.Any(p => p.Id == selected)) selected = artifacts[0].Id;
        int page = artifacts.FindIndex(p => p.Id == selected) / CatalogPageSize;
        int pages = (artifacts.Count + CatalogPageSize - 1) / CatalogPageSize;
        int row = 0;
        foreach (var artifact in artifacts.Skip(page * CatalogPageSize).Take(CatalogPageSize))
            DrawArtifactRow(b, artifact, new Rectangle(left.X + 6, left.Y + (showCatalogFilters?146:54) + row++ * 56, left.Width - 12, 52), JournalCatalog.IsKnown(mod.State, artifact));
        if(!showCatalogFilters && artifacts.Count==1 && mod.State.Restored.Count==0)
            Paragraph(b,mod.T("journey.catalog-start"),new Rectangle(left.X+18,left.Y+138,left.Width-36,left.Height-184),Muted);
        int buttonW = Math.Min(100, (left.Width - 108) / 2);
        if(pages>1)
        {
            Button(b, "catalog:prev", mod.T("unusual.page-prev"), new Rectangle(left.X + 6, left.Bottom - 31, buttonW, 28), page > 0);
            Button(b, "catalog:next", mod.T("unusual.page-next"), new Rectangle(left.Right - 6 - buttonW, left.Bottom - 31, buttonW, 28), page + 1 < pages);
            Center(b, mod.T("unusual.page", new { page = page + 1, pages }), new Rectangle(left.X + buttonW + 8, left.Bottom - 31, left.Width - buttonW * 2 - 16, 28), Muted, fontScale * .7f);
        }
        var item = artifacts.First(p => p.Id == selected);
        DrawDetail(b, item, mod.Research.Project(mod.State, item.Culture, mod.RandomFor("project:" + item.Culture)), mod.Research.Progress(mod.State, item.Culture));
    }

    private void DrawArtifactRow(SpriteBatch b, ArtifactDefinition artifact, Rectangle rect, bool known)
    {
        Button(b, "artifact:" + artifact.Id, "", rect, active: selected == artifact.Id);
        b.Draw(mod.Atlas, new Vector2(rect.X + 8, rect.Y + (rect.Height - 32) / 2), Sprite(artifact.Sprite), known ? Color.White : new Color(67, 55, 43), 0, Vector2.Zero, 2, SpriteEffects.None, 1);
        string name = known ? mod.ArtifactName(artifact) : mod.T("journal.unknown");
        float scale = Math.Min(fontScale, (rect.Width - 61f) / Math.Max(1, Game1.smallFont.MeasureString(name).X));
        Text(b, name, rect.X + 49, rect.Y + 3, known ? ModEntry.RarityColor(artifact.Rarity) : Muted, scale);
        bool restored = mod.State.Restored.ContainsKey(artifact.Id);
        string status = PendingArtifact(artifact.Id) ? mod.T("catalog.reward-ready") : restored && artifact.Effect is not ("" or "lamp") ? mod.RelicStatus(artifact)
            : mod.T(mod.State.Donated.Contains(artifact.Id) ? "journal.donated" : restored ? "journal.restored"
                : mod.State.Cultures.Values.Any(p => p.Project == artifact.Id) ? "journal.current" : "journal.undiscovered");
        Text(b, status, rect.X + 49, rect.Bottom - (int)(24 * fontScale), restored && artifact.Effect is not ("" or "lamp") && mod.RelicDaysUntilReady(artifact) == 0 ? new Color(56, 112, 57) : Muted, fontScale * .72f);
    }

    private void HandleCatalogClick(string id)
    {
        static string Next(string[] choices, string value) => choices[(Array.IndexOf(choices, value) + 1) % choices.Length];
        if (id == "catalog:search") { showCatalogFilters=true; if (searchBox != null) { searchBox.Selected = true; Game1.showTextEntry(searchBox); } return; }
        if(id=="catalog:filters")
        {
            showCatalogFilters=!showCatalogFilters;
            if(!showCatalogFilters){filter.Query=filter.Culture=filter.Source=filter.Discovery="";filter.Rarity=-1;searchBox!.Text="";}
            selected="";scroll=0;return;
        }
        if (id == "catalog:culture") filter.Culture = Next(new[] { "" }.Concat(Research.Cultures.Where(c=>Journey.IsOpen(mod.State,c))).ToArray(), filter.Culture);
        else if (id == "catalog:rarity")
        {
            var rarities = new[] { -1 }.Concat(mod.Research.Artifacts.Where(p=>Journey.VisibleArtifact(mod.State,p)).Select(p=>p.Rarity).Distinct().OrderBy(p=>p)).ToList();
            filter.Rarity = rarities[(rarities.IndexOf(filter.Rarity)+1)%rarities.Count];
        }
        else if (id == "catalog:source") filter.Source = Next(new[] { "" }.Concat(mod.Research.Artifacts.Where(p=>Journey.VisibleArtifact(mod.State,p)).Select(JournalCatalog.SourceGroup).Distinct()).ToArray(), filter.Source);
        else if (id == "catalog:discovery") filter.Discovery = Next(new[] { "", "found", "projects" }, filter.Discovery);
        else if (id == "catalog:reset") { filter.Culture = filter.Source = filter.Discovery = ""; filter.Rarity = -1; filter.Query = ""; searchBox!.Text = ""; }
        else if (id is "catalog:prev" or "catalog:next")
        {
            var items = CatalogItems();
            int page = Math.Max(0, items.FindIndex(p => p.Id == selected)) / CatalogPageSize + (id == "catalog:next" ? 1 : -1);
            if (page >= 0 && page * CatalogPageSize < items.Count) selected = items[page * CatalogPageSize].Id;
            scroll = 0; return;
        }
        selected = ""; scroll = 0;
    }
}
