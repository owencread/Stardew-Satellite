using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewValley;

namespace SatelliteDish;

public enum RepeatMode { Daily, Weekly, Seasonal, Once }

public class Reminder
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Message { get; set; } = "";
    public RepeatMode Repeat { get; set; } = RepeatMode.Daily;

    /// <summary>Game time (e.g. 600 = 6:00 am, 1330 = 1:30 pm).</summary>
    public int Time { get; set; } = 600;

    /// <summary>0 = Monday ... 6 = Sunday (Weekly).</summary>
    public int DayOfWeek { get; set; }

    /// <summary>1-28 (Seasonal and Once).</summary>
    public int DayOfMonth { get; set; } = 1;

    /// <summary>0 = spring ... 3 = winter (Once).</summary>
    public int Season { get; set; }

    /// <summary>Year (Once).</summary>
    public int Year { get; set; } = 1;

    public static int DateKey(int year, int season, int day) => year * 112 + season * 28 + day;
    public static int TodayKey => DateKey(Game1.year, Game1.seasonIndex, Game1.dayOfMonth);

    /// <summary>Whether the reminder should be shown right now (ignoring whether it already fired today).</summary>
    public bool IsDueNow()
    {
        switch (Repeat)
        {
            case RepeatMode.Once:
                int when = DateKey(Year, Season, DayOfMonth);
                if (when < TodayKey) return true; // missed while away — show it anyway
                return when == TodayKey && Game1.timeOfDay >= Time;
            case RepeatMode.Weekly:
                return (Game1.dayOfMonth - 1) % 7 == DayOfWeek && Game1.timeOfDay >= Time;
            case RepeatMode.Seasonal:
                return Game1.dayOfMonth == DayOfMonth && Game1.timeOfDay >= Time;
            default:
                return Game1.timeOfDay >= Time;
        }
    }

    public string Describe()
    {
        string t = Game1.getTimeOfDayString(Time);
        return Repeat switch
        {
            RepeatMode.Weekly => $"{Names.WeekDays[DayOfWeek]}s at {t}",
            RepeatMode.Seasonal => $"Day {DayOfMonth} of every season at {t}",
            RepeatMode.Once => $"{Names.Seasons[Season]} {DayOfMonth}, Year {Year} at {t}",
            _ => $"Every day at {t}"
        };
    }
}

internal static class Names
{
    public static readonly string[] WeekDays = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
    public static readonly string[] Seasons = { "Spring", "Summer", "Fall", "Winter" };
    public static readonly string[] Repeat = { "Every day", "Every week", "Every season", "Once" };
}

public class SaveModel
{
    public List<Reminder> Reminders { get; set; } = new();
}

/// <summary>Per-save, per-player reminder storage (a JSON file in the mod's data folder, so it works for farmhands too).</summary>
internal class ReminderStore
{
    private readonly IModHelper helper;
    private string? path;
    private SaveModel model = new();

    public ReminderStore(IModHelper helper) => this.helper = helper;

    public List<Reminder> Reminders => model.Reminders;

    public void Load()
    {
        path = $"data/{Game1.uniqueIDForThisGame}_{Game1.player.UniqueMultiplayerID}.json";
        model = helper.Data.ReadJsonFile<SaveModel>(path) ?? new SaveModel();
    }

    public void Save()
    {
        if (path != null)
            helper.Data.WriteJsonFile(path, model);
    }

    public void Upsert(Reminder r)
    {
        if (!model.Reminders.Contains(r))
            model.Reminders.Add(r);
        Save();
    }

    public void Remove(Reminder r)
    {
        model.Reminders.Remove(r);
        Save();
    }

    public void Clear()
    {
        path = null;
        model = new SaveModel();
    }
}
