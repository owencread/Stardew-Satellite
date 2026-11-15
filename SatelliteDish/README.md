# Satellite Dish Reminders (Stardew Valley 1.6 / SMAPI 4)

Adds a **Satellite Dish** that Robin can build. Click its console (the green screen on the front) to open a menu where you can add, edit and delete reminders. When a reminder is due, an info box pops up in the bottom-left corner of the screen.

## Building
| | |
|---|---|
| Builder | Robin (Carpenter's Shop → Construct Farm Buildings) |
| Cost | 15,000g, 10 Iron Bars, 5 Refined Quartz, 2 Battery Packs |
| Build time | 2 days |
| Footprint | 3×3 tiles; the console is the middle tile of the bottom row |

Change any of this in `ModEntry.OnAssetRequested`.

## Reminders
- **Repeat:** every day, every week (pick a weekday), every season (pick a day 1–28), or once (pick a season and day; it uses the next time that date comes around and deletes itself after it fires).
- **Time:** 6:00 am – 1:30 am in 30-minute steps.
- A once-only reminder you missed (because you weren't playing that day) is shown as soon as you load in.
- Click a reminder box to dismiss it. Boxes also fade out after `DisplaySeconds`.
- Reminders are saved per save file **and** per player in `Mods/SatelliteDish/data/`, so farmhands in multiplayer each get their own.

## Config (`config.json`, created on first launch)
| Setting | Default | Meaning |
|---|---|---|
| `DisplaySeconds` | 20 | How long a box stays up. `0` = until clicked. |
| `MaxVisible` | 3 | Max boxes stacked at once. |
| `PlaySound` | true | Chime when a reminder appears. |
| `RequireDish` | true | Only show reminders while a finished dish exists. |

## Build
1. Install .NET 6 SDK, Stardew Valley 1.6+, and SMAPI 4.
2. `dotnet build` in this folder. `Pathoschild.Stardew.ModBuildConfig` finds your game install and copies the built mod (DLL, `manifest.json`, `assets/`) into `Stardew Valley/Mods/SatelliteDish`.
   If it can't find the game, add `<GamePath>C:\path\to\Stardew Valley</GamePath>` to the `.csproj`.
3. Launch through SMAPI. To test fast, use the SMAPI console: `debug money 100000` to afford it, or `debug build OwenC.SatelliteDish_SatelliteDish` (vanilla debug command) to place it instantly.

## Files
- `ModEntry.cs` – registers the building in `Data/Buildings`, the console tile action, reminder timing, and HUD events.
- `ReminderMenu.cs` – the console menu (list + editor).
- `ReminderHud.cs` – the bottom-left info boxes.
- `Reminder.cs` – reminder model + JSON storage.
- `assets/SatelliteDish.png` – 48×80 placeholder sprite (bottom 48 px = 3×3 footprint). Swap in your own art at the same size.

## Known limits / ideas
- No controller navigation in the menu yet (mouse + keyboard only).
- Vanilla multiplayer chat also uses the bottom-left corner and may overlap.
- Possible additions: i18n, Generic Mod Config Menu support, reminders tied to NPC birthdays or festivals.
