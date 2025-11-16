using Netick;
using Netick.Transport;
using Netick.Transports.Steamworks;
using Netick.Unity;
using Steamworks;
using System;
using System.Collections.Generic;
using Unary.Core;
using UnityEngine;

namespace Unary.Recusant
{
    public class NetworkManager : System<NetworkManager>
    {
        // This is hardcoded for a reason. Only change if you are fully aware of the repercussions.
        public const int MaxPlayerCount = 4;

        public int OnlineProviderPort { get; set; } = 0;

        [SystemAssetInject("other/netickscenehandler.prefab")]
        private readonly AssetRef<GameObject> _sandboxPrefab = default;

        [SystemAssetInject("other/litenet.asset")]
        private readonly AssetRef<LiteNetLibTransportProvider> _offlineTransportProvider = default;

        [SystemAssetInject("other/steamworks.asset")]
        private readonly AssetRef<SteamworksTransportProvider> _onlineTransportProvider = default;

        public bool IsRunning { get; private set; } = false;

        public NetworkSandbox Sandbox { get; private set; } = null;

        [HideInInspector]
        public bool IsServer { get; private set; } = false;

        [HideInInspector]
        public bool IsClient { get; private set; } = false;

        public const int AreaOfInterestCellSize = 125;

        private NetickConfig InitConfig()
        {
            NetickConfig config = ScriptableObject.CreateInstance<NetickConfig>();

            // General

            config.TickRate = 20;
            config.ServerDivisor = 1;
            config.MaxPlayers = MaxPlayerCount;
            config.MaxObjects = 2048;
            config.MaxAdditiveScenes = 1;
            config.PhysicsPrediction = false;
            config.PhysicsType = PhysicsType.Physics3D;
            config.InputReuseOnLowFPS = false;
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
            config.StateAllocatorBlockSize = 131072 * MaxPlayerCount;
            config.MetaAllocatorBlockSize = 1048576 * MaxPlayerCount;
            config.FastSerialization = true;
            config.EnableMultithreading = false;
            config.AggressivePreAllocation = false;
            config.MaxAllowedTimestep = 0.2f;
            config.MaxPredictedTicks = 16;
            config.IncludeInactiveObjects = false;

            List<GameObject> networkedPrefabs = PoolManager.Instance.GetNetworkedPrefabs();
            List<NetworkObject> objects = new();

            foreach (var prefab in networkedPrefabs)
            {
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
                Sandbox = null;
                Network.ShutdownImmediately();
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
            Sandbox.Events.OnConnectRequest += ConnectRequest;
            Sandbox.Events.OnPlayerJoined += PlayerConnected;
            Sandbox.Events.OnPlayerLeft += PlayerDisconnected;
            Sandbox.Events.OnSceneOperationBegan += SceneOperationBegan;
            Sandbox.Events.OnSceneOperationDone += SceneOperationDone;
            Sandbox.Events.OnStartup += Startup;
        }

        private void Unsubscribe()
        {
            Sandbox.Events.OnConnectRequest -= ConnectRequest;
            Sandbox.Events.OnPlayerJoined -= PlayerConnected;
            Sandbox.Events.OnPlayerLeft -= PlayerDisconnected;
            Sandbox.Events.OnSceneOperationBegan -= SceneOperationBegan;
            Sandbox.Events.OnSceneOperationDone -= SceneOperationDone;
            Sandbox.Events.OnStartup -= Startup;
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

            if (startOnline)
            {
                OnlineProviderPort = UnityEngine.Random.Range(53000, 55000);
                Sandbox = Network.StartAsHost(_onlineTransportProvider.Value, OnlineProviderPort, _sandboxPrefab.Value, InitConfig());
            }
            else
            {
                Sandbox = Network.StartAsHost(_offlineTransportProvider.Value, 53495, _sandboxPrefab.Value, InitConfig());
            }

            Subscribe();
            LevelManager.Instance.LoadLevelNetworked("train");
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

            if (startOnline)
            {
                Sandbox = Network.StartAsClient(_onlineTransportProvider.Value, UnityEngine.Random.Range(53000, 55000), _sandboxPrefab.Value, InitConfig());
            }
            else
            {
                Sandbox = Network.StartAsClient(_offlineTransportProvider.Value, 53555, _sandboxPrefab.Value, InitConfig());
            }

            Subscribe();

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

        public struct NetworkConnectionRequestData
        {
            public NetworkSandbox Sandbox;
            public NetworkConnectionRequest Request;
        }

        public EventFunc<NetworkConnectionRequestData> OnNetworkConnectionRequest { get; } = new();

        public void ConnectRequest(NetworkSandbox sandbox, NetworkConnectionRequest request)
        {
            OnNetworkConnectionRequest.Publish(new()
            {
                Sandbox = sandbox,
                Request = request
            });
        }

        public struct PlayerChangedData
        {
            public NetworkSandbox Sandbox;
            public NetworkPlayerId Id;
        }

        // Fired when player connected, but didnt join the game yet
        public EventFunc<PlayerChangedData> OnPlayerConnected { get; } = new();
        // Fired when player disconnected from the game entirely
        public EventFunc<PlayerChangedData> OnPlayerDisconnected { get; } = new();

        private void PlayerConnected(NetworkSandbox sandbox, NetworkPlayerId id)
        {
            OnPlayerConnected.Publish(new()
            {
                Sandbox = sandbox,
                Id = id
            });
        }

        private void PlayerDisconnected(NetworkSandbox sandbox, NetworkPlayerId id)
        {
            OnPlayerDisconnected.Publish(new()
            {
                Sandbox = sandbox,
                Id = id
            });
        }

        public struct NetworkSceneOperationData
        {
            public NetworkSandbox Sandbox;
            public NetworkSceneOperation SceneOperation;
        }

        public EventFunc<NetworkSceneOperationData> OnSceneOperationBegan { get; } = new();
        public EventFunc<NetworkSceneOperationData> OnSceneOperationDone { get; } = new();

        private void SceneOperationBegan(NetworkSandbox sandbox, NetworkSceneOperation sceneOperation)
        {
            LoadingManager.Instance.ShowLoading();

            OnSceneOperationBegan.Publish(new()
            {
                Sandbox = sandbox,
                SceneOperation = sceneOperation
            });
        }

        private void SceneOperationDone(NetworkSandbox sandbox, NetworkSceneOperation sceneOperation)
        {
            LoadingManager.Instance.AddJob("Waiting for the server to respond");

            OnSceneOperationDone.Publish(new()
            {
                Sandbox = sandbox,
                SceneOperation = sceneOperation
            });
        }

        private void Startup(NetworkSandbox sandbox)
        {
            PoolManager.Instance.InitializeNetworkedPools(sandbox);
        }
    }
}
