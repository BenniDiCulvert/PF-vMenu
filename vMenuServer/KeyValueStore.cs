using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CitizenFX.Core;

using MySqlConnector;

using Newtonsoft.Json;

using vMenuShared;

using static CitizenFX.Core.Native.API;
using static vMenuServer.KeyValueStore;
using static vMenuShared.KeyValueStoreSync;

namespace vMenuServer
{
    public static class DatabaseKeyValueStore
    {
        private static readonly Dictionary<string, Task<Dictionary<string, ValueInfo>>> getAllTasks = [];

        public static string Truncate(this string value, int length)
        {
            if (value.Length <= length)
                return value;

            return value.Substring(0, length);
        }

        public const string SERVER_LICENSE = "0000000000000000000000000000000000000000";

        public static string ConnectionString { get; private set; }
        private static MySqlConnection connection = null;


        private static void CreateTable()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                var command = new MySqlCommand
                {
                    Connection = connection,
                    CommandText = "CREATE TABLE IF NOT EXISTS `vMenu` (`PlayerLicense` CHAR(40) NOT NULL, `Key` VARCHAR(64) NOT NULL, `Value` TEXT, `Type` BIT(2) NOT NULL DEFAULT 0, PRIMARY KEY (`PlayerLicense`, `Key`)) CHARACTER SET = utf8mb4 COLLATE = utf8mb4_bin ROW_FORMAT = COMPRESSED"
                };
                command.ExecuteNonQuery();
            }
        }

        public static void Connect()
        {
            SetConvarReplicated(ConfigManager.Setting.vmenu_server_store.ToString(), "false");

            var connectionStringVar = ConfigManager.GetSettingsString(ConfigManager.Setting.vmenu_mysql_connection_string_var);
            if (string.IsNullOrEmpty(connectionStringVar))
            {
                Debug.WriteLine("\"vmenu_mysql_connection_string_var\" not specified or empty. Running without database.");
                return;
            }

            ConnectionString = GetConvar(connectionStringVar, "");
            if (string.IsNullOrEmpty(ConnectionString))
            {
                Debug.WriteLine($"Invalid or missing MySQL connection string convar \"{connectionStringVar}\". Running without database.");
                ConnectionString = null;
                return;
            }

            try
            {
                connection = new MySqlConnection(ConnectionString);
                connection.Open();
            }
            catch (MySqlException)
            {
                Debug.WriteLine($"Could not open connection to database. Running without database.");
                ConnectionString = null;
                connection = null;
                return;
            }

            try
            {
                CreateTable();
            }
            catch (MySqlException e)
            {
                Debug.WriteLine($"Could create key-value store table: {e}");
                ConnectionString = null;
                connection = null;
                return;
            }

            Debug.WriteLine("Successfully connected to database.");
            SetConvarReplicated(ConfigManager.Setting.vmenu_server_store.ToString(), "true");
        }

        public static async Task<Dictionary<string, ValueInfo>> GetAll(string playerLicense)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync();

                var command = new MySqlCommand
                {
                    Connection = connection,
                    CommandText = "SELECT `Key`, `Value`, `Type` FROM `vMenu` WHERE `PlayerLicense`=@playerLicense",
                };
                command.Parameters.AddWithValue("@playerLicense", playerLicense);

                var keyValues = new Dictionary<string, ValueInfo>();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var key = reader.GetFieldValue<string>(0);
                        var value = reader.GetFieldValue<string>(1);
                        var type = reader.GetFieldValue<int>(2);
                        keyValues[key] = new ValueInfo(value, (KeyValueStore.ValueType)type);
                    }
                }

                var estimatedSize = 1.25 * keyValues.Sum(kv => kv.Key.Length + kv.Value.Value.Length);
                Debug.WriteLine($"KVS::GetAll, license={playerLicense}, count={keyValues.Count}, estimated size={estimatedSize / 1024.0:0.000} KiB");

                return keyValues;
            }
        }

        public static async Task<Dictionary<string, ValueInfo>> GetAll()
        {
            if (string.IsNullOrEmpty(ConnectionString))
                return new Dictionary<string, ValueInfo>();

            return await GetAll(SERVER_LICENSE);
        }

        public static async Task Remove(string playerLicense, string key)
        {
            Debug.WriteLine($"KVS::Remove, license={playerLicense}, key={key}");

            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync();

                var command = new MySqlCommand
                {
                    Connection = connection,
                    CommandText = "DELETE FROM `vMenu` WHERE `PlayerLicense`=@playerLicense AND `Key`=@key"
                };
                command.Parameters.AddWithValue("@playerLicense", playerLicense);
                command.Parameters.AddWithValue("@key", key.Truncate(64));

                await command.ExecuteNonQueryAsync();
            }
        }

        public static async Task RemoveMany(string playerLicense, List<string> keys)
        {
            Debug.WriteLine($"KVS::RemoveMany, license={playerLicense}, count={keys.Count}");

            if (keys == null || keys.Count == 0)
                return;

            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync();

                var command = new MySqlCommand
                {
                    Connection = connection,
                    CommandText = "DELETE FROM `vMenu` WHERE `PlayerLicense`=@playerLicense AND `Key`=@key"
                };
                command.Parameters.AddWithValue("@playerLicense", playerLicense);
                command.Parameters.AddWithValue("@key", null);
                command.Prepare();

                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        foreach (var key in keys)
                        {
                            command.Transaction = transaction;
                            command.Parameters["@key"].Value = key.Truncate(64);
                            await command.ExecuteNonQueryAsync();
                            await BaseScript.Delay(0);
                        }
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public static async Task Remove(string key)
        {
            if (string.IsNullOrEmpty(ConnectionString))
                return;

            await Remove(SERVER_LICENSE, key);
        }

        public static async Task Set(string playerLicense, string key, ValueInfo vi)
        {
            Debug.WriteLine($"KVS::Set, license={playerLicense}, key={key}, value={vi.Value}");

            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync();

                var command = new MySqlCommand
                {
                    Connection = connection,
                    CommandText = "INSERT INTO `vMenu` (`PlayerLicense`, `Key`, `Value`, `Type`) VALUES (@playerLicense, @key, @value, @type) ON DUPLICATE KEY UPDATE `Value`=@value, `Type`=@type"
                };
                command.Parameters.AddWithValue("@playerLicense", playerLicense);
                command.Parameters.AddWithValue("@key", key.Truncate(64));
                command.Parameters.AddWithValue("@value", vi.Value);
                command.Parameters.AddWithValue("@type", (int)vi.Type);

                await command.ExecuteNonQueryAsync();
            }
        }

        public static async Task Set(string key, ValueInfo vi)
        {
            if (string.IsNullOrEmpty(ConnectionString))
                return;

            await Set(SERVER_LICENSE, key, vi);
        }

        public static async Task SetAll(string playerLicense, Dictionary<string, ValueInfo> keyValues)
        {
            var estimatedSize = 1.25 * keyValues.Sum(kv => kv.Key.Length + kv.Value.Value.Length);
            Debug.WriteLine($"KVS::SetAll, license={playerLicense}, count={keyValues.Count}, estimated size={estimatedSize / 1024.0:0.000} KiB");

            if (keyValues == null || keyValues.Count == 0)
                return;

            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync();

                var command = new MySqlCommand
                {
                    Connection = connection,
                    CommandText = "INSERT INTO `vMenu` (`PlayerLicense`, `Key`, `Value`, `Type`) VALUES (@playerLicense, @key, @value, @type) ON DUPLICATE KEY UPDATE `Value`=@value, `Type`=@type"
                };
                command.Parameters.AddWithValue("@playerLicense", playerLicense);
                command.Parameters.AddWithValue("@key", null);
                command.Parameters.AddWithValue("@value", null);
                command.Parameters.AddWithValue("@type", null);
                command.Prepare();

                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        foreach (var kv in keyValues)
                        {
                            command.Transaction = transaction;
                            command.Parameters["@key"].Value = kv.Key.Truncate(64);
                            command.Parameters["@value"].Value = kv.Value.Value;
                            command.Parameters["@type"].Value = kv.Value.Type;
                            await command.ExecuteNonQueryAsync();
                            await BaseScript.Delay(0);
                        }
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public static async Task SetAll(Dictionary<string, ValueInfo> keyValues)
        {
            if (string.IsNullOrEmpty(ConnectionString))
                return;

            await SetAll(SERVER_LICENSE, keyValues);
        }
    }

    public static class ClientRemoteKeyValueStore
    {
        private static void SendResponse(Player player, Response response)
        {
            player.TriggerEventDynamicLatent("vMenu:ServerKeyValueStoreResponse", JsonConvert.SerializeObject(response));
        }


        public static async Task HandleRequest(Player player, string json)
        {
            Request request;
            Response response;
            try
            {
                request = JsonConvert.DeserializeObject<Request>(json);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error handling database key-value store request from player {player.Name}: {e}");
                response = new Response
                {
                    Type = Response.ResponseType.Error,
                    Error = "Error"
                };
                SendResponse(player, response);
                return;
            }

            response = await HandleRequest(player, request);
            SendResponse(player, response);
        }

        private static async Task<Response> HandleRequest(Player player, Request request)
        {
            if (string.IsNullOrEmpty(DatabaseKeyValueStore.ConnectionString))
            {
                return new Response
                {
                    Id = request.Id,
                    Type = Response.ResponseType.NoServerStore
                };
            }

            string license = "UNKNOWN";
            Response response;
            try
            {
                license = player.Identifiers["license"]?.Truncate(40);
                if (string.IsNullOrEmpty(license))
                {
                    return new Response
                    {
                        Type = Response.ResponseType.Error,
                        Error = "Player license could not be verified",
                    };
                }

                switch (request.Type)
                {
                    case Request.RequestType.GetAll:
                        response = await HandleRequestGetAll(license, request);
                        try
                        {
                            var keyValues = response.DataGetAll.Value.KeyValues;
                            var json = JsonConvert.SerializeObject(keyValues);
                        }
                        catch
                        {
                        }
                        break;
                    case Request.RequestType.UpdateMany:
                        response = await HandleRequestUpdateMany(license, request);
                        break;
                    default:
                        response = new Response
                        {
                            Type = Response.ResponseType.Error,
                            Error = "The request was invalid"
                        };
                        break;
                }
                response.Type = Response.ResponseType.Ok;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error handling database key-value store request {request.Type} (id {request.Id}) from player {player.Name} ({license}): {e}");
                response = new Response
                {
                    Type = Response.ResponseType.Error,
                    Error = $"request id {request.Id}"
                };
            }
            response.Id = request.Id;

            return response;
        }

        private static async Task<Response> HandleRequestGetAll(string playerLicense, Request _)
        {
            var keyValues = await DatabaseKeyValueStore.GetAll(playerLicense);
            return new Response
            {
                DataGetAll = new Response.ResponseDataGetAll
                {
                    KeyValues = keyValues
                }
            };
        }

        private static async Task<Response> HandleRequestUpdateMany(string playerLicense, Request requestUpdateMany)
        {
            await DatabaseKeyValueStore.SetAll(playerLicense, requestUpdateMany.DataUpdateMany?.Set);
            await DatabaseKeyValueStore.RemoveMany(playerLicense, requestUpdateMany.DataUpdateMany?.Remove);
            return new Response
            {
                DataUpdateMany = new Response.ResponseDataUpdateMany
                {
                }
            };
        }
    }

    public static partial class KeyValueStore
    {
        public async static Task SyncWithDatabase()
        {
            var localKvs = GetAll();
            var databaseKvs = await DatabaseKeyValueStore.GetAll();

            foreach (var kv in databaseKvs)
            {
                SetLocal(kv.Key, kv.Value);
            }

            if (ConfigManager.GetSettingsBool(ConfigManager.Setting.vmenu_kvs_sync_local))
            {
                try
                {
                    var localNonDatabaseKvs = localKvs
                        .Where(kv => !databaseKvs.ContainsKey(kv.Key))
                        .ToDictionary(kv => kv.Key, kv => kv.Value);
                    await DatabaseKeyValueStore.SetAll(localNonDatabaseKvs);
                }
                catch (MySqlException e)
                {
                    Debug.WriteLine($"Error setting multiple keys in database key-value store: {e.Message}");
                }
            }
        }

        public async static Task Remove(string key)
        {
            RemoveLocal(key);
            try
            {
                await DatabaseKeyValueStore.Remove(key);
            }
            catch (MySqlException e)
            {
                Debug.WriteLine($"Error removing key \"{key}\" from database key-value store: {e.Message}");
            }
        }

        private async static Task Set(string key, ValueInfo vi)
        {
            SetLocal(key, vi);
            await DatabaseKeyValueStore.Set(key, vi);
            try
            {
                await DatabaseKeyValueStore.Set(key, vi);
            }
            catch (MySqlException e)
            {
                Debug.WriteLine($"Error setting \"{key}={vi.Value}\" in database key-value store: {e.Message}");
            }
        }

        public async static Task Set(string key, string value) => await Set(key, new ValueInfo(value));
    }
}
