using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CitizenFX.Core;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using vMenuShared;

using static CitizenFX.Core.Native.API;
using static vMenuServer.DebugLog;
using static vMenuShared.ConfigManager;

namespace vMenuServer
{

    public static class DebugLog
    {
        public enum LogLevel
        {
            error = 1,
            success = 2,
            info = 4,
            warning = 3,
            none = 0
        }

        /// <summary>
        /// Global log data function, only logs when debugging is enabled.
        /// </summary>
        /// <param name="data"></param>
        public static void Log(dynamic data, LogLevel level = LogLevel.none)
        {
            if (MainServer.DebugMode || level == LogLevel.error || level == LogLevel.warning)
            {
                var prefix = "[vMenu] ";
                if (level == LogLevel.error)
                {
                    prefix = "^1[vMenu] [ERROR]^7 ";
                }
                else if (level == LogLevel.info)
                {
                    prefix = "^5[vMenu] [INFO]^7 ";
                }
                else if (level == LogLevel.success)
                {
                    prefix = "^2[vMenu] [SUCCESS]^7 ";
                }
                else if (level == LogLevel.warning)
                {
                    prefix = "^3[vMenu] [WARNING]^7 ";
                }
                Debug.WriteLine($"{prefix}[DEBUG LOG] {data.ToString()}");
            }
        }
    }

    public class RequestManager : BaseScript
    {
        private static RequestManager instance;

        private readonly Dictionary<ulong, string> responses = new Dictionary<ulong, string>();

        private ulong nextId = 0;

        [EventHandler("vMenu:RequestManager:Response")]
        private void HandleResponse(ulong id, string responseJson)
        {
            responses[id] = responseJson;
        }

        public RequestManager()
        {
            if (instance != null)
                throw new InvalidOperationException("RequestManager may only be instantiated once.");

            instance = this;
        }

        private async Task<string> SendImpl(string eventName, string argsJson)
        {
            var thisId = nextId++;
            TriggerEvent($"vMenu:{eventName}", thisId, argsJson);

            do
            {
                await Delay(1);
            } while (!responses.ContainsKey(thisId));

            var response = responses[thisId];
            responses.Remove(thisId);

            return response;
        }

        public static Task<string> Send(string eventName, string argsJson = null)
        {
            if (instance == null)
                throw new InvalidOperationException("RequestManager must be instantiated.");

            return instance.SendImpl(eventName, argsJson);
        }
    }

    public class MainServer : BaseScript
    {
        #region vars
        // Debug shows more information when doing certain things. Leave it off to improve performance!
        public static bool DebugMode = GetResourceMetadata(GetCurrentResourceName(), "server_debug_mode", 0) == "true";

        public static string Version { get { return GetResourceMetadata(GetCurrentResourceName(), "version", 0); } }

        private readonly List<string> CloudTypes = new()
        {
            "Cloudy 01",
            "RAIN",
            "horizonband1",
            "horizonband2",
            "Puffs",
            "Wispy",
            "Horizon",
            "Stormy 01",
            "Clear 01",
            "Snowy 01",
            "Contrails",
            "altostratus",
            "Nimbus",
            "Cirrus",
            "cirrocumulus",
            "stratoscumulus",
            "horizonband3",
            "Stripey",
            "horsey",
            "shower",
        };
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor.
        /// </summary>
        public MainServer()
        {
            DatabaseKeyValueStore.Connect();
            Task.Run(KeyValueStore.SyncWithDatabase).Wait();

            var gamebuild = 2372;
            var gamebuildcurr = GetConvarInt("sv_enforcegamebuild", 0);
            // build check
            if (gamebuildcurr < gamebuild)
            {
                var InvalidGameBuild = new Exception($"\r\n\r\n^1 Wrong game build! Your server's game build is v{gamebuildcurr}! You need atleast the v{gamebuild} or later game builds to use PF-vMenu. Tutorial on how to change this: https://forum.cfx.re/t/tutorial-forcing-gamebuilds-on-fivem/4784977\r\n\r\n\r\n^7");
                try
                {
                    throw InvalidGameBuild;
                }
                catch (Exception e)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        Debug.Write(e.Message);
                        System.Threading.Thread.Sleep(5000);

                    }
                    return;
                }
            }
            else
            {
                Debug.WriteLine($"Game build is: v{gamebuildcurr}");
                // id check
                if (GetSettingsString(Setting.vmenu_individual_server_id) == "" || GetSettingsString(Setting.vmenu_individual_server_id) == null || GetSettingsString(Setting.vmenu_individual_server_id) == "null")
                {
                    var InvalidServerId = new Exception("\r\n\r\n^1 Invalid Server ID or Server ID not found! Change or add 'setr vmenu_individual_server_id' to your server.cfg or permissions.cfg. \r\n\r\n\r\n^7");
                    try
                    {
                        throw InvalidServerId;
                    }
                    catch (Exception e)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            Debug.Write(e.Message);
                            System.Threading.Thread.Sleep(5000);

                        }
                        return;
                    }
                }
                else
                {
                    Debug.WriteLine($"Server ID: {GetSettingsString(Setting.vmenu_individual_server_id)}");
                    if (GetCurrentResourceName() != "vMenu")
                    {
                        var InvalidNameException = new Exception("\r\n\r\n^1[vMenu] INSTALLATION ERROR!\r\nThe name of the resource is not valid. " +
                            "Please change the folder name from '^3" + GetCurrentResourceName() + "^1' to '^2vMenu^1' (case sensitive) instead!\r\n\r\n\r\n^7");
                        try
                        {
                            throw InvalidNameException;
                        }
                        catch (Exception e)
                        {
                            Debug.Write(e.Message);
                        }
                    }
                    else
                    {
                        Hooks.VehicleInfo.FetchResult = RequestManager.Send(Hooks.VehicleInfo.FETCH_EVENT_NAME);
                        Hooks.Usersettings.GetInfoResult = RequestManager.Send(Hooks.Usersettings.GET_INFO_EVENT_NAME);

                        // Add event handlers.
                        EventHandlers.Add("vMenu:GetPlayerIdentifiers", new Action<Player, object>(([FromSource] Player player, object callback) =>
                        {
                            var data = new List<string>();
                            player.Identifiers.ToList().ForEach(e =>
                            {
                                if (!e.Contains("ip:"))
                                {
                                    data.Add(e);
                                }
                            });
                            callback.InvokeAsCallback(player, data);
                        }));
                        EventHandlers.Add("vMenu:RequestPermissions", new Action<Player>(PermissionsManager.SetPermissionsForPlayer));
                        EventHandlers.Add("vMenu:RequestServerState", new Action<Player>(RequestServerStateFromPlayer));

                        // check addons file for errors
                        var addons = LoadResourceFile(GetCurrentResourceName(), "config/addons.json") ?? "{}";
                        try
                        {
                            JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(addons);
                            // If the above crashes, then the json is invalid and it'll throw warnings in the console.
                        }
                        catch (JsonReaderException ex)
                        {
                            Debug.WriteLine($"\n\n^1[vMenu] [ERROR] ^7Your addons.json file contains a problem! Error details: {ex.Message}\n\n");
                        }

                        // check veh blips file for errors
                        string vehblips = LoadResourceFile(GetCurrentResourceName(), "config/vehblips.json") ?? "{}";
                        try
                        {
                            JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, int>>>(vehblips);
                            // If the above crashes, then the json is invalid and it'll throw warnings in the console.
                        }
                        catch (JsonReaderException ex)
                        {
                            Debug.WriteLine($"\n\n^1[vMenu] [ERROR] ^7Your vehblips.json file contains a problem! Error details: {ex.Message}\n\n");
                        }

                        // check extras file for errors
                        string extras = LoadResourceFile(GetCurrentResourceName(), "config/extras.json") ?? "{}";
                        try
                        {
                            JsonConvert.DeserializeObject<Dictionary<string, Dictionary<int, string>>>(extras);
                            // If the above crashes, then the json is invalid and it'll throw warnings in the console.
                        }
                        catch (JsonReaderException ex)
                        {
                            Debug.WriteLine($"\n\n^1[vMenu] [ERROR] ^7Your extras.json file contains a problem! Error details: {ex.Message}\n\n");
                        }

                        // check if permissions are setup (correctly)
                        if (!GetSettingsBool(Setting.vmenu_use_permissions))
                        {
                            Debug.WriteLine("^3[vMenu] [WARNING] vMenu is set up to ignore permissions!\nIf you did this on purpose then you can ignore this warning.\nIf you did not set this on purpose, then you must have made a mistake while setting up vMenu.\nPlease read the vMenu documentation (^5https://docs.vespura.com/vmenu^3).\nMost likely you are not executing the permissions.cfg (correctly).^7");
                        }

                        Tick += PlayersFirstTick;

                        if (GetSettingsBool(Setting.vmenu_enable_time_weather_sync))
                        {
                            PrepareTimeWeatherSync();

                            Tick += TimeWeatherLoop;
                        }
                    }
                }
            }
        }
        #endregion

        #region command handler
        [Command("vmenuserver", Restricted = true)]
        internal async void ServerCommandHandler(int source, List<object> args, string _)
        {
            if (args != null)
            {
                if (args.Count > 0)
                {
                    if (args[0].ToString().ToLower() == "debug")
                    {
                        DebugMode = !DebugMode;
                        if (source < 1)
                        {
                            Debug.WriteLine($"Debug mode is now set to: {DebugMode}.");
                        }
                        else
                        {
                            Players[source].TriggerEvent("chatMessage", $"vMenu Debug mode is now set to: {DebugMode}.");
                        }
                        return;
                    }
                    else if (args[0].ToString().ToLower() == "unban" && (source < 1))
                    {
                        if (args.Count() > 1 && !string.IsNullOrEmpty(args[1].ToString()))
                        {
                            var uuid = args[1].ToString().Trim();
                            var bans = BanManager.GetBanList();
                            var banRecord = bans.Find(b => { return b.uuid.ToString() == uuid; });
                            if (banRecord != null)
                            {
                                await BanManager.RemoveBan(banRecord);
                                Debug.WriteLine("Player has been successfully unbanned.");
                            }
                            else
                            {
                                Debug.WriteLine($"Could not find a banned player with the provided uuid '{uuid}'.");
                            }
                        }
                        else
                        {
                            Debug.WriteLine("You did not specify a player to unban, you must enter the FULL playername. Usage: vmenuserver unban \"playername\"");
                        }
                        return;
                    }
                    else if (args[0].ToString().ToLower() == "ban" && source < 1)  // only do this via server console (server id < 1)
                    {
                        if (args.Count > 3)
                        {
                            Player p = null;

                            var findByServerId = args[1].ToString().ToLower() == "id";
                            var identifier = args[2].ToString().ToLower();

                            if (findByServerId)
                            {
                                if (Players.Any(player => player.Handle == identifier))
                                {
                                    p = Players.Single(pl => pl.Handle == identifier);
                                }
                                else
                                {
                                    Debug.WriteLine("[vMenu] Could not find this player, make sure they are online.");
                                    return;
                                }
                            }
                            else
                            {
                                if (Players.Any(player => player.Name.ToLower() == identifier.ToLower()))
                                {
                                    p = Players.Single(pl => pl.Name.ToLower() == identifier.ToLower());
                                }
                                else
                                {
                                    Debug.WriteLine("[vMenu] Could not find this player, make sure they are online.");
                                    return;
                                }
                            }

                            var reason = "Banned by staff for:";
                            args.GetRange(3, args.Count - 3).ForEach(arg => reason += " " + arg);

                            if (p != null)
                            {
                                var ban = new BanManager.BanRecord(
                                    BanManager.GetSafePlayerName(p.Name),
                                    p.Identifiers.ToList(),
                                    new DateTime(3000, 1, 1),
                                    reason,
                                    "Server Console",
                                    new Guid()
                                );

                                await BanManager.AddBan(ban);
                                BanManager.BanLog($"[vMenu] Player {p.Name}^7 has been banned by Server Console for [{reason}].");
                                TriggerEvent("vMenu:BanSuccessful", JsonConvert.SerializeObject(ban).ToString());
                                var timeRemaining = BanManager.GetRemainingTimeMessage(ban.bannedUntil.Subtract(DateTime.Now));
                                p.Drop($"You are banned from this server. Ban time remaining: {timeRemaining}. Banned by: {ban.bannedBy}. Ban reason: {ban.banReason}. Additional information: {vMenuShared.ConfigManager.GetSettingsString(vMenuShared.ConfigManager.Setting.vmenu_default_ban_message_information)}.");
                            }
                            else
                            {
                                Debug.WriteLine("[vMenu] Player not found, could not ban player.");
                            }
                        }
                        else
                        {
                            Debug.WriteLine("[vMenu] Not enough arguments, syntax: ^5vmenuserver ban <id|name> <server id|username> <reason>^7.");
                        }
                    }
                    else if (args[0].ToString().ToLower() == "help")
                    {
                        Debug.WriteLine("Available commands:");
                        Debug.WriteLine("(server console only): vmenuserver ban <id|name> <server id|username> <reason> (player must be online!)");
                        Debug.WriteLine("(server console only): vmenuserver unban <uuid>");
                        Debug.WriteLine("vmenuserver weather <new weather type | dynamic <true | false>>");
                        Debug.WriteLine("vmenuserver time <freeze|<hour> <minute>>");
                        Debug.WriteLine("vmenuserver migrate (This copies all banned players in the bans.json file to the new ban system in vMenu v3.3.0, you only need to do this once)");
                    }
                    else if (args[0].ToString().ToLower() == "migrate" && source < 1)
                    {
                        var file = LoadResourceFile(GetCurrentResourceName(), "bans.json");
                        if (string.IsNullOrEmpty(file) || file == "[]")
                        {
                            Debug.WriteLine("&1[vMenu] [ERROR]^7 No bans.json file found or it's empty.");
                            return;
                        }
                        Debug.WriteLine("^5[vMenu] [INFO]^7 Importing all ban records from the bans.json file into the new storage system. ^3This may take some time...^7");
                        var bans = JsonConvert.DeserializeObject<List<BanManager.BanRecord>>(file);
                        foreach (var br in bans)
                        {
                            var record = new BanManager.BanRecord(br.playerName, br.identifiers, br.bannedUntil, br.banReason, br.bannedBy, Guid.NewGuid());
                            await BanManager.AddBan(record);
                        }
                        Debug.WriteLine("^2[vMenu] [SUCCESS]^7 All ban records have been imported. You now no longer need the bans.json file.");
                    }
                    else
                    {
                        Debug.WriteLine($"vMenu is currently running version: {Version}. Try ^5vmenuserver help^7 for info.");
                    }
                }
                else
                {
                    Debug.WriteLine($"vMenu is currently running version: {Version}. Try ^5vmenuserver help^7 for info.");
                }
            }
            else
            {
                Debug.WriteLine($"vMenu is currently running version: {Version}. Try ^5vmenuserver help^7 for info.");
            }
        }
        #endregion

        #region Hooks
        public static class Hooks
        {
            private const string HOOKS_PREFIX_ = "Hooks:";

            public static class VehicleInfo
            {
                private const string HOOKS_PREFIX = HOOKS_PREFIX_ + "VehicleInfo:";

                public const string FETCH_EVENT_NAME = HOOKS_PREFIX + "fetch";

                public static Task<string> FetchResult { get; set; }
            }

            public static class Usersettings
            {
                private const string HOOKS_PREFIX = HOOKS_PREFIX_ + "Usersettings:";

                public const string GET_INFO_EVENT_NAME = HOOKS_PREFIX + "getInfo";
                public const string FETCH_ALL_FOR_EVENT_NAME = HOOKS_PREFIX + "fetchAllFor";
                public const string STORE_FOR_EVENT_NAME = HOOKS_PREFIX + "storeFor";

                public static Task<string> GetInfoResult { get; set; }
            }

            public static class PlayerPermissions
            {
                private const string HOOKS_PREFIX = HOOKS_PREFIX_ + "PlayerPermissions:";

                public const string FETCH_FOR_EVENT_NAME = HOOKS_PREFIX + "fetchFor";
            }
        }
        #endregion

        [EventHandler("vMenu:ServerKeyValueStoreRequest")]
        public async void ReceiveRequest([FromSource] Player player, string json)
        {
            await ClientRemoteKeyValueStore.HandleRequest(player, json);
        }

        [EventHandler("vMenu:SyncUpdatedUsersettings")]
        public async void SyncUpdatedUsersettingsHandler([FromSource] Player player, string json)
        {
            await RequestManager.Send(
                Hooks.Usersettings.STORE_FOR_EVENT_NAME,
                JsonConvert.SerializeObject(new
                {
                    player = player.Handle,
                    settings = JsonConvert.DeserializeObject(json)
                }));
        }

        #region kick players from personal vehicle
        /// <summary>
        /// Makes the player leave the personal vehicle.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="vehicleNetId"></param>
        /// <param name="playerOwner"></param>
        private bool RaceScriptProtectsVehicle(int vehicle)
        {
            try
            {
                if (GetResourceState("racescript") != "started")
                {
                    return false;
                }

                var protectedVehicle = Exports["racescript"].isVehicleDeleteProtected(vehicle);
                return protectedVehicle is bool value && value;
            }
            catch
            {
                return false;
            }
        }

        private bool VehicleHasPlayerInAnySeat(int vehicle)
        {
            for (var seat = -1; seat <= 15; seat++)
            {
                var ped = GetPedInVehicleSeat(vehicle, seat);
                if (ped != 0 && IsPedAPlayer(ped))
                {
                    return true;
                }
            }

            foreach (var player in Players)
            {
                var ped = GetPlayerPed(player.Handle);
                if (ped != 0 && GetVehiclePedIsIn(ped, false) == vehicle)
                {
                    return true;
                }
            }

            return false;
        }

        [EventHandler("vMenu:DelAllVehServ")]
        public void DelAllVehServ([FromSource] Player source)
        {
            var vehdelnum = 0;
            foreach (int veh in GetAllVehicles())
            {
                if (!DoesEntityExist(veh))
                {
                    continue;
                }

                if (RaceScriptProtectsVehicle(veh))
                {
                    continue;
                }

                if (VehicleHasPlayerInAnySeat(veh))
                {
                    continue;
                }

                vehdelnum++;
                DeleteEntity(veh);
            }
            if (vMenuShared.ConfigManager.GetSettingsBool(vMenuShared.ConfigManager.Setting.pfvmenu_moshnotify_setting))
            {
                source.TriggerEvent("mosh_notify:notify", "SUCCESS", $"<span class=\"text-white\">{vehdelnum} Vehicles Have Been Deleted!</span>", "success", "success", 5000);
            }
            source.TriggerEvent("vMenu:Notify", $"{vehdelnum} Vehicles Have Been Deleted!.", "success");

        }
        [EventHandler("vMenu:GetOutOfCar")]
        internal void GetOutOfCar([FromSource] Player source, int vehicleNetId, int playerOwner)
        {
            if (source != null)
            {
                if (PermissionsManager.IsAllowed(PermissionsManager.Permission.PVKickPassengers, source))
                {
                    TriggerClientEvent("vMenu:GetOutOfCar", vehicleNetId, playerOwner);
                    source.TriggerEvent("vMenu:Notify", "All passengers will be kicked out as soon as the vehicle stops moving, or after 10 seconds if they refuse to stop the vehicle.", "info");
                }
            }
        }
        #endregion

        #region clear area near pos
        /// <summary>
        /// Clear the area near this point for all players.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        [EventHandler("vMenu:ClearArea")]
        internal void ClearAreaNearPos([FromSource] Player _, float x, float y, float z, float radius)
        {
            TriggerClientEvent("vMenu:ClearArea", x, y, z, radius);
        }
        #endregion

        #region Manage weather and time changes.
        private void FixInvalidTimeSelection(TimeWeatherCommon.TimeState timeState)
        {
            var rand = new Random();

            if (timeState.Hour < 0)
            {
                timeState.Hour = rand.Next(0, 24);
            }
            else
            {
                timeState.Hour %= 24;
            }

            if (timeState.Minute < 0)
            {
                timeState.Minute = rand.Next(0, 60);
            }
            else
            {
                timeState.Minute %= 60;
            }

            // Set time speed to the closest available speed
            timeState.Speed = TimeWeatherCommon.TimeSpeeds[TimeWeatherCommon.TimeSpeedIndex(timeState.Speed)];
        }

        private int GetRandomWeatherCycleDay(int day, TimeWeatherCommon.WeatherCycle weatherCycle)
        {
            int cycleDays = weatherCycle.Days;
            if (day < 0)
            {
                var rand = new Random();
                day = rand.Next(0, cycleDays);
            }
            else
            {
                day %= weatherCycle.Days;
            }
            return day;
        }

        private void FixInvalidWeatherSelection(TimeWeatherCommon.ServerWeatherSelection weatherSelection)
        {
            TimeWeatherCommon.WeatherCycle weatherCycle = null;
            if (!string.IsNullOrEmpty(weatherSelection.Dynamic.CycleName) &&
                TimeWeatherCommon.WeatherCyclesDict.TryGetValue(weatherSelection.Dynamic.CycleName, out weatherCycle))
            {
                weatherSelection.Dynamic.Day = GetRandomWeatherCycleDay(weatherSelection.Dynamic.Day, weatherCycle);
                return;
            }

            if (weatherCycle == null)
            {
                var firstCycle = TimeWeatherCommon.WeatherCycles.FirstOrDefault();
                if (firstCycle != null)
                {
                    weatherSelection.Dynamic.CycleName = firstCycle.Name;
                    weatherSelection.Dynamic.Day = weatherSelection.Dynamic.Day / firstCycle.Days;
                }
                else
                {
                    weatherSelection.IsDynamic = false;
                    weatherSelection.Dynamic.CycleName = "";
                    weatherSelection.Dynamic.Day = 0;
                }
            }
        }

        void PrepareTimeWeatherSync()
        {
            var time = TimeWeatherCommon.GetServerTime();
            FixInvalidTimeSelection(time);
            SetServerTime(time);

            var weatherSelection = TimeWeatherCommon.GetServerWeatherSelection();
            FixInvalidWeatherSelection(weatherSelection);
            SetServerWeatherSelection(weatherSelection);
        }

        public void SetServerTime(TimeWeatherCommon.TimeState serverTime)
        {
            var json = JsonConvert.SerializeObject(serverTime);
            SetConvarReplicated(Setting.vmenu_server_time.ToString(), json);
        }

        public void SetServerWeatherSelection(TimeWeatherCommon.ServerWeatherSelection serverWeatherSelection)
        {
            var json = JsonConvert.SerializeObject(serverWeatherSelection);
            SetConvarReplicated(Setting.vmenu_server_weather.ToString(), json);
        }

        public void SetServerWeather(TimeWeatherCommon.WeatherState serverWeather)
        {
            var json = JsonConvert.SerializeObject(serverWeather);
            SetConvarReplicated(Setting.vmenu_server_weather_state.ToString(), json);
        }


        private TimeWeatherCommon.ServerWeatherSelection GetDynamicWeatherSelection()
        {
            var weatherSelection = TimeWeatherCommon.GetServerWeatherSelection();
            return weatherSelection.IsDynamic ? weatherSelection : null;
        }

        private void DynamicWeatherSelectionIncrementDay()
        {
            var dynamicWeatherSelection = GetDynamicWeatherSelection();
            if (dynamicWeatherSelection == null)
                return;

            var dynamicWeatherCycle = TimeWeatherCommon.GetSelectedDynamicWeatherCycle();

            dynamicWeatherSelection.Dynamic.Day++;
            dynamicWeatherSelection.Dynamic.Day %= dynamicWeatherCycle.Days;
            SetServerWeatherSelection(dynamicWeatherSelection);
        }

        private void UpdateDynamicWeather()
        {
            var dynamicWeatherSelection = GetDynamicWeatherSelection();
            if (dynamicWeatherSelection == null)
                return;

            var cycle = TimeWeatherCommon.WeatherCyclesDict[dynamicWeatherSelection.Dynamic.CycleName];
            int day = dynamicWeatherSelection.Dynamic.Day;
            int hour = TimeWeatherCommon.GetServerTime().Hour;

            SetServerWeather(cycle.GetStateAt(day, hour));
        }


        private async Task TimeWeatherLoop()
        {
            bool retry = true;
            while (true)
            {
                try
                {
                    var time = TimeWeatherCommon.GetServerTime();
                    if (!time.Frozen)
                    {
                        time.Minute += 0.5f * time.Speed;
                        if (time.Minute >= 60)
                        {
                            time.Minute -= 60;
                            if (++time.Hour == 24)
                            {
                                time.Hour = 0;
                                DynamicWeatherSelectionIncrementDay();
                            }
                        }
                        SetServerTime(time);
                        UpdateDynamicWeather();
                    }
                }
                catch
                {
                    if (!retry)
                    {
                        Debug.WriteLine("^1[vMenu] [ERROR]^7 Something went wrong while trying to sync time and weather with clients. Stopping the time-weather loop.");
                        Tick -= TimeWeatherLoop;
                        break;
                    }

                    PrepareTimeWeatherSync();
                    retry = false;
                    continue;
                }

                await Delay(1000);
                break;
            }
        }
        #endregion

        #region Sync weather & time with clients
        [EventHandler("vMenu:UpdateOverrideClientTW")]
        internal void UpdateServerWeather(bool override_)
        {
            SetConvarReplicated(Setting.vmenu_override_client_time_weather.ToString(), override_.ToString().ToLower());
        }

        /// <summary>
        /// Set and sync the time to all clients.
        /// </summary>
        [EventHandler("vMenu:UpdateServerTime")]
        internal void UpdateServerTime(string json)
        {
            var value = JsonConvert.DeserializeObject<TimeWeatherCommon.TimeState>(json);

            FixInvalidTimeSelection(value);
            SetServerTime(value);
            UpdateDynamicWeather();
        }

        /// <summary>
        /// Update the weather for all clients.
        /// </summary>
        [EventHandler("vMenu:UpdateServerWeatherSelection")]
        internal void UpdateServerWeatherSelection(string json)
        {
            var value = JsonConvert.DeserializeObject<TimeWeatherCommon.ServerWeatherSelection>(json);

            FixInvalidWeatherSelection(value);
            SetServerWeatherSelection(value);

            if (value.IsDynamic)
            {
                UpdateDynamicWeather();
            }
            else
            {
                SetServerWeather(new TimeWeatherCommon.WeatherState
                {
                    WeatherType = value.Static.WeatherType,
                    Snow = value.Static.Snow,
                    Blackout = value.Static.Blackout,
                });
            }
        }
        #endregion

        #region Online Players Menu Actions
        /// <summary>
        /// Kick a specific player.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="kickReason"></param>
        [EventHandler("vMenu:KickPlayer")]
        internal async void KickPlayer([FromSource] Player source, int target, string kickReason = "You have been kicked from the server.")
        {
            if (IsPlayerAceAllowed(source.Handle, "vMenu.OnlinePlayers.Kick") || IsPlayerAceAllowed(source.Handle, "vMenu.Everything") ||
                IsPlayerAceAllowed(source.Handle, "vMenu.OnlinePlayers.All"))
            {
                // If the player is allowed to be kicked.
                var targetPlayer = Players[target];
                if (targetPlayer != null)
                {
                    if (!IsPlayerAceAllowed(targetPlayer.Handle, "vMenu.DontKickMe"))
                    {
                        TriggerEvent("vMenu:KickSuccessful", source.Name, kickReason, targetPlayer.Name);

                        KickLog($"Player: {source.Name} has kicked: {targetPlayer.Name} for: {kickReason}.");
                        TriggerClientEvent(source, "vMenu:Notify", $"The target player (<C>{targetPlayer.Name}</C>) has been kicked.", "info");

                        // Kick the player from the server using the specified reason.
                        DropPlayer(targetPlayer.Handle, kickReason);
                        return;
                    }
                    // Trigger the client event on the source player to let them know that kicking this player is not allowed.
                    TriggerClientEvent(source, "vMenu:Notify", "Sorry, this player can ~r~not ~w~be kicked.", "info");
                    return;
                }
                TriggerClientEvent(source, "vMenu:Notify", "An unknown error occurred. Report it here: vespura.com/vmenu", "info");
            }
            else
            {
                await BanManager.BanCheater(source);
            }
        }

        /// <summary>
        /// Kill a specific player.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        [EventHandler("vMenu:KillPlayer")]
        internal async void KillPlayer([FromSource] Player source, int target)
        {
            if (IsPlayerAceAllowed(source.Handle, "vMenu.OnlinePlayers.Kill") || IsPlayerAceAllowed(source.Handle, "vMenu.Everything") ||
                IsPlayerAceAllowed(source.Handle, "vMenu.OnlinePlayers.All"))
            {
                var targetPlayer = Players[target];
                if (targetPlayer != null)
                {
                    // Trigger the client event on the target player to make them kill themselves. R.I.P.
                    TriggerClientEvent(player: targetPlayer, eventName: "vMenu:KillMe", args: source.Name);
                    return;
                }
                TriggerClientEvent(source, "vMenu:Notify", "An unknown error occurred. Report it here: vespura.com/vmenu", "info");
            }
            else
            {
                await BanManager.BanCheater(source);
            }
        }

        /// <summary>
        /// Teleport a specific player to another player.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        [EventHandler("vMenu:SummonPlayer")]
        internal async void SummonPlayer([FromSource] Player source, int target)
        {
            if (IsPlayerAceAllowed(source.Handle, "vMenu.OnlinePlayers.Summon") || IsPlayerAceAllowed(source.Handle, "vMenu.Everything") ||
                IsPlayerAceAllowed(source.Handle, "vMenu.OnlinePlayers.All"))
            {
                // Trigger the client event on the target player to make them teleport to the source player.
                var targetPlayer = Players[target];
                if (targetPlayer != null)
                {
                    TriggerClientEvent(player: targetPlayer, eventName: "vMenu:GoToPlayer", args: source.Handle);
                    return;
                }
                TriggerClientEvent(source, "vMenu:Notify", "An unknown error occurred. Report it here: vespura.com/vmenu", "info");
            }
            else
            {
                await BanManager.BanCheater(source);
            }
        }

        [EventHandler("vMenu:SendMessageToPlayer")]
        internal void SendPrivateMessage([FromSource] Player source, int targetServerId, string message)
        {
            var targetPlayer = Players[targetServerId];
            if (targetPlayer != null)
            {
                targetPlayer.TriggerEvent("vMenu:PrivateMessage", source.Handle, message);

                foreach (var p in Players)
                {
                    if (p != source && p != targetPlayer)
                    {
                        if (vMenuShared.PermissionsManager.IsAllowed(vMenuShared.PermissionsManager.Permission.OPSeePrivateMessages, p))
                        {
                            p.TriggerEvent("vMenu:Notify", $"[vMenu Staff Log] <C>{source.Name}</C>~s~ sent a PM to <C>{targetPlayer.Name}</C>~s~: {message}", "");
                        }
                    }
                }
            }
        }

        [EventHandler("vMenu:PmsDisabled")]
        internal void NotifySenderThatDmsAreDisabled([FromSource] Player source, string senderServerId)
        {
            var p = Players[int.Parse(senderServerId)];
            p?.TriggerEvent("vMenu:Notify", $"Sorry, your private message to <C>{source.Name}</C>~s~ could not be delivered because they disabled private messages.", "info");
        }
        #endregion

        #region logging and update checks notifications
        /// <summary>
        /// If enabled using convars, will log all kick actions to the server console as well as an external file.
        /// </summary>
        /// <param name="kickLogMesage"></param>
        private static void KickLog(string kickLogMesage)
        {
            //if (GetConvar("vMenuLogKickActions", "true") == "true")
            if (GetSettingsBool(Setting.vmenu_log_kick_actions))
            {
                var file = LoadResourceFile(GetCurrentResourceName(), "vmenu.log") ?? "";
                var date = DateTime.Now;
                var formattedDate = (date.Day < 10 ? "0" : "") + date.Day + "-" +
                    (date.Month < 10 ? "0" : "") + date.Month + "-" +
                    (date.Year < 10 ? "0" : "") + date.Year + " " +
                    (date.Hour < 10 ? "0" : "") + date.Hour + ":" +
                    (date.Minute < 10 ? "0" : "") + date.Minute + ":" +
                    (date.Second < 10 ? "0" : "") + date.Second;
                var outputFile = file + $"[\t{formattedDate}\t] [KICK ACTION] {kickLogMesage}\n";
                SaveResourceFile(GetCurrentResourceName(), "vmenu.log", outputFile, -1);
                Debug.WriteLine("^3[vMenu] [KICK]^7 " + kickLogMesage + "\n");
            }
        }

        #endregion

        #region Add teleport location
        [EventHandler("vMenu:SaveTeleportLocation")]
        internal void AddTeleportLocation([FromSource] Player _, string locationJson, string jsonname)
        {
            var location = JsonConvert.DeserializeObject<TeleportLocation>(locationJson);
            var jsonFile = LoadResourceFile(GetCurrentResourceName(), "config/locations/" + jsonname);
            var locs = JsonConvert.DeserializeObject<TeleportLocationsJson>(jsonFile);
            if (locs.teleports.Any(loc => loc.name == location.name))
            {
                Log("A teleport location with this name already exists, location was not saved.", LogLevel.error);
                return;
            }

            //var locs = GetLocations();
            locs.teleports.Add(location);
            if (!SaveResourceFile(GetCurrentResourceName(), "config/locations/" + jsonname, JsonConvert.SerializeObject(locs, Formatting.Indented), -1))
            {
                Log($"Could not save {jsonname} file, reason unknown.", LogLevel.error);
            }
        }
        #endregion

        #region Infinity bits
        private void RequestServerStateFromPlayer([FromSource] Player player)
        {
            player.TriggerEvent("vMenu:SetServerState", new
            {
                IsInfinity = GetConvar("onesync_enableInfinity", "false") == "true"
            });
        }

        [EventHandler("vMenu:RequestPlayerList")]
        internal void RequestPlayerListFromPlayer([FromSource] Player player)
        {
            player.TriggerEvent("vMenu:ReceivePlayerList", Players.Select(p => new
            {
                n = p.Name,
                s = int.Parse(p.Handle),
            }));
        }

        [EventHandler("vMenu:GetPlayerCoords")]
        internal void GetPlayerCoords([FromSource] Player source, int playerId, object callback)
        {
            if (IsPlayerAceAllowed(source.Handle, "vMenu.OnlinePlayers.Teleport") || IsPlayerAceAllowed(source.Handle, "vMenu.Everything") ||
                IsPlayerAceAllowed(source.Handle, "vMenu.OnlinePlayers.All"))
            {
                var coords = Players[playerId]?.Character?.Position ?? Vector3.Zero;

                callback.InvokeAsCallback(source, coords);

                return;
            }

            callback.InvokeAsCallback(source, Vector3.Zero);
        }
        #endregion

        #region Player join/quit
        private readonly HashSet<string> joinedPlayers = new();

        private Task PlayersFirstTick()
        {
            Tick -= PlayersFirstTick;

            foreach (var player in Players)
            {
                joinedPlayers.Add(player.Handle);
            }

            return Task.FromResult(0);
        }

        [EventHandler("playerJoining")]
        internal void OnPlayerJoining([FromSource] Player sourcePlayer)
        {
            joinedPlayers.Add(sourcePlayer.Handle);

            foreach (var player in Players)
            {
                if (IsPlayerAceAllowed(player.Handle, "vMenu.MiscSettings.JoinQuitNotifs") ||
                    IsPlayerAceAllowed(player.Handle, "vMenu.MiscSettings.All"))
                {
                    player.TriggerEvent("vMenu:PlayerJoinQuit", sourcePlayer.Name, null);
                }
            }
        }

        [EventHandler("playerDropped")]
        internal void OnPlayerDropped([FromSource] Player sourcePlayer, string reason)
        {
            if (!joinedPlayers.Contains(sourcePlayer.Handle))
            {
                return;
            }

            joinedPlayers.Remove(sourcePlayer.Handle);

            foreach (var player in Players)
            {
                if (IsPlayerAceAllowed(player.Handle, "vMenu.MiscSettings.JoinQuitNotifs") ||
                    IsPlayerAceAllowed(player.Handle, "vMenu.MiscSettings.All"))
                {
                    player.TriggerEvent("vMenu:PlayerJoinQuit", sourcePlayer.Name, reason);
                }
            }
        }
        #endregion

        #region Language template dumper

        [EventHandler("vMenu:DumpLanguageTemplate:Server")]
        private void DumpLangaugeTemplate(string data)
        {
            try
            {
                bool successful = SaveResourceFile(GetCurrentResourceName(), "config/languages/TEMPLATE.json", data, -1);
                if (successful)
                {
                    Debug.WriteLine($"\n\n^2[vMenu] [SUCCESS] ^7Template created successfully!\n\n");
                }
                else
                {
                    Debug.WriteLine($"\n\n^1[vMenu] [ERROR] ^7Could not save the language template!\n\n");
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"\n\n^1[vMenu] [ERROR] ^7Your TEMPLATE.json file could not be created or accessed! Error details: {e.Message}\n\n");
            }
        }

        #endregion

        #region Set drift suspension

        [EventHandler("vMenu:SetDriftSuspension")]
        private void SetDriftSuspension(int vehNetId)
        {
            Entity vehEntity = Entity.FromNetworkId(vehNetId);
            if (vehEntity == null) return;

            StateBag vehState = vehEntity.State;
            bool? reduceDriftSuspension = vehState["Set:ReduceDriftSuspension"] ?? false;

            vehEntity.State["Set:ReduceDriftSuspension"] = reduceDriftSuspension.Value ? false : true;
            TriggerClientEvent("vMenu:SetDriftSuspension", vehNetId, vehEntity.State["Set:ReduceDriftSuspension"]);
        }

        #endregion

        #region Entity Spawner

        private Dictionary<string, Stack<Entity>> playerEntities = new Dictionary<string, Stack<Entity>>();

        [EventHandler("vMenu:EntitySpawnerAdd")]
        public void EntitySpawnerAdd([FromSource] Player player, int entityId)
        {
            var license = player.Identifiers["license"];

            playerEntities.TryGetValue(license, out var entities);
            if (entities == null)
            {
                entities = playerEntities[license] = new Stack<Entity>();
            }
            var entity = Entity.FromNetworkId(entityId);
            if (entity != null && DoesEntityExist(entity.Handle))
            {
                entities.Push(entity);
            }
        }

        [EventHandler("vMenu:EntitySpawnerRemoveMostRecent")]
        public void EntitySpawnerRemoveMostRecent([FromSource] Player player)
        {
            var license = player.Identifiers["license"];
            playerEntities.TryGetValue(license, out var entities);

            if (entities == null)
                return;

            while (entities.Count > 0)
            {
                var entity = entities.Pop();
                if (entity != null && DoesEntityExist(entity.Handle))
                {
                    DeleteEntity(entity.Handle);
                    break;
                }
            }
        }

        [EventHandler("vMenu:EntitySpawnerRemoveAll")]
        public void EntitySpawnerRemoveAll()
        {
            foreach (var entities in playerEntities.Values)
            {
                if (entities == null)
                    continue;

                foreach (var entity in entities)
                {
                    if (DoesEntityExist(entity.Handle))
                    {
                        DeleteEntity(entity.Handle);
                    }
                }
            }
            playerEntities.Clear();
        }

        private static string PrintEntityInfo(StringBuilder sb, Entity e)
        {
            int hash = e.Model;
            var coords = e.Position;
            var rot = e.Rotation;

            sb.AppendLine($"Hash: {hash} | {(uint)hash} | 0x{hash:X}");
            sb.AppendLine($"Coords: X={coords.X}, Y={coords.Y}, Z={coords.Z}");
            sb.AppendLine($"Rot: X={rot.X}, Y={rot.Y}, Z={rot.Z}");
            sb.AppendLine();

            return sb.ToString();
        }

        [EventHandler("vMenu:EntitySpawnerCopyToClipboard")]
        public void EntitySpawnerCopyToClipboard([FromSource] Player player, object callback)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var entities in playerEntities.Values)
            {
                if (entities == null)
                    continue;

                foreach (var entity in entities)
                {
                    if (entity != null && DoesEntityExist(entity.Handle))
                    {
                        PrintEntityInfo(sb, entity);
                    }
                }
            }

            callback.InvokeAsCallback(player, sb.ToString());
        }

        [EventHandler("onResourceStop")]
        public void OnResourceStop(string resource)
        {
            if (resource == GetCurrentResourceName())
            {
                EntitySpawnerRemoveAll();
            }
        }

        #endregion
    }
}
