using System;
using System.Collections.Generic;
using System.Linq;

using CitizenFX.Core;

using static vMenuShared.ConfigManager;

using static CitizenFX.Core.Native.API;

using Newtonsoft.Json;

namespace vMenuShared
{
    public static class PermissionsManager
    {
        public const string VMENU_ACE_PREFIX = "vMenu";

        public enum Permission
        {
            // Permission debugging
            AceDebugPermissionsIsAdmin,
            AceDebugPermissionsIsModerator,

            // Global
            #region global
            Everything,
            DontKickMe,
            DontBanMe,
            NoClip,
            Staff,
            DumpLang,
            DVAll,
            Freecam,
            #endregion

            // Online Players
            #region online players
            OPMenu,
            OPAll,
            OPTeleport,
            OPWaypoint,
            OPSpectate,
            OPIdentifiers,
            OPSummon,
            OPKill,
            OPKick,
            OPPermBan,
            OPTempBan,
            OPUnban,
            OPViewBannedPlayers,
            OPSeePrivateMessages,
            #endregion

            // Player Options
            #region player options
            POMenu,
            POAll,
            POGod,
            POInvisible,
            POFastRun,
            POFastSwim,
            POSuperjump,
            PONoRagdoll,
            PONeverWanted,
            POSetWanted,
            POIgnored,
            POStayInVehicle,
            POMaxHealth,
            POMaxArmor,
            POCleanPlayer,
            PODryPlayer,
            POWetPlayer,
            POVehicleAutoPilotMenu,
            POFreeze,
            POScenarios,
            POUnlimitedStamina,
            #endregion

            // Vehicle Options
            #region vehicle options
            VOMenu,
            VOAll,
            VOGod,
            VOKeepClean,
            VORepair,
            VOWash,
            VOEngine,
            VODestroyEngine,
            VOBikeSeatbelt,
            VOSpeedLimiter,
            VOChangePlate,
            VOMod,
            VOColors,
            VOLiveries,
            VOComponents,
            VODoors,
            VOWindows,
            VOFreeze,
            VOInvisible,
            VOTorqueMultiplier,
            VOPowerMultiplier,
            VOFlip,
            VOAlarm,
            VOCycleSeats,
            VOEngineAlwaysOn,
            VONoSiren,
            VONoHelmet,
            VOLights,
            VOFixOrDestroyTires,
            VODelete,
            VOUnderglow,
            VOFlashHighbeamsOnHonk,
            VODisableTurbulence,
            VOInfiniteFuel,
            VOReduceDriftSuspension,
            VOFlares,
            VOPlaneBombs,
            VOVehiclesBlacklist,
            VODisableFromDefaultList,
            VOSaveMods,
            #endregion

            // Vehicle Spawner
            #region vehicle spawner
            VSMenu,
            VSAll,
            VSDisableReplacePrevious,
            VSSpawnByName,
            VSNoSpawnDelay,
            VSBoatsNotInWater,
            VSAddon,
            VSCompacts,
            VSSedans,
            VSSUVs,
            VSCoupes,
            VSMuscle,
            VSSportsClassic,
            VSSports,
            VSSuper,
            VSMotorcycles,
            VSOffRoad,
            VSIndustrial,
            VSUtility,
            VSVans,
            VSCycles,
            VSBoats,
            VSHelicopters,
            VSPlanes,
            VSService,
            VSEmergency,
            VSMilitary,
            VSCommercial,
            VSTrains,
            VSOpenWheel,
            #endregion

            // Saved Vehicles
            #region saved vehicles
            SVMenu,
            SVAll,
            SVSpawn,
            #endregion

            // Personal Vehicle
            #region personal vehicle
            PVMenu,
            PVAll,
            PVToggleEngine,
            PVToggleLights,
            PVToggleStance,
            PVKickPassengers,
            PVLockDoors,
            PVDoors,
            PVSoundHorn,
            PVToggleAlarm,
            PVAddBlip,
            PVExclusiveDriver,
            #endregion

            // Player Appearance
            #region player appearance
            PAMenu,
            PAAll,
            PACustomize,
            PASpawnSaved,
            PASpawnNew,
            PAAddonPeds,
            PAAnimalPeds,
            PASpawnAsDefault,
            #endregion

            // Teleport Options
            #region teleport options
            TPMenu,
            TPAll,
            TPTeleportToPrev,
            TPTeleportToWp,
            TPTeleportToCoord,
            TPTeleportLocations,
            TPTeleportPersonalLocations,
            TPTeleportSaveLocation,
            #endregion

            // Time/Weather Options
            #region time & weather options
            TWClientMenu,
            TWServerMenu,
            #endregion

            // Weapon Options
            #region weapon options
            WPMenu,
            WPAll,
            WPGetAll,
            WPRemoveAll,
            WPUnlimitedAmmo,
            WPNoReload,
            WPSpawn,
            WPSpawnByName,
            WPSetAllAmmo,
            #endregion

            // World Related Options
            #region world related options
            WRNPCOptions,
            #endregion

            //Weapons Permissions
            #region weapon specific permissions
            WPAPPistol,
            WPAdvancedRifle,
            WPAssaultRifle,
            WPAssaultRifleMk2,
            WPAssaultSMG,
            WPAssaultShotgun,
            WPBZGas,
            WPBall,
            WPBat,
            WPBattleAxe,
            WPBottle,
            WPBullpupRifle,
            WPBullpupRifleMk2,
            WPBullpupShotgun,
            WPCarbineRifle,
            WPCarbineRifleMk2,
            WPCombatMG,
            WPCombatMGMk2,
            WPCombatPDW,
            WPCombatPistol,
            WPCompactGrenadeLauncher,
            WPCompactRifle,
            WPCrowbar,
            WPDagger,
            WPDoubleAction,
            WPDoubleBarrelShotgun,
            WPFireExtinguisher,
            WPFirework,
            WPFlare,
            WPFlareGun,
            WPFlashlight,
            WPGolfClub,
            WPGrenade,
            WPGrenadeLauncher,
            WPGrenadeLauncherSmoke,
            WPGusenberg,
            WPHammer,
            WPHatchet,
            WPHeavyPistol,
            WPHeavyShotgun,
            WPHeavySniper,
            WPHeavySniperMk2,
            WPHomingLauncher,
            WPKnife,
            WPKnuckleDuster,
            WPMG,
            WPMachete,
            WPMachinePistol,
            WPMarksmanPistol,
            WPMarksmanRifle,
            WPMarksmanRifleMk2,
            WPMicroSMG,
            WPMiniSMG,
            WPMinigun,
            WPMolotov,
            WPMusket,
            WPNightVision,
            WPNightstick,
            WPParachute,
            WPPetrolCan,
            WPPipeBomb,
            WPPistol,
            WPPistol50,
            WPPistolMk2,
            WPPoolCue,
            WPProximityMine,
            WPPumpShotgun,
            WPPumpShotgunMk2,
            WPRPG,
            WPRailgun,
            WPRevolver,
            WPRevolverMk2,
            WPSMG,
            WPSMGMk2,
            WPSNSPistol,
            WPSNSPistolMk2,
            WPSawnOffShotgun,
            WPSmokeGrenade,
            WPSniperRifle,
            WPSnowball,
            WPSpecialCarbine,
            WPSpecialCarbineMk2,
            WPStickyBomb,
            WPStunGun,
            WPSweeperShotgun,
            WPSwitchBlade,
            WPUnarmed,
            WPVintagePistol,
            WPWrench,
            WPPlasmaPistol, // xmas 2018 dlc (1604)
            WPPlasmaCarbine, // xmas 2018 dlc (1604)
            WPPlasmaMinigun, // xmas 2018 dlc (1604)
            WPStoneHatchet, // xmas 2018 dlc (1604)
            WPCeramicPistol, // xmas 2019 dlc (1868)
            WPNavyRevolver, // xmas 2019 dlc (1868)
            WPHazardCan, // xmas 2019 dlc (1868) (Does not have label text)
            WPPericoPistol, // xmas 2020 dlc (2189)
            WPMilitaryRifle, // xmas 2020 dlc (2189)
            WPCombatShotgun, // xmas 2020 dlc (2189)
            // MPSECURITY DLC (v 2545)
            WPEMPLauncher,
            WPHeavyRifle,
            WPFertilizerCan,
            WPStunGunMP,
            // MPSUM2 DLC (v 2699)
            WPPrecisionRifle,
            WPTacticalRifle,
            // MPCHRISTMAS3 DLC (v 2802)
            WPAcidPackage,
            WPCandyCane,
            WPPistolXM3,
            WPRailgunXM3,
            // MP2023_01 DLC (V 2944)
            WPTecPistol,
            #endregion

            // Weapon Loadouts Menu
            #region weapon loadouts
            WLMenu,
            WLAll,
            WLEquip,
            WLEquipOnRespawn,
            #endregion

            // Enhanced Camera Menu
            #region enhanced camera
            ECMenu,
            ECAll,
            ECLeadCamera,
            ECChaseCamera,
            ECDroneCamera,
            #endregion

            // Misc Settings
            #region misc settings
            MSAll,
            MSSpeed,
            MSTime,
            MSClearArea,
            MSTeleportToWp,
            MSTeleportToCoord,
            MSShowCoordinates,
            MSShowLocation,
            MSJoinQuitNotifs,
            MSDeathNotifs,
            MSNightVision,
            MSThermalVision,
            MSLocationBlips,
            MSPlayerBlips,
            MSOverheadNames,
            MSTeleportLocations,
            MSTeleportSaveLocation,
            MSConnectionMenu,
            MSRestoreAppearance,
            MSRestoreWeapons,
            MSDriftMode,
            MSDevTools,
            MSEntityInfo,
            MSTimecycleMofifiers,
            MSEntitySpawner,
            #endregion

            // Plugin Menu
            #region plugin menu
            PNMenu,
            PNAll,
            PNEasyDrift,
            #endregion

            // Bug Prevention
            #region bug prevention
            BPCarlaunch,
            #endregion

            #region  reset index
            // ResetIndex Permission
            ResetIndex,
            #endregion
        }

        private static HashSet<Permission> GetParentPermissions(Permission permission)
        {
            var parentPermissions = new HashSet<Permission>() { Permission.Everything, permission };
            var permStr = permission.ToString();

            var permStr2 = permStr.Substring(0, 2);

            // if the first 2 characters are both uppercase
            if (permStr2.ToUpper() == permStr2 &&
                (permStr.Substring(2) is not ("All" or "Menu" or "VehiclesBlacklist" or "DisableFromDefaultList" or "AllowOpenWheel")))
            {
                var allPerms = Enum
                    .GetValues(typeof(Permission))
                    .Cast<Permission>()
                    .Where(p => p.ToString() == permStr2 + "All");
                foreach (var p in allPerms)
                {
                    parentPermissions.Add(p);
                }
            }
            // else it's one of the .Everything, .DontKickMe, DontBanMe, NoClip, Staff, etc perms that are not menu specific so do nothing
            return parentPermissions;
        }

        public static readonly Dictionary<Permission, HashSet<Permission>> ParentPermissions = Enum
            .GetValues(typeof(Permission))
            .Cast<Permission>()
            .ToDictionary(p => p, GetParentPermissions);

        /// <summary>
        /// Gets the full permission ace name for the specific <see cref="Permission"/> enum.
        /// </summary>
        /// <param name="permission"></param>
        /// <returns></returns>
        private static string GetAceName(Permission permission)
        {
            var name = permission.ToString();

            var prefix = $"{VMENU_ACE_PREFIX}.";

            switch (name.Substring(0, 2))
            {
                case "OP":
                    prefix += "OnlinePlayers";
                    break;
                case "PO":
                    prefix += "PlayerOptions";
                    break;
                case "VO":
                    prefix += "VehicleOptions";
                    break;
                case "VS":
                    prefix += "VehicleSpawner";
                    break;
                case "SV":
                    prefix += "SavedVehicles";
                    break;
                case "PV":
                    prefix += "PersonalVehicle";
                    break;
                case "PA":
                    prefix += "PlayerAppearance";
                    break;
                case "TO":
                    prefix += "TimeOptions";
                    break;
                case "WO":
                    prefix += "WeatherOptions";
                    break;
                case "WP":
                    prefix += "WeaponOptions";
                    break;
                case "TW":
                    prefix += "TimeWeatherOptions";
                    break;
                case "WL":
                    prefix += "WeaponLoadouts";
                    break;
                case "MS":
                    prefix += "MiscSettings";
                    break;
                case "TP":
                    prefix += "TeleportOptions";
                    break;
                case "PN":
                    prefix += "PluginMenu";
                    break;
                case "BP":
                    prefix += "BugPrevention";
                    break;
                case "EC":
                    prefix += "EnhancedCamera";
                    break;
                case "WR":
                    prefix += "WorldRelated";
                    break;
                default:
                    return prefix + name;
            }

            return prefix + "." + name.Substring(2);
        }

        public static readonly Dictionary<Permission, string> PermissionToAceName = Enum
            .GetValues(typeof(Permission))
            .Cast<Permission>()
            .ToDictionary(p => p, GetAceName);

        public static readonly Dictionary<string, Permission> AceNameToPermission = PermissionToAceName
            .ToDictionary(kv => kv.Value, kv => kv.Key);

#if SERVER
        /// <summary>
        /// Checks if the player is allowed that specific permission.
        /// </summary>
        /// <param name="permission"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public static bool IsAllowed(Permission permission, Player source)
        {
            if (source == null)
            {
                return false;
            }

            return ParentPermissions[permission]
                .Any(pperm => IsPlayerAceAllowed(source.Handle, PermissionToAceName[pperm]));
        }

        /// <summary>
        /// Sets the permissions for a specific player (checks server side, sends event to client side).
        /// </summary>
        /// <param name="player"></param>
        public async static void SetPermissionsForPlayer([FromSource] Player player)
        {
            if (player == null)
            {
                return;
            }

            if (GetSettingsBool(Setting.vmenu_debug_permissions))
            {
                var identifiers = player.Identifiers;
                var isAdmin = IsAllowed(Permission.AceDebugPermissionsIsAdmin, player);
                var isMod = IsAllowed(Permission.AceDebugPermissionsIsModerator, player);
                Debug.WriteLine(
                    $"Sending permissions to {player.Name} with IDs " +
                    $"fivem:{identifiers["fivem"] ?? "NULL"}, " +
                    $"license:{identifiers["license"] ?? "NULL"}, " +
                    $"and roles " +
                    $"admin={isAdmin}, moderator={isMod}");
            }

            var allowedBuiltinPermissions = new HashSet<Permission>();

            if (!GetSettingsBool(Setting.vmenu_use_permissions))
            {
                foreach (var p in Enum.GetValues(typeof(Permission)))
                {
                    var permission = (Permission)p;
                    switch (permission)
                    {
                        // don't allow any of the following permissions if perms are ignored.
                        case Permission.Everything:
                        case Permission.OPAll:
                        case Permission.OPKick:
                        case Permission.OPKill:
                        case Permission.OPPermBan:
                        case Permission.OPTempBan:
                        case Permission.OPUnban:
                        case Permission.OPIdentifiers:
                        case Permission.OPViewBannedPlayers:
                            break;
                        // do allow the rest
                        default:
                            allowedBuiltinPermissions.Add(permission);
                            break;
                    }
                }
            }
            else
            {
                allowedBuiltinPermissions = [.. Enum
                    .GetValues(typeof(Permission))
                    .Cast<Permission>()
                    .Where(p => IsAllowed(p, player))];
            }

            var playerPermissionsJson = await vMenuServer.RequestManager.Send(
                vMenuServer.MainServer.Hooks.PlayerPermissions.FETCH_FOR_EVENT_NAME,
                $"{player.Handle}");
            var playerPermissions = JsonConvert.DeserializeObject<Dictionary<string, bool>>(playerPermissionsJson);

            var filteredPlayerPermissions = (bool allowed) =>
            {
                return playerPermissions
                    .Where(kv => kv.Value == allowed)
                    .Select(kv => kv.Key);
            };

            var allowedPlayerPermissions = filteredPlayerPermissions(true);
            var deniedPlayerPermissions = filteredPlayerPermissions(false);

            var allowedPermissions = allowedBuiltinPermissions
                .Select(p => PermissionToAceName[p])
                .Union(allowedPlayerPermissions)
                .Where(p => !deniedPlayerPermissions.Contains(p))
                .ToList();

            if (GetSettingsBool(Setting.vmenu_menu_staff_only) &&
                !new[] { Permission.Staff, Permission.Everything }.Any(p => IsAllowed(p, player)))
            {
                allowedPermissions = new();
            }

            // Send the permissions to the client.
            player.TriggerEvent("vMenu:SetPermissions", JsonConvert.SerializeObject(allowedPermissions));

            // Also tell the client to do the addons setup.
            player.TriggerEvent("vMenu:SetAddons");

            var usersettings = await vMenuServer.RequestManager.Send(
                vMenuServer.MainServer.Hooks.Usersettings.FETCH_ALL_FOR_EVENT_NAME,
                $"{player.Handle}");

            Dictionary<string, string> extras = new Dictionary<string, string>
            {
                ["vehicleInfo"] = await vMenuServer.MainServer.Hooks.VehicleInfo.FetchResult,
                ["usersettingsInfo"] = await vMenuServer.MainServer.Hooks.Usersettings.GetInfoResult,
                ["usersettings"] = usersettings,
            };
            player.TriggerEvent("vMenu:SetExtras", JsonConvert.SerializeObject(extras));
        }
#endif

#if CLIENT
        public static HashSet<string> Permissions { get; private set; } = new();
        public static bool ArePermissionsSetup { get; set; } = false;

        /// <summary>
        /// Public function to check if a permission is allowed.
        /// </summary>
        /// <param name="permission"></param>
        /// <param name="checkAnyway">If true, the permissions will be checked even if they aren't setup yet.</param>
        /// <returns></returns>
        public static bool IsAllowed(Permission permission, bool checkAnyway = false) =>
            IsAllowed(PermissionToAceName[permission], checkAnyway);

        public static bool IsAllowed(string permission, bool checkAnyway = false)
        {
            if (!ArePermissionsSetup && !checkAnyway)
            {
                return false;
            }

            return Permissions.Contains(permission);
        }

        /// <summary>
        /// Sets the permission (client side event handler).
        /// </summary>
        /// <param name="permissions"></param>
        public static void SetPermissions(string permissionsJson)
        {
            Permissions = [.. Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(permissionsJson)];

            // if debug logging.
            if (GetResourceMetadata(GetCurrentResourceName(), "client_debug_mode", 0) == "true")
            {
                Debug.WriteLine("[vMenu] [Permissions] " + Newtonsoft.Json.JsonConvert.SerializeObject(Permissions, Newtonsoft.Json.Formatting.None));
            }

            ArePermissionsSetup = true;

            if (GetSettingsBool(Setting.vmenu_debug_permissions))
            {
                var roles = new List<string> { };
                if (IsAllowed(Permission.AceDebugPermissionsIsAdmin))
                {
                    roles.Add("admin");
                }
                if (IsAllowed(Permission.AceDebugPermissionsIsModerator))
                {
                    roles.Add("moderator");
                }

                Debug.WriteLine("[INFO] Your roles: {" + string.Join(",", roles) + "}");
            }
        }
#endif
    }
}
