using Archeo.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;

namespace Archeo.Menus;

/// <summary>A responsive, translated journal rendered with Stardew's fonts and pixel assets.</summary>
internal sealed partial class JournalMenu : IClickableMenu
{
    private readonly ModEntry mod;
    private readonly Dictionary<string, Rectangle> buttons = new();
    private string tab = "overview";
    private string selected = "";
    private bool keystone;
    private int scroll;
    private int maxScroll;
    private string hover = "";
    private Rectangle left;
    private Rectangle right;
    private float fontScale;
    private static readonly Color Ink = new(71, 47, 35);
    private static readonly Color Muted = new(131, 105, 78);
    private static readonly Color Paper = new(244, 226, 184);
    private static readonly string[] Tabs = { "overview", "valley", "dwarf", "sea", "workshop", "weather", "gallery", "shipwreck", "nature", "residents" };

    public JournalMenu(ModEntry mod, string initialTab = "overview") : base(0, 0, 100, 100, true)
    {
        this.mod = mod;
        tab = initialTab;
        Layout();
    }

    private void Layout()
    {
        width = Math.Min(1160, Game1.uiViewport.Width - 40);
        height = Math.Min(760, Game1.uiViewport.Height - 40);
        xPositionOnScreen = (Game1.uiViewport.Width - width) / 2;
        yPositionOnScreen = (Game1.uiViewport.Height - height) / 2;
        fontScale = width < 1000 || height < 650 ? .76f : .88f;
        bool twoRows = width < 1000 || Tabs.Length > 8;
        int bodyY = yPositionOnScreen + (twoRows ? 164 : 142);
        int bodyH = height - (twoRows ? 228 : 206);
        int leftW = (int)((width - 68) * .43f);
        left = new Rectangle(xPositionOnScreen + 28, bodyY, leftW, bodyH);
        right = new Rectangle(left.Right + 24, bodyY, width - leftW - 80, bodyH);
        initializeUpperRightCloseButton();
        buttons.Clear();
        int columns = twoRows ? (Tabs.Length + 1) / 2 : Tabs.Length;
        int tabW = (width - 64) / columns;
        for (int i = 0; i < Tabs.Length; i++)
            buttons["tab:" + Tabs[i]] = new Rectangle(xPositionOnScreen + 32 + tabW * (i % columns), yPositionOnScreen + 87 + (i / columns) * 34, tabW - 8, twoRows ? 30 : 42);
        buttons["tools"] = new Rectangle(left.X + 12, left.Bottom - 48, left.Width - 24, 38);
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds) => Layout();

    private void Text(SpriteBatch b, string text, int x, int y, Color? color = null, float? scale = null)
        => b.DrawString(Game1.smallFont, text, new Vector2(x, y), color ?? Ink, 0, Vector2.Zero, scale ?? fontScale, SpriteEffects.None, 1);

    private void Center(SpriteBatch b, string text, Rectangle rect, Color? color = null, float? scale = null)
    {
        float s = scale ?? fontScale;
        Vector2 size = Game1.smallFont.MeasureString(text) * s;
        Text(b, text, rect.X + (int)((rect.Width - size.X) / 2), rect.Y + (int)((rect.Height - size.Y) / 2), color, s);
    }

    private IEnumerable<string> Wrap(string text, int pixelWidth)
    {
        foreach (string paragraph in text.Split('\n'))
        {
            string line = "";
            foreach (string word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                string candidate = line.Length == 0 ? word : line + " " + word;
                if (Game1.smallFont.MeasureString(candidate).X * fontScale > pixelWidth && line.Length > 0)
                { yield return line; line = word; }
                else line = candidate;
            }
            yield return line;
        }
    }

    private int Paragraph(SpriteBatch b, string text, Rectangle area, Color? color = null)
    {
        int lineHeight = (int)(Game1.smallFont.LineSpacing * fontScale);
        int y = area.Y;
        foreach (string line in Wrap(text, area.Width))
        {
            if (y + lineHeight > area.Bottom) break;
            Text(b, line, area.X, y, color);
            y += lineHeight;
        }
        return y;
    }

    private void Button(SpriteBatch b, string id, string label, Rectangle rect, bool enabled = true, bool active = false)
    {
        buttons[id] = rect;
        bool hovered = rect.Contains(Game1.getMouseX(), Game1.getMouseY());
        Color color = !enabled ? new(209, 195, 166) : active ? new(211, 179, 121) : hovered ? new(238, 207, 148) : new(230, 208, 163);
        b.Draw(Game1.staminaRect, rect, new Color(130, 94, 58));
        b.Draw(Game1.staminaRect, new Rectangle(rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height - 4), color);
        Center(b, label, rect, enabled ? Ink : Muted, Math.Min(fontScale, (rect.Width - 16f) / Math.Max(1, Game1.smallFont.MeasureString(label).X)));
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * .65f);
        drawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);
        b.Draw(Game1.staminaRect, new Rectangle(xPositionOnScreen + 16, yPositionOnScreen + 16, width - 32, height - 32), Paper);
        b.Draw(Game1.staminaRect, new Rectangle(right.X - 13, left.Y, 2, left.Height), new Color(184, 153, 111));
        // Binding stitches and page edges keep the journal distinct from a standard inventory menu.
        for (int y = left.Y + 10; y < left.Bottom; y += 23)
            b.Draw(Game1.staminaRect, new Rectangle(right.X - 16, y, 8, 2), new Color(158, 117, 75));
        Text(b, mod.T("journal.title"), xPositionOnScreen + 34, yPositionOnScreen + 27, Ink, fontScale * 1.28f);
        string counter = mod.T("journal.counter", new { count = mod.State.Restored.Count, total = mod.Research.Artifacts.Count, level = Research.Level(mod.State.Experience) });
        Text(b, counter, xPositionOnScreen + 36, yPositionOnScreen + 59, Muted, fontScale * .83f);
        foreach (string key in Tabs)
            Button(b, "tab:" + key, mod.T(key == "overview" ? "journal.overview" : key is "workshop" or "weather" or "gallery" or "shipwreck" or "nature" or "residents" ? key + ".tab" : "culture." + key), buttons["tab:" + key], active: tab == key);
        buttons.Remove("restore"); buttons.Remove("keystone"); buttons.Remove("donate"); buttons.Remove("use");
        foreach (string key in buttons.Keys.Where(p => p.StartsWith("artifact:")).ToList()) buttons.Remove(key);
        foreach (string key in buttons.Keys.Where(p => p.StartsWith("story:")).ToList()) buttons.Remove(key);
        foreach (string key in buttons.Keys.Where(p => p.StartsWith("claim:")).ToList()) buttons.Remove(key);
        foreach (string key in buttons.Keys.Where(p => p.StartsWith("gallery:")).ToList()) buttons.Remove(key);
        foreach (string key in buttons.Keys.Where(p => p.StartsWith("ship:")).ToList()) buttons.Remove(key);
        foreach (string key in buttons.Keys.Where(p => p.StartsWith("culture-page:")).ToList()) buttons.Remove(key);
        foreach (string key in buttons.Keys.Where(p => p.StartsWith("nature:")).ToList()) buttons.Remove(key);
        foreach (string key in buttons.Keys.Where(p => p.StartsWith("resident:")).ToList()) buttons.Remove(key);
        foreach (string key in buttons.Keys.Where(p => p.StartsWith("field:")).ToList()) buttons.Remove(key);
        if (tab == "overview") DrawOverview(b); else if (tab == "workshop") DrawWorkshop(b); else if (tab == "weather") DrawWeather(b); else if (tab == "gallery") DrawGallery(b); else if (tab == "shipwreck") DrawShipwreck(b); else if (tab == "nature") DrawNature(b); else if (tab == "residents") DrawResidents(b); else DrawCulture(b);
        Text(b, mod.T("journal.footer", new { key = mod.Config.SurveyKey.ToString() }), xPositionOnScreen + 34, yPositionOnScreen + height - 43, Muted, fontScale * .82f);
        upperRightCloseButton?.draw(b);
        drawMouse(b);
        if (hover != "") drawHoverText(b, hover, Game1.smallFont);
    }

    private void DrawCulture(SpriteBatch b)
    {
        buttons.Remove("tools");
        var progress = mod.Research.Progress(mod.State, tab);
        var project = mod.Research.Project(mod.State, tab, mod.RandomFor("project:" + tab));
        var artifacts = mod.Research.Artifacts.Where(p => p.Culture == tab).OrderBy(p => p.Rarity).ToList();
        if (!artifacts.Any(p => p.Id == selected)) selected = project.Id;
        Text(b, mod.T("journal.fragments", new { count = progress.Fragments, keys = progress.Keystones }), left.X + 12, left.Y + 6, Muted, fontScale * .88f);
        int pageSize = CulturePageSize(artifacts.Count);
        int pages = (artifacts.Count + pageSize - 1) / pageSize;
        int page = artifacts.FindIndex(p => p.Id == selected) / pageSize;
        int rowH = Math.Min(62, (left.Height - (pages > 1 ? 82 : 44)) / Math.Min(pageSize,artifacts.Count));
        int y = left.Y + 38;
        foreach (var artifact in artifacts.Skip(page * pageSize).Take(pageSize))
        {
            bool known = mod.State.Restored.ContainsKey(artifact.Id) || artifact.Id == project.Id || PendingArtifact(artifact.Id);
            var rect = new Rectangle(left.X + 6, y, left.Width - 12, rowH - 3);
            buttons["artifact:" + artifact.Id] = rect;
            if (selected == artifact.Id) b.Draw(Game1.staminaRect, rect, new Color(220, 193, 145));
            else if (rect.Contains(Game1.getMouseX(), Game1.getMouseY())) b.Draw(Game1.staminaRect, rect, new Color(235, 215, 176));
            b.Draw(mod.Atlas, new Vector2(rect.X + 8, rect.Y + (rect.Height - 32) / 2), Sprite(artifact.Sprite), known ? Color.White : new Color(67, 55, 43), 0, Vector2.Zero, 2, SpriteEffects.None, 1);
            string name = known ? mod.ArtifactName(artifact) : mod.T("journal.unknown");
            float nameScale = Math.Min(fontScale, (rect.Width - 61f) / Math.Max(1, Game1.smallFont.MeasureString(name).X));
            Text(b, name, rect.X + 49, rect.Y + 4, known ? ModEntry.RarityColor(artifact.Rarity) : Muted, nameScale);
            string status = mod.State.Donated.Contains(artifact.Id) ? mod.T("journal.donated")
                : mod.State.Restored.ContainsKey(artifact.Id) ? mod.T("journal.restored")
                : project.Id == artifact.Id ? mod.T("journal.current") : ResidentStories.Sources.Contains(artifact.Source) ? mod.T("residents.status") : NaturalEvents.Kinds.Contains(artifact.Source) ? mod.T("nature.status") : artifact.Source != "research" ? mod.T("weather.status") : mod.T("journal.undiscovered");
            Text(b, status, rect.X + 49, rect.Y + rect.Height - (int)(24 * fontScale), Muted, fontScale * .72f);
            y += rowH;
        }
        if (pages > 1)
        {
            int buttonW = Math.Min(110,(left.Width - 102) / 2);
            Button(b,"culture-page:prev",mod.T("unusual.page-prev"),new Rectangle(left.X+8,left.Bottom-35,buttonW,30),page > 0);
            Button(b,"culture-page:next",mod.T("unusual.page-next"),new Rectangle(left.Right-8-buttonW,left.Bottom-35,buttonW,30),page+1 < pages);
            Center(b,mod.T("unusual.page",new { page = page+1, pages }),new Rectangle(left.X+buttonW+12,left.Bottom-35,left.Width-buttonW*2-24,30),Muted,fontScale*.8f);
        }
        var item = artifacts.First(p => p.Id == selected);
        DrawDetail(b, item, project, progress);
    }

    private static Rectangle Sprite(int sprite) => new(sprite % 8 * 16, sprite / 8 * 16, 16, 16);

    private bool PendingArtifact(string id) => mod.State.Weather.PendingRewards.Contains(id) || mod.State.Nature.PendingRewards.Contains(id)
        || ResidentStories.PendingArtifact(mod.State.Residents,id);

    private int CulturePageSize(int count)
        => count * 48 <= left.Height - 44 ? count : Math.Max(1,(left.Height-82)/48);

    private void DrawDetail(SpriteBatch b, ArtifactDefinition artifact, ArtifactDefinition project, CultureProgress progress)
    {
        bool restored = mod.State.Restored.ContainsKey(artifact.Id);
        bool pending = PendingArtifact(artifact.Id);
        bool known = restored || artifact.Id == project.Id || pending;
        int x = right.X + 16, w = right.Width - 32;
        b.Draw(mod.Atlas, new Vector2(x + 6, right.Y + 10), Sprite(artifact.Sprite), known ? Color.White : new Color(67, 55, 43), 0, Vector2.Zero, 4, SpriteEffects.None, 1);
        Paragraph(b, known ? mod.ArtifactName(artifact) : mod.T("journal.unknown"), new Rectangle(x + 84, right.Y + 10, w - 84, 58), ModEntry.RarityColor(artifact.Rarity));
        Text(b, mod.T("rarity." + artifact.Rarity), x + 84, right.Y + 66, ModEntry.RarityColor(artifact.Rarity), fontScale * .85f);
        int actionY = right.Bottom - 48;
        int textTop = right.Y + 108;
        int textBottom = actionY - (artifact.Id == project.Id ? 94 : 14);
        string content = known ? mod.T("artifact." + artifact.Id + ".description") + "\n\n" + (restored ? mod.T("artifact." + artifact.Id + ".lore") : mod.T("journal.lore-locked"))
            : artifact.Source == "research" ? mod.T("journal.hidden-hint", new { culture = mod.CultureName(artifact.Culture) })
            : mod.T((ResidentStories.Sources.Contains(artifact.Source) ? "residents.artifact-hint." : NaturalEvents.Kinds.Contains(artifact.Source) ? "nature.artifact-hint." : "weather.artifact-hint.") + artifact.Source);
        if (restored && UnusualRelics.Effects.Contains(artifact.Effect))
        {
            int days = mod.RelicDaysUntilReady(artifact);
            content = mod.T(days == 0 ? "unusual.ready" : "unusual.ready-in",new { days }) + "\n\n" + content;
        }
        var lines = Wrap(content, w).ToList();
        int lineH = (int)(Game1.smallFont.LineSpacing * fontScale);
        int visible = Math.Max(1, (textBottom - textTop) / lineH);
        maxScroll = Math.Max(0, lines.Count - visible);
        scroll = Math.Clamp(scroll, 0, maxScroll);
        for (int i = scroll; i < Math.Min(lines.Count, scroll + visible); i++) Text(b, lines[i], x, textTop + (i - scroll) * lineH);
        if (maxScroll > 0)
            Text(b, (scroll > 0 ? "↑ " : "") + (scroll < maxScroll ? "↓" : ""), right.Right - 24, textBottom - 16, Muted, .65f);
        if (artifact.Id == project.Id)
        {
            int cost = Research.Required(artifact, progress, keystone);
            var bar = new Rectangle(x, actionY - 75, w, 12);
            b.Draw(Game1.staminaRect, bar, new Color(190, 166, 123));
            b.Draw(Game1.staminaRect, new Rectangle(bar.X, bar.Y, (int)(bar.Width * Math.Min(1, progress.Fragments / (float)cost)), bar.Height), ModEntry.RarityColor(artifact.Rarity));
            Text(b, mod.T("journal.progress", new { count = progress.Fragments, cost }), x, actionY - 60, Muted, fontScale * .82f);
            Button(b, "keystone", mod.T(keystone ? "journal.key-on" : "journal.key-off", new { count = progress.Keystones }), new Rectangle(x, actionY - 34, w, 28), progress.Keystones > 0);
            bool canDonate = restored && Game1.currentLocation.Name == "ArchaeologyHouse" && !mod.State.Donated.Contains(artifact.Id);
            Button(b, "restore", mod.T("journal.restore"), new Rectangle(x, actionY, canDonate ? w / 2 - 3 : w, 38), progress.Fragments >= cost);
            if (canDonate) Button(b, "donate", mod.T("journal.donate"), new Rectangle(x + w / 2 + 3, actionY, w / 2 - 3, 38));
        }
        else if (pending)
            Button(b, "claim:" + artifact.Id, mod.T("weather.claim"), new Rectangle(x, actionY, w, 38));
        else if (restored)
        {
            if (Game1.currentLocation.Name == "ArchaeologyHouse" && !mod.State.Donated.Contains(artifact.Id))
                Button(b, "donate", mod.T("journal.donate"), new Rectangle(x, actionY, w, 38));
            else if (artifact.Effect is not ("" or "lamp"))
                Button(b, "use", mod.T("journal.use"), new Rectangle(x, actionY, w, 38));
            else
                Paragraph(b, mod.T("journal.display"), new Rectangle(x, actionY - 10, w, 65), Muted);
        }
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (upperRightCloseButton?.containsPoint(x, y) == true) { exitThisMenu(); return; }
        string? id = buttons.FirstOrDefault(p => p.Value.Contains(x, y)).Key;
        if (id == null) return;
        Game1.playSound("smallSelect");
        if (id.StartsWith("tab:")) { tab = id[4..]; selected = ""; scroll = 0; keystone = false; }
        else if (id.StartsWith("field:") && tab == "overview") HandleObjectiveClick(id);
        else if (id.StartsWith("artifact:")) { selected = id[9..]; scroll = 0; }
        else if (id == "tools" && tab == "overview") mod.ClaimTools();
        else if (id.StartsWith("story:")) mod.WorkshopAction(id[6..]);
        else if (id.StartsWith("claim:"))
        {
            var story = ResidentStories.All.FirstOrDefault(p => p.Artifact == id[6..]);
            if (story != null) mod.ResidentAction(story.Id,"claim");
            else if (mod.State.Nature.PendingRewards.Contains(id[6..])) mod.ClaimNatureReward(id[6..]);
            else mod.ClaimWeatherReward(id[6..]);
        }
        else if (id.StartsWith("resident:select:")) { selected = id[16..]; scroll = 0; residentArchive = false; }
        else if (id.StartsWith("resident:action:"))
        {
            string[] parts = id.Split(':');
            mod.ResidentAction(parts[2],parts[3]);
        }
        else if (id == "resident:archive") { residentArchive = !residentArchive; scroll = 0; }
        else if (id.StartsWith("nature:")) { selected = id[7..]; scroll = 0; }
        else if (id.StartsWith("gallery:select:")) { selected = id[15..]; scroll = 0; }
        else if (id.StartsWith("gallery:reward:")) mod.ClaimCollection(id[15..]);
        else if (id == "gallery:craft") mod.CraftGallery();
        else if (id.StartsWith("ship:action:")) mod.ShipwreckAction(id[12..]);
        else if (id == "ship:letter") { selected = selected == "letter" ? "" : "letter"; scroll = 0; }
        else if (id.StartsWith("culture-page:") && Research.Cultures.Contains(tab))
        {
            var artifacts = mod.Research.Artifacts.Where(p => p.Culture == tab).OrderBy(p => p.Rarity).ToList();
            int size = CulturePageSize(artifacts.Count), current = artifacts.FindIndex(p => p.Id == selected) / size;
            int next = current + (id.EndsWith("next") ? 1 : -1);
            if (next >= 0 && next * size < artifacts.Count) { selected = artifacts[next * size].Id; scroll = 0; }
        }
        else if (id == "restore") { mod.Restore(tab, keystone); scroll = 0; }
        else if (id == "keystone" && mod.Research.Progress(mod.State, tab).Keystones > 0) keystone = !keystone;
        else if (id == "donate") mod.Donate(mod.Research.Artifacts.First(p => p.Id == selected));
        else if (id == "use")
        {
            var artifact = mod.Research.Artifacts.First(p => p.Id == selected);
            exitThisMenu();
            mod.UseRelic(artifact);
        }
    }

    public override void receiveScrollWheelAction(int direction) => scroll = Math.Clamp(scroll - Math.Sign(direction) * 2, 0, maxScroll);

    public override void receiveKeyPress(Keys key)
    {
        if (key == Keys.Escape) { exitThisMenu(); return; }
        if (key is Keys.Left or Keys.Right or Keys.Tab)
        {
            tab = Tabs[(Array.IndexOf(Tabs, tab) + (key == Keys.Left ? Tabs.Length - 1 : 1)) % Tabs.Length];
            selected = ""; scroll = 0; keystone = false;
        }
        else if ((key is Keys.Up or Keys.Down) && tab == "overview")
        {
            var goals = mod.FieldObjectives();
            if (goals.Count == 0) return;
            int i = goals.FindIndex(p => p.Id == objectiveSelection);
            int next = i < 0 ? (key == Keys.Up ? goals.Count-1 : 0) : (i + (key == Keys.Up ? goals.Count-1 : 1)) % goals.Count;
            objectiveSelection = goals[next].Id;
            overviewGuide = false; scroll = 0;
        }
        else if ((key is Keys.Up or Keys.Down) && Research.Cultures.Contains(tab))
        {
            var items = mod.Research.Artifacts.Where(p => p.Culture == tab).OrderBy(p => p.Rarity).ToList();
            int i = items.FindIndex(p => p.Id == selected);
            selected = items[(i + (key == Keys.Up ? items.Count - 1 : 1)) % items.Count].Id;
            scroll = 0;
        }
        else if (key == Keys.Enter && Research.Cultures.Contains(tab)) mod.Restore(tab, keystone);
        else if (key == Keys.Enter && tab == "overview")
        {
            var goal = mod.FieldObjectives().FirstOrDefault(p => p.Id == objectiveSelection);
            if (!overviewGuide && goal != null) OpenObjective(goal);
        }
    }

    public override void performHoverAction(int x, int y)
    {
        hover = "";
        if (buttons.TryGetValue("keystone", out var rect) && rect.Contains(x, y)) hover = mod.T("journal.key-help");
    }
}
