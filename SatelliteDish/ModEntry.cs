using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Buildings;

namespace SatelliteDish;

public class ModEntry : Mod
{
    private string BuildingId => $"{ModManifest.UniqueID}_SatelliteDish";
    private string TextureAsset => $"Mods/{ModManifest.UniqueID}/SatelliteDish";
    private string ActionKey => $"{ModManifest.UniqueID}_OpenReminders";

    private ModConfig config = null!;
    private ReminderStore store = null!;
    private readonly ReminderHud hud = new();
    private readonly HashSet<string> firedToday = new();

    public override void Entry(IModHelper helper)
    {
        config = helper.ReadConfig<ModConfig>();
        store = new ReminderStore(helper);

        helper.Events.Content.AssetRequested += OnAssetRequested;
        helper.Events.GameLoop.SaveLoaded += (_, _) => store.Load();
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => { store.Clear(); hud.Clear(); firedToday.Clear(); };
        helper.Events.GameLoop.DayStarted += (_, _) => { firedToday.Clear(); CheckReminders(); };
        helper.Events.GameLoop.TimeChanged += (_, _) => CheckReminders();
        helper.Events.GameLoop.UpdateTicked += (_, _) => { if (Context.IsWorldReady) hud.Update(); };
        helper.Events.Display.RenderedHud += (_, e) => hud.Draw(e.SpriteBatch, config.MaxVisible);
        helper.Events.Input.ButtonPressed += OnButtonPressed;

        // The building's console tile runs this action when clicked.
        GameLocation.RegisterTileAction(ActionKey, OnDishAction);
    }

    /*********
    ** Building
    *********/
    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(TextureAsset))
        {
            e.LoadFromModFile<Texture2D>("assets/SatelliteDish.png", AssetLoadPriority.Exclusive);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo("Data/Buildings"))
        {
            e.Edit(asset =>
            {
                var data = asset.AsDictionary<string, BuildingData>().Data;
                data[BuildingId] = new BuildingData
                {
                    Name = "Satellite Dish",
                    Description = "Receives signals from the stars. Use the console to set reminders that pop up while you play.",
                    Texture = TextureAsset,
                    Size = new Point(3, 3),
                    Builder = "Robin",
                    BuildCost = 75000,
                    BuildDays = config.InstantBuild ? 0 : 2,
                    BuildMaterials = new List<BuildingMaterial>
                    {
                        new() { ItemId = "(O)335", Amount = 10 }, // Iron Bar
                        new() { ItemId = "(O)338", Amount = 5 },  // Refined Quartz
                        new() { ItemId = "(O)787", Amount = 5 }   // Battery Pack
                    },
                    ActionTiles = new List<BuildingActionTile>
                    {
                        // Middle of the bottom row = the console screen on the sprite.
                        new() { Id = "Console", Tile = new Point(1, 2), Action = ActionKey }
                    }
                };
            });
        }
    }

    private bool OnDishAction(GameLocation location, string[] args, Farmer who, Point tile)
    {
        if (!who.IsLocalPlayer)
            return false;

        Game1.playSound("bigSelect");
        Game1.activeClickableMenu = new ReminderMenu(store, OnReminderSaved);
        return true;
    }

    private bool HasDish()
    {
        return Game1.locations.Any(loc =>
            loc.buildings.Any(b => b.buildingType.Value == BuildingId && b.daysOfConstructionLeft.Value <= 0));
    }

    /*********
    ** Reminders
    *********/
    private void OnReminderSaved(Reminder r)
    {
        // A reminder created/edited for a time that already passed today shouldn't pop up instantly.
        firedToday.Remove(r.Id);
        if (r.Repeat != RepeatMode.Once && r.IsDueNow())
            firedToday.Add(r.Id);
    }

    private void CheckReminders()
    {
        if (!Context.IsWorldReady)
            return;
        if (config.RequireDish && !HasDish())
            return;

        bool shown = false;
        foreach (var r in store.Reminders.ToList())
        {
            if (firedToday.Contains(r.Id) || !r.IsDueNow())
                continue;

            firedToday.Add(r.Id);
            hud.Show(r.Describe(), r.Message, config.DisplaySeconds * 60);
            shown = true;

            if (r.Repeat == RepeatMode.Once)
                store.Remove(r);
        }

        if (shown && config.PlaySound)
            Game1.playSound("newRecipe");
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.activeClickableMenu != null || e.Button != SButton.MouseLeft)
            return;

        Vector2 pos = e.Cursor.GetScaledScreenPixels();
        if (hud.TryDismiss((int)pos.X, (int)pos.Y))
            Helper.Input.Suppress(e.Button);
    }
}
