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

using vMenuShared;

namespace vMenuClient
{
    internal static class RemoteKeyValueStore
    {
        private static ulong _nextId = 0;
        public static ulong NewId() => _nextId++;

        private static RemoteKeyValueStoreUpdates updates = new();
        private static bool isSyncing;

        private static Dictionary<ulong, Response> responses = new Dictionary<ulong, Response>();

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
            if (isSyncing || !updates.HasUpdates)
            {
                return;
            }
            isSyncing = true;

            var currUpdates = updates;
            updates = new();

            var request = new Request
            {
                Id = NewId(),
                Type = Request.RequestType.UpdateMany,
                DataUpdateMany = currUpdates.ToRequest(),
            };
            var response = await SendRequest(request);

            bool ok = response.Type != Response.ResponseType.Error;
            if (!ok)
            {
                Debug.WriteLine($"ERROR: UpdateMany: {response.Error}");
                updates.MergeOlderIntoThis(currUpdates);
            }

            isSyncing = false;
        }

        public static void RemoveDelayed(string key)
        {
            updates.Remove(key);
        }

        public static void SetDelayed(string key, ValueInfo vi)
        {
            updates.Set(key, vi);
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
                if (localNonServerKvs.Count > 0)
                {
                    foreach (var kv in localNonServerKvs)
                    {
                        RemoteKeyValueStore.SetDelayed(kv.Key, kv.Value);
                    }
                    await RemoteKeyValueStore.SyncUnsynced();
                }
            }

            return remoteDiffLocal.Any();
        }

        public static void Remove(string key)
        {
            RemoveLocal(key);
            RemoteKeyValueStore.RemoveDelayed(key);
        }

        private static void SetDelayed(string key, ValueInfo vi)
        {
            SetLocal(key, vi);
            RemoteKeyValueStore.SetDelayed(key, vi);
        }

        public static void Set(string key, string value) => SetDelayed(key, new ValueInfo(value));
        public static void Set(string key, int value) => SetDelayed(key, new ValueInfo(value));
        public static void Set(string key, float value) => SetDelayed(key, new ValueInfo(value));
    }
}
