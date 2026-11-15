using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace SatelliteDish;

/// <summary>Draws reminder info boxes stacked in the bottom-left corner of the HUD.</summary>
internal class ReminderHud
{
    private class Notice
    {
        public string Title = "";
        public string Message = "";
        public int TicksLeft; // -1 = until clicked
        public Rectangle Bounds;
    }

    private const int BoxWidth = 440;
    private const int Padding = 24;
    private readonly List<Notice> notices = new();

    public void Show(string title, string message, int ticks)
    {
        notices.Add(new Notice { Title = title, Message = message, TicksLeft = ticks <= 0 ? -1 : ticks });
    }

    public void Clear() => notices.Clear();

    public void Update()
    {
        // Don't count down while paused or in a menu, so you don't miss reminders.
        if (Game1.paused || Game1.activeClickableMenu != null)
            return;

        for (int i = notices.Count - 1; i >= 0; i--)
        {
            var n = notices[i];
            if (n.TicksLeft > 0 && --n.TicksLeft == 0)
                notices.RemoveAt(i);
        }
    }

    public void Draw(SpriteBatch b, int maxVisible)
    {
        if (notices.Count == 0)
            return;

        foreach (var n in notices)
            n.Bounds = Rectangle.Empty;

        int x = 16;
        // Sit above vanilla HUD messages ("+1 Wood" etc.), which also use the bottom-left corner.
        int bottom = Game1.uiViewport.Height - 16 - Game1.hudMessages.Count * 84;

        var visible = notices.Skip(Math.Max(0, notices.Count - Math.Max(1, maxVisible))).ToList();
        foreach (var n in visible)
        {
            int textWidth = BoxWidth - Padding * 2;
            string body = Game1.parseText(n.Message, Game1.smallFont, textWidth);
            Vector2 titleSize = Game1.smallFont.MeasureString(n.Title);
            Vector2 bodySize = Game1.smallFont.MeasureString(body);
            int boxHeight = Padding * 2 + (int)titleSize.Y + 8 + (int)bodySize.Y;
            int y = bottom - boxHeight;

            float alpha = n.TicksLeft > 0 ? Math.Min(1f, n.TicksLeft / 30f) : 1f;

            IClickableMenu.drawTextureBox(b, x, y, BoxWidth, boxHeight, Color.White * alpha);
            b.DrawString(Game1.smallFont, n.Title, new Vector2(x + Padding, y + Padding), new Color(40, 90, 150) * alpha);
            Utility.drawTextWithShadow(b, body, Game1.smallFont, new Vector2(x + Padding, y + Padding + titleSize.Y + 8), Game1.textColor * alpha);

            n.Bounds = new Rectangle(x, y, BoxWidth, boxHeight);
            bottom = y - 8;
        }
    }

    /// <summary>Dismiss the box under the cursor, if any.</summary>
    public bool TryDismiss(int x, int y)
    {
        var hit = notices.LastOrDefault(n => n.Bounds.Contains(x, y));
        if (hit == null)
            return false;
        notices.Remove(hit);
        Game1.playSound("bigDeSelect");
        return true;
    }
}
