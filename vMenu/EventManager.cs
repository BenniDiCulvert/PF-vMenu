using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CitizenFX.Core;

using Newtonsoft.Json;

using vMenuClient.data;
using vMenuClient.menus;

using static CitizenFX.Core.Native.API;
using static vMenuClient.CommonFunctions;
using static vMenuShared.ConfigManager;
using static vMenuShared.PermissionsManager;

namespace vMenuClient
{
    public class EventManager : BaseScript
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public EventManager()
        {
            // Add event handlers.
            EventHandlers.Add("vMenu:SetAddons", new Action(SetAddons));
            EventHandlers.Add("vMenu:SetExtras", new Action<string>(SetExtras));
            EventHandlers.Add("vMenu:SetPermissions", new Action<string>(MainMenu.SetPermissions));
            EventHandlers.Add("vMenu:GoToPlayer", new Action<string>(SummonPlayer));
            EventHandlers.Add("vMenu:KillMe", new Action<string>(KillMe));
            EventHandlers.Add("vMenu:Notify", new Action<string, string>(NotifyPlayer));
            EventHandlers.Add("vMenu:SetClouds", new Action<float, string>(SetClouds));
            EventHandlers.Add("vMenu:GoodBye", new Action(GoodBye));
            EventHandlers.Add("vMenu:SetBanList", new Action<string>(UpdateBanList));
            EventHandlers.Add("vMenu:ClearArea", new Action<float, float, float, float>(ClearAreaNearPos));
            EventHandlers.Add("vMenu:updatePedDecors", new Action(UpdatePedDecors));
            EventHandlers.Add("playerSpawned", new Action(SetAppearanceOnFirstSpawn));
            EventHandlers.Add("vMenu:GetOutOfCar", new Action<int, int>(GetOutOfCar));
            EventHandlers.Add("vMenu:SetDriftSuspension", new Action<int, bool>(SetDriftSuspension));
            EventHandlers.Add("vMenu:PrivateMessage", new Action<string, string>(PrivateMessage));
            EventHandlers.Add("vMenu:SetDefaultPlate", new Action<string>(plate => SetDefaultPlate(JsonConvert.DeserializeObject<SavedPlate>(plate))));
            EventHandlers.Add("onClientResourceStart", async (string resourceName) =>
            {
                if (resourceName == GetCurrentResourceName())
                {
                    // wait a bit before syncing with the server
                    await Delay(2000);

                    var localChanges = await KeyValueStore.SyncWithServer();
                    if (localChanges)
                    {
                        Notify.Info("vMenu save data loaded from server. You may need to restart your game to use it.");
                    }
                }
            });
            EventHandlers.Add("vMenu:ServerKeyValueStoreResponse", RemoteKeyValueStore.ReceiveResponse);

#if CARBON_MILE
            void CarbonMileRequestSponsorPlate() => TriggerServerEvent("vMenu:RequestSponsorPlate");

            EventHandlers.Add("onClientResourceStart", async (string resourceName) =>
            {
                // Reset this first before we load it from the server
                SetSponsorPlate(null, -1);

                if (resourceName == GetCurrentResourceName())
                {
                    // wait a bit before syncing with the server
                    await Delay(2000);
                    CarbonMileRequestSponsorPlate();
                }
            });

            void SetSponsorPlate(string text, int style)
            {
                SetDefaultPlate(new SavedPlate
                {
                    Text = text,
                    Style = style >= 0 ? (LicensePlateStyle)style : null
                });
            }

            ulong nextSponsorPlateRequestId = 0;

            async void RequestSponsorPlateDelayed(object _)
            {
                var requestId = nextSponsorPlateRequestId++;

                await Delay(SERVER_SYNC_INTERVAL + 1500);

                if (requestId != nextSponsorPlateRequestId - 1)
                {
                    return;
                }

                CarbonMileRequestSponsorPlate();
            }

            EventHandlers.Add("vMenu:SetSponsorPlate", new Action<string, int>(SetSponsorPlate));
            EventHandlers.Add("vMenu:UsersettingUpdated:racePlateText", new Action<object>(RequestSponsorPlateDelayed));
            EventHandlers.Add("vMenu:UsersettingUpdated:racePlateStyle", new Action<object>(RequestSponsorPlateDelayed));
#endif

            RegisterNuiCallbackType("disableImportExportNUI");
            RegisterNuiCallbackType("importData");
            RegisterNuiCallbackType("userInputReceiveResult");
            RegisterNuiCallbackType("userInputCancel");
        }

        [EventHandler("vMenu:UpdateUsersetting")]
        public void UpdateUsersettingHandler(string key, object value, bool sync = false)
        {
            var ok = data.Usersettings.TryUpdateUsersetting(key, value, false, sync);
            if (!ok || MainMenu.MiscSettingsMenu == null)
            {
                return;
            }

            var usersettingsMenu = MainMenu.UsersettingsMenu;
            if (usersettingsMenu == null)
            {
                return;
            }

            usersettingsMenu.UpdateUsersettingItemState(key, value);
        }

        [EventHandler("vMenu:GetUsersetting")]
        public void GetUsersettingHandler(string key, object requestId)
        {
            object response;

            var ok = data.Usersettings.UsersettingsDict.TryGetValue(key, out var value);
            if (ok)
            {
                response = new { key, value, requestId };
            }
            else
            {
                response = new
                {
                    key,
                    value = (object)null,
                    requestId,
                    error = $"Usersetting with key \"{key}\" does not exist"
                };
            }

            TriggerEvent("vMenu:GetUsersettingResponse", response);
        }

        [EventHandler("vMenu:GetSavedVehicleMods")]
        public void GetSavedVehicleMods(string shortname)
        {
            var vehicleInfo = StorageManager.TryGetSavedVehicleMods(shortname);
            TriggerEvent("vMenu:GetSavedVehicleModsResponse", vehicleInfo);
        }

        public const int SERVER_SYNC_INTERVAL = 5000;

        [Tick]
        public async Task SyncServerStore()
        {
            // Auto-sync KVS and usersettings regularly
            await Delay(SERVER_SYNC_INTERVAL);

            _ = RemoteKeyValueStore.SyncUnsynced();

            if (data.Usersettings.UsersettingsDict.Count != 0)
            {
                data.Usersettings.SyncUpdatedUsersettings();
            }
        }

        [EventHandler("__cfx_nui:importData")]
        internal void ImportData(IDictionary<string, object> data, CallbackDelegate cb)
        {
            SetNuiFocus(false, false);
            Notify.Info("Debug info: This feature is not yet available, check back later.");
            cb(JsonConvert.SerializeObject(new { ok = true }));
        }

        [EventHandler("__cfx_nui:disableImportExportNUI")]
        internal void DisableImportExportNUI(IDictionary<string, object> data, CallbackDelegate cb)
        {
            SetNuiFocus(false, false);
            Notify.Info("Debug info: Closing import/export NUI window.");
            cb(JsonConvert.SerializeObject(new { ok = true }));
        }

        [EventHandler("__cfx_nui:userInputReceiveResult")]
        internal static void ReceiveUserInputResult(IDictionary<string, object> data, CallbackDelegate cb)
        {
            SetNuiFocus(false, false);
            UserInputActive = false;
            UserInputResult = data["userInput"] as string;
            cb(new { });
        }

        [EventHandler("__cfx_nui:userInputCancel")]
        internal static void CancelUserInput(IDictionary<string, object> _, CallbackDelegate cb)
        {
            SetNuiFocus(false, false);
            UserInputActive = false;
            UserInputResult = null;
            cb(new { });
        }

        private bool firstSpawn = true;
        /// <summary>
        /// Sets the saved character whenever the player first spawns.
        /// </summary>
        private async void SetAppearanceOnFirstSpawn()
        {
            if (firstSpawn)
            {
                firstSpawn = false;

                const int MAX_SPAWN_DEFAULT_DELAY = 10000;

                var start = (uint)GetGameTimer();
                while (!MainMenu.IsPostPermissionsSetupComplete && (uint)GetGameTimer() - start <= MAX_SPAWN_DEFAULT_DELAY)
                {
                    await Delay(0);
                }

                if (MainMenu.MiscSettingsMenu != null && MainMenu.MpPedCustomizationMenu != null && MainMenu.MiscSettingsMenu.MiscRespawnDefaultCharacter && !string.IsNullOrEmpty(KeyValueStore.GetString("vmenu_default_character")) && IsAllowed(Permission.PASpawnAsDefault))
                {
                    await MainMenu.MpPedCustomizationMenu.SpawnThisCharacter(KeyValueStore.GetString("vmenu_default_character"), false);
                }
                else if (MainMenu.PlayerAppearanceMenu != null && !GetSettingsBool(Setting.vmenu_disable_spawn_as_allowed_ped))
                {
                    if (GetSettingsBool(Setting.vmenu_spawn_as_deterministic_allowed_ped))
                    {
                        await MainMenu.PlayerAppearanceMenu.SpawnDefaultAllowedPed();
                    }
                    else
                    {
                        await MainMenu.PlayerAppearanceMenu.SpawnRandomAllowedPed();
                    }
                }
                while (!IsScreenFadedIn() || IsPlayerSwitchInProgress() || IsPauseMenuActive() || GetIsLoadingScreenActive())
                {
                    await Delay(0);
                }
                if (MainMenu.WeaponLoadoutsMenu != null && MainMenu.WeaponLoadoutsMenu.WeaponLoadoutsSetLoadoutOnRespawn && IsAllowed(Permission.WLEquipOnRespawn))
                {
                    var saveName = KeyValueStore.GetString("vmenu_string_default_loadout");
                    if (!string.IsNullOrEmpty(saveName))
                    {
                        await SpawnWeaponLoadoutAsync(saveName, true, false, true);
                    }

                }
            }
        }

        /// <summary>
        /// Sets the addon models from the addons.json file.
        /// </summary>
        private void SetAddons()
        {
            // reset addons
            WeaponOptions.AddonWeapons = new Dictionary<string, uint>();
            PlayerAppearance.AddonPeds = new Dictionary<string, uint>();

            var jsonData = LoadResourceFile(GetCurrentResourceName(), "config/addons.json") ?? "{}";
            try
            {
                // load new addons.
                var addons = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(jsonData);

                // load weapons
                if (addons.ContainsKey("weapons"))
                {
                    foreach (var addon in addons["weapons"])
                    {
                        if (!WeaponOptions.AddonWeapons.ContainsKey(addon))
                        {
                            WeaponOptions.AddonWeapons.Add(addon, (uint)GetHashKey(addon));
                        }
                        else
                        {
                            Debug.WriteLine($"[vMenu] [Error] Your addons.json file contains 2 or more entries with the same weapon name! ({addon}) Please remove duplicate lines!");
                        }
                    }
                }

                // load peds.
                if (addons.ContainsKey("peds"))
                {
                    foreach (var addon in addons["peds"])
                    {
                        if (!PlayerAppearance.AddonPeds.ContainsKey(addon))
                        {
                            PlayerAppearance.AddonPeds.Add(addon, (uint)GetHashKey(addon));
                        }
                        else
                        {
                            Debug.WriteLine($"[vMenu] [Error] Your addons.json file contains 2 or more entries with the same ped name! ({addon}) Please remove duplicate lines!");
                        }
                    }
                }
            }
            catch (JsonReaderException ex)
            {
                Debug.WriteLine($"\n\n^1[vMenu] [ERROR] ^7Your addons.json file contains a problem! Error details: {ex.Message}\n\n");
            }
        }

        /// <summary>
        /// Sets the extras labels from the extras.json file.
        /// </summary>
        private void SetVehicleExtras()
        {
            // reset addons
            VehicleCustomization.VehicleExtras = new Dictionary<uint, Dictionary<int, string>>();

            string jsonData = LoadResourceFile(GetCurrentResourceName(), "config/extras.json") ?? "{}";

            try
            {
                // load new extras.
                var extras = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<int, string>>>(jsonData);

                foreach (string model in extras.Keys)
                {
                    uint modelHash = (uint)GetHashKey(model);

                    if (extras[model] != null && extras[model].Count > 0)
                    {
                        if (!VehicleCustomization.VehicleExtras.ContainsKey(modelHash) || VehicleCustomization.VehicleExtras[modelHash] == null)
                            VehicleCustomization.VehicleExtras.Add(modelHash, extras[model]);
                        else
                        {
                            foreach (int extra in extras[model].Keys)
                            {
                                if (!VehicleCustomization.VehicleExtras[modelHash].ContainsKey(extra))
                                    VehicleCustomization.VehicleExtras[modelHash].Add(extra, extras[model][extra]);
                                else
                                    Debug.WriteLine($"[vMenu] [Warning] Your extras.json file contains 2 or more entries with the same extra index! ({model}, Extra {extra}) Please remove duplicate!");
                            }
                        }
                    }
                }
            }
            catch (JsonReaderException ex)
            {
                Debug.WriteLine($"\n\n^1[vMenu] [ERROR] ^7Your extras.json file contains a problem! Error details: {ex.Message}\n\n");
            }
        }

        private bool areExtrasSetup = false;
        private void SetExtras(string json)
        {
            if (areExtrasSetup)
            {
                return;
            }

            SetVehicleExtras();

            var extras = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            VehicleData.InitVehicleInfo(JsonConvert.DeserializeObject<VehicleData.VehicleInfoJson>(extras["vehicleInfo"]));

            extras.TryGetValue("usersettingsInfo", out var usersettingsInfo);
            data.Usersettings.InitUsersettingsInfo(usersettingsInfo);

            extras.TryGetValue("usersettings", out var usersettings);
            data.Usersettings.InitUsersettings(usersettings);

            MainMenu.ConfigOptionsSetupComplete = true;
            areExtrasSetup = true;
        }

        /// <summary>
        /// Update ban list.
        /// </summary>
        /// <param name="list"></param>
        private void UpdateBanList(string list)
        {
            MainMenu.BannedPlayersMenu?.UpdateBanList(list);
        }

        /// <summary>
        /// Used for cheaters.
        /// </summary>
        private void GoodBye()
        {
            Log("what a ######.");
            ForceSocialClubUpdate();
        }

        /// <summary>
        /// Set the cloud hat type.
        /// </summary>
        /// <param name="opacity"></param>
        /// <param name="cloudsType"></param>
        private void SetClouds(float opacity, string cloudsType)
        {
            if (opacity == 0f && cloudsType == "removed")
            {
                ClearCloudHat();
            }
            else
            {
                SetCloudHatOpacity(opacity);
                SetCloudHatTransition(cloudsType, 4f);
            }
        }

        /// <summary>
        /// Used by events triggered from the server to notify a user.
        /// </summary>
        /// <param name="message"></param>
        private void NotifyPlayer(string message, string type)
        {
            if (type.ToLower() == "alert")
            {
                Notify.Alert(message, true, true);
            }
            else if (type.ToLower() == "error")
            {
                Notify.Error(message, true, true);
            }
            else if (type.ToLower() == "info")
            {
                Notify.Info(message, true, true);
            }
            else if (type.ToLower() == "success")
            {
                Notify.Success(message, true, true);
            }
            else
            {
                Notify.Custom(message, true, true, type);
            }

        }

        /// <summary>
        /// Kill this player, poor thing, someone wants you dead... R.I.P.
        /// </summary>
        private void KillMe(string sourceName)
        {
            Notify.Alert($"You have been killed by <C>{GetSafePlayerName(sourceName)}</C>~s~ using the ~r~Kill Player~s~ option in vMenu.");
            SetEntityHealth(Game.PlayerPed.Handle, 0);
        }

        /// <summary>
        /// Teleport to the specified player.
        /// </summary>
        /// <param name="targetPlayer"></param>
        private async void SummonPlayer(string targetPlayer)
        {
            // ensure the player list is requested in case of Infinity
            MainMenu.PlayersList.RequestPlayerList();
            await MainMenu.PlayersList.WaitRequested();

            var player = MainMenu.PlayersList.FirstOrDefault(a => a.ServerId == int.Parse(targetPlayer));

            if (player != null)
            {
                await TeleportToPlayer(player);
            }
        }

        /// <summary>
        /// Clear the area around the provided x, y, z coordinates. Clears everything like (destroyed) objects, peds, (ai) vehicles, etc.
        /// Also restores broken streetlights, etc.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        private void ClearAreaNearPos(float x, float y, float z, float radius = 100f)
        {
            ClearAreaOfEverything(x, y, z, radius, false, false, false, false);
        }

        /// <summary>
        /// Kicks the current player from the specified vehicle if they're inside and don't own the vehicle themselves.
        /// </summary>
        /// <param name="vehNetId"></param>
        /// <param name="vehicleOwnedBy"></param>
        private async void GetOutOfCar(int vehNetId, int vehicleOwnedBy)
        {
            if (NetworkDoesNetworkIdExist(vehNetId))
            {
                var veh = NetToVeh(vehNetId);
                if (DoesEntityExist(veh))
                {
                    var vehicle = new Vehicle(veh);

                    if (vehicle == null || !vehicle.Exists())
                    {
                        return;
                    }

                    if (Game.PlayerPed.IsInVehicle(vehicle) && vehicleOwnedBy != Game.Player.ServerId)
                    {
                        if (!vehicle.IsStopped)
                        {
                            Notify.Alert("The owner of this vehicle is reclaiming their personal vehicle. You will be kicked from this vehicle in about 10 seconds. Stop the vehicle now to avoid taking damage.", false, true);
                        }

                        // Wait for the vehicle to come to a stop, or 10 seconds, whichever is faster.
                        var timer = GetGameTimer();
                        while (vehicle != null && vehicle.Exists() && !vehicle.IsStopped)
                        {
                            await Delay(0);
                            if (GetGameTimer() - timer > (10 * 1000)) // 10 second timeout
                            {
                                break;
                            }
                        }

                        // just to make sure they're actually still inside the vehicle and the vehicle still exists.
                        if (vehicle != null && vehicle.Exists() && Game.PlayerPed.IsInVehicle(vehicle))
                        {
                            // Make the ped jump out because the car isn't stopped yet.
                            if (!vehicle.IsStopped)
                            {
                                Notify.Info("You were warned, now you'll have to suffer the consequences!");
                                TaskLeaveVehicle(Game.PlayerPed.Handle, vehicle.Handle, 4160);
                            }
                            // Make the ped exit gently.
                            else
                            {
                                TaskLeaveVehicle(Game.PlayerPed.Handle, vehicle.Handle, 0);
                            }
                        }
                    }
                }
            }
        }

        private void SetDriftSuspension(int vehNetId, bool status)
        {
            int veh = NetToVeh(vehNetId);

            // We apply thes flags
            SetVehicleHandlingField(veh, "CCarHandlingData", "fBackEndPopUpCarImpulseMult", (int)0.100000);
            SetVehicleHandlingField(veh, "CCarHandlingData", "fBackEndPopUpBuildingImpulseMult", (int)0.030000);
            SetVehicleHandlingField(veh, "CCarHandlingData", "fBackEndPopUpMaxDeltaSpeed", (int)0.600000);

            SetVehicleHandlingField(veh, "CCarHandlingData", "strAdvancedFlags", 0x8000 + 0x4000000);

            // We enable or disable the suspension
            SetReduceDriftVehicleSuspension(veh, status);

        }

        /// <summary>
        /// Updates ped decorators for the clothing animation when players have joined.
        /// </summary>
        private async void UpdatePedDecors()
        {
            await Delay(1000);
            var backup = PlayerAppearance.ClothingAnimationType;
            PlayerAppearance.ClothingAnimationType = -1;
            await Delay(100);
            PlayerAppearance.ClothingAnimationType = backup;
        }
    }
}
