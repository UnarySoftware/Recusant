using Netick;
using Netick.Unity;
using System.Collections.Generic;
using Unary.Core;
using UnityEngine;

namespace Unary.Recusant
{
    public class PlayerManagerShared : SystemShared
    {

    }

    public class PlayerManager : SystemNetworkRoot<PlayerManager, PlayerManagerShared>
    {
        [SystemAssetInject("prefabsnetwork/player.prefab")]
        public AssetRef<GameObject> PlayerPrefab;

        [HideInInspector]
        public NetworkPlayerId LocalPlayer { get; private set; }

        public struct PlayerData
        {
            public PlayerNetworkInfo Info;
            public PlayerIdentity Identity;
            public GameObject GameObject;
        };

        [HideInInspector]
        private readonly Dictionary<NetworkPlayerId, PlayerData> _players = new();

        public Dictionary<NetworkPlayerId, PlayerData> Players
        {
            get
            {
                return _players;
            }
        }

        private int _spawnCounter = 0;
        private readonly List<PlayerSpawnPoint> _spawnPoints = new();

        public override void Initialize()
        {
            NetworkManager.Instance.OnNetworkConnectionRequest.Subscribe(ConnectionRequest, this);
            LevelManager.Instance.OnStartNetwork.Subscribe(OnLevelLoaded, this);
            Steam.Instance.OnIdentityUpdate.Subscribe(OnIdentityUpdate, this);
        }

        public override void PostInitialize()
        {

        }

        public override void Deinitialize()
        {
            NetworkManager.Instance.OnNetworkConnectionRequest.Unsubscribe(this);
            LevelManager.Instance.OnStartNetwork.Unsubscribe(this);
            Steam.Instance.OnIdentityUpdate.Unsubscribe(this);
        }

        private bool OnIdentityUpdate(ref Steam.PersonaStateChangeData data)
        {
            foreach (var player in _players)
            {
                if (player.Value.Identity.SteamId == data.SteamId.m_SteamID)
                {
                    data.PlayerId = player.Key;
                    break;
                }
            }

            return true;
        }

        public void RegisterSpawn(PlayerSpawnPoint spawn)
        {
            _spawnPoints.Add(spawn);
        }

        public void UnregisterSpawn(PlayerSpawnPoint spawn)
        {
            _spawnCounter = 0;
            _spawnPoints.Remove(spawn);
        }

        private bool ConnectionRequest(ref NetworkManager.NetworkConnectionRequestData data)
        {
            data.Request.Accept();
            return true;
        }

        public void SpawnPlayer(NetworkSandbox sandbox, NetworkPlayerId id, string name = null)
        {
            Vector3 position = _spawnPoints[_spawnCounter].transform.position;
            Quaternion rotation = _spawnPoints[_spawnCounter].GetValidRotation();

            var spawnedPlayer = sandbox.NetworkInstantiate(PoolManager.Instance.GetAvailable(PlayerPrefab), position, rotation, id);

            if (name != null)
            {
                spawnedPlayer.GetComponent<PlayerIdentity>().OfflineName = name;
            }

            _spawnCounter++;

            if (_spawnCounter >= _spawnPoints.Count)
            {
                _spawnCounter = 0;
            }

            GameObject gameObject = spawnedPlayer.gameObject;

            sandbox.SetPlayerObject(id, gameObject.GetComponent<NetworkObject>());
        }

        private bool OnLevelLoaded(ref LevelManager.LevelEventData data)
        {
            var sandbox = NetworkManager.Instance.Sandbox;

            if (sandbox.LocalPlayer.PlayerId == 0)
            {
                SpawnPlayer(sandbox, sandbox.LocalPlayer.PlayerId);
            }
            else
            {
                SaveState state = SaveManager.Instance.State;
                string ourName = state.Replicated.Name;
                PlayerManagerNetwork.Instance.SendSpawnInfo(ourName, state.Replicated.Health);
            }

            return true;
        }

        public struct PlayerChangedData
        {
            public NetworkPlayerId Id;
            public GameObject GameObject;
        }

        // Fired after player has been instantiated, added to the scene
        // and all components ran their Start/NetworkStart methods already
        public EventFunc<PlayerChangedData> OnPlayerAdded { get; } = new();
        // Fired after player object got removed from the scene
        public EventFunc<PlayerChangedData> OnPlayerRemoved { get; } = new();

        public void AddPlayer(NetworkPlayerId id, NetworkObject networkObject)
        {
            if (_players.ContainsKey(id))
            {
                Core.Logger.Instance.Error($"Tried adding player with id {id} that was already registered in PlayerManager");
                return;
            }

            if (networkObject.IsInputSource)
            {
                LocalPlayer = networkObject.InputSourcePlayerId;
            }

            PlayerData newData = new()
            {
                Info = networkObject.GetComponent<PlayerNetworkInfo>(),
                Identity = networkObject.GetComponent<PlayerIdentity>(),
                GameObject = networkObject.gameObject
            };

            _players[id] = newData;

            OnPlayerAdded.Publish(new()
            {
                Id = id,
                GameObject = networkObject.gameObject
            });

            if (Steam.Initialized)
            {
                Steam.Instance.RequestUserInformation(new(newData.Identity.SteamId));
            }
        }

        public void RemovePlayer(NetworkPlayerId id, NetworkObject networkObject)
        {
            if (!_players.ContainsKey(id))
            {
                Core.Logger.Instance.Error($"Tried removing player with id {id} that was not registered in PlayerManager");
                return;
            }

            OnPlayerRemoved.Publish(new()
            {
                Id = id,
                GameObject = networkObject.gameObject
            });

            if (networkObject.IsInputSource)
            {
                LocalPlayer = default;
            }

            _players.Remove(id);
        }
    }
}
