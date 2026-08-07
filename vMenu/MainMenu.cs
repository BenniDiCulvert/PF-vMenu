using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using CitizenFX.Core;

using MenuAPI;

using Newtonsoft.Json;

using vMenuClient.MenuAPIWrapper;
using vMenuClient.menus;

using static CitizenFX.Core.Native.API;
using static vMenuClient.CommonFunctions;
using static vMenuShared.ConfigManager;
using static vMenuShared.PermissionsManager;

namespace vMenuClient
{
    public class MainMenu : BaseScript
    {
        public const string RUNTIME_TXD = "vmenu_textures";

        #region Variables

        public static bool PermissionsSetupComplete => ArePermissionsSetup;
        public static bool ConfigOptionsSetupComplete = false;

        public static Dictionary<string, string> MenuText { get; private set; } = new();

        public static string NoClipKey { get; private set; } = "F2"; // F2 by default (ReplayStartStopRecordingSecondary)
        public static WMenu Menu { get; private set; }
        public static WMenu PlayerSubmenu { get; private set; }
        public static WMenu VehicleSubmenu { get; private set; }

        public static PlayerOptions PlayerOptionsMenu { get; private set; }
        public static OnlinePlayers OnlinePlayersMenu { get; private set; }
        public static BannedPlayers BannedPlayersMenu { get; private set; }
        public static SavedVehicles SavedVehiclesMenu { get; private set; }
        public static PersonalVehicle PersonalVehicleMenu { get; private set; }
        public static VehicleCustomization VehicleCustomizationMenu { get; private set; }
        public static VehicleOptions VehicleOptionsMenu { get; private set; }
        public static VehicleSpawner VehicleSpawnerMenu { get; private set; }
        public static PlayerAppearance PlayerAppearanceMenu { get; private set; }
        public static MpPedCustomization MpPedCustomizationMenu { get; private set; }
        public static PlayerTimeWeatherOptions PlayerTimeWeatherOptionsMenu { get; private set; }
        public static TeleportOptions TeleportOptionsMenu { get; private set; }
        public static TimeWeatherOptions TimeWeatherOptionsMenu { get; private set; }
        public static NPCDensityMenu DensityOptions { get; private set; }
        public static WeaponOptions WeaponOptionsMenu { get; private set; }
        public static WeaponLoadouts WeaponLoadoutsMenu { get; private set; }
        public static Recording RecordingMenu { get; private set; }
        public static EnhancedCamera EnhancedCameraMenu { get; private set; }
        public static PluginSettings PluginSettingsMenu { get; private set; }
        public static MiscSettings MiscSettingsMenu { get; private set; }
        public static menus.Usersettings UsersettingsMenu { get; private set; }
        public static About AboutMenu { get; private set; }
        public static bool NoClipEnabled { get { return NoClip.IsNoclipActive(); } set { NoClip.SetNoclipActive(value); } }
        public static IPlayerList PlayersList;

        public static WMenuItem PracticeTimerCheckbox { get; set; }

        public static bool DebugMode = GetResourceMetadata(GetCurrentResourceName(), "client_debug_mode", 0) == "true";
        public static bool EnableExperimentalFeatures = (GetResourceMetadata(GetCurrentResourceName(), "experimental_features_enabled", 0) ?? "0") == "1";
        private string vMenuKey;

        public static string Version { get { return GetResourceMetadata(GetCurrentResourceName(), "version", 0); } }

        public static bool DontOpenMenus { get { return MenuController.DontOpenAnyMenu; } set { MenuController.DontOpenAnyMenu = value; } }
        public static bool DisableControls { get { return MenuController.DisableMenuButtons; } set { MenuController.DisableMenuButtons = value; } }

        private static bool _vMenuEnabled = true;
        public static bool vMenuEnabled
        {
            get => _vMenuEnabled;
            private set
            {
                _vMenuEnabled = value;
                MenuController.EnableMenuToggleKeyOnController = value;
                DontOpenMenus = !value;
                if (!value)
                {
                    NoClipEnabled = false;
                    MenuController.CloseAllMenus();
                    _ = CancelUserInput();
                    PracticeTimerCheckbox.AsCheckboxItem().Checked = false;
                    TogglePracticeTimer(false);
                }
            }
        }

        public static async Task<bool> CheckVMenuEnabled()
        {
            if (vMenuEnabled)
                return true;

            await Delay(1000);
            return false;
        }

        private const int cleanupVersion = 3;
        private static readonly LanguageManager Lm = new LanguageManager();
        #endregion

        private void SetDefaultMenuAppearance(long runtimeTxdHandle)
        {
            const string texture_name = "menu_header_background";
            CreateRuntimeTextureFromImage(runtimeTxdHandle, texture_name, "menu-header-background.png");

            MenuController._texture_dict = RUNTIME_TXD;
            MenuController._header_texture = texture_name;

            var subtitleColor = GetSettingsString(Setting.vmenu_menu_subtitle_color);
            if (string.IsNullOrEmpty(subtitleColor))
            {
                subtitleColor = "HUD_COLOUR_FREEMODE";
            }
            MenuController._menu_subtitle_color = $"~{subtitleColor}~";
        }

        public static VehicleThumbnailDrawer VehicleThumbnailDrawer { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public MainMenu()
        {
            var menuTextJson = LoadResourceFile(GetCurrentResourceName(), "config/menu-text.json");
            MenuText = JsonConvert.DeserializeObject<Dictionary<string, string>>(menuTextJson);

            PlayersList = new NativePlayerList(Players);

            // Get the languages.
            LanguageManager.Languages = GetLanguages();

            var runtimeTxdHandle = CreateRuntimeTxd(RUNTIME_TXD);

            SetDefaultMenuAppearance(runtimeTxdHandle);
            if (GetSettingsBool(Setting.vmenu_show_vehicle_thumbnails))
            {
                var vehicleThumbnailTextureManager = new VehicleThumbnailTextureManager(runtimeTxdHandle, RUNTIME_TXD);
                VehicleThumbnailDrawer = new VehicleThumbnailDrawer(vehicleThumbnailTextureManager);
            }

            #region cleanup unused kvps
            var tmp_kvp_handle = StartFindKvp("");
            var cleanupVersionChecked = false;
            var tmp_kvp_names = new List<string>();
            var currentCleanupVersion = cleanupVersion;
            while (true)
            {
                var k = FindKvp(tmp_kvp_handle);
                if (string.IsNullOrEmpty(k))
                {
                    break;
                }
                if (k == "vmenu_cleanup_version")
                {
                    currentCleanupVersion = KeyValueStore.GetInt("vmenu_cleanup_version");
                    cleanupVersionChecked = currentCleanupVersion >= cleanupVersion;
                    break;
                }
                tmp_kvp_names.Add(k);
            }
            EndFindKvp(tmp_kvp_handle);

            if (!cleanupVersionChecked)
            {
                foreach (var kvp in tmp_kvp_names)
                {
                    if (currentCleanupVersion is 1 or 2)
                    {
                        if (!kvp.StartsWith("settings_") && !kvp.StartsWith("vmenu") && !kvp.StartsWith("veh_") && !kvp.StartsWith("ped_") && !kvp.StartsWith("mp_ped_"))
                        {
                            KeyValueStore.Remove(kvp);
                            Debug.WriteLine($"[vMenu] [cleanup id: 1] Removed unused (old) KVP: {kvp}.");
                        }
                    }
                    if (currentCleanupVersion == 2)
                    {
                        if (kvp.StartsWith("mp_char"))
                        {
                            KeyValueStore.Remove(kvp);
                            Debug.WriteLine($"[vMenu] [cleanup id: 2] Removed unused (old) KVP: {kvp}.");
                        }
                    }
                    if (currentCleanupVersion < 3)
                    {
                        UserDefaults.VehicleSpawnerSpawnUpgraded = false;
                    }
                }
                Debug.WriteLine("[vMenu] Cleanup of old unused KVP items completed.");
            }
            KeyValueStore.Set("vmenu_cleanup_version", cleanupVersion);
            #endregion

            #region keymapping stuff
            RegisterCommand($"{GetSettingsString(Setting.vmenu_individual_server_id)}vMenu:NoClip", new Action<dynamic, List<dynamic>, string>((dynamic source, List<dynamic> args, string rawCommand) =>
               {
                   if (!vMenuEnabled)
                       return;

                   if (IsAllowed(Permission.NoClip))
                   {
                       if (Game.PlayerPed.IsInVehicle())
                       {
                           var veh = GetVehicle();
                           if (veh != null && veh.Exists() && veh.Driver == Game.PlayerPed)
                           {
                               NoClipEnabled = !NoClipEnabled;
                           }
                           else
                           {
                               NoClipEnabled = false;
                               Notify.Error("This vehicle does not exist (somehow) or you need to be the driver of this vehicle to enable noclip!");
                           }
                       }
                       else
                       {
                           NoClipEnabled = !NoClipEnabled;
                       }
                   }
               }), false);

            Debug.WriteLine("[INFO] Registering vMenu toggle command.");
            bool openCloseLogged = false;
            RegisterCommand($"{GetSettingsString(Setting.vmenu_individual_server_id)}vMenu:toggle", new Action<dynamic, List<dynamic>, string>((dynamic source, List<dynamic> args, string rawCommand) =>
            {
                if (!vMenuEnabled || Menu == null)
                {
                    Debug.WriteLine($"[INFO] You cannot open vMenu right now; {(Menu == null ? "the menu does not exist" : "vMenu disabled")}.");
                    return;
                }

                if (!openCloseLogged)
                {
                    Debug.WriteLine($"[INFO] Opening/closing vMenu.");
                    openCloseLogged = true;
                }

                if (!MenuController.IsAnyMenuOpen())
                {
                    Menu.Menu.OpenMenu();
                }
                else
                {
                    MenuController.CloseAllMenus();
                }
            }), false);
            if (!(GetSettingsString(Setting.vmenu_menu_toggle_key) == null))
            {
                vMenuKey = GetSettingsString(Setting.vmenu_menu_toggle_key);
            }
            else
            {
                vMenuKey = "M";
            }

            Debug.WriteLine("[INFO] Registering vMenu toggle key mapping.");
            RegisterKeyMapping($"{GetSettingsString(Setting.vmenu_individual_server_id)}vMenu:toggle", "Menu Open/Close", "keyboard", vMenuKey);
            #endregion

            if (EnableExperimentalFeatures)
            {
                RegisterCommand("testped", new Action<dynamic, List<dynamic>, string>((dynamic source, List<dynamic> args, string rawCommand) =>
                {
                    var data = Game.PlayerPed.GetHeadBlendData();
                    Debug.WriteLine(JsonConvert.SerializeObject(data, Formatting.Indented));
                }), false);

                RegisterCommand("tattoo", new Action<dynamic, List<dynamic>, string>((dynamic source, List<dynamic> args, string rawCommand) =>
                {
                    if (args != null && args[0] != null && args[1] != null)
                    {
                        Debug.WriteLine(args[0].ToString() + " " + args[1].ToString());
                        TattooCollectionData d = Game.GetTattooCollectionData(int.Parse(args[0].ToString()), int.Parse(args[1].ToString()));
                        Debug.WriteLine("check");
                        Debug.Write(JsonConvert.SerializeObject(d, Formatting.Indented) + "\n");
                    }
                }), false);

                RegisterCommand("clearfocus", new Action<dynamic, List<dynamic>, string>((dynamic source, List<dynamic> args, string rawCommand) =>
                {
                    SetNuiFocus(false, false);
                }), false);
            }


            if (GetSettingsBool(Setting.vmenu_enable_dv_command))
            {
                RegisterCommand("dv", new Action<dynamic, List<dynamic>, string>((dynamic source, List<dynamic> args, string rawCommand) =>
                {
                    if (IsAllowed(Permission.VODelete) && (vMenuEnabled || IsAllowed(Permission.DVAll)))
                    {
                        var player = Game.PlayerPed.Handle;
                        if (DoesEntityExist(player) && !IsEntityDead(player))
                        {
                            var position = GetEntityCoords(player, true);
                            if (IsPedSittingInAnyVehicle(player))
                            {
                                var veh = GetVehicle();
                                if (GetPedInVehicleSeat(veh.Handle, -1) == player)
                                {
                                    DelVeh(veh, 5, veh.Handle);
                                }
                                else
                                {
                                    Notify.Error("You must be in the driver's seat to delete this vehicle!");
                                    if (vMenuShared.ConfigManager.GetSettingsBool(vMenuShared.ConfigManager.Setting.pfvmenu_moshnotify_setting))
                                    {
                                        //TriggerEvent("mosh_notify:notify", "ERROR", "<span class=\"text-white\">You must be in the driver's seat to delete this vehicle!</span>", "darkred", "error", 5000);
                                    }
                                }
                            }
                            else if (IsAllowed(Permission.DVAll))
                            {
                                var inFrontOfPlayer = GetOffsetFromEntityInWorldCoords(player, (float)0.0, (float)GetSettingsFloat(Setting.vmenu_dv_distance), (float)0.0);
                                var vehicle = GetVehInDirection(player, position, inFrontOfPlayer);
                                if (!(vehicle == 0))
                                {
                                    Vehicle veh = (Vehicle)Entity.FromHandle(vehicle);
                                    DelVeh(veh, GetSettingsInt(Setting.vmenu_dv_retries), vehicle);
                                }
                                else
                                {
                                    Notify.Error("No vehicle found. Maybe it's not close to you?");
                                    if (vMenuShared.ConfigManager.GetSettingsBool(vMenuShared.ConfigManager.Setting.pfvmenu_moshnotify_setting))
                                    {
                                        //TriggerEvent("mosh_notify:notify", "ERROR", "<span class=\"text-white\">No vehicle found. Maybe it's not close to you?</span>", "darkred", "error", 5000);
                                    }
                                }
                            }
                            else
                            {
                                Notify.Error("You do NOT have permission to use this command on other vehicles.");
                            }
                        }
                    }
                    else
                    {
                        Notify.Error("You do NOT have permission to use this command.");
                    }
                }), false);
                TriggerEvent("chat:addSuggestion", "/dv", "Deletes the vehicle you're sat in, or standing next to.");
            }

            RegisterCommand("dvall", new Action<dynamic, List<dynamic>, string>((dynamic source, List<dynamic> args, string rawCommand) =>
            {


                if (IsAllowed(Permission.DVAll))
                {
                    TriggerServerEvent("vMenu:DelAllVehServ");
                }
                else
                {
                    Notify.Error("You do NOT have permission to use this command.");
                    if (vMenuShared.ConfigManager.GetSettingsBool(vMenuShared.ConfigManager.Setting.pfvmenu_moshnotify_setting))
                    {
                        //TriggerEvent("mosh_notify:notify", "ERROR", "<span class=\"text-white\">You do NOT have permission to use this command.</span>", "darkred", "error", 5000);
                    }
                }
            }), false);


            TriggerEvent("chat:addSuggestion", "/dvall", "Deletes all vehicles");
            static async void DelVeh(Vehicle veh, int maxtimeout, int vehicle)
            {
                var timeout = 0;
                if (NetworkHasControlOfEntity(vehicle))
                {
                    veh.Delete();
                }
                if (DoesEntityExist(vehicle) && timeout < maxtimeout)
                {
                    while (DoesEntityExist(vehicle) && timeout < maxtimeout)
                    {
                        if (IsPedAPlayer(GetPedInVehicleSeat(vehicle, -1)))
                        {
                            Notify.Error("You can't delete this vehicle, someone else is driving it!");
                            if (vMenuShared.ConfigManager.GetSettingsBool(vMenuShared.ConfigManager.Setting.pfvmenu_moshnotify_setting))
                            {
                                //TriggerEvent("mosh_notify:notify", "ERROR", "<span class=\"text-white\">You can't delete this vehicle, someone else is driving it!</span>", "darkred", "error", 5000);
                            }
                            return;
                        }
                        NetworkRequestControlOfEntity(vehicle);
                        var retry = 0;
                        while (!(NetworkHasControlOfEntity(vehicle) || (retry > 10)))
                        {
                            retry++;
                            await Delay(10);
                            NetworkRequestControlOfEntity(vehicle);
                        }

                        var vehval = (Vehicle)Entity.FromHandle(vehicle);
                        vehval.Delete();
                        if (!DoesEntityExist(vehicle))
                        {
                            Notify.Success("The vehicle has been deleted!");
                            if (vMenuShared.ConfigManager.GetSettingsBool(vMenuShared.ConfigManager.Setting.pfvmenu_moshnotify_setting))
                            {
                                //TriggerEvent("mosh_notify:notify", "SUCCESS", "<span class=\"text-white\">The vehicle has been deleted!</span>", "success", "success", 5000);
                            }
                        }
                        timeout++;
                        await Delay(1000);
                        if (DoesEntityExist(vehicle) && timeout == maxtimeout - 1)
                        {
                            Notify.Error($"Failed to delete vehicle, after {maxtimeout} retries.");
                            if (vMenuShared.ConfigManager.GetSettingsBool(vMenuShared.ConfigManager.Setting.pfvmenu_moshnotify_setting))
                            {
                                //TriggerEvent("mosh_notify:notify", "ERROR", $"<span class=\"text-white\">Failed to delete vehicle, after {maxtimeout} retries.</span>", "darkred", "error", 5000);
                            }
                        }
                    }
                }
                else
                {
                    Notify.Success("The vehicle has been deleted!");
                    if (vMenuShared.ConfigManager.GetSettingsBool(vMenuShared.ConfigManager.Setting.pfvmenu_moshnotify_setting))
                    {
                        //TriggerEvent("mosh_notify:notify", "SUCCESS", "<span class=\"text-white\">The vehicle has been deleted!</span>", "success", "success", 5000);
                    }
                }
                return;
            }

            static int GetVehInDirection(int ped, Vector3 pos, Vector3 posinfront)
            {
                var ray = StartShapeTestCapsule(pos.X, pos.Y, pos.Z, posinfront.X, posinfront.Y, posinfront.Z, (float)5.0, (int)10, ped, (int)7);
                bool hit = false;
                Vector3 endCoords = Vector3.Zero;
                Vector3 surfaceNormal = Vector3.Zero;
                var vehicle = 0;
                GetShapeTestResult(ray, ref hit, ref endCoords, ref surfaceNormal, ref vehicle);
                if (IsEntityAVehicle(vehicle))
                {

                    return vehicle;
                }
                else
                {
                    return 0;
                }
            }

            RegisterCommand("vmenuclient", new Action<dynamic, List<dynamic>, string>((dynamic source, List<dynamic> args, string rawCommand) =>
            {
                if (args != null)
                {
                    if (args.Count > 0)
                    {
                        if (args[0].ToString().ToLower() == "debug")
                        {
                            DebugMode = !DebugMode;
                            Notify.Custom($"Debug mode is now set to: {DebugMode}.");
                            // Set discord rich precense once, allowing it to be overruled by other resources once those load.
                            if (DebugMode)
                            {
                                SetRichPresence($"Debugging vMenu {Version}!");
                            }
                            else
                            {
                                SetRichPresence($"Enjoying FiveM!");
                            }
                        }
                        else if (args[0].ToString().ToLower() == "gc")
                        {
                            GC.Collect();
                            Debug.Write("Cleared memory.\n");
                        }
                        else if (args[0].ToString().ToLower() == "dump")
                        {
                            Notify.Info("A full config dump will be made to the console. Check the log file. This can cause lag!");
                            Debug.WriteLine("\n\n\n########################### vMenu ###########################");
                            Debug.WriteLine($"Running vMenu Version: {Version}, Experimental features: {EnableExperimentalFeatures}, Debug mode: {DebugMode}.");
                            Debug.WriteLine("\nDumping a list of all KVPs:");
                            var handle = StartFindKvp("");
                            var names = new List<string>();
                            while (true)
                            {
                                var k = FindKvp(handle);
                                if (string.IsNullOrEmpty(k))
                                {
                                    break;
                                }
                                //if (!k.StartsWith("settings_") && !k.StartsWith("vmenu") && !k.StartsWith("veh_") && !k.StartsWith("ped_") && !k.StartsWith("mp_ped_"))
                                //{
                                //    KeyValueStore.Remove(k);
                                //}
                                names.Add(k);
                            }
                            EndFindKvp(handle);

                            var kvps = new Dictionary<string, dynamic>();
                            foreach (var kvp in names)
                            {
                                var type = 0; // 0 = string, 1 = float, 2 = int.
                                if (kvp.StartsWith("settings_"))
                                {
                                    if (kvp == "settings_clothingAnimationType") // int
                                    {
                                        type = 2;
                                    }
                                    else if (kvp == "settings_miscLastTimeCycleModifierIndex") // int
                                    {
                                        type = 2;
                                    }
                                    else if (kvp == "settings_miscLastTimeCycleModifierStrength") // int
                                    {
                                        type = 2;
                                    }
                                }
                                else if (kvp == "vmenu_cleanup_version") // int
                                {
                                    type = 2;
                                }
                                switch (type)
                                {
                                    case 0:
                                        var s = KeyValueStore.GetString(kvp);
                                        if (s.StartsWith("{") || s.StartsWith("["))
                                        {
                                            kvps.Add(kvp, JsonConvert.DeserializeObject(s));
                                        }
                                        else
                                        {
                                            kvps.Add(kvp, KeyValueStore.GetString(kvp));
                                        }
                                        break;
                                    case 1:
                                        kvps.Add(kvp, KeyValueStore.GetFloat(kvp));
                                        break;
                                    case 2:
                                        kvps.Add(kvp, KeyValueStore.GetInt(kvp));
                                        break;
                                }
                            }
                            Debug.WriteLine(@JsonConvert.SerializeObject(kvps, Formatting.None) + "\n");

                            Debug.WriteLine("\n\nDumping a list of allowed permissions:");
                            Debug.WriteLine(@JsonConvert.SerializeObject(Permissions, Formatting.None));

                            Debug.WriteLine("\n\nDumping vmenu server configuration settings:");
                            var settings = new Dictionary<string, string>();
                            foreach (var a in Enum.GetValues(typeof(Setting)))
                            {
                                settings.Add(a.ToString(), GetSettingsString((Setting)a));
                            }
                            Debug.WriteLine(@JsonConvert.SerializeObject(settings, Formatting.None));
                            Debug.WriteLine("\nEnd of vMenu dump!");
                            Debug.WriteLine("\n########################### vMenu ###########################");
                        }
                        else if (args[0].ToString().ToLower() == "dumplang")
                        {
                            if (IsAllowed(Permission.DumpLang))
                            {
                                TriggerEvent("vMenu:DumpLanguageTamplate:Client");
                            }
                            else
                            {
                                Notify.Error("This is only for admins!");
                            }
                        }
                    }
                    else
                    {
                        Notify.Custom($"vMenu is currently running version: {Version}.");
                    }
                }
            }), false);

            if (GetCurrentResourceName() != "vMenu")
            {
                MenuController.MainMenu = null;
                MenuController.DontOpenAnyMenu = true;
                MenuController.DisableMenuButtons = true;
                throw new Exception("\n[vMenu] INSTALLATION ERROR!\nThe name of the resource is not valid. Please change the folder name from '" + GetCurrentResourceName() + "' to 'vMenu' (case sensitive)!\n");
            }
            else
            {
                Tick += OnTick;
            }

            // Clear all previous pause menu info/brief messages on resource start.
            ClearBrief();

            // Request the permissions data from the server.
            TriggerServerEvent("vMenu:RequestPermissions");

            // Request server state from the server.
            TriggerServerEvent("vMenu:RequestServerState");
            MenuController.MenuToggleKey = (Control)(-1); // disables the menu toggle key

            Exports.Add("enable", new Action(() => vMenuEnabled = true));
            Exports.Add("disable", new Action(() => vMenuEnabled = false));

            Exports.Add("enable_time_weather_control", new Action(() =>
            {
                if (PlayerTimeWeatherOptionsMenu != null)
                    PlayerTimeWeatherOptionsMenu.Enabled = true;
                FunctionsController.IsTimeWeatherControlEnabled = true;
            }));
            Exports.Add("disable_time_weather_control", new Action(() =>
            {
                if (PlayerTimeWeatherOptionsMenu != null)
                    PlayerTimeWeatherOptionsMenu.Enabled = false;
                FunctionsController.IsTimeWeatherControlEnabled = false;
            }));

            Exports.Add("is_any_menu_open", new Func<bool>(MenuController.IsAnyMenuOpen));
            Exports.Add("is_freezing_ped", new Func<bool>(() =>
            {
                if (FunctionsController.IsMpCharEditorOpen())
                    return true;

                if (NoClipEnabled)
                    return true;

                if (PlayerOptionsMenu != null && PlayerOptionsMenu.PlayerFrozen)
                    return true;

                return false;
            }));
        }

        #region Infinity bits
        [EventHandler("vMenu:SetServerState")]
        public void SetServerState(IDictionary<string, object> data)
        {
            if (data.TryGetValue("IsInfinity", out var isInfinity))
            {
                if (isInfinity is bool isInfinityBool)
                {
                    if (isInfinityBool)
                    {
                        PlayersList = new InfinityPlayerList(Players);
                    }
                }
            }
        }

        [EventHandler("vMenu:ReceivePlayerList")]
        public void ReceivedPlayerList(IList<object> players)
        {
            PlayersList?.ReceivedPlayerList(players);
        }

        public static async Task<Vector3> RequestPlayerCoordinates(int serverId)
        {
            var coords = Vector3.Zero;
            var completed = false;

            // TODO: replace with client<->server RPC once implemented in CitizenFX!
            Action<Vector3> CallbackFunction = (data) =>
            {
                coords = data;
                completed = true;
            };

            TriggerServerEvent("vMenu:GetPlayerCoords", serverId, CallbackFactory.Create(CallbackFunction));

            while (!completed)
            {
                await Delay(0);
            }

            return coords;
        }
        #endregion

        #region Set Permissions function
        /// <summary>
        /// Set the permissions for this client.
        /// </summary>
        /// <param name="dict"></param>
        public static async void SetPermissions(string permissionsList)
        {
            vMenuShared.PermissionsManager.SetPermissions(permissionsList);

            while (!ConfigOptionsSetupComplete)
            {
                await Delay(100);
            }
            await PostPermissionsSetup();
        }
        #endregion

        /// <summary>
        /// This setups things as soon as the permissions are loaded.
        /// It triggers the menu creations, setting of initial flags like PVP, player stats,
        /// and triggers the creation of Tick functions from the FunctionsController class.
        /// </summary>
        public static bool IsPostPermissionsSetupComplete { get; private set; } = false;

        private static async Task PostPermissionsSetup()
        {
            if (IsPostPermissionsSetupComplete)
            {
                return;
            }

            switch (GetSettingsInt(Setting.vmenu_pvp_mode))
            {
                case 1:
                    NetworkSetFriendlyFireOption(true);
                    SetCanAttackFriendly(Game.PlayerPed.Handle, true, false);
                    break;
                case 2:
                    NetworkSetFriendlyFireOption(false);
                    SetCanAttackFriendly(Game.PlayerPed.Handle, false, false);
                    break;
                case 0:
                default:
                    break;
            }

            static bool canUseMenu()
            {
                if (GetSettingsBool(Setting.vmenu_menu_staff_only) == false)
                {
                    return true;
                }
                else if (IsAllowed(Permission.Staff))
                {
                    return true;
                }

                IsPostPermissionsSetupComplete = true;
                return false;
            }

            if (!canUseMenu())
            {
                MenuController.MainMenu = null;
                MenuController.DisableMenuButtons = true;
                MenuController.DontOpenAnyMenu = true;
                vMenuEnabled = false;
                return;
            }


            if (!(GetSettingsString(Setting.vmenu_noclip_toggle_key) == null))
            {
                NoClipKey = GetSettingsString(Setting.vmenu_noclip_toggle_key);
            }
            else
            {
                NoClipKey = "F2";
            }

            if (IsAllowed(Permission.NoClip))
            {
                RegisterKeyMapping($"{GetSettingsString(Setting.vmenu_individual_server_id)}vMenu:NoClip", "NoClip Toggle", "keyboard", NoClipKey);
            }
            // Create the main menu.
            Menu = new WMenu(MenuTitle, "Main Menu");

            PlayerSubmenu = new WMenu(MenuTitle, "My Character");
            VehicleSubmenu = new WMenu(MenuTitle, "Vehicles");

            // Add the main menu to the menu pool.
            MenuController.AddMenu(Menu.Menu);
            MenuController.MainMenu = Menu.Menu;

            MenuController.MainMenu.HeaderTexture = new KeyValuePair<string, string>(RUNTIME_TXD, "menu_header_background");

            // Waiting 2 seconds maybe avoids car names not being query-able using GetLabelText(), etc. right away
            await Delay(2000);

            // Create all (sub)menus.
            CreateSubmenus();

            // Grab the original language
            LanguageManager.UpdateOriginalLanguage();

            if (!GetSettingsBool(Setting.vmenu_disable_player_stats_setup))
            {
                // Manage Stamina
                if (PlayerOptionsMenu != null && PlayerOptionsMenu.PlayerStamina && IsAllowed(Permission.POUnlimitedStamina))
                {
                    StatSetInt((uint)GetHashKey("MP0_STAMINA"), 100, true);
                }
                else
                {
                    StatSetInt((uint)GetHashKey("MP0_STAMINA"), 0, true);
                }

                // Manage other stats, in order of appearance in the pause menu (stats) page.
                StatSetInt((uint)GetHashKey("MP0_SHOOTING_ABILITY"), 100, true);        // Shooting
                StatSetInt((uint)GetHashKey("MP0_STRENGTH"), 100, true);                // Strength
                StatSetInt((uint)GetHashKey("MP0_STEALTH_ABILITY"), 100, true);         // Stealth
                StatSetInt((uint)GetHashKey("MP0_FLYING_ABILITY"), 100, true);          // Flying
                StatSetInt((uint)GetHashKey("MP0_WHEELIE_ABILITY"), 100, true);         // Driving
                StatSetInt((uint)GetHashKey("MP0_LUNG_CAPACITY"), 100, true);           // Lung Capacity
                StatSetFloat((uint)GetHashKey("MP0_PLAYER_MENTAL_STATE"), 0f, true);    // Mental State
            }

            IsPostPermissionsSetupComplete = true;
            TriggerEvent("vMenu:SetupTickFunctions");
        }

        /// <summary>
        /// Main OnTick task runs every game tick and handles all the menu stuff.
        /// </summary>
        /// <returns></returns>
        private async Task OnTick()
        {
            if (!await CheckVMenuEnabled())
                return;

            // If the setup (permissions) is done, and it's not the first tick, then do this:
            if (ConfigOptionsSetupComplete)
            {
                #region Handle Opening/Closing of the menu.
                var tmpMenu = GetOpenMenu();
                if (MpPedCustomizationMenu != null)
                {
                    static bool IsOpen()
                    {
                        return
                            MpPedCustomizationMenu.appearanceMenu.Visible ||
                            MpPedCustomizationMenu.faceShapeMenu.Visible ||
                            MpPedCustomizationMenu.createCharacterMenu.Visible ||
                            MpPedCustomizationMenu.inheritanceMenu.Visible ||
                            MpPedCustomizationMenu.propsMenu.Visible ||
                            MpPedCustomizationMenu.clothesMenu.Visible ||
                            MpPedCustomizationMenu.tattoosMenu.Visible;
                    }

                    if (IsOpen())
                    {
                        if (tmpMenu == MpPedCustomizationMenu.createCharacterMenu)
                        {
                            MpPedCustomization.DisableBackButton = true;
                        }
                        else
                        {
                            MpPedCustomization.DisableBackButton = false;
                        }
                        MpPedCustomization.DontCloseMenus = true;
                    }
                    else
                    {
                        MpPedCustomization.DisableBackButton = false;
                        MpPedCustomization.DontCloseMenus = false;
                    }
                }

                if (Game.IsDisabledControlJustReleased(0, Control.PhoneCancel) && MpPedCustomization.DisableBackButton)
                {
                    await Delay(0);
                    Notify.Alert("You must save your ped first before exiting, or click the ~r~Exit Without Saving~s~ button.");
                }



                #endregion


            }
        }

        private static void CreateUsersettingsMenu()
        {
            var menuSpec = data.Usersettings.MenuSpec;

            if (menuSpec == null || menuSpec.deserializedSpecs.Count == 0)
            {
                return;
            }

            UsersettingsMenu = new Usersettings();
            var usersettingsMenu = UsersettingsMenu.GetMenu().Menu;

            if (usersettingsMenu.GetMenuItems().Count == 0)
            {
                return;
            }

            Menu.AddSubmenu(usersettingsMenu, menuSpec.menuDescription);
        }

        #region Create Submenus
        /// <summary>
        /// Creates all the submenus depending on the permissions of the user.
        /// </summary>
        private static void CreateSubmenus()
        {
            CreateUsersettingsMenu();

            #region Submenu Creation
            if (IsAllowed(Permission.VSMenu))
            {
                VehicleSpawnerMenu = new VehicleSpawner();
                VehicleSpawnerMenu.GetMenu();
            }
            if (IsAllowed(Permission.VOMenu))
            {
                VehicleOptionsMenu = new VehicleOptions();
                VehicleOptionsMenu.GetMenu();
            }
            if (IsAllowed(Permission.TPMenu))
            {
                TeleportOptionsMenu = new TeleportOptions();
                TeleportOptionsMenu.GetMenu();
            }
            #endregion

            #region Player Submenu
            var pedSect = new List<WMenuItem>();

            // Add the player appearance menu.
            if (IsAllowed(Permission.PAMenu))
            {
                MpPedCustomizationMenu = new MpPedCustomization();
                PlayerSubmenu.BindSubmenu(
                    MpPedCustomizationMenu.GetMenu(),
                    out WMenuItem mpPedCustomizationButton,
                    "Create, edit and load multiplayer peds.~n~~y~Ped models created in other submenus or resources cannot be saved here.~s~");


                PlayerAppearanceMenu = new PlayerAppearance();
                PlayerSubmenu.BindSubmenu(
                    PlayerAppearanceMenu.GetMenu(),
                    out WMenuItem genericPedCustomizationButton,
                    "Spawn, customize, and save and load generic ped models.");

                pedSect.AddRange([mpPedCustomizationButton, genericPedCustomizationButton]);
            }

            // Add the player options menu.
            if (IsAllowed(Permission.POMenu))
            {
                PlayerOptionsMenu = new PlayerOptions();
                PlayerSubmenu.BindSubmenu(
                    PlayerOptionsMenu.GetMenu(),
                    out WMenuItem playerOptionsButton,
                    "Change ped options.");

                pedSect.Add(playerOptionsButton);
            }


            var weaponsSect = new List<WMenuItem>();

            // Add the weapons menu.
            if (IsAllowed(Permission.WPMenu))
            {
                WeaponOptionsMenu = new WeaponOptions();
                PlayerSubmenu.BindSubmenu(
                    WeaponOptionsMenu.GetMenu(),
                    out WMenuItem weaponsMenu,
                    "Add, remove and modify weapons, and change ammo options.");

                weaponsSect.Add(weaponsMenu);
            }

            // Add Weapon Loadouts menu.
            if (IsAllowed(Permission.WLMenu))
            {
                WeaponLoadoutsMenu = new WeaponLoadouts();
                PlayerSubmenu.BindSubmenu(
                    WeaponLoadoutsMenu.GetMenu(),
                    out WMenuItem weaponLoadoutsMenu,
                    "Mange and load saved weapon loadouts.");

                weaponsSect.Add(weaponLoadoutsMenu);
            }


            PlayerSubmenu.AddSections([
                new Section("Ped", pedSect),
                new Section("Weapons", weaponsSect)
            ]);

            Menu.AddSubmenu(PlayerSubmenu, "Customize your character.");
            #endregion

            #region Vehicle Submenu

            {
                var spawnLast = new MenuItem("Spawn Last Vehicle", "Spawn your last spawned vehicle again.").ToWrapped();
                VehicleSubmenu.AddItem(spawnLast);

                spawnLast.Selected += async (_s, _args) => await SpawnLastVehicle();
            }

            // Add the vehicle spawner menu.
            if (IsAllowed(Permission.VSMenu))
            {
                VehicleSubmenu.AddSubmenu(
                    VehicleSpawnerMenu.GetMenu(),
                    "Search and spawn vehicles by name, or choose one from several categories.");
            }

            // Add the vehicle options Menu.
            if (IsAllowed(Permission.VOMenu))
            {
                VehicleSubmenu.AddSubmenu(
                    VehicleOptionsMenu.GetMenu(),
                    "Change options of your current vehicle.");
            }

            // Add the vehicle options Menu.
            if (IsAllowed(Permission.VOMenu))
            {
                VehicleCustomizationMenu = new VehicleCustomization();
                VehicleSubmenu.AddSubmenu(
                    VehicleCustomizationMenu.GetMenu(),
                    "Tune and style your vehicle.");
            }

            // Add Saved Vehicles menu.
            if (IsAllowed(Permission.SVMenu))
            {
                SavedVehiclesMenu = new SavedVehicles();
                VehicleSubmenu.BindSubmenu(
                    SavedVehiclesMenu.GetMenu(),
                    out WMenuItem button,
                    "Save and manage customized vehicles.");

                VehicleSubmenu.AddItem(button);
            }

            // Add the Personal Vehicle menu.
            if (IsAllowed(Permission.PVMenu))
            {
                PersonalVehicleMenu = new PersonalVehicle();
                VehicleSubmenu.AddSubmenu(
                    PersonalVehicleMenu.GetMenu(),
                    "Set and manage your personal vehicle.");
            }

            Menu.AddSubmenu(VehicleSubmenu, "Spawn, customize, and save vehicles.");
            #endregion

            // Add Teleport Menu.
            if (IsAllowed(Permission.TPMenu))
            {
                Menu.AddSubmenu(
                    TeleportOptionsMenu.GetMenu(),
                    "Teleport to your waypoint or various other locations, and save custom teleport locations.");
            }

            #region Practice Menu
            if (IsAllowed(Permission.VSMenu) && IsAllowed(Permission.TPMenu) && IsAllowed(Permission.TPTeleportToPrev))
            {
                var practiceMenu = new WMenu(MenuTitle, "Practice");
                var practiceQaVehicleMenu = new WMenu(MenuTitle, "Practice Vehicle");
                var practiceQaTeleportMenu = new WMenu(MenuTitle, "Practice Teleport");
                TeleportOptions.PrevTpState? practiceTpState = null;

                async Task PracticeRetry()
                {
                    if (practiceTpState == null)
                    {
                        Notify.Error("You do not have a practice location set. To do so, use the ~b~Set Practice Location~s~ button below.");
                        return;
                    }

                    await TeleportOptionsMenu.TeleportToPrevTpLocation(practiceTpState.Value);

                    Vehicle vehicle = Game.PlayerPed.CurrentVehicle;
                    if (LastVehicleModel is uint && (vehicle == null || LastVehicleModel != vehicle.Model.Hash))
                    {
                        await SpawnLastVehicle(spawnInside: true, replacePrevious: true);
                        vehicle = Game.PlayerPed.CurrentVehicle;
                    }

                    if (vehicle != null && vehicle.Driver == Game.PlayerPed)
                    {
                        vehicle?.Repair();

                        if (practiceTpState.Value.VehicleState is TeleportOptions.PrevTpVehicleState vehicleState)
                        {
                            TeleportOptionsMenu.ApplyPrevTpVehicleState(vehicleState);
                        }
                    }

                    SendNuiMessage(JsonConvert.SerializeObject(new
                    {
                        type = "practiceTimer:restart"
                    }));
                }

                void SetPracticeLocation()
                {
                    practiceTpState = TeleportOptionsMenu.CurrentTpLocationState;
                    Notify.Info("Practice location set.");
                }

                {
                    var resetButton = new MenuItem("Retry", "Retry from your practice location in a repaired version of your last spawned vehicle. ~g~You can create a key bind for this in the GTA settings.~s~").ToWrapped();
                    practiceMenu.AddItem(resetButton);

                    resetButton.Selected += async (_s, _args) =>
                    {
                        await PracticeRetry();
                    };

                    RegisterKeyMapping($"{GetSettingsString(Setting.vmenu_individual_server_id)}vMenu:practiceRetry", "Practice: Retry", "keyboard", "");
                    RegisterCommand($"{GetSettingsString(Setting.vmenu_individual_server_id)}vMenu:practiceRetry", new Action<dynamic, List<dynamic>, string>(async (dynamic source, List<dynamic> args, string rawCommand) =>
                    {
                        if (!vMenuEnabled)
                            return;

                        await PracticeRetry();
                    }), false);

                    RegisterKeyMapping($"{GetSettingsString(Setting.vmenu_individual_server_id)}vMenu:practiceLocationSet", "Practice: Set Practice Location", "keyboard", "");
                    RegisterCommand($"{GetSettingsString(Setting.vmenu_individual_server_id)}vMenu:practiceLocationSet", new Action<dynamic, List<dynamic>, string>((dynamic source, List<dynamic> args, string rawCommand) =>
                    {
                        if (!vMenuEnabled)
                            return;

                        SetPracticeLocation();
                    }), false);


                    var setPracticeLocationBtn = new MenuItem(
                        "Set Practice Location",
                        "Set the practice location to your current position. If you are in a vehicle, its momentum will also be saved and re-applied when you retry. ~g~You can create a key bind for this in the GTA settings.~s~").ToWrapped();
                    setPracticeLocationBtn.Selected += (_s, _args) => SetPracticeLocation();

                    practiceMenu.AddItem(setPracticeLocationBtn);


                    PracticeTimerCheckbox = new MenuCheckboxItem(
                        "Practice Timer",
                        "Enable or disable the practice timer. The timer will be restarted whenever you retry.",
                        false).ToWrapped();
                    PracticeTimerCheckbox.CheckboxChanged += (_s, args) =>
                    {
                        TogglePracticeTimer(args.Checked);
                    };

                    practiceMenu.AddItem(PracticeTimerCheckbox);


                    var spawnLastBtn = new MenuItem("Spawn Last Vehicle", "Spawn your last spawned vehicle again.").ToWrapped();
                    spawnLastBtn.Selected += async (_s, _args) => await SpawnLastVehicle();
                    practiceQaVehicleMenu.AddItem(spawnLastBtn);

                    practiceQaVehicleMenu.AddSubmenu(VehicleSpawnerMenu.AllVehiclesMenu);

                    var spawnRandomButton = new MenuItem("Spawn Random Sporty Vehicle", "Spawn a random, but sporty land-based vehicle.").ToWrapped();
                    spawnRandomButton.Selected += async (_s, _args) => await VehicleSpawnerMenu.SpawnRandomSportyVehicle();
                    practiceQaVehicleMenu.AddItem(spawnRandomButton);
                }

                if (IsAllowed(Permission.VOMenu) && IsAllowed(Permission.VORepair))
                {
                    var repairVehicleBtn = new MenuItem("Repair Vehicle", "Repair your current vehicle.").ToWrapped();
                    repairVehicleBtn.Selected += (_s, _args) =>
                    {
                        var veh = TryGetDriverVehicle("repair");
                        veh?.Repair();
                    };
                    practiceQaVehicleMenu.AddItem(repairVehicleBtn);
                }

                practiceMenu.AddSubmenu(practiceQaVehicleMenu, "Spawn a new practice vehicle, or repair your current one.");


                if (IsAllowed(Permission.TPTeleportToWp))
                {
                    var tpToWaypoint = new MenuItem(
                        "Teleport To Waypoint",
                        "Teleport to the waypoint on your map.").ToWrapped();
                    tpToWaypoint.Selected += async (_s, _args) => await TeleportOptionsMenu.TeleportToWaypoint();
                    practiceQaTeleportMenu.AddItem(tpToWaypoint);
                }

                if (IsAllowed(Permission.TPTeleportPersonalLocations))
                {
                    practiceQaTeleportMenu.AddSubmenu(TeleportOptionsMenu.PersonalTpLocationsMenu, "Teleport to your personal teleport locations.");
                }

                if (IsAllowed(Permission.TPTeleportLocations))
                {
                    practiceQaTeleportMenu.AddSubmenu(TeleportOptionsMenu.ServerTpLocationsMenu, "Teleport to pre-configured locations, added by the server owner.");
                }

                practiceMenu.AddSubmenu(practiceQaTeleportMenu, "Teleport to your waypoint, or pre-set locations useful for practicing. ~y~Do not forget to set the practice location after you teleported!~s~");


                Menu.AddSubmenu(practiceMenu, "Access to functions that are useful for practicing.");
            }
            #endregion

            if (IsAllowed(Permission.TWClientMenu))
            {
                PlayerTimeWeatherOptionsMenu = new PlayerTimeWeatherOptions();
                Menu.AddSubmenu(
                    PlayerTimeWeatherOptionsMenu.GetMenu(),
                    "Change the local time and weather.");
            }

            {
                RecordingMenu = new Recording();
                Menu.AddSubmenu(
                    RecordingMenu.GetMenu(),
                    "In-game screenshots and recording.");
            }

            // Add enhanced camera menu.
            if (IsAllowed(Permission.ECMenu))
            {
                EnhancedCameraMenu = new EnhancedCamera();
                Menu.AddSubmenu(
                    EnhancedCameraMenu.GetMenu(),
                    "Opens the enhanced camera menu.");
            }

            // Add misc settings menu.
            {
                MiscSettingsMenu = new MiscSettings();
                Menu.AddSubmenu(
                    MiscSettingsMenu.GetMenu(),
                    "Miscellaneous settings and options.");
            }

            // Add Help Menu.
            {
                var helpMenu = new WMenu(MenuTitle, "Help");

                var helpItems = MenuItemsFromJsonTuples("config/help.json");
                helpItems.ForEach(i => helpMenu.AddItem(i.ToWrapped()));

                Menu.AddSubmenu(helpMenu, "Helpful tips to get you familiar with the server.");
            }

            // Add About Menu.
            {
                AboutMenu = new About();
                Menu.AddSubmenu(
                    AboutMenu.GetMenu(),
                    "Information about this server.");
            }

            #region Admin Stuff
            WMenu adminMenu = new WMenu(MenuTitle, "Admin Menus");

            // Add the online players menu.
            if (IsAllowed(Permission.OPMenu))
            {
                OnlinePlayersMenu = new OnlinePlayers();
                Menu.BindSubmenu(
                    OnlinePlayersMenu.GetMenu(),
                    out WMenuItem button,
                    "View and manage all currently connected players.", true);

                button.Selected += async (_s, _args) =>
                {
                    PlayersList.RequestPlayerList();

                    await OnlinePlayersMenu.UpdatePlayerlist();
                    OnlinePlayersMenu.GetMenu().RefreshIndex();
                };

                adminMenu.AddItem(button);
            }

            if (IsAllowed(Permission.OPUnban) || IsAllowed(Permission.OPViewBannedPlayers))
            {
                BannedPlayersMenu = new BannedPlayers();
                Menu.BindSubmenu(
                    BannedPlayersMenu.GetMenu(),
                    out WMenuItem button,
                    "View and manage all banned players.", true);

                button.Selected += (_s, _args) =>
                {
                    TriggerServerEvent("vMenu:RequestBanList", Game.Player.Handle);
                    BannedPlayersMenu.GetMenu().RefreshIndex();
                };

                adminMenu.AddItem(button);
            }

            if (IsAllowed(Permission.NoClip))
            {
                var toggleNoclip = new MenuItem("Toggle NoClip", "Toggle NoClip.").ToWrapped();
                toggleNoclip.Selected += (_s, _args) =>
                {
                    NoClipEnabled = !NoClipEnabled;
                };

                adminMenu.AddItem(toggleNoclip);
            }

            if (IsAllowed(Permission.TWServerMenu) && GetSettingsBool(Setting.vmenu_enable_time_weather_sync))
            {
                TimeWeatherOptionsMenu = new TimeWeatherOptions();
                Menu.BindSubmenu(
                    TimeWeatherOptionsMenu.GetMenu(),
                    out WMenuItem button,
                    "Change the server time and weather.");

                adminMenu.AddItem(button);
            }

            if (IsAllowed(Permission.WRNPCOptions, true) && GetSettingsBool(Setting.vmenu_enable_npc_density))
            {
                DensityOptions = new NPCDensityMenu();
                Menu.BindSubmenu(
                    DensityOptions.GetMenu(),
                    out WMenuItem button,
                    "Change NPC density.");

                button.Selected += (_s, _args) =>
                {
                    PlayersList.RequestPlayerList();

                    DensityOptions.RefreshMenu();
                    DensityOptions.GetMenu().RefreshIndex();
                };

                adminMenu.AddItem(button);
            }

            // Add Plugin Settings Menu
            if (IsAllowed(Permission.PNMenu))
            {
                PluginSettingsMenu = new PluginSettings();
                Menu.BindSubmenu(
                    PluginSettingsMenu.GetMenu(),
                    out WMenuItem button,
                    "Plugins settings and status.");

                adminMenu.AddItem(button);
            }


            Menu.AddSubmenu(adminMenu);
            #endregion

            Menu.Closed += (_s, _args) =>
            {
                if (MiscSettingsMenu.ResetIndex.Checked)
                {
                    Menu.Menu.RefreshIndex();
                    MenuController.Menus.ForEach(delegate (Menu m)
                    {
                        m.RefreshIndex();
                    });
                }
            };

            // Refresh everything.
            MenuController.Menus.ForEach((m) => m.RefreshIndex());

            if (!GetSettingsBool(Setting.vmenu_use_permissions))
            {
                Notify.Alert("vMenu is set up to ignore permissions, default permissions will be used.");
            }

            if (MiscSettingsMenu != null)
            {
                MenuController.EnableMenuToggleKeyOnController = !MiscSettingsMenu.MiscDisableControllerSupport;
            }
        }
        #endregion
    }
}
