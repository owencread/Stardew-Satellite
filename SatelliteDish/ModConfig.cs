namespace SatelliteDish;

public class ModConfig
{
    /// <summary>How long a reminder box stays on screen, in real seconds. 0 = until clicked.</summary>
    public int DisplaySeconds { get; set; } = 20;

    /// <summary>Maximum number of reminder boxes stacked at once.</summary>
    public int MaxVisible { get; set; } = 3;

    /// <summary>Play a chime when a reminder appears.</summary>
    public bool PlaySound { get; set; } = true;

    /// <summary>Only show reminders while a finished Satellite Dish exists.</summary>
    public bool RequireDish { get; set; } = true;

    /// <summary>Testing: Robin builds the dish instantly instead of taking 2 days. Set to false before release.</summary>
    public bool InstantBuild { get; set; } = false;
}
