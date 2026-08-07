using System;
using System.Collections.Generic;

using CitizenFX.Core;

using Newtonsoft.Json;


using static CitizenFX.Core.Native.API;

namespace vMenuShared
{
    public static class ConfigManager
    {
        public enum Setting
        {
            // Id for RegisterKeyMapping
            vmenu_individual_server_id,

            vmenu_max_concurrent_remote_kvs_events,
            vmenu_max_concurrent_player_setup_events,

            // General settings
            vmenu_server_name,
            vmenu_menu_subtitle_color,
            vmenu_use_permissions,
            vmenu_use_only_hook_permissions,
            vmenu_menu_staff_only,
            vmenu_menu_toggle_key,
            vmenu_noclip_toggle_key,
            vmenu_keep_spawned_vehicles_persistent,
            vmenu_use_els_compatibility_mode,
            vmenu_handle_invisibility,
            vmenu_quit_session_in_rockstar_editor,
            vmenu_server_info_message,
            vmenu_server_info_website_url,
            vmenu_teleport_to_wp_keybind_key,
            vmenu_pvp_mode,
            keep_player_head_props,
            vmenu_disable_server_info_convars,
            vmenu_player_names_distance,
            vmenu_disable_entity_outlines_tool,
            vmenu_disable_player_stats_setup,
            pfvmenu_moshnotify_setting,
            vmenu_disable_radar_control,

            // Vehicle Chameleon Colours
            vmenu_using_chameleon_colours,

            // Vehicle spawner related settings
            vmenu_only_custom_classes,
            vmenu_show_vehicle_thumbnails,
            vmenu_vehicle_spawn_delay,

            // Kick & ban settings
            vmenu_default_ban_message_information,
            vmenu_auto_ban_cheaters,
            vmenu_auto_ban_cheaters_ban_message,
            vmenu_log_ban_actions,
            vmenu_log_kick_actions,

            // Player appearance settings
            vmenu_disallowed_player_models,
            vmenu_disable_spawn_as_allowed_ped,
            vmenu_spawn_as_deterministic_allowed_ped,

            // Weather & time settings
            vmenu_enable_time_weather_sync,
            vmenu_override_client_time_weather,
            vmenu_server_time,
            vmenu_server_weather,
            vmenu_server_weather_state,
            vmenu_weather_change_duration_server,
            vmenu_weather_change_duration_client,

            // Discord Rich Presence
            vmenu_discord_appid,
            vmenu_disable_richpresence,
            vmenu_discord_link_two,
            vmenu_discord_link_two_text,
            vmenu_discord_link_one_text,
            vmenu_discord_link_one,
            vmenu_discord_text,
            vmenu_discord_large_image,
            vmenu_discord_small_image,
            vmenu_discord_small_image_text,
            vmenu_discord_large_image_text,

            // DV Script
            vmenu_enable_dv_command,
            vmenu_dv_retries,
            vmenu_dv_distance,
            vmenu_freecam_toggle_key,

            // notification
            vmenu_notification_type,


            // osd settings
            vmenu_showlocationblips_on_default,
            vmenu_showlocation_on_default,


            // world related
            vmenu_set_vehicle_density_multiplier,
            vmenu_set_ped_density_multiplier,
            vmenu_set_random_vehicle_density_multiplier,
            vmenu_set_parked_vehicle_density_multiplier,
            vmenu_set_scenario_ped_density_multiplier,
            vmenu_set_garbage_trucks,
            vmenu_set_random_boats,
            vmenu_set_create_random_cops,
            vmenu_set_create_random_cops_not_onscenarios,
            vmenu_set_create_random_cops_on_scenarios,
            vmenu_enable_npc_density,
            vmenu_enable_replace_plates,

            // Key-value store
            vmenu_kvs_local_prefix,
            vmenu_kvs_sync_local,
            vmenu_mysql_connection_string_var,
            vmenu_server_store,

            vmenu_debug_permissions,
        }

        /// <summary>
        /// Get a boolean setting.
        /// </summary>
        /// <param name="setting"></param>
        /// <returns></returns>
        public static bool GetSettingsBool(Setting setting)
        {
            return GetConvar(setting.ToString(), "false") == "true";
        }

        /// <summary>
        /// Get an integer setting.
        /// </summary>
        /// <param name="setting"></param>
        /// <returns></returns>
        public static int GetSettingsInt(Setting setting)
        {
            var convarInt = GetConvarInt(setting.ToString(), -1);
            if (convarInt == -1)
            {
                if (int.TryParse(GetConvar(setting.ToString(), "-1"), out var convarIntAlt))
                {
                    return convarIntAlt;
                }
            }
            return convarInt;
        }

        /// <summary>
        /// Get a float setting.
        /// </summary>
        /// <param name="setting"></param>
        /// <returns></returns>
        public static float GetSettingsFloat(Setting setting)
        {
            if (float.TryParse(GetConvar(setting.ToString(), "-1.0"), out var result))
            {
                return result;
            }
            return -1f;
        }

        /// <summary>
        /// Get a string setting.
        /// </summary>
        /// <param name="setting"></param>
        /// <returns></returns>
        public static string GetSettingsString(Setting setting, string defaultValue = null)
        {
            var value = GetConvar(setting.ToString(), defaultValue ?? "");
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            return value;
        }

        /// <summary>
        /// Debugging mode
        /// </summary>
        public static bool DebugMode => IsDuplicityVersion() ? IsServerDebugModeEnabled() : IsClientDebugModeEnabled();

        /// <summary>
        /// Default value for server debugging mode.
        /// </summary>
        /// <returns></returns>
        public static bool IsServerDebugModeEnabled()
        {
            return GetResourceMetadata("vMenu", "server_debug_mode", 0).ToLower() == "true";
        }

        /// <summary>
        /// Default value for client debugging mode.
        /// </summary>
        /// <returns></returns>
        public static bool IsClientDebugModeEnabled()
        {
            return GetResourceMetadata("vMenu", "client_debug_mode", 0).ToLower() == "true";
        }

        #region Get saved locations from the locations.json
        /// <summary>
        /// Gets the locations.json data.
        /// </summary>
        /// <returns></returns>
        public static Blips GetBlips()
        {
            var data = new Blips();

            var jsonFile = LoadResourceFile(GetCurrentResourceName(), "config/blips.json");
            try
            {
                if (string.IsNullOrEmpty(jsonFile))
                {
#if CLIENT
                    vMenuClient.Notify.Error("The blips.json file is empty or does not exist, please tell the server owner to fix this.");
#endif
#if SERVER
                    vMenuServer.DebugLog.Log("The blips.json file is empty or does not exist, please fix this.", vMenuServer.DebugLog.LogLevel.error);
#endif
                }
                else
                {
                    data = JsonConvert.DeserializeObject<Blips>(jsonFile);
                }
            }
            catch (Exception e)
            {
#if CLIENT
                vMenuClient.Notify.Error("An error occurred while processing the locations.json file. Teleport Locations and Location Blips will be unavailable. Please correct any errors in the locations.json file.");
#endif
                Debug.WriteLine($"[vMenu] json exception details: {e.Message}\nStackTrace:\n{e.StackTrace}");
            }

            return data;
        }


        /// <summary>
        /// Gets just the blips data from the locations.json.
        /// </summary>
        /// <returns></returns>
        public static List<LocationBlip> GetLocationBlipsData()
        {
            return GetBlips().blips;
        }

        /// <summary>
        /// Struct used for deserializing json only.
        /// </summary>
        public struct TeleportLocationsJson
        {
            public List<TeleportLocation> teleports;
        }
        public struct Blips
        {
            public List<LocationBlip> blips;
        }

        /// <summary>
        /// Teleport location struct.
        /// </summary>
        public struct TeleportLocation : IEquatable<TeleportLocation>
        {
            public string name;
            public Vector3 coordinates;
            public float heading;

            public TeleportLocation(string name, Vector3 coordinates, float heading)
            {
                this.name = name;
                this.coordinates = coordinates;
                this.heading = heading;
            }

            public bool Equals(TeleportLocation other)
            {
                return name == other.name && coordinates == other.coordinates && heading == other.heading;
            }
        }
        public struct TpCategoriesJson
        {
            public List<TpCategory> teleports;
        }
        public struct TpCategory
        {
            public string JsonName;
            public string name;
            public TpCategory(string JsonName, string name)
            {
                this.JsonName = JsonName;
                this.name = name;
            }
        }
        public struct PlateStruct
        {
            public string fileName;
            public string normalName;
            public string vMenuPlateName;
            public string pattern;

            public PlateStruct(string fileName, string normalName, string vMenuPlateName, string pattern)
            {
                this.fileName = fileName;
                this.normalName = normalName;
                this.vMenuPlateName = vMenuPlateName;
                this.pattern = pattern;
            }
        }
        /// <summary>
        /// Location blip struct.
        /// </summary>
        public struct LocationBlip
        {
            public string name;
            public Vector3 coordinates;
            public int spriteID;
            public int color;

            public LocationBlip(string name, Vector3 coordinates, int spriteID, int color)
            {
                this.name = name;
                this.coordinates = coordinates;
                this.spriteID = spriteID;
                this.color = color;
            }
        }
        #endregion

        #region Get all the languages from the appropriate json file

        /// <summary>
        /// Gets and stores the languages from the multiple .json's.
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, Dictionary<string, string>> GetLanguages()
        {
            Dictionary<string, Dictionary<string, string>> data = new Dictionary<string, Dictionary<string, string>>();

            var metaData = GetResourceMetadata(GetCurrentResourceName(), "languages", GetNumResourceMetadata(GetCurrentResourceName(), "languages") - 1).Replace(" ", "");
            if (!string.IsNullOrEmpty(metaData))
            {
                var languages = metaData.Split(',');
                foreach (var lang in languages)
                {
                    try
                    {
                        string jsonFile = LoadResourceFile(GetCurrentResourceName(), $"config/languages/{lang}.json");
                        if (!string.IsNullOrEmpty(jsonFile))
                        {
                            data.Add(lang, JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonFile));
                        }
                        else
                        {
#if CLIENT
                            vMenuClient.Notify.Error($"Unable to load {lang}.json");
#endif
                        }
                    }
                    catch
                    {
#if CLIENT
                        vMenuClient.Notify.Error($"Unable to load {lang}.json");
#endif
                    }
                }
            }

            return data;
        }

        #endregion
    }
}
