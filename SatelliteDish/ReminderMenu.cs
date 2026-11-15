using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace SatelliteDish;

/// <summary>The console menu: a list of saved reminders on top, an editor below.</summary>
internal class ReminderMenu : IClickableMenu
{
    private const int MenuWidth = 960;
    private const int MenuHeight = 700;
    private const int RowHeight = 56;
    private const int VisibleRows = 5;

    private static readonly Rectangle ButtonSource = new(432, 439, 9, 9);
    private static readonly Rectangle RowSource = new(384, 396, 15, 15);
    private static readonly int[] Times = BuildTimes();

    private readonly ReminderStore store;
    private readonly Action<Reminder> onSaved;
    private readonly TextBox messageBox;

    // form state
    private Reminder? editing;
    private RepeatMode repeat = RepeatMode.Daily;
    private int weekDay;
    private int monthDay = 1;
    private int season;
    private int timeIndex;
    private string status = "";
    private int scroll;

    // layout
    private int left, innerWidth, formTop;
    private Rectangle listArea;
    private readonly List<Rectangle> rows = new();
    private readonly List<ClickableTextureComponent> deleteButtons = new();
    private ClickableTextureComponent upArrow = null!, downArrow = null!;
    private Selector repeatSel = null!, sel1 = null!, sel2 = null!, timeSel = null!;
    private Rectangle newButton, saveButton;
    private Point hover;

    public ReminderMenu(ReminderStore store, Action<Reminder> onSaved)
    {
        this.store = store;
        this.onSaved = onSaved;
        messageBox = new TextBox(Game1.content.Load<Texture2D>("LooseSprites\\textBox"), null, Game1.smallFont, Game1.textColor)
        {
            textLimit = 100
        };
        timeIndex = Math.Max(0, Array.IndexOf(Times, 900));
        season = Game1.seasonIndex;
        monthDay = Game1.dayOfMonth;
        Layout();
    }

    /*********
    ** Layout
    *********/
    private void Layout()
    {
        int x = (Game1.uiViewport.Width - MenuWidth) / 2;
        int y = (Game1.uiViewport.Height - MenuHeight) / 2 + 32;
        initialize(x, y, MenuWidth, MenuHeight, showUpperRightCloseButton: true);

        left = x + 44;
        innerWidth = MenuWidth - 88;
        int top = y + 40;

        listArea = new Rectangle(left, top, innerWidth - 64, RowHeight * VisibleRows);
        rows.Clear();
        deleteButtons.Clear();
        for (int i = 0; i < VisibleRows; i++)
        {
            var row = new Rectangle(left, top + i * RowHeight, listArea.Width, RowHeight - 4);
            rows.Add(row);
            deleteButtons.Add(new ClickableTextureComponent(
                new Rectangle(row.Right - 56, row.Y + (row.Height - 48) / 2, 48, 48),
                Game1.mouseCursors, new Rectangle(337, 494, 12, 12), 4f));
        }
        upArrow = new ClickableTextureComponent(new Rectangle(listArea.Right + 16, top, 44, 48), Game1.mouseCursors, new Rectangle(421, 459, 11, 12), 4f);
        downArrow = new ClickableTextureComponent(new Rectangle(listArea.Right + 16, listArea.Bottom - 48, 44, 48), Game1.mouseCursors, new Rectangle(421, 472, 11, 12), 4f);

        formTop = listArea.Bottom + 36;
        int fieldX = left + 160;
        messageBox.X = fieldX;
        messageBox.Y = formTop;
        messageBox.Width = innerWidth - 160;

        repeatSel = new Selector(fieldX, formTop + 72, 260,
            () => Names.Repeat[(int)repeat],
            d => { repeat = (RepeatMode)Wrap((int)repeat, d, 4); });
        sel1 = new Selector(fieldX, formTop + 136, 220, Sel1Text, Sel1Change);
        sel2 = new Selector(fieldX + 360, formTop + 136, 160,
            () => $"Day {monthDay}",
            d => monthDay = Wrap(monthDay - 1, d, 28) + 1);
        timeSel = new Selector(fieldX, formTop + 200, 220,
            () => Game1.getTimeOfDayString(Times[timeIndex]),
            d => timeIndex = Wrap(timeIndex, d, Times.Length));

        saveButton = new Rectangle(x + MenuWidth - 44 - 200, formTop + 264, 200, 64);
        newButton = new Rectangle(saveButton.X - 220, saveButton.Y, 200, 64);
    }

    private string Sel1Text() => repeat switch
    {
        RepeatMode.Weekly => Names.WeekDays[weekDay],
        RepeatMode.Seasonal => $"Day {monthDay}",
        RepeatMode.Once => Names.Seasons[season],
        _ => ""
    };

    private void Sel1Change(int d)
    {
        switch (repeat)
        {
            case RepeatMode.Weekly: weekDay = Wrap(weekDay, d, 7); break;
            case RepeatMode.Seasonal: monthDay = Wrap(monthDay - 1, d, 28) + 1; break;
            case RepeatMode.Once: season = Wrap(season, d, 4); break;
        }
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds) => Layout();

    /*********
    ** Input
    *********/
    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound); // close button

        if (new Rectangle(messageBox.X, messageBox.Y, messageBox.Width, messageBox.Height).Contains(x, y))
            messageBox.SelectMe();
        else
            messageBox.Selected = false;

        var list = store.Reminders;
        for (int i = 0; i < VisibleRows; i++)
        {
            int idx = scroll + i;
            if (idx >= list.Count) break;

            if (deleteButtons[i].containsPoint(x, y))
            {
                var r = list[idx];
                store.Remove(r);
                if (editing == r) ClearForm();
                scroll = Math.Clamp(scroll, 0, MaxScroll());
                status = "Reminder deleted.";
                Game1.playSound("trashcan");
                return;
            }
            if (rows[i].Contains(x, y))
            {
                LoadIntoForm(list[idx]);
                Game1.playSound("smallSelect");
                return;
            }
        }

        if (upArrow.containsPoint(x, y)) { Scroll(-1); return; }
        if (downArrow.containsPoint(x, y)) { Scroll(1); return; }

        if (repeatSel.Click(x, y) || timeSel.Click(x, y)) return;
        if (repeat != RepeatMode.Daily && sel1.Click(x, y)) return;
        if (repeat == RepeatMode.Once && sel2.Click(x, y)) return;

        if (newButton.Contains(x, y))
        {
            ClearForm();
            status = "New reminder.";
            Game1.playSound("smallSelect");
        }
        else if (saveButton.Contains(x, y))
        {
            Save();
        }
    }

    public override void receiveKeyPress(Keys key)
    {
        // Typing in the text box shouldn't close the menu (E) or trigger other hotkeys.
        if (messageBox.Selected)
        {
            if (key == Keys.Escape)
                messageBox.Selected = false;
            return;
        }
        base.receiveKeyPress(key);
    }

    public override void receiveScrollWheelAction(int direction) => Scroll(direction > 0 ? -1 : 1);

    public override void performHoverAction(int x, int y)
    {
        base.performHoverAction(x, y);
        hover = new Point(x, y);
        upArrow.tryHover(x, y);
        downArrow.tryHover(x, y);
        foreach (var d in deleteButtons) d.tryHover(x, y, 0.2f);
        repeatSel.Hover(x, y); sel1.Hover(x, y); sel2.Hover(x, y); timeSel.Hover(x, y);
    }

    protected override void cleanupBeforeExit()
    {
        messageBox.Selected = false;
        base.cleanupBeforeExit();
    }

    private void Scroll(int delta)
    {
        int old = scroll;
        scroll = Math.Clamp(scroll + delta, 0, MaxScroll());
        if (old != scroll) Game1.playSound("shiny4");
    }

    private int MaxScroll() => Math.Max(0, store.Reminders.Count - VisibleRows);

    /*********
    ** Form
    *********/
    private void LoadIntoForm(Reminder r)
    {
        editing = r;
        messageBox.Text = r.Message;
        repeat = r.Repeat;
        weekDay = r.DayOfWeek;
        monthDay = r.DayOfMonth;
        season = r.Season;
        timeIndex = Math.Max(0, Array.IndexOf(Times, r.Time));
        status = "Editing - press Save to update.";
    }

    private void ClearForm()
    {
        editing = null;
        messageBox.Text = "";
        repeat = RepeatMode.Daily;
        weekDay = 0;
        monthDay = Game1.dayOfMonth;
        season = Game1.seasonIndex;
        timeIndex = Math.Max(0, Array.IndexOf(Times, 900));
    }

    private void Save()
    {
        string text = messageBox.Text?.Trim() ?? "";
        if (text.Length == 0)
        {
            status = "Type a message first.";
            Game1.playSound("cancel");
            return;
        }

        var r = editing ?? new Reminder();
        r.Message = text;
        r.Repeat = repeat;
        r.Time = Times[timeIndex];
        r.DayOfWeek = weekDay;
        r.DayOfMonth = monthDay;
        r.Season = season;

        if (repeat == RepeatMode.Once)
        {
            // Pick the next occurrence of that date.
            int year = Game1.year;
            int key = Reminder.DateKey(year, season, monthDay);
            if (key < Reminder.TodayKey || (key == Reminder.TodayKey && r.Time <= Game1.timeOfDay))
                year++;
            r.Year = year;
        }

        store.Upsert(r);
        onSaved(r);
        status = editing == null ? "Reminder added!" : "Reminder updated!";
        ClearForm();
        Game1.playSound("coin");
    }

    /*********
    ** Draw
    *********/
    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.5f);
        drawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);
        SpriteText.drawStringWithScrollCenteredAt(b, "Satellite Dish", xPositionOnScreen + width / 2, yPositionOnScreen - 72);

        // list
        var list = store.Reminders;
        if (list.Count == 0)
        {
            Utility.drawTextWithShadow(b, "No reminders yet. Write one below and press Save.", Game1.smallFont,
                new Vector2(left + 8, listArea.Y + 16), Game1.textColor * 0.7f);
        }
        for (int i = 0; i < VisibleRows; i++)
        {
            int idx = scroll + i;
            if (idx >= list.Count) break;
            var r = list[idx];
            var row = rows[i];
            Color tint = r == editing ? Color.Wheat : row.Contains(hover) ? new Color(255, 245, 225) : Color.White;
            drawTextureBox(b, Game1.mouseCursors, RowSource, row.X, row.Y, row.Width, row.Height, tint, 4f, false);

            string line = Truncate($"{r.Describe()}:  {r.Message}", row.Width - 90);
            Vector2 size = Game1.smallFont.MeasureString(line);
            Utility.drawTextWithShadow(b, line, Game1.smallFont, new Vector2(row.X + 20, row.Y + (row.Height - size.Y) / 2 + 2), Game1.textColor);
            deleteButtons[i].draw(b);
        }
        if (scroll > 0) upArrow.draw(b);
        if (scroll < MaxScroll()) downArrow.draw(b);

        // divider
        b.Draw(Game1.staminaRect, new Rectangle(left, listArea.Bottom + 16, innerWidth, 4), Game1.textColor * 0.25f);

        // form
        DrawLabel(b, editing == null ? "Message" : "Message*", formTop);
        messageBox.Draw(b);
        DrawLabel(b, "Repeat", formTop + 72);
        repeatSel.Draw(b);
        if (repeat != RepeatMode.Daily)
        {
            DrawLabel(b, "On", formTop + 136);
            sel1.Draw(b);
            if (repeat == RepeatMode.Once) sel2.Draw(b);
        }
        DrawLabel(b, "Time", formTop + 200);
        timeSel.Draw(b);

        DrawButton(b, newButton, "New");
        DrawButton(b, saveButton, editing == null ? "Add" : "Save");
        if (status.Length > 0)
            Utility.drawTextWithShadow(b, status, Game1.smallFont, new Vector2(left, newButton.Y + 18), Game1.textColor * 0.8f);

        base.draw(b); // close button
        drawMouse(b);
    }

    private void DrawLabel(SpriteBatch b, string text, int y)
        => Utility.drawTextWithShadow(b, text, Game1.smallFont, new Vector2(left, y + 8), Game1.textColor);

    private void DrawButton(SpriteBatch b, Rectangle r, string label)
    {
        drawTextureBox(b, Game1.mouseCursors, ButtonSource, r.X, r.Y, r.Width, r.Height, r.Contains(hover) ? Color.Wheat : Color.White, 4f, false);
        Vector2 size = Game1.smallFont.MeasureString(label);
        Utility.drawTextWithShadow(b, label, Game1.smallFont, new Vector2(r.Center.X - size.X / 2, r.Center.Y - size.Y / 2 + 2), Game1.textColor);
    }

    private static string Truncate(string text, int maxWidth)
    {
        if (Game1.smallFont.MeasureString(text).X <= maxWidth) return text;
        while (text.Length > 0 && Game1.smallFont.MeasureString(text + "...").X > maxWidth)
            text = text[..^1];
        return text + "...";
    }

    private static int Wrap(int value, int delta, int count) => ((value + delta) % count + count) % count;

    private static int[] BuildTimes()
    {
        var list = new List<int>();
        for (int t = 600; t < 2600; t = Utility.ModifyTime(t, 30))
            list.Add(t);
        return list.ToArray();
    }

    /// <summary>A "&lt; value &gt;" picker.</summary>
    private class Selector
    {
        private readonly ClickableTextureComponent leftArrow, rightArrow;
        private readonly Rectangle textArea;
        private readonly Func<string> getText;
        private readonly Action<int> change;

        public Selector(int x, int y, int textWidth, Func<string> getText, Action<int> change)
        {
            this.getText = getText;
            this.change = change;
            leftArrow = new ClickableTextureComponent(new Rectangle(x, y, 48, 44), Game1.mouseCursors, new Rectangle(352, 495, 12, 11), 4f);
            textArea = new Rectangle(x + 56, y - 2, textWidth, 48);
            rightArrow = new ClickableTextureComponent(new Rectangle(x + 64 + textWidth, y, 48, 44), Game1.mouseCursors, new Rectangle(365, 495, 12, 11), 4f);
        }

        public bool Click(int x, int y)
        {
            int d = leftArrow.containsPoint(x, y) ? -1 : rightArrow.containsPoint(x, y) ? 1 : 0;
            if (d == 0) return false;
            change(d);
            Game1.playSound("smallSelect");
            return true;
        }

        public void Hover(int x, int y)
        {
            leftArrow.tryHover(x, y, 0.2f);
            rightArrow.tryHover(x, y, 0.2f);
        }

        public void Draw(SpriteBatch b)
        {
            leftArrow.draw(b);
            rightArrow.draw(b);
            drawTextureBox(b, Game1.mouseCursors, ButtonSource, textArea.X, textArea.Y, textArea.Width, textArea.Height, Color.White, 4f, false);
            string text = getText();
            Vector2 size = Game1.smallFont.MeasureString(text);
            Utility.drawTextWithShadow(b, text, Game1.smallFont, new Vector2(textArea.Center.X - size.X / 2, textArea.Center.Y - size.Y / 2 + 2), Game1.textColor);
        }
    }
}
