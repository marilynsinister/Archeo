using Archeo.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;

namespace Archeo.Menus;

internal sealed partial class JournalMenu
{
    private string controllerFocus = "";
    private bool controllerActive;

    protected override void cleanupBeforeExit()
    {
        if (searchBox != null) searchBox.Selected = false;
        base.cleanupBeforeExit();
    }

    private void SwitchTab(string next)
    {
        if (!AccessibleTab(next)) return;
        tab = next; selected = ""; scroll = 0; keystone = false; bindingSetting = "";
        if (searchBox != null) searchBox.Selected = false;
        controllerFocus = "tab:" + TabSection(tab);
        // A page's old buttons must never be actionable before its next draw.
        buttons.Clear();
    }

    private void DrawControllerFocus(SpriteBatch b)
    {
        if (!controllerActive) return;
        if (!buttons.ContainsKey(controllerFocus) || disabledButtons.Contains(controllerFocus))
            controllerFocus = buttons.Keys.FirstOrDefault(p => !p.StartsWith("tab:") && !disabledButtons.Contains(p)) ?? "tab:" + TabSection(tab);
        if (!buttons.TryGetValue(controllerFocus, out var rect)) return;
        Color color = new(77, 118, 139);
        b.Draw(Game1.staminaRect, new Rectangle(rect.X - 2, rect.Y - 2, rect.Width + 4, 3), color);
        b.Draw(Game1.staminaRect, new Rectangle(rect.X - 2, rect.Bottom - 1, rect.Width + 4, 3), color);
        b.Draw(Game1.staminaRect, new Rectangle(rect.X - 2, rect.Y - 2, 3, rect.Height + 4), color);
        b.Draw(Game1.staminaRect, new Rectangle(rect.Right - 1, rect.Y - 2, 3, rect.Height + 4), color);
    }

    public override void receiveGamePadButton(Buttons button)
    {
        controllerActive = true;
        if (CaptureSettingButton(button)) return;
        if (button == Buttons.B || button == Buttons.Back)
        {
            if (searchBox?.Selected == true) { searchBox.Selected = false; return; }
            exitThisMenu(); return;
        }
        if (button is Buttons.LeftShoulder or Buttons.RightShoulder)
        {
            SwitchTab(Tabs[(Array.IndexOf(Tabs, TabSection(tab)) + (button == Buttons.LeftShoulder ? Tabs.Length - 1 : 1)) % Tabs.Length]);
            return;
        }
        if (button is Buttons.RightThumbstickUp or Buttons.RightThumbstickDown)
        { receiveScrollWheelAction(button == Buttons.RightThumbstickUp ? 1 : -1); return; }
        if (button == Buttons.X && tab == "catalog") { HandleCatalogClick("catalog:search"); return; }
        if (button == Buttons.A)
        {
            if (buttons.TryGetValue(controllerFocus, out var rect) && !disabledButtons.Contains(controllerFocus))
                receiveLeftClick(rect.Center.X, rect.Center.Y);
            return;
        }
        int dx = button is Buttons.DPadLeft or Buttons.LeftThumbstickLeft ? -1 : button is Buttons.DPadRight or Buttons.LeftThumbstickRight ? 1 : 0;
        int dy = button is Buttons.DPadUp or Buttons.LeftThumbstickUp ? -1 : button is Buttons.DPadDown or Buttons.LeftThumbstickDown ? 1 : 0;
        if (dx == 0 && dy == 0) return;
        var controls = buttons.Where(p => !disabledButtons.Contains(p.Key)).Select(p => new JournalControl(p.Key, p.Value.X, p.Value.Y, p.Value.Width, p.Value.Height)).ToList();
        controllerFocus = JournalNavigation.Move(controls, controllerFocus, dx, dy);
    }
}
