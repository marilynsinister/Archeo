using Archeo.Core;
using Archeo.Menus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewValley;

namespace Archeo.Smoke;

public sealed partial class SmokeEntry
{
    private void CheckJournalAppearance(ModEntry mod)
    {
        var viewport = Game1.uiViewport;
        foreach (var size in new[] { new Point(1280,720), new Point(900,640) })
        {
            Game1.uiViewport = new xTile.Dimensions.Rectangle(0,0,size.X,size.Y);
            Game1.setMousePosition(size.X - 8,size.Y - 8);
            Capture(new JournalMenu(mod), $"book-invitation-ru-{size.X}.png",size.X,size.Y);
        }
        Move("ArchaeologyHouse");
        Check(mod.ClaimFieldKit(), "visual fixture obtains the real field kit in an unsaved world");
        mod.State.Opening.Stage = OpeningStage.Complete;
        mod.State.Journey.Chapters.UnionWith(Journey.Chapters);
        mod.State.Experience = 1280;
        foreach (var culture in Research.Cultures)
            mod.Research.Progress(mod.State,culture).Fragments = 30;
        foreach (var artifact in mod.Research.Artifacts.Take(14)) mod.State.Restored[artifact.Id] = 1;
        Move("Forest");
        foreach (var language in new[] { LocalizedContentManager.LanguageCode.ru, LocalizedContentManager.LanguageCode.en })
        foreach (var size in new[] { new Point(1280,720), new Point(900,640) })
        {
            LocalizedContentManager.CurrentLanguageCode = language;
            Game1.uiViewport = new xTile.Dimensions.Rectangle(0,0,size.X,size.Y);
            Game1.setMousePosition(size.X - 8,size.Y - 8);
            foreach (string page in new[] { "overview", "catalog", "chapters", "equipment", "settings" })
            {
                var book = new JournalMenu(mod,page);
                string name = $"book-{page}-{language}-{size.X}";
                Capture(book,name+".png",size.X,size.Y);
                var bounds = new Rectangle(0,0,size.X,size.Y);
                Check(ButtonsOf(book).All(p => bounds.Contains(p.Value)), "book controls remain inside the viewport: "+name);
                Check(ButtonsOf(book).Count(p => p.Key.StartsWith("tab:")) == 5, "five index tabs remain actionable: "+name);
                if (page != "catalog") continue;
                Click(book,"catalog:filters");
                Capture(book,name+"-filters.png",size.X,size.Y);
                Check(ButtonsOf(book).ContainsKey("catalog:search"), "catalog search remains accessible: "+name);
                Click(book,"catalog:filters");
                Set(book,"selected","brass_gear");
                mod.Config.JournalTextScale = 1.3f;
                Capture(book,name+"-large.png",size.X,size.Y);
                int max = Field<int>(book,"maxScroll");
                for (int i=0; i<=max; i++) book.receiveScrollWheelAction(-1);
                Capture(book,name+"-bottom.png",size.X,size.Y);
                Check(Field<int>(book,"scroll") == max, "long artifact entry reaches its last line at 130%: "+name);
                mod.Config.JournalTextScale = 1;
                book.receiveGamePadButton(Buttons.RightShoulder);
                Capture(book,name+"-controller.png",size.X,size.Y);
                Check(Field<string>(book,"tab") == "chapters", "controller still follows the book index: "+name);
            }
        }
        Game1.uiViewport = viewport;
    }
}
