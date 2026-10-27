using Archeo.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace Archeo.Menus;

internal sealed partial class JournalMenu
{
    private void DrawWeather(SpriteBatch b)
    {
        buttons.Remove("tools");
        Text(b, mod.T("weather.title"), left.X + 12, left.Y + 8, Ink, fontScale * 1.08f);
        string guide = mod.T("weather.guide", new { days = mod.Config.WeatherSiteLifetimeDays, cooldown = mod.Config.WeatherEventCooldownDays });
        if (!mod.Config.EnableWeatherEvents) guide += "\n\n" + mod.T("weather.disabled");
        if (mod.State.Weather.Lead is { } lead)
            guide += "\n\n" + mod.T("weather.lead", new { name = mod.T("weather.name." + lead.Kind), days = lead.Expires - ModEntry.Today + 1 });
        var lines = Wrap(guide, left.Width - 24).ToList();
        int lineH = (int)(Game1.smallFont.LineSpacing * fontScale);
        int visible = Math.Max(1, (left.Height - 65) / lineH);
        maxScroll = Math.Max(0, lines.Count - visible);
        scroll = Math.Clamp(scroll, 0, maxScroll);
        for (int i = scroll; i < Math.Min(lines.Count, scroll + visible); i++) Text(b, lines[i], left.X + 12, left.Y + 48 + (i - scroll) * lineH);
        if (maxScroll > scroll) Text(b, "↓", left.Right - 24, left.Bottom - 24, Muted);

        int cardH = (right.Height - 12) / 2;
        for (int index = 0; index < 2; index++)
        {
            string kind = WeatherExpeditions.Kinds[index];
            int y = right.Y + index * (cardH + 12);
            int x = right.X + 12, w = right.Width - 24;
            b.Draw(Game1.staminaRect, new Rectangle(right.X + 4, y, right.Width - 8, cardH), new Color(230, 210, 172));
            Text(b, mod.T("weather.name." + kind), x, y + 8, Ink);
            var artifact = mod.Research.Artifacts.First(p => p.Source == kind);
            bool pending = mod.State.Weather.PendingRewards.Contains(artifact.Id);
            bool known = mod.State.Restored.ContainsKey(artifact.Id) || pending;
            bool compact = cardH < 185;
            int iconSize = compact ? 2 : 3;
            b.Draw(mod.Atlas, new Vector2(x + 4, y + 40), Sprite(artifact.Sprite), known ? Color.White : new Color(67,55,43), 0, Vector2.Zero, iconSize, SpriteEffects.None, 1);
            int infoH = compact ? 36 : Math.Min(75, cardH - 122);
            int infoX = compact ? 48 : 68;
            Paragraph(b, known ? mod.ArtifactName(artifact) : mod.T("weather.reward-hint." + kind), new Rectangle(x + infoX, y + 41, w - infoX - 4, Math.Max(30, infoH)), known ? ModEntry.RarityColor(artifact.Rarity) : Muted);
            var active = mod.State.Sites.FirstOrDefault(s => s.EventKind == kind && s.Expires >= ModEntry.Today && s.Points.Any(p => !p.Dug));
            string status = active == null ? mod.T("weather.waiting." + kind)
                : mod.T(compact ? "weather.active-short" : "weather.active", new { location = mod.LocationName(active.Location), count = active.Points.Count(p => !p.Dug), days = active.Expires - ModEntry.Today + 1 });
            int statusY = y + (compact ? 81 : 113);
            if (!compact || !pending)
                Paragraph(b, status, new Rectangle(x, statusY, w, Math.Max(0, cardH - (statusY - y) - 38)), active == null ? Muted : Ink);
            if (pending)
                Button(b, "claim:" + artifact.Id, mod.T("weather.claim"), new Rectangle(x, y + cardH - 42, w, 34));
            else
                Text(b, mod.T("weather.record", new { count = mod.State.Weather.Completed.GetValueOrDefault(kind) }), x, y + cardH - 32, Muted, fontScale * .82f);
        }
    }
}
