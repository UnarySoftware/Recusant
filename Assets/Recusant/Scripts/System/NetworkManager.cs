using Core;
using Netick;
using Netick.Transport;
using Netick.Transports.Steamworks;
using Netick.Unity;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Recusant
{
    public class NetworkManager : System<NetworkManager>
    {
        public int OnlineProviderPort { get; set; } = 0;

        [SystemAssetInject("other/litenet.asset")]
        public LiteNetLibTransportProvider OfflineTransportProvider;

        [SystemAssetInject("other/steamworks.asset")]
        public SteamworksTransportProvider OnlineTransportProvider;

        public bool IsRunning { get; private set; } = false;

        public NetworkSandbox Sandbox { get; private set; } = null;

        [HideInInspector]
        public bool IsServer { get; private set; } = false;

        [HideInInspector]
        public bool IsClient { get; private set; } = false;

        public const int AreaOfInterestCellSize = 125;

        public GameObject ResolveGameObject(GameObject unresolvedObject)
        {
            string path = ContentLoader.Instance.GetInstancePath(unresolvedObject);

            if(_resolver.TryGetValue(path, out var resolved))
            {
                return resolved;
            }

            return null;
        }

        // TODO Implement general pooling solution
        Dictionary<string, GameObject> _resolver = new();

        private NetickConfig InitConfig()
        {
            NetickConfig config = Network.CloneDefaultConfig();

            // General

            config.TickRate = 20;
            config.ServerDivisor = 1;
            config.MaxPlayers = 4;
            config.MaxObjects = 2048;
            config.MaxAdditiveScenes = 1;
            config.PhysicsPrediction = false;
            config.PhysicsType = PhysicsType.Physics3D;
            config.InputReuseAtLowFPS = true;
            config.InvokeUpdate = true;
            config.InvokeRenderInHeadless = false;
            config.RenderInvokeOrder = NetworkRenderInvokeOrder.LateUpdate;
            config.EnableLogging = true;
            config.EnableProfiling = false;

            // Interest Management

            config.EnableInterestManagement = true;
            config.EnableNarrowphaseFiltering = false;
            config.CustomGroupCount = 0;
            config.WorldSize = new(5000.0f, 1.0f, 5000.0f);
            config.AoILayerCount = 1;
            config.AoILayer0CellSize = AreaOfInterestCellSize;
            config.RenderWorldGrid = false;

            // Lag Compensation

            config.EnableLagCompensation = false;

            // Advanced

            config.MaxSendableDataSize = 50000;
            config.StateAllocatorBlockSize = 131072 * 4;
            config.MetaAllocatorBlockSize = 1048576 * 4;
            config.FastSerialization = true;
            config.EnableMultithreading = false;
            config.AggressivePreAllocation = false;
            config.MaxAllowedTimestep = 0.1f;
            config.MaxPredictedTicks = 16;
            config.IncludeInactiveObjects = false;

            _resolver.Clear();

            List<string> paths = ContentLoader.Instance.GetAssetPaths("PrefabsNetwork");
            List<NetworkObject> objects = new();

            foreach (var path in paths)
            {
                GameObject prefab = ContentLoader.Instance.LoadAsset<GameObject>(path);
                _resolver[path] = prefab;
                objects.Add(prefab.GetComponent<NetworkObject>());
            }

            var entries = Systems.Instance.GetSystemEntries();

            foreach (var entry in entries)
            {
                if (entry.EntryType == Systems.EntryType.NetworkPrefab)
                {
                    objects.Add(entry.NetworkPrefab.GetComponent<NetworkObject>());
                }
            }

            config.Prefabs = objects;

            return config;
        }

        public override void Initialize()
        {

        }

        public override void PostInitialize()
        {

        }

        public override void Deinitialize()
        {
            if (IsRunning)
            {
                Unsubscribe();
                Network.Shutdown();
            }
        }

        public string GetDeviceName(bool asClient = false)
        {
            if (asClient)
            {
                return Environment.UserName + "-Client";
            }
            else
            {
                return Environment.UserName + "-Host";
            }
        }

        private void Subscribe()
        {
            Sandbox.Events.OnConnectRequest += OnConnectRequest;
            Sandbox.Events.OnPlayerJoined += OnPlayerJoined;
            Sandbox.Events.OnPlayerLeft += OnPlayerLeft;
            Sandbox.Events.OnSceneOperationBegan += OnSceneOperationBegan;
            Sandbox.Events.OnSceneOperationDone += OnSceneOperationDone;
        }

        private void Unsubscribe()
        {
            Sandbox.Events.OnConnectRequest -= OnConnectRequest;
            Sandbox.Events.OnPlayerJoined -= OnPlayerJoined;
            Sandbox.Events.OnPlayerLeft -= OnPlayerLeft;
            Sandbox.Events.OnSceneOperationBegan -= OnSceneOperationBegan;
            Sandbox.Events.OnSceneOperationDone -= OnSceneOperationDone;
        }

        public void StartHost()
        {
            bool startOnline = true;

#if UNITY_EDITOR
            if (!Launcher.Data.Online)
            {
                startOnline = false;
            }
#endif

            GameObject _sandboxPrefab = new();
            _sandboxPrefab.AddComponent<NetickSceneHandler>();

            if (startOnline)
            {
                OnlineProviderPort = UnityEngine.Random.Range(53000, 55000);
                Sandbox = Network.StartAsHost(OnlineTransportProvider, OnlineProviderPort, _sandboxPrefab, InitConfig());
            }
            else
            {
                Sandbox = Network.StartAsHost(OfflineTransportProvider, 53495, _sandboxPrefab, InitConfig());
            }

            Subscribe();
            Sandbox.LoadCustomSceneAsync(NetickSceneHandler.Instance.GetSceneIndex("Quarry"), new() { loadSceneMode = LoadSceneMode.Single, localPhysicsMode = LocalPhysicsMode.Physics3D });
            IsRunning = true;
            IsServer = true;
        }

        public void StartClient()
        {
            bool startOnline = true;

#if UNITY_EDITOR
            if (!Launcher.Data.Online)
            {
                startOnline = false;
            }
#endif

            GameObject _sandboxPrefab = new();
            _sandboxPrefab.AddComponent<NetickSceneHandler>();

            if (startOnline)
            {
                Sandbox = Network.StartAsClient(OnlineTransportProvider, UnityEngine.Random.Range(53000, 55000), _sandboxPrefab, InitConfig());
            }
            else
            {
                Sandbox = Network.StartAsClient(OfflineTransportProvider, 53555, _sandboxPrefab, InitConfig());
            }

            Subscribe();

            GameplayVariables.Instance.ResetToOriginal();

            SaveManager.Instance.State.Replicated.Name = GetDeviceName(true);

            if (startOnline && SteamLobbyManager.Instance.GotLobby)
            {
                Sandbox.Connect(OnlineProviderPort, SteamMatchmaking.GetLobbyOwner(SteamLobbyManager.Instance.Lobby).ToString());
            }
            else
            {
                Sandbox.Connect(53495, "127.0.0.1");
            }

            IsRunning = true;
            IsClient = true;
        }

        public void OnConnectRequest(NetworkSandbox sandbox, NetworkConnectionRequest request)
        {
            NetworkConnectionRequestEvent.Instance.Publish(sandbox, request);
        }

        public void OnPlayerJoined(NetworkSandbox sandbox, NetworkPlayerId id)
        {
            NetworkPlayerJoinedEvent.Instance.Publish(sandbox, id);
        }
        
        // TODO Remove all suppress messages
        public void OnPlayerLeft(NetworkSandbox sandbox, NetworkPlayerId id)
        {
            NetworkPlayerLeftEvent.Instance.Publish(sandbox, id);
        }

        private void OnSceneOperationDone(NetworkSandbox sandbox, NetworkSceneOperation sceneOperation)
        {
            NetworkSceneOperationEvent.Instance.Publish(NetworkSceneOperationType.Done, sandbox, sceneOperation);
        }

        private void OnSceneOperationBegan(NetworkSandbox sandbox, NetworkSceneOperation sceneOperation)
        {
            Ui.Instance.GoForward(typeof(LoadingState));

            NetworkSceneOperationEvent.Instance.Publish(NetworkSceneOperationType.Began, sandbox, sceneOperation);
        }
    }
}
