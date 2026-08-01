using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using CitizenFX.Core;

using MenuAPI;

using Newtonsoft.Json;

using vMenuClient.data;

using static CitizenFX.Core.Native.API;
using static vMenuClient.CommonFunctions;
using static vMenuShared.ConfigManager;
using static vMenuShared.PermissionsManager;

namespace vMenuClient.menus
{
    public class MiscSettings
    {
        // Variables
        private Menu menu;

        private VehicleHandlingEditor vehicleHandlingEditor;
        private Menu vehicleHandlingEditorMenu;

        private Menu developerToolsMenu;
        private Menu entityOutlinesMenu;
        private Menu timecycleModifiersMenu;
        private static Menu entitySpawnerMenu;

        private Menu hudMenu;

        public enum SpeedDisplayState
        {
            Off,
            Kmh,
            Mph
        }

        public SpeedDisplayState SpeedDisplay { get; private set; } = IsAllowed(Permission.MSSpeed) ? (SpeedDisplayState)UserDefaults.MiscSpeedDisplay : SpeedDisplayState.Off;
        public bool ShowCoordinates { get; private set; } = false;
        public bool HideHud { get; private set; } = false;
        public bool HideRadar { get; private set; } = false;
        public bool ShowLocation { get; private set; } = UserDefaults.MiscShowLocation || vMenuShared.ConfigManager.GetSettingsBool(vMenuShared.ConfigManager.Setting.vmenu_showlocation_on_default);
        public bool DeathNotifications { get; private set; } = UserDefaults.MiscDeathNotifications;
        public bool JoinQuitNotifications { get; private set; } = false;
        public bool LockCameraX { get; private set; } = false;
        public bool LockCameraY { get; private set; } = false;
        public bool ShowLocationBlips { get; private set; } = UserDefaults.MiscLocationBlips || vMenuShared.ConfigManager.GetSettingsBool(vMenuShared.ConfigManager.Setting.vmenu_showlocationblips_on_default);
        public bool ShowPlayerBlips { get; private set; } = true;
        public bool MiscShowOverheadNames { get; private set; } = true;
        public bool ShowVehicleModelDimensions { get; private set; } = false;
        public bool ShowPedModelDimensions { get; private set; } = false;
        public bool ShowPropModelDimensions { get; private set; } = false;
        public bool ShowEntityHandles { get; private set; } = false;
        public bool ShowEntityModels { get; private set; } = false;
        public bool ShowEntityCoordinates { get; private set; } = false;
        public bool ShowEntityNetOwners { get; private set; } = false;
        public float ShowEntityRange { get; private set; } = 2;
        public bool MiscRespawnDefaultCharacter { get; private set; } = UserDefaults.MiscRespawnDefaultCharacter;
        public bool RestorePlayerAppearance { get; private set; } = UserDefaults.MiscRestorePlayerAppearance;
        public bool RestorePlayerWeapons { get; private set; } = UserDefaults.MiscRestorePlayerWeapons;
        public bool DrawTimeOnScreen { get; internal set; } = UserDefaults.MiscShowTime && IsAllowed(Permission.MSTime);
        public bool MiscRightAlignMenu { get; private set; } = UserDefaults.MiscRightAlignMenu;
        public bool MiscDisablePrivateMessages { get; private set; } = UserDefaults.MiscDisablePrivateMessages;
        public bool MiscDisableControllerSupport { get; private set; } = UserDefaults.MiscDisableControllerSupport;

        public MenuCheckboxItem ResetIndex;

        internal bool TimecycleEnabled { get; private set; } = false;
        internal int LastTimeCycleModifierIndex { get; private set; } = UserDefaults.MiscLastTimeCycleModifierIndex;
        internal int LastTimeCycleModifierStrength { get; private set; } = UserDefaults.MiscLastTimeCycleModifierStrength;

        private static float entitySpawnerRotationSnap = 15f;

        /// <summary>
        /// The current language used by the player.
        /// </summary>
        public static string CurrentLanguage { get; private set; } = !string.IsNullOrEmpty(UserDefaults.MiscCurrentLanguage) ? UserDefaults.MiscCurrentLanguage : GetResourceMetadata(GetCurrentResourceName(), "default_language", 0);

        // keybind states
        public bool KbTpToWaypoint { get; private set; } = UserDefaults.KbTpToWaypoint;
        public int KbTpToWaypointKey { get; } = vMenuShared.ConfigManager.GetSettingsInt(vMenuShared.ConfigManager.Setting.vmenu_teleport_to_wp_keybind_key) != -1
            ? vMenuShared.ConfigManager.GetSettingsInt(vMenuShared.ConfigManager.Setting.vmenu_teleport_to_wp_keybind_key)
            : 168; // 168 (F7 by default)
        public bool KbDriftMode { get; private set; } = UserDefaults.KbDriftMode;
        public bool KbRecordKeys { get; private set; } = UserDefaults.KbRecordKeys;
        public bool KbRadarKeys { get; private set; } = UserDefaults.KbRadarKeys;
        public bool KbPointKeys { get; private set; } = UserDefaults.KbPointKeys;

        internal static List<vMenuShared.ConfigManager.TeleportLocation> TpLocations = new();

        private static readonly LanguageManager Lm = new LanguageManager();

        /// <summary>
        /// Creates the menu.
        /// </summary>
        private void CreateMenu()
        {
            MenuController.MenuAlignment = MiscRightAlignMenu ? MenuController.MenuAlignmentOption.Right : MenuController.MenuAlignmentOption.Left;
            if (MenuController.MenuAlignment != (MiscRightAlignMenu ? MenuController.MenuAlignmentOption.Right : MenuController.MenuAlignmentOption.Left))
            {
                Notify.Error(CommonErrors.RightAlignedNotSupported);

                // (re)set the default to left just in case so they don't get this error again in the future.
                MenuController.MenuAlignment = MenuController.MenuAlignmentOption.Left;
                MiscRightAlignMenu = false;
                UserDefaults.MiscRightAlignMenu = false;
            }

            // Create the menu.
            menu = new Menu(MenuTitle, "Miscellaneous");

            developerToolsMenu = Lm.GetMenu(new Menu(MenuTitle, "Developer Tools"));

            vehicleHandlingEditor = new VehicleHandlingEditor();
            vehicleHandlingEditorMenu = vehicleHandlingEditor.GetMenu();

            entityOutlinesMenu = Lm.GetMenu(new Menu(MenuTitle, "Entity Info"));
            timecycleModifiersMenu = Lm.GetMenu(new Menu(MenuTitle, "Timecycle Modifiers"));
            entitySpawnerMenu = Lm.GetMenu(new Menu(MenuTitle, "Entity Spawner"));

            hudMenu = Lm.GetMenu(new Menu(MenuTitle, "HUD Options"));

            // keybind settings menu
            var keybindMenu = Lm.GetMenu(new Menu(MenuTitle, "Control Settings"));
            var keybindMenuBtn = new MenuItem("Control Settings", "Enable or disable some controls.");
            MenuController.AddSubmenu(menu, keybindMenu);
            MenuController.BindMenuItem(menu, keybindMenu, keybindMenuBtn);

            // keybind settings menu items
            var kbTpToWaypoint = new MenuCheckboxItem("Teleport To Waypoint", "Teleport to your waypoint when pressing the keybind. By default, this keybind is set to ~r~F7~s~, server owners are able to change this however so ask them if you don't know what it is.", KbTpToWaypoint);
            var kbDriftMode = new MenuCheckboxItem("Drift Mode", "Makes your vehicle have almost no traction while holding left shift on keyboard, or X on controller.", KbDriftMode);
            var kbRecordKeys = new MenuCheckboxItem("Recording Controls", "Enables or disables the Rockstar Editor recording hotkeys on both keyboard and controller.", KbRecordKeys);
            var kbRadarKeys = new MenuCheckboxItem("Minimap Controls", "Press the Multiplayer Info (z on keyboard, down arrow on controller) key to switch between expanded radar and normal radar.", KbRadarKeys);
            var kbPointKeysCheckbox = new MenuCheckboxItem("Finger Point Controls", "Enables the finger point toggle key. The default QWERTY keyboard mapping for this is ~b~B~s~, or for controller quickly double tap the right analog stick.", KbPointKeys);
            var disableControllerKey = new MenuCheckboxItem("Disable Controller Menu Toggle", "This disables the controller menu toggle key, but ~y~not~s~ the navigation buttons.", MiscDisableControllerSupport);

            // Create the menu items.
            var copyCoordinates = new MenuItem("Copy Coordinates", "Copy your current coordinates to the clipboard.");
            var alignMenu = new MenuListItem("Align Menu", new List<string> { "Left", "Right" }, MiscRightAlignMenu ? 1 : 0, "Align the menu left or right.");
            var disablePms = new MenuCheckboxItem("Disable Private Messages", "Prevent others from sending you private messages.", MiscDisablePrivateMessages);
            var speed = new MenuListItem("Show Speed", new List<string> { "Off", "km/h", "mph" }, (int)SpeedDisplay, "Display a speedometer on the screen.");
            var coords = new MenuCheckboxItem("Show Coordinates", "Display your current coordinates at the top of the screen.", ShowCoordinates);
            var hideRadar = new MenuCheckboxItem("Hide Radar", "Hide the radar/minimap.", HideRadar);
            var hideHud = new MenuCheckboxItem("Hide HUD", "Hide all HUD elements.", HideHud);
            var showLocation = new MenuCheckboxItem("Location Display", "Display your current location and heading, as well as the nearest cross road on the screen. ~y~Warning: This feature (can) take(s) up to -4.6 FPS when running at 60 Hz.~s~", ShowLocation) { LeftIcon = MenuItem.Icon.WARNING };
            var drawTime = new MenuCheckboxItem("Show Time On Screen", "Add a clock to the screen.", DrawTimeOnScreen);
            var languageList = new List<string>();
            for (var i = 0; i < LanguageManager.Languages.Keys.Count; i++)
            {
                languageList.Add(LanguageManager.Languages.Keys.ToArray()[i]);
            }
            var saveInfoWarning = GetSettingsBool(Setting.vmenu_server_store)
                ? ""
                : " ~y~All saving is done on the client side; if you delete FiveM you will lose your settings.~s~";
            var saveSettings = new MenuItem("~b~~h~Save Personal Settings~h~~s~", "Save your current settings.")
            {
                RightIcon = MenuItem.Icon.TICK
            };
            var exportData = new MenuItem("Export/Import Data", "Coming soon (TM): the ability to import and export your saved data.");
            var joinQuitNotifs = new MenuCheckboxItem("Join / Quit Notifications", "Receive notifications when someone joins or leaves the server.", JoinQuitNotifications);
            var deathNotifs = new MenuCheckboxItem("Death Notifications", "Receive notifications when someone dies or gets killed.", DeathNotifications);
            var nightVision = new MenuCheckboxItem("Toggle Night Vision", "Enable or disable night vision.", false);
            var thermalVision = new MenuCheckboxItem("Toggle Thermal Vision", "Enable or disable thermal vision.", false);

            var vehModelDimensions = new MenuCheckboxItem("Show Vehicle Outlines", "Draws the model outlines for every vehicle that's currently close to you.", ShowVehicleModelDimensions);
            var propModelDimensions = new MenuCheckboxItem("Show Prop Outlines", "Draws the model outlines for every prop that's currently close to you.", ShowPropModelDimensions);
            var pedModelDimensions = new MenuCheckboxItem("Show Ped Outlines", "Draws the model outlines for every ped that's currently close to you.", ShowPedModelDimensions);
            var showEntityHandles = new MenuCheckboxItem("Show Entity Handles", "Draws the the entity handles for all close entities (you must enable at least one of the outline functions above for this to work).", ShowEntityHandles);
            var showEntityModels = new MenuCheckboxItem("Show Entity Models", "Draws the the entity models for all close entities (you must enable at least one of the outline functions above for this to work).", ShowEntityModels);
            var showEntityCoords = new MenuCheckboxItem("Show Entity Coordinates", "Draws the the entity coordinates for all close entities (you must enable at least one of the outline functions above for this to work).", ShowEntityCoordinates);
            var showEntityNetOwners = new MenuCheckboxItem("Show Network Owners", "Draws the the entity net owner for all close entities (you must enable at least one of the outline functions above for this to work).", ShowEntityNetOwners);
            var dimensionsDistanceSlider = new MenuSliderItem("Show Outlines Radius", "Change the outline draw range.", 0, 20, 0, false);
            var copyEntityInfo = new MenuItem("Copy Entity Info", "Copies information about the entities surrounding you to the clipboard (you must enable at least one of the outline and entity information checkboxes below for this to work).");

            var clearArea = new MenuListItem("Clear Area", new List<string> { "5 m", "10 m", "25 m", "50 m", "100 m", "250 m", "500 m", "1000 m" }, 2, "Clears the area around your player. Damage, dirt, peds, props, vehicles, etc. get cleaned up, fixed and reset to the default world state.");

            ResetIndex = new MenuCheckboxItem("Reset Index", "Resets index once you go to main menu.", false);

            // Entity spawner
            var spawnNetworkedEntities = new MenuCheckboxItem("Spawn Networked Entities", "If enabled spawns networked entities. Otherwise local entities are spawned. ~y~Networked entities are limited! Spawning too many, the server will (silently) fail to properly keep track of them!~s~");
            var spawnDynamicEntities = new MenuCheckboxItem("Spawn Dynamic Entities", "If enabled spawns dynamic (movable) entities. Otherwise static (frozen) entities are spawned.", false);
            var fixEntityPosition = new MenuCheckboxItem("Fix Entity Position", "If enabled, the object does not move and rotate with the camera.", false);
            var rotationAxiss = Enum.GetNames(typeof(EntitySpawner.RotationAxis));
            var rotationAxis = new MenuListItem("Rotation Axis", rotationAxiss.ToList(), 2, "Sets the axis around which to rotate the entity.");
            var rotationSnaps = new float[] { 1f, 5f, 10f, 15f, 45f, 90f };
            var rotationSnap = new MenuListItem("Rotation Snap", rotationSnaps.Select(r => $"{r}°").ToList(), 3, "Sets the rotation snap amount when rotating the entity.");
            var rotationRelativeToChoices = Enum.GetNames(typeof(EntitySpawner.RotationReference)).ToList();
            var rotationRelativeTo = new MenuListItem("Rotation Relative To", rotationRelativeToChoices, (int)EntitySpawner.RotationReference_, "The reference coordinate system rotations will be relative to.");
            var resetRotation = new MenuItem("Reset Rotation", "Reset the rotation");
            var spawnEntityOnGround = new MenuCheckboxItem("Place On Ground", "If enabled, the entity will be placed on the ground.", true);
            var surfaceAlignments = new List<string> { "Once", "Continuously" };
            var alignEntityToSurface = new MenuListItem(
                "Align To Surface",
                surfaceAlignments,
                0,
                "If set to \"Once\", clicking this item will align the entity to the surface. If set to \"Continuously\", the item will be continuously aligned to the surface. Note that this overrides any custom pitch or roll.");
            var entityDistanceChoices = new float[] { 5f, 10f, 20f, 30f, 40f, 50f, 100f, 200f, 300f, 400f, 500f };
            var entityDistance = new MenuListItem("Entity Distance", entityDistanceChoices.Select(d => $"{d}").ToList(), 2, "Sets the distance of the entity you're placing.")
            {
                Enabled = false
            };
            var spawnNewEntity = new MenuItem("Spawn New Entity", "Spawns a new entity into the world and lets you set its position and rotation.");
            var confirmEntityPosition = new MenuItem("Confirm Entity Position", "Stops placing the entity, and sets it at its current location.");
            var confirmAndDuplicate = new MenuItem("Confirm Entity Position And Duplicate", "Stops placing the entity, sets it at its current location, and creates new one to place.");
            var cancelEntity = new MenuItem("Cancel", "Deletes the entity and cancels its placement.");
            var copySpawnedToClipboard = new MenuItem("Copy To Clipboard", "Copy spawned entity info to clipboard.");
            var removeLastSpawnedEntity = new MenuItem("Undo Place", "Undo the placement of entities in reverse order.");
            var removeSpawnedEntities = new MenuItem("~y~Remove All~s~", "Deletes all entities placed by all players.");

            var connectionSubmenu = Lm.GetMenu(new Menu(MenuTitle, "Connection Options"));
            var connectionSubmenuBtn = new MenuItem("Connection Options", "Server connection/game quit options.");

            var quitSession = new MenuItem("Quit Session", "Leaves you connected to the server, but quits the network session. ~r~Can not be used when you are the host.");
            var rejoinSession = new MenuItem("Re-join Session", "This may not work in all cases, but you can try to use this if you want to re-join the previous session after clicking 'Quit Session'.");
            var quitGame = new MenuItem("Quit Game", "Exits the game after 5 seconds.");
            var disconnectFromServer = new MenuItem("Disconnect From Server", "Disconnects you from the server and returns you to the serverlist. ~r~This feature is not recommended, quit the game completely instead and restart it for a better experience.");
            connectionSubmenu.AddMenuItem(quitSession);
            connectionSubmenu.AddMenuItem(rejoinSession);
            connectionSubmenu.AddMenuItem(quitGame);
            connectionSubmenu.AddMenuItem(disconnectFromServer);

            var enableTimeCycle = new MenuCheckboxItem("Enable Timecycle Modifier", "Enable or disable the timecycle modifier from the list below.", TimecycleEnabled);
            var timeCycleModifiersListData = TimeCycles.Timecycles.ToList();
            for (var i = 0; i < timeCycleModifiersListData.Count; i++)
            {
                timeCycleModifiersListData[i] += $" ({i + 1}/{timeCycleModifiersListData.Count})";
            }
            var timeCycles = new MenuListItem("TM", timeCycleModifiersListData, MathUtil.Clamp(LastTimeCycleModifierIndex, 0, Math.Max(0, timeCycleModifiersListData.Count - 1)), "Select a timecycle modifier and enable the checkbox above.");
            var timeCycleIntensity = new MenuSliderItem("Timecycle Modifier Intensity", "Set the timecycle modifier intensity.", 0, 20, LastTimeCycleModifierStrength, true);

            var locationBlips = new MenuCheckboxItem("Location Blips", "Shows blips on the map for some common locations.", ShowLocationBlips);
            var playerBlips = new MenuCheckboxItem("Show Player Blips", "Shows blips on the map for all players. ~y~Note for when the server is using OneSync Infinity: this won't work for players that are too far away.", ShowPlayerBlips);
            var playerNames = new MenuCheckboxItem("Show Player Names", "Enables or disables player overhead names.", MiscShowOverheadNames);
            var respawnDefaultCharacter = new MenuCheckboxItem("Spawn As Default Multiplayer Character", "If you enable this, you will (re)spawn as your default multiplayer character. To set your default character, go to one of your saved multiplayer characters and click ~b~Set As Default Character~s~.", MiscRespawnDefaultCharacter);
            var restorePlayerAppearance = new MenuCheckboxItem("Restore Player Appearance", "Restore your player's skin whenever you respawn after being dead. Re-joining a server will not restore your previous skin.", RestorePlayerAppearance);
            var restorePlayerWeapons = new MenuCheckboxItem("Restore Player Weapons", "Restore your weapons whenever you respawn after being dead. Re-joining a server will not restore your previous weapons.", RestorePlayerWeapons);

            MenuController.AddSubmenu(menu, connectionSubmenu);
            MenuController.BindMenuItem(menu, connectionSubmenu, connectionSubmenuBtn);

            keybindMenu.OnCheckboxChange += (sender, item, index, _checked) =>
            {
                if (item == disableControllerKey)
                {
                    MiscDisableControllerSupport = _checked;
                    MenuController.EnableMenuToggleKeyOnController = !_checked;
                }
                else if (item == kbTpToWaypoint)
                {
                    KbTpToWaypoint = _checked;
                }
                else if (item == kbDriftMode)
                {
                    KbDriftMode = _checked;
                }
                else if (item == kbRecordKeys)
                {
                    KbRecordKeys = _checked;
                }
                else if (item == kbRadarKeys)
                {
                    KbRadarKeys = _checked;
                }
                else if (item == kbPointKeysCheckbox)
                {
                    KbPointKeys = _checked;
                }
            };

            connectionSubmenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == quitGame)
                {
                    CommonFunctions.QuitGame();
                }
                else if (item == quitSession)
                {
                    if (NetworkIsSessionActive())
                    {
                        if (NetworkIsHost())
                        {
                            Notify.Error("Sorry, you cannot leave the session when you are the host. This would prevent other players from joining/staying on the server.");
                        }
                        else
                        {
                            QuitSession();
                        }
                    }
                    else
                    {
                        Notify.Error("You are currently not in any session.");
                    }
                }
                else if (item == rejoinSession)
                {
                    if (NetworkIsSessionActive())
                    {
                        Notify.Error("You are already connected to a session.");
                    }
                    else
                    {
                        Notify.Info("Attempting to re-join the session.");
                        NetworkSessionHost(-1, 32, false);
                    }
                }
                else if (item == disconnectFromServer)
                {

                    RegisterCommand("disconnect", new Action<dynamic, dynamic, dynamic>((a, b, c) => { }), false);
                    ExecuteCommand("disconnect");
                }
            };

            #region dev tools menu

            var devToolsBtn = new MenuItem("Developer Tools", "Various development and debug tools.") { Label = "→→→" };

            if (IsAllowed(Permission.MSDevTools))
            {
                menu.AddMenuItem(devToolsBtn);
            }

            MenuController.AddSubmenu(menu, developerToolsMenu);
            MenuController.BindMenuItem(menu, developerToolsMenu, devToolsBtn);

            // clear area and coordinates
            if (IsAllowed(Permission.MSClearArea))
            {
                developerToolsMenu.AddMenuItem(clearArea);
            }
            if (IsAllowed(Permission.MSShowCoordinates))
            {
                developerToolsMenu.AddMenuItem(coords);
            }

            {
                var menuBtn = new MenuItem("Vehicle Handling Editor", "Edit a vehicle's handling data.")
                {
                    Label = "→→→"
                };
                developerToolsMenu.AddMenuItem(menuBtn);
                MenuController.BindMenuItem(developerToolsMenu, vehicleHandlingEditorMenu, menuBtn);
            }

            // model outlines
            if (!GetSettingsBool(Setting.vmenu_disable_entity_outlines_tool) && IsAllowed(Permission.MSEntityInfo))
            {
                var menuBtn = new MenuItem("Entity Info", "Display information about entities.")
                {
                    Label = "→→→"
                };
                developerToolsMenu.AddMenuItem(menuBtn);
                MenuController.BindMenuItem(developerToolsMenu, entityOutlinesMenu, menuBtn);

                entityOutlinesMenu.AddMenuItem(copyEntityInfo);
                entityOutlinesMenu.AddMenuItem(dimensionsDistanceSlider);
                entityOutlinesMenu.AddMenuItem(propModelDimensions);
                entityOutlinesMenu.AddMenuItem(vehModelDimensions);
                entityOutlinesMenu.AddMenuItem(pedModelDimensions);
                entityOutlinesMenu.AddMenuItem(showEntityHandles);
                entityOutlinesMenu.AddMenuItem(showEntityModels);
                entityOutlinesMenu.AddMenuItem(showEntityCoords);
                entityOutlinesMenu.AddMenuItem(showEntityNetOwners);

                entityOutlinesMenu.OnSliderPositionChange += (sender, item, oldPos, newPos, itemIndex) =>
                {
                    if (item == dimensionsDistanceSlider)
                    {
                        // Goes from 4 -> 2500
                        ShowEntityRange = (float)(2496 * Math.Pow((double)newPos / dimensionsDistanceSlider.Max, 2.7) + 4);
                    }
                };

                entityOutlinesMenu.OnItemSelect += (sender, item, index) =>
                {
                    if (item == copyEntityInfo)
                    {
                        var playerPos = Game.PlayerPed.Position;

                        List<Prop> props = null;
                        if (propModelDimensions.Checked)
                            props = World.GetAllProps().Where(e => e.IsOnScreen && e.Position.DistanceToSquared(playerPos) < ShowEntityRange).ToList();

                        List<Ped> peds = null;
                        if (pedModelDimensions.Checked)
                            peds = World.GetAllPeds().Where(e => e.IsOnScreen && e.Position.DistanceToSquared(playerPos) < ShowEntityRange).ToList();

                        List<Vehicle> vehicles = null;
                        if (vehModelDimensions.Checked)
                            vehicles = World.GetAllVehicles().Where(e => e.IsOnScreen && e.Position.DistanceToSquared(playerPos) < ShowEntityRange).ToList();

                        if (props == null && peds == null && vehicles == null)
                        {
                            Notify.Error("You must select at least one of the outline checkboxes.");
                            return;
                        }

                        bool printHandle = showEntityHandles.Checked;
                        bool printHash = showEntityModels.Checked;
                        bool printCoords = showEntityCoords.Checked;
                        bool printOwner = showEntityNetOwners.Checked;

                        if (!(printHandle || printHash || printCoords || printOwner))
                        {
                            Notify.Error("You must select at least one of the entity information checkboxes.");
                            return;
                        }

                        StringBuilder sbProps = new StringBuilder();
                        StringBuilder sbPeds = new StringBuilder();
                        StringBuilder sbVehicles = new StringBuilder();

                        var printEntityInfo =
                            (StringBuilder sb, Entity e) => sb.AppendLine(PrintEntityInfo(e, printHandle, printHash, printCoords, printOwner));

                        bool multipleCategories = ((props != null ? 1 : 0) + (peds != null ? 1 : 0) + (vehicles != null ? 1 : 0)) > 1;

                        if (props?.Count > 0)
                        {
                            if (multipleCategories)
                            {
                                sbProps.AppendLine("====================\nPROPS\n====================");
                            }
                            props.ForEach(e => printEntityInfo(sbProps, e));
                        }

                        if (peds?.Count > 0)
                        {
                            if (multipleCategories)
                            {
                                sbPeds.AppendLine("====================\nPEDS\n====================");
                            }
                            peds.ForEach(e => printEntityInfo(sbPeds, e));
                        }

                        if (vehicles?.Count > 0)
                        {
                            if (multipleCategories)
                            {
                                sbVehicles.AppendLine("====================\nVEHICLES\n====================");
                            }
                            vehicles.ForEach(e => printEntityInfo(sbVehicles, e));
                        }

                        var infos = new StringBuilder[] { sbProps, sbPeds, sbVehicles }
                            .Select(x => x.ToString())
                            .Where(i => !string.IsNullOrEmpty(i))
                            .ToArray();
                        var info = string.Join("\n", infos);
                        if (string.IsNullOrWhiteSpace(info))
                        {
                            Notify.Info("There were no entities matching your selected criteria in the chosen range.");
                            return;
                        }

                        CopyToClipboard(info);
                        Notify.Info("Entity information copied to the clipboard.");
                    }
                };

                entityOutlinesMenu.OnCheckboxChange += (sender, item, index, _checked) =>
                {
                    if (item == vehModelDimensions)
                    {
                        ShowVehicleModelDimensions = _checked;
                    }
                    else if (item == propModelDimensions)
                    {
                        ShowPropModelDimensions = _checked;
                    }
                    else if (item == pedModelDimensions)
                    {
                        ShowPedModelDimensions = _checked;
                    }
                    else if (item == showEntityHandles)
                    {
                        ShowEntityHandles = _checked;
                    }
                    else if (item == showEntityModels)
                    {
                        ShowEntityModels = _checked;
                    }
                    else if (item == showEntityCoords)
                    {
                        ShowEntityCoordinates = _checked;
                    }
                    else if (item == showEntityNetOwners)
                    {
                        ShowEntityNetOwners = _checked;
                    }
                };
            }

            if (IsAllowed(Permission.MSTimecycleMofifiers))
            {
                var menuBtn = new MenuItem("Timecycle Modifiers", "Change timecycle modifiers.")
                {
                    Label = "→→→"
                };
                developerToolsMenu.AddMenuItem(menuBtn);
                MenuController.BindMenuItem(developerToolsMenu, timecycleModifiersMenu, menuBtn);
                // timecycle modifiers
                timecycleModifiersMenu.AddMenuItem(enableTimeCycle);
                timecycleModifiersMenu.AddMenuItem(timeCycles);
                timecycleModifiersMenu.AddMenuItem(timeCycleIntensity);

                timecycleModifiersMenu.OnSliderPositionChange += (sender, item, oldPos, newPos, itemIndex) =>
                {
                    if (item == timeCycleIntensity)
                    {
                        ClearTimecycleModifier();
                        if (TimecycleEnabled)
                        {
                            SetTimecycleModifier(TimeCycles.Timecycles[timeCycles.ListIndex]);
                            var intensity = newPos / 20f;
                            SetTimecycleModifierStrength(intensity);
                        }
                        UserDefaults.MiscLastTimeCycleModifierIndex = timeCycles.ListIndex;
                        UserDefaults.MiscLastTimeCycleModifierStrength = timeCycleIntensity.Position;
                    }
                };

                timecycleModifiersMenu.OnListIndexChange += (sender, item, oldIndex, newIndex, itemIndex) =>
                {
                    if (item == timeCycles)
                    {
                        ClearTimecycleModifier();
                        if (TimecycleEnabled)
                        {
                            SetTimecycleModifier(TimeCycles.Timecycles[timeCycles.ListIndex]);
                            var intensity = timeCycleIntensity.Position / 20f;
                            SetTimecycleModifierStrength(intensity);
                        }
                        UserDefaults.MiscLastTimeCycleModifierIndex = timeCycles.ListIndex;
                        UserDefaults.MiscLastTimeCycleModifierStrength = timeCycleIntensity.Position;
                    }
                };

                timecycleModifiersMenu.OnCheckboxChange += (sender, item, index, _checked) =>
                {
                    if (item == enableTimeCycle)
                    {
                        TimecycleEnabled = _checked;
                        ClearTimecycleModifier();
                        if (TimecycleEnabled)
                        {
                            SetTimecycleModifier(TimeCycles.Timecycles[timeCycles.ListIndex]);
                            var intensity = timeCycleIntensity.Position / 20f;
                            SetTimecycleModifierStrength(intensity);
                        }
                    }
                };
            }

            developerToolsMenu.OnListItemSelect += (sender, item, selectedIndex, itemIndex) =>
            {
                if (item == clearArea)
                {
                    var pos = Game.PlayerPed.Position;
                    BaseScript.TriggerServerEvent("vMenu:ClearArea", pos.X, pos.Y, pos.Z, float.Parse(item.GetCurrentSelection().Split([' '])[0]));
                }
            };

            developerToolsMenu.OnCheckboxChange += (sender, item, index, _checked) =>
            {
                if (item == coords)
                {
                    ShowCoordinates = _checked;
                }
            };

            if (IsAllowed(Permission.MSEntitySpawner))
            {
                var entSpawnerMenuBtn = new MenuItem("Entity Spawner", "Spawn and move entities") { Label = "→→→" };
                developerToolsMenu.AddMenuItem(entSpawnerMenuBtn);
                MenuController.BindMenuItem(developerToolsMenu, entitySpawnerMenu, entSpawnerMenuBtn);

                entitySpawnerMenu.AddMenuItem(spawnNewEntity);
                entitySpawnerMenu.AddMenuItem(spawnEntityOnGround);
                entitySpawnerMenu.AddMenuItem(entityDistance);
                entitySpawnerMenu.AddMenuItem(alignEntityToSurface);
                entitySpawnerMenu.AddMenuItem(fixEntityPosition);
                entitySpawnerMenu.AddMenuItem(resetRotation);
                entitySpawnerMenu.AddMenuItem(rotationRelativeTo);
                entitySpawnerMenu.AddMenuItem(rotationAxis);
                entitySpawnerMenu.AddMenuItem(rotationSnap);
                entitySpawnerMenu.AddMenuItem(confirmEntityPosition);
                entitySpawnerMenu.AddMenuItem(confirmAndDuplicate);
                entitySpawnerMenu.AddMenuItem(cancelEntity);
                entitySpawnerMenu.AddMenuItem(copySpawnedToClipboard);
                entitySpawnerMenu.AddMenuItem(removeLastSpawnedEntity);
                entitySpawnerMenu.AddMenuItem(spawnNetworkedEntities);
                entitySpawnerMenu.AddMenuItem(spawnDynamicEntities);
                entitySpawnerMenu.AddMenuItem(removeSpawnedEntities);

                void RotateEntity(EntitySpawner.RotationDirection direction)
                {
                    EntitySpawner.RotateEntity(
                        direction,
                        entitySpawnerRotationSnap);
                }

                var cmdName = (string cmd) => $"{GetSettingsString(Setting.vmenu_individual_server_id)}vMenu:entitySpawer:{cmd}";
                var rotateCmdName = (string dir) => cmdName($"rotate_{dir}");
                RegisterKeyMapping(rotateCmdName("forwd"), "vMenu Rot. Entity Forwd.", "keyboard", "Numpad1");
                RegisterCommand(rotateCmdName("forwd"), new Action<int, List<object>, string>((source, args, raw) =>
                {
                    RotateEntity(EntitySpawner.RotationDirection.Forward);
                }), false);
                RegisterKeyMapping(rotateCmdName("backwd"), "vMenu Rot. Entity Backwd.", "keyboard", "Numpad3");
                RegisterCommand(rotateCmdName("backwd"), new Action<int, List<object>, string>((source, args, raw) =>
                {
                    RotateEntity(EntitySpawner.RotationDirection.Backward);
                }), false);
                RegisterKeyMapping(cmdName("cycleRotationAxis"), "vMenu Cycle Entity Rot. Axis", "keyboard", "Numpad0");
                RegisterCommand(cmdName("cycleRotationAxis"), new Action<int, List<object>, string>((source, args, raw) =>
                {
                    var index = rotationAxis.ListIndex = (rotationAxis.ListIndex + 1) % rotationAxis.ItemsCount;
                    EntitySpawner.RotationAxis_ = (EntitySpawner.RotationAxis)index;
                }), false);

                void SetIsEntityFixed(bool value)
                {
                    fixEntityPosition.Checked = value;
                    EntitySpawner.IsEntityPositionFixed = value;
                    EntitySpawner.RotateEntityWithCameraYaw = !value;
                }

                void ResetMenuBeforeAfterPlace()
                {
                    SetIsEntityFixed(false);
                }

                entitySpawnerMenu.OnItemSelect += async (sender, item, index) =>
                {
                    if (item == resetRotation)
                    {
                        EntitySpawner.ResetRotation();
                    }
                    else if (item == spawnNewEntity)
                    {
                        if (EntitySpawner.CurrentEntity != null || EntitySpawner.Active)
                        {
                            Notify.Error("You are already placing one entity, set its location or cancel and try again!");
                            return;
                        }
                        ResetMenuBeforeAfterPlace();

                        var result = await GetUserInput(windowTitle: "Enter model name or hash", 100);

                        if (string.IsNullOrEmpty(result))
                        {
                            Notify.Error(CommonErrors.InvalidInput);
                        }

                        EntitySpawner.SpawnEntity(result, Game.PlayerPed.Position);
                    }
                    else if (item == confirmEntityPosition || item == confirmAndDuplicate)
                    {
                        if (EntitySpawner.CurrentEntity != null)
                        {
                            EntitySpawner.FinishPlacement(item == confirmAndDuplicate);
                            ResetMenuBeforeAfterPlace();
                        }
                        else
                        {
                            Notify.Error("No entity to confirm position for!");
                        }
                    }
                    else if (item == cancelEntity)
                    {
                        if (EntitySpawner.CurrentEntity != null)
                        {
                            EntitySpawner.CurrentEntity.Delete();
                            ResetMenuBeforeAfterPlace();
                        }
                        else
                        {
                            Notify.Error("No entity to cancel!");
                        }
                    }
                    else if (item == copySpawnedToClipboard)
                    {
                        await EntitySpawner.CopyEntitiesToClipboard();
                    }
                    else if (item == removeLastSpawnedEntity)
                    {
                        EntitySpawner.RemoveMostRecent();
                    }
                    else if (item == removeSpawnedEntities)
                    {
                        EntitySpawner.RemoveAll();
                    }
                };
                entitySpawnerMenu.OnListIndexChange += (sender, item, oldIndex, newIndex, itemIndex) =>
                {
                    if (item == entityDistance)
                    {
                        EntitySpawner.PlacementDistance = entityDistanceChoices[newIndex];
                    }
                    else if (item == rotationAxis)
                    {
                        EntitySpawner.RotationAxis_ = (EntitySpawner.RotationAxis)newIndex;
                    }
                    else if (item == rotationRelativeTo)
                    {
                        EntitySpawner.RotationReference_ = (EntitySpawner.RotationReference)newIndex;
                    }
                    else if (item == rotationSnap)
                    {
                        entitySpawnerRotationSnap = rotationSnaps[newIndex];
                    }
                    else if (item == alignEntityToSurface)
                    {
                        EntitySpawner.AlignToSurfaceContinuously = newIndex == 1;
                    }
                };
                entitySpawnerMenu.OnListItemSelect += (sender, item, index, itemIndex) =>
                {
                    if (item == alignEntityToSurface && index == 0)
                    {
                        EntitySpawner.AlignEntityToSurface();
                    }
                };
                entitySpawnerMenu.OnCheckboxChange += (_sender, item, ix, checked_) =>
                {
                    if (item == spawnNetworkedEntities)
                    {
                        bool old = !checked_;
                        if (!EntitySpawner.SetSpawnNetworked(checked_))
                        {
                            spawnNetworkedEntities.Checked = old;
                            Notify.Error("Cannot change networked mode while placing entity is in progress.");
                        }
                    }
                    else if (item == spawnDynamicEntities)
                    {
                        EntitySpawner.SpawnDynamic = checked_;
                    }
                    else if (item == spawnEntityOnGround)
                    {
                        EntitySpawner.PlaceOnGround = checked_;
                        entityDistance.Enabled = !checked_;
                        alignEntityToSurface.Enabled = checked_;
                    }
                    else if (item == fixEntityPosition)
                    {
                        SetIsEntityFixed(checked_);
                    }
                };
            }

            #endregion

            keybindMenu.AddMenuItem(disableControllerKey);
            // Keybind options
            if (IsAllowed(Permission.MSDriftMode))
            {
                keybindMenu.AddMenuItem(kbDriftMode);
            }
            // always allowed keybind menu options
            keybindMenu.AddMenuItem(kbRecordKeys);
            if (!GetSettingsBool(Setting.vmenu_disable_radar_control))
            {
                keybindMenu.AddMenuItem(kbRadarKeys);
            }
            keybindMenu.AddMenuItem(kbPointKeysCheckbox);

            #region HUD menu

            var hudMenuBtn = new MenuItem("HUD Options", "Enable, disable, and customize some HUD elements.")
            {
                Label = "→→→"
            };

            MenuController.AddSubmenu(menu, hudMenu);
            MenuController.BindMenuItem(menu, hudMenu, hudMenuBtn);

            hudMenu.AddMenuItem(alignMenu);
            if (IsAllowed(Permission.MSTime))
            {
                hudMenu.AddMenuItem(drawTime);
            }
            if (IsAllowed(Permission.MSSpeed))
            {
                hudMenu.AddMenuItem(speed);
            }

            if (!GetSettingsBool(Setting.vmenu_disable_radar_control))
            {
                hudMenu.AddMenuItem(hideRadar);
            }
            if (IsAllowed(Permission.MSShowLocation))
            {
                hudMenu.AddMenuItem(showLocation);
            }
            hudMenu.AddMenuItem(hideHud);

            menu.AddMenuItem(hudMenuBtn);

            hudMenu.OnListIndexChange += (_menu, item, _oldIx, newIx, _itemIx) =>
            {
                if (item == speed)
                {
                    SpeedDisplay = (SpeedDisplayState)newIx;
                }
                else if (item == alignMenu)
                {
                    MenuController.MenuAlignment = newIx != 0 ? MenuController.MenuAlignmentOption.Right : MenuController.MenuAlignmentOption.Left;
                    MiscRightAlignMenu = newIx != 0;
                    UserDefaults.MiscRightAlignMenu = MiscRightAlignMenu;

                    if (MenuController.MenuAlignment != (newIx != 0 ? MenuController.MenuAlignmentOption.Right : MenuController.MenuAlignmentOption.Left))
                    {
                        Notify.Error(CommonErrors.RightAlignedNotSupported);
                        // (re)set the default to left just in case so they don't get this error again in the future.
                        MenuController.MenuAlignment = MenuController.MenuAlignmentOption.Left;
                        MiscRightAlignMenu = false;
                        UserDefaults.MiscRightAlignMenu = false;
                    }

                }
            };

            hudMenu.OnCheckboxChange += (_menu, item, _index, checked_) =>
            {
                if (item == hideHud)
                {
                    HideHud = checked_;
                    DisplayHud(!checked_);
                }
                else if (item == hideRadar)
                {
                    HideRadar = checked_;
                    if (!checked_)
                    {
                        DisplayRadar(true);
                    }
                }
                else if (item == showLocation)
                {
                    ShowLocation = checked_;
                }
                else if (item == drawTime)
                {
                    DrawTimeOnScreen = checked_;
                }
            };

            #endregion

            #region keybinds

            // Always allowed
            menu.AddMenuItem(keybindMenuBtn);
            keybindMenuBtn.Label = "→→→";

            #endregion

            // always allowed, it just won't do anything if the server owner disabled the feature, but players can still toggle it.
            menu.AddMenuItem(respawnDefaultCharacter);
            menu.AddMenuItem(disablePms);

            if (IsAllowed(Permission.MSConnectionMenu))
            {
                menu.AddMenuItem(connectionSubmenuBtn);
                connectionSubmenuBtn.Label = "→→→";
            }
            if (IsAllowed(Permission.MSJoinQuitNotifs))
            {
                menu.AddMenuItem(joinQuitNotifs);
            }
            if (IsAllowed(Permission.MSDeathNotifs))
            {
                menu.AddMenuItem(deathNotifs);
            }
            if (IsAllowed(Permission.MSNightVision))
            {
                menu.AddMenuItem(nightVision);
            }
            if (IsAllowed(Permission.MSThermalVision))
            {
                menu.AddMenuItem(thermalVision);
            }
            if (IsAllowed(Permission.MSLocationBlips))
            {
                menu.AddMenuItem(locationBlips);
                ToggleBlips(ShowLocationBlips);
            }
            if (IsAllowed(Permission.MSPlayerBlips))
            {
                menu.AddMenuItem(playerBlips);
            }
            if (IsAllowed(Permission.MSOverheadNames))
            {
                menu.AddMenuItem(playerNames);
            }
            if (IsAllowed(Permission.MSRestoreAppearance))
            {
                menu.AddMenuItem(restorePlayerAppearance);
            }
            if (IsAllowed(Permission.MSRestoreWeapons))
            {
                menu.AddMenuItem(restorePlayerWeapons);
            }

            if (IsAllowed(Permission.MSShowCoordinates))
            {
                menu.AddMenuItem(copyCoordinates);
            }

            // Always allowed
            if (IsAllowed(Permission.ResetIndex))
            {
                menu.AddMenuItem(ResetIndex);
            }
            if (MainMenu.EnableExperimentalFeatures)
            {
                menu.AddMenuItem(exportData);
            }
            menu.AddMenuItem(saveSettings);

            // Handle checkbox changes.
            menu.OnCheckboxChange += (sender, item, index, _checked) =>
            {
                if (item == disablePms)
                {
                    MiscDisablePrivateMessages = _checked;
                }
                else if (item == deathNotifs)
                {
                    DeathNotifications = _checked;
                }
                else if (item == joinQuitNotifs)
                {
                    JoinQuitNotifications = _checked;
                }
                else if (item == nightVision)
                {
                    SetNightvision(_checked);
                }
                else if (item == thermalVision)
                {
                    SetSeethrough(_checked);
                }
                else if (item == locationBlips)
                {
                    ToggleBlips(_checked);
                    ShowLocationBlips = _checked;
                }
                else if (item == playerBlips)
                {
                    ShowPlayerBlips = _checked;
                }
                else if (item == playerNames)
                {
                    MiscShowOverheadNames = _checked;
                }
                else if (item == respawnDefaultCharacter)
                {
                    MiscRespawnDefaultCharacter = _checked;
                }
                else if (item == restorePlayerAppearance)
                {
                    RestorePlayerAppearance = _checked;
                }
                else if (item == restorePlayerWeapons)
                {
                    RestorePlayerWeapons = _checked;
                }

            };

            // Handle button presses.
            menu.OnItemSelect += (sender, item, index) =>
            {
                if (item == copyCoordinates)
                {
                    var ped = Game.PlayerPed;
                    var pos = ped.Position;
                    var heading = ped.Heading;
                    CopyToClipboard($"X={pos.X}, Y={pos.Y}, Z={pos.Z}, H={heading}");
                    Notify.Info("Coordinates copied to the clipboard.");
                }
                // export data
                else if (item == exportData)
                {
                    MenuController.CloseAllMenus();
                    var vehicles = GetSavedVehicles();
                    var normalPeds = StorageManager.GetSavedPeds();
                    var mpPeds = StorageManager.GetSavedMpPeds();
                    var weaponLoadouts = WeaponLoadouts.GetSavedWeapons();
                    var data = JsonConvert.SerializeObject(new
                    {
                        saved_vehicles = vehicles,
                        normal_peds = normalPeds,
                        mp_characters = mpPeds,
                        weapon_loadouts = weaponLoadouts
                    });
                    SendNuiMessage(data);
                    SetNuiFocus(true, true);
                }
                // save settings
                else if (item == saveSettings)
                {
                    UserDefaults.SaveSettings();
                    saveSettings.Enabled = false;
                    async void TimeoutFunction()
                    {
                        await Delay(2000);
                        saveSettings.Enabled = true;
                    }
                    ;
                    TimeoutFunction();
                    data.Usersettings.SyncUpdatedUsersettings();
                }
            };
        }


        /// <summary>
        /// Create the menu if it doesn't exist, and then returns it.
        /// </summary>
        /// <returns>The Menu</returns>
        public Menu GetMenu()
        {
            if (menu == null)
            {
                CreateMenu();
            }
            return menu;
        }

        private readonly struct Blip
        {
            public readonly Vector3 Location;
            public readonly int Sprite;
            public readonly string Name;
            public readonly int Color;
            public readonly int blipID;

            public Blip(Vector3 Location, int Sprite, string Name, int Color, int blipID)
            {
                this.Location = Location;
                this.Sprite = Sprite;
                this.Name = Name;
                this.Color = Color;
                this.blipID = blipID;
            }
        }

        private readonly List<Blip> blips = new();

        /// <summary>
        /// Toggles blips on/off.
        /// </summary>
        /// <param name="enable"></param>
        private void ToggleBlips(bool enable)
        {
            if (enable)
            {
                try
                {
                    foreach (var bl in vMenuShared.ConfigManager.GetLocationBlipsData())
                    {
                        var blipID = AddBlipForCoord(bl.coordinates.X, bl.coordinates.Y, bl.coordinates.Z);
                        SetBlipSprite(blipID, bl.spriteID);
                        BeginTextCommandSetBlipName("STRING");
                        AddTextComponentSubstringPlayerName(bl.name);
                        EndTextCommandSetBlipName(blipID);
                        SetBlipColour(blipID, bl.color);
                        SetBlipAsShortRange(blipID, true);

                        var b = new Blip(bl.coordinates, bl.spriteID, bl.name, bl.color, blipID);
                        blips.Add(b);
                    }
                }
                catch (JsonReaderException ex)
                {
                    Debug.Write($"\n\n[vMenu] An error occurred while loading the locations.json file. Please contact the server owner to resolve this.\nWhen contacting the owner, provide the following error details:\n{ex.Message}.\n\n\n");
                }
            }
            else
            {
                if (blips.Count > 0)
                {
                    foreach (var blip in blips)
                    {
                        var id = blip.blipID;
                        if (DoesBlipExist(id))
                        {
                            RemoveBlip(ref id);
                        }
                    }
                }
                blips.Clear();
            }
        }

    }
}
