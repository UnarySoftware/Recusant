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

        [AssetInject("Assets/Recusant/Other/LiteNet.asset")]
        public LiteNetLibTransportProvider OfflineTransportProvider;

        [AssetInject("Assets/Recusant/Other/Steamworks.asset")]
        public SteamworksTransportProvider OnlineTransportProvider;

        public bool IsRunning { get; private set; } = false;

        private NetickConfig _config;

        public NetworkSandbox Sandbox { get; private set; } = null;

        [HideInInspector]
        public bool IsServer { get; private set; } = false;

        [HideInInspector]
        public bool IsClient { get; private set; } = false;

        public const int AreaOfInterestCellSize = 125;

        private void InitConfig()
        {
            _config = Network.CloneDefaultConfig();

            // General

            _config.TickRate = 20;
            _config.ServerDivisor = 1;
            _config.MaxPlayers = 4;
            _config.MaxObjects = 2048;
            _config.MaxAdditiveScenes = 1;
            _config.PhysicsPrediction = false;
            _config.PhysicsType = PhysicsType.Physics3D;
            _config.InputReuseAtLowFPS = true;
            _config.InvokeUpdate = true;
            _config.InvokeRenderInHeadless = false;
            _config.RenderInvokeOrder = NetworkRenderInvokeOrder.LateUpdate;
            _config.EnableLogging = true;
            _config.EnableProfiling = false;

            // Interest Management

            _config.EnableInterestManagement = true;
            _config.EnableNarrowphaseFiltering = false;
            _config.CustomGroupCount = 0;
            _config.WorldSize = new(5000.0f, 1.0f, 5000.0f);
            _config.AoILayerCount = 1;
            _config.AoILayer0CellSize = AreaOfInterestCellSize;
            _config.RenderWorldGrid = false;

            // Lag Compensation

            _config.EnableLagCompensation = false;

            // Advanced

            _config.MaxSendableDataSize = 50000;
            _config.StateAllocatorBlockSize = 131072 * 4;
            _config.MetaAllocatorBlockSize = 1048576 * 4;
            _config.FastSerialization = true;
            _config.EnableMultithreading = false;
            _config.AggressivePreAllocation = false;
            _config.MaxAllowedTimestep = 0.1f;
            _config.MaxPredictedTicks = 16;
            _config.IncludeInactiveObjects = false;

            // TODO Load additional mods
            List<GameObject> prefabs = ContentLoader.Instance.LoadAssets<GameObject>("PrefabsNetwork");

            List<NetworkObject> objects = new();

            foreach (var prefab in prefabs)
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

            _config.Prefabs = objects;
        }

        public override void Initialize()
        {

        }

        public override void PostInitialize()
        {
            InitConfig();
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
                Sandbox = Network.StartAsHost(OnlineTransportProvider, OnlineProviderPort, _sandboxPrefab, _config);
            }
            else
            {
                Sandbox = Network.StartAsHost(OfflineTransportProvider, 53495, _sandboxPrefab, _config);
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
                Sandbox = Network.StartAsClient(OnlineTransportProvider, UnityEngine.Random.Range(53000, 55000), _sandboxPrefab, _config);
            }
            else
            {
                Sandbox = Network.StartAsClient(OfflineTransportProvider, 53555, _sandboxPrefab, _config);
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
