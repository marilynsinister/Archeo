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
    private readonly Dictionary<string, Rectangle> tabBounds = new();
    private readonly HashSet<string> disabledButtons = new();
    private string tab = "overview";
    private string selected = "";
    private bool keystone;
    private int scroll;
    private int maxScroll;
    private string hover = "";
    private Rectangle left;
    private Rectangle right;
    private float fontScale;
    private float ReadingScale => mod.Config.JournalTextScale;
    private int ReadingLineHeight => (int)Math.Ceiling(Game1.smallFont.LineSpacing * ReadingScale) + 5;
    private static readonly Color Ink = new(64, 51, 38);
    private static readonly Color Muted = new(111, 94, 68);
    private static readonly Color Paper = new(249, 239, 212);
    private string[] Tabs => mod.State.Journey.Started ? new[] { "overview", "catalog", "chapters", "equipment", "settings" } : new[] { "overview", "settings" };

    public JournalMenu(ModEntry mod, string initialTab = "overview") : base(0, 0, 100, 100, true)
    {
        this.mod = mod;
        tab = AccessibleTab(initialTab) ? initialTab : "overview";
        Layout();
    }

    private void Layout()
    {
        width = Math.Min(1160, Game1.uiViewport.Width - 40);
        height = Math.Min(760, Game1.uiViewport.Height - 40);
        xPositionOnScreen = (Game1.uiViewport.Width - width) / 2;
        yPositionOnScreen = (Game1.uiViewport.Height - height) / 2;
        fontScale = 1f;
        int bodyY = yPositionOnScreen + 148;
        int bodyH = height - 212;
        int leftW = (int)((width - 68) * .43f);
        left = new Rectangle(xPositionOnScreen + 28, bodyY, leftW, bodyH);
        right = new Rectangle(left.Right + 24, bodyY, width - leftW - 80, bodyH);
        initializeUpperRightCloseButton();
        buttons.Clear(); tabBounds.Clear();
        int columns = Tabs.Length;
        int tabW = (width - 64) / columns;
        for (int i = 0; i < Tabs.Length; i++)
            tabBounds["tab:" + Tabs[i]] = new Rectangle(xPositionOnScreen + 32 + tabW * i, yPositionOnScreen + 98, tabW - 8, 34);
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

    private void ReadingText(SpriteBatch b, string text, int x, int y, Color? color = null)
        => Text(b, text, x, y, color, ReadingScale);

    private IEnumerable<string> Wrap(string text, int pixelWidth, float? scale = null)
    {
        foreach (string paragraph in text.Split('\n'))
        {
            string line = "";
            foreach (string word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                string candidate = line.Length == 0 ? word : line + " " + word;
                if (Game1.smallFont.MeasureString(candidate).X * (scale ?? ReadingScale) > pixelWidth && line.Length > 0)
                { yield return line; line = word; }
                else line = candidate;
            }
            yield return line;
        }
    }

    private int Paragraph(SpriteBatch b, string text, Rectangle area, Color? color = null)
    {
        int lineHeight = (int)(Game1.smallFont.LineSpacing * fontScale) + 4;
        int y = area.Y;
        foreach (string line in Wrap(text, area.Width, fontScale))
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
        if (!enabled) disabledButtons.Add(id);
        bool hovered = rect.Contains(Game1.getMouseX(), Game1.getMouseY());
        bool indexTab = id.StartsWith("tab:");
        DrawButtonSurface(b, rect, enabled, active, hovered, indexTab);
        Color textColor = indexTab && active ? Paper : enabled ? Ink : Muted;
        Center(b, label, rect, textColor, Math.Min(fontScale * (indexTab ? .94f : 1f), (rect.Width - 20f) / Math.Max(1, Game1.smallFont.MeasureString(label).X)));
    }

    public override void draw(SpriteBatch b)
    {
        if (tabBounds.Count != Tabs.Length) Layout();
        if (!AccessibleTab(tab)) tab = "overview";
        buttons.Clear(); disabledButtons.Clear(); maxScroll = 0;
        DrawBook(b);
        DrawBookHeading(b);
        foreach (string key in Tabs)
            Button(b, "tab:" + key, mod.T("journey.nav."+key), tabBounds["tab:" + key], active: TabSection(tab) == key);
        if (TabSection(tab) != tab) Button(b,"book:back",mod.T("journey.back"),new Rectangle(xPositionOnScreen+width-174,yPositionOnScreen+47,138,28));
        if (tab == "help") DrawLearning(b); else if (tab == "routes") DrawRoutes(b); else if (tab == "opening") DrawOpening(b); else if (tab == "overview" && !mod.State.Journey.Started) DrawInvitation(b); else if (tab == "overview") DrawOverview(b); else if(tab=="expeditions")DrawExpeditions(b); else if (tab == "museum") DrawMuseum(b); else if (tab == "frontiers") DrawFrontiers(b); else if (tab == "equipment") DrawEquipment(b); else if (tab == "chapters") DrawChapters(b); else if (tab == "catalog") DrawCatalog(b); else if (tab == "mastery") DrawMastery(b); else if (tab == "settings") DrawSettings(b); else if (tab == "workshop") DrawWorkshop(b); else if (tab == "weather") DrawWeather(b); else if (tab == "gallery") DrawGallery(b); else if (tab == "shipwreck") DrawShipwreck(b); else if (tab == "nature") DrawNature(b); else if (tab == "residents") DrawResidents(b); else DrawCulture(b);
        DrawControllerFocus(b);
        string footer = mod.T(controllerActive ? "journey.controller-footer" : "journey.footer");
        Text(b, footer, xPositionOnScreen + 34, yPositionOnScreen + height - 43, Muted, Math.Min(fontScale * .82f, (width - 68f) / Math.Max(1, Game1.smallFont.MeasureString(footer).X)));
        DrawBookCloseButton(b);
        drawMouse(b);
        if (hover != "") drawHoverText(b, hover, Game1.smallFont);
    }

    private void DrawCulture(SpriteBatch b)
    {
        buttons.Remove("tools");
        var progress = mod.Research.Progress(mod.State, tab);
        var project = mod.Research.Project(mod.State, tab, mod.RandomFor("project:" + tab));
        var artifacts = CultureItems();
        if (!artifacts.Any(p => p.Id == selected)) selected = project.Id;
        Text(b, mod.T("journal.fragments", new { count = progress.Fragments, keys = progress.Keystones }), left.X + 12, left.Y + 6, Muted, fontScale * .88f);
        int pageSize = CulturePageSize(artifacts.Count);
        int pages = (artifacts.Count + pageSize - 1) / pageSize;
        int page = artifacts.FindIndex(p => p.Id == selected) / pageSize;
        int rowH = Math.Min(62, (left.Height - (pages > 1 ? 82 : 44)) / Math.Min(pageSize,artifacts.Count));
        int y = left.Y + 38;
        foreach (var artifact in artifacts.Skip(page * pageSize).Take(pageSize))
        {
            bool known = JournalCatalog.IsKnown(mod.State, artifact);
            var rect = new Rectangle(left.X + 6, y, left.Width - 12, rowH - 3);
            DrawArtifactRow(b, artifact, rect, known);
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
        DrawSpecimenMount(b, new Rectangle(x, right.Y + 6, 74, 80), ModEntry.RarityColor(artifact.Rarity));
        b.Draw(mod.Atlas, new Vector2(x + 6, right.Y + 10), Sprite(artifact.Sprite), known ? Color.White : new Color(67, 55, 43), 0, Vector2.Zero, 4, SpriteEffects.None, 1);
        string title = known ? mod.ArtifactName(artifact) : mod.T("journal.unknown");
        int titleHeight = Wrap(title, w - 84, fontScale).Count() * (Game1.smallFont.LineSpacing + 4);
        Paragraph(b, title, new Rectangle(x + 84, right.Y + 10, w - 84, titleHeight), ModEntry.RarityColor(artifact.Rarity));
        int rarityY = right.Y + Math.Max(66, titleHeight + 14);
        Text(b, mod.T("rarity." + artifact.Rarity), x + 84, rarityY, ModEntry.RarityColor(artifact.Rarity), fontScale * .85f);
        DrawPageRule(b, x + 84, rarityY + 31, w - 84);
        int actionY = right.Bottom - 48;
        int textTop = rarityY + 42;
        int textBottom = actionY - (artifact.Id == project.Id ? 94 : 14);
        string content = known ? mod.T("artifact." + artifact.Id + ".description") + "\n\n" + (restored ? mod.T("artifact." + artifact.Id + ".lore") : mod.T("journal.lore-locked"))
            : artifact.Source == "research" ? mod.T("journal.hidden-hint", new { culture = mod.CultureName(artifact.Culture) }) : "";
        content += "\n\n" + mod.ArtifactHint(artifact);
        if (restored && artifact.Effect is not ("" or "lamp"))
        {
            content = mod.RelicStatus(artifact) + "\n" + mod.T(Game1.player.Items.Any(p => p?.QualifiedItemId == ModEntry.QualifiedId(artifact)) ? "catalog.carried" : "message.bring-artifact") + "\n\n" + content;
        }
        var lines = Wrap(content, w).ToList();
        int lineH = ReadingLineHeight;
        int visible = Math.Max(1, (textBottom - textTop) / lineH);
        maxScroll = Math.Max(0, lines.Count - visible);
        scroll = Math.Clamp(scroll, 0, maxScroll);
        for (int i = scroll; i < Math.Min(lines.Count, scroll + visible); i++) ReadingText(b, lines[i], x, textTop + (i - scroll) * lineH);
        if (maxScroll > 0)
            Text(b, (scroll > 0 ? "^ " : "") + (scroll < maxScroll ? "v" : ""), right.Right - 24, textBottom - 16, Muted, .65f);
        if (artifact.Id == project.Id && mod.State.Opening.Stage != OpeningStage.Restored)
        {
            int cost = Research.Required(artifact, progress, keystone, mod.State);
            var bar = new Rectangle(x, actionY - 75, w, 12);
            b.Draw(Game1.staminaRect, bar, new Color(190, 166, 123));
            b.Draw(Game1.staminaRect, new Rectangle(bar.X, bar.Y, (int)(bar.Width * Math.Min(1, progress.Fragments / (float)cost)), bar.Height), ModEntry.RarityColor(artifact.Rarity));
            Text(b, mod.T("journal.progress", new { count = progress.Fragments, cost }), x, actionY - 60, Muted, fontScale * .82f);
            Button(b, "keystone", mod.T(keystone ? "journal.key-on" : "journal.key-off", new { count = progress.Keystones }), new Rectangle(x, actionY - 34, w, 28), progress.Keystones > 0);
            bool canDonate = restored && Game1.currentLocation.Name == "ArchaeologyHouse" && !mod.State.Donated.Contains(artifact.Id);
            bool canUse = restored && !canDonate && artifact.Effect is not ("" or "lamp");
            Button(b, "restore", mod.T("journal.restore"), new Rectangle(x, actionY, canDonate || canUse ? w / 2 - 3 : w, 38), progress.Fragments >= cost);
            if (canDonate) Button(b, "donate", mod.T("journal.donate"), new Rectangle(x + w / 2 + 3, actionY, w / 2 - 3, 38));
            else if (canUse) Button(b, "use", mod.T("journal.use"), new Rectangle(x + w / 2 + 3, actionY, w / 2 - 3, 38), mod.RelicDaysUntilReady(artifact) == 0 && Game1.player.Items.Any(p => p?.QualifiedItemId == ModEntry.QualifiedId(artifact)));
        }
        else if (pending)
            Button(b, "claim:" + artifact.Id, mod.T("weather.claim"), new Rectangle(x, actionY, w, 38));
        else if (restored)
        {
            if (OpeningStory.Active(mod.State) && artifact.Id == OpeningStory.Artifact)
                Button(b, "opening:open", mod.T("opening.read"), new Rectangle(x, actionY, w, 38));
            else if (Game1.currentLocation.Name == "ArchaeologyHouse" && !mod.State.Donated.Contains(artifact.Id))
                Button(b, "donate", mod.T("journal.donate"), new Rectangle(x, actionY, w, 38));
            else if (artifact.Effect is not ("" or "lamp"))
                Button(b, "use", mod.T("journal.use"), new Rectangle(x, actionY, w, 38), mod.RelicDaysUntilReady(artifact) == 0 && Game1.player.Items.Any(p => p?.QualifiedItemId == ModEntry.QualifiedId(artifact)));
            else
                Paragraph(b, mod.T("journal.display"), new Rectangle(x, actionY - 10, w, 65), Muted);
        }
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (upperRightCloseButton?.containsPoint(x, y) == true) { exitThisMenu(); return; }
        string? id = buttons.FirstOrDefault(p => p.Value.Contains(x, y)).Key;
        if (id != "catalog:search" && searchBox != null) searchBox.Selected = false;
        if (id == null || disabledButtons.Contains(id)) return;
        Game1.playSound("smallSelect");
        if (id.StartsWith("tab:")) SwitchTab(id[4..]);
        else if (id.StartsWith("learn:")) HandleLearningClick(id);
        else if (id.StartsWith("opening:")) HandleOpeningClick(id);
        else if (id.StartsWith("route:")) HandleRouteClick(id);
        else if(id.StartsWith("frontier:select:")){selected=id[16..];scroll=0;}
        else if(id=="frontier:open")SwitchTab(selected);
        else if(id=="frontier:clue"){string route=selected;SwitchTab("routes");selected=route;}
        else if (id == "book:back") SwitchTab(TabSection(tab));
        else if (id == "book:kit") mod.ClaimFieldKit();
        else if (id == "book:upgrade") mod.UpgradeInstrument();
        else if (id.StartsWith("book:chapter:")) SwitchTab(id[13..]);
        else if (id == "book:prev") chapterPage--;
        else if (id == "book:next") chapterPage++;
        else if (id is "book:equipment" or "tools") SwitchTab("equipment");
        else if (id.StartsWith("catalog:")) HandleCatalogClick(id);
        else if (id.StartsWith("mastery:")) HandleMasteryClick(id);
        else if (id.StartsWith("settings:")) HandleSettingsClick(id);
        else if(id.StartsWith("museum:"))HandleMuseumClick(id);
        else if(id.StartsWith("exp:"))HandleExpeditionClick(id);
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
        else if(id is "gallery:prev" or "gallery:next")TurnGalleryPage(id=="gallery:next"?1:-1);
        else if (id.StartsWith("gallery:select:")) { selected = id[15..]; scroll = 0; }
        else if (id.StartsWith("gallery:reward:")) mod.ClaimCollection(id[15..]);
        else if (id == "gallery:craft") mod.CraftGallery();
        else if (id.StartsWith("ship:action:")) mod.ShipwreckAction(id[12..]);
        else if (id == "ship:letter") { selected = selected == "letter" ? "" : "letter"; scroll = 0; }
        else if (id.StartsWith("culture-page:") && Research.Cultures.Contains(tab))
        {
            var artifacts = CultureItems();
            int size = CulturePageSize(artifacts.Count), current = artifacts.FindIndex(p => p.Id == selected) / size;
            int next = current + (id.EndsWith("next") ? 1 : -1);
            if (next >= 0 && next * size < artifacts.Count) { selected = artifacts[next * size].Id; scroll = 0; }
        }
        else if (id == "restore") { mod.Restore(mod.Research.Artifacts.First(p => p.Id == selected).Culture, keystone); scroll = 0; }
        else if (id == "keystone" && mod.Research.Progress(mod.State, mod.Research.Artifacts.First(p => p.Id == selected).Culture).Keystones > 0) keystone = !keystone;
        else if (id == "donate") mod.Donate(mod.Research.Artifacts.First(p => p.Id == selected));
        else if (id == "use")
        {
            var artifact = mod.Research.Artifacts.First(p => p.Id == selected);
            exitThisMenu();
            mod.UseRelic(artifact);
        }
        buttons.Clear(); disabledButtons.Clear();
    }

    public override void receiveScrollWheelAction(int direction) => scroll = Math.Clamp(scroll - Math.Sign(direction) * 2, 0, maxScroll);

    public override void receiveKeyPress(Keys key)
    {
        if (CaptureSettingKey(key)) return;
        if (searchBox?.Selected == true)
        {
            if (key is Keys.Escape or Keys.Enter or Keys.Tab) searchBox.Selected = false;
            return;
        }
        if (key == Keys.Escape) { exitThisMenu(); return; }
        if (key is Keys.Left or Keys.Right or Keys.Tab)
        {
            SwitchTab(Tabs[(Array.IndexOf(Tabs, TabSection(tab)) + (key == Keys.Left ? Tabs.Length - 1 : 1)) % Tabs.Length]);
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
        else if ((key is Keys.Up or Keys.Down) && (Research.Cultures.Contains(tab) || tab == "catalog"))
        {
            var items = tab == "catalog" ? CatalogItems() : CultureItems();
            if (items.Count == 0) return;
            int i = items.FindIndex(p => p.Id == selected);
            selected = items[(i + (key == Keys.Up ? items.Count - 1 : 1)) % items.Count].Id;
            scroll = 0;
        }
        else if (key == Keys.PageUp || key == Keys.PageDown) receiveScrollWheelAction(key == Keys.PageUp ? 1 : -1);
        else if (key == Keys.Enter && (Research.Cultures.Contains(tab) || tab == "catalog") && buttons.ContainsKey("restore") && !disabledButtons.Contains("restore")
            && mod.State.Cultures.Values.Any(p => p.Project == selected))
            mod.Restore(mod.Research.Artifacts.First(p => p.Id == selected).Culture, keystone);
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
