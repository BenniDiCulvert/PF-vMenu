using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CitizenFX.Core;

using Newtonsoft.Json;

using static CitizenFX.Core.Native.API;
using static vMenuClient.CommonFunctions;
using static vMenuClient.KeyValueStore;
using static vMenuShared.KeyValueStoreSync;
using static vMenuShared.ConfigManager;

namespace vMenuClient
{
    internal static class RemoteKeyValueStore
    {
        private static ulong _nextId = 0;
        public static ulong NewId() => _nextId++;

        private static Dictionary<string, ValueInfo> notYetSyncedValues = [];

        private static Dictionary<ulong, Response> responses = new Dictionary<ulong, Response>();

        private static void UnionUnsyncedValues(Dictionary<string, ValueInfo> kvs1, Dictionary<string, ValueInfo> kvs2)
        {
            foreach (var kv in kvs2)
            {
                kvs1[kv.Key] = kv.Value;
            }
        }

        public static void ReceiveResponse(string json)
        {
            var response = JsonConvert.DeserializeObject<Response>(json);
            responses[response.Id] = response;
        }

        private static async Task<Response> SendRequest(Request request)
        {
            var json = JsonConvert.SerializeObject(request);
            BaseScript.TriggerServerEvent($"vMenu:ServerKeyValueStoreRequest", json);

            Response response;
            while (!responses.TryGetValue(request.Id, out response))
            {
                await Delay(0);
            }

            responses.Remove(response.Id);
            return response;
        }

        public static async Task SyncUnsynced()
        {
            await SetAll([]);
        }

        public static async Task Remove(string key)
        {
            notYetSyncedValues.Remove(key);

            var request = new Request
            {
                Id = NewId(),
                Type = Request.RequestType.Remove,
                DataRemove = new Request.RequestDataRemove
                {
                    Key = key
                }
            };
            var response = await SendRequest(request);
            if (response.Type == Response.ResponseType.Error)
            {
                Debug.WriteLine($"Error removing \"{key}\" from remote key-value store: {response.Error}");
            }
        }

        public static async Task Set(string key, ValueInfo vi)
        {
            await SetAll(new() { { key, vi } });
        }

        public static void SetDelayed(string key, ValueInfo vi)
        {
            notYetSyncedValues[key] = vi;
        }

        private static bool isSetAllInProgress = false;

        public static async Task SetAll(Dictionary<string, ValueInfo> keyValues)
        {
            UnionUnsyncedValues(notYetSyncedValues, keyValues);

            if (notYetSyncedValues.Count == 0)
            {
                return;
            }
            if (isSetAllInProgress)
            {
                Debug.WriteLine("INFO: KVS was not synced with server because another sync is still in progress, retrying later");
                return;
            }

            isSetAllInProgress = true;

            var notYetSyncedValuesNow = notYetSyncedValues;
            notYetSyncedValues = [];

            var request = new Request
            {
                Id = NewId(),
                Type = Request.RequestType.SetAll,
                DataSetAll = new Request.RequestDataSetAll
                {
                    KeyValues = notYetSyncedValuesNow,
                }
            };
            var response = await SendRequest(request);
            if (response.Type == Response.ResponseType.Error)
            {
                Debug.WriteLine($"ERROR: SetAll: {response.Error}");
                UnionUnsyncedValues(notYetSyncedValuesNow, notYetSyncedValues);
                notYetSyncedValues = notYetSyncedValuesNow;
            }

            isSetAllInProgress = false;
        }

        public static async Task<Dictionary<string, ValueInfo>> GetAll()
        {
            var request = new Request
            {
                Id = NewId(),
                Type = Request.RequestType.GetAll,
                DataGetAll = new Request.RequestDataGetAll
                {
                }
            };
            var response = await SendRequest(request);
            switch (response.Type)
            {
                case Response.ResponseType.Error:
                    Debug.WriteLine($"ERROR: GetAll: {response.Error}");
                    goto case Response.ResponseType.NoServerStore;
                case Response.ResponseType.NoServerStore:
                    return new Dictionary<string, ValueInfo>();
                case Response.ResponseType.Ok:
                    return response.DataGetAll?.KeyValues;
                default:
                    throw new InvalidOperationException("ERROR: GetAll: Invalid response type received from server.");
            }
        }
    }

    public static partial class KeyValueStore
    {
        public static async Task<bool> SyncWithServer()
        {
            var localKvs = GetAll();
            var remoteKvs = await RemoteKeyValueStore.GetAll();

            var remoteDiffLocal = remoteKvs.Where(kv =>
                !localKvs.ContainsKey(kv.Key) ||
                localKvs[kv.Key].Value != kv.Value.Value ||
                localKvs[kv.Key].Type != kv.Value.Type);

            foreach (var kv in remoteKvs)
            {
                SetLocal(kv.Key, kv.Value);
            }

            if (GetSettingsBool(Setting.vmenu_kvs_sync_local))
            {
                var localNonServerKvs = localKvs
                    .Where(kv => !remoteKvs.ContainsKey(kv.Key))
                    .ToDictionary(kv => kv.Key, kv => kv.Value);
                await RemoteKeyValueStore.SetAll(localNonServerKvs);
            }

            return remoteDiffLocal.Any();
        }

        public static async Task RemoveAsync(string key)
        {
            RemoveLocal(key);
            await RemoteKeyValueStore.Remove(key);
        }
        public static void Remove(string key) => _ = RemoveAsync(key);

        private static void SetDelayed(string key, ValueInfo vi)
        {
            SetLocal(key, vi);
            RemoteKeyValueStore.SetDelayed(key, vi);
        }

        public static async Task SetAsync(string key, string value)
        {
            SetLocal(key, value);
            await RemoteKeyValueStore.Set(key, new ValueInfo(value));
        }
        public static void Set(string key, string value) => SetDelayed(key, new ValueInfo(value));

        public static async Task SetAsync(string key, int value)
        {
            SetLocal(key, value);
            await RemoteKeyValueStore.Set(key, new ValueInfo(value));
        }
        public static void Set(string key, int value) => SetDelayed(key, new ValueInfo(value));

        public static async Task SetAsync(string key, float value)
        {
            SetLocal(key, value);
            await RemoteKeyValueStore.Set(key, new ValueInfo(value));
        }
        public static void Set(string key, float value) => SetDelayed(key, new ValueInfo(value));
    }
}
