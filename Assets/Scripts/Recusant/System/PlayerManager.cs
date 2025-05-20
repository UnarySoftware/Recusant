using Core;
using Netick;
using Netick.Transports.Steamworks;
using Netick.Unity;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Recusant
{
    public class PlayerManagerShared : SystemShared
    {

    }

    public class PlayerManager : SystemNetworkRoot<PlayerManager, PlayerManagerShared>
    {
        [AssetInject("Assets/Recusant/PrefabsNetwork/Player.prefab")]
        public GameObject PlayerPrefab = null;

        [HideInInspector]
        public PlayerNetworkData LocalPlayer { get; private set; } = null;

        [HideInInspector]
        private List<PlayerNetworkData> _players = new();

        private int _spawnCounter = 0;
        private readonly List<PlayerSpawnPoint> _spawnPoints = new();

        public override void Initialize()
        {
            NetworkConnectionRequestEvent.Instance.Subscribe(ConnectionRequest, this);

            //NetworkManager.Instance.ConnectRequest.Register(this, OnConnectionRequest);

            // TODO Rename all LevelLoaded events into OnLevelChanged
            LevelEvent.Instance.Subscribe(OnLevelLoaded, this);
        }

        public override void PostInitialize()
        {

        }

        public override void Deinitialize()
        {
            NetworkConnectionRequestEvent.Instance.Unsubscribe(this);
            LevelEvent.Instance.Unsubscribe(this);
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

        private bool ConnectionRequest(NetworkConnectionRequestEvent data)
        {
            data.Request.AllowConnection = true;

            return true;
        }

        public void SpawnPlayer(NetworkSandbox sandbox, NetworkPlayer player, string name = "")
        {
            Vector3 position = _spawnPoints[_spawnCounter].transform.position;
            Quaternion rotation = _spawnPoints[_spawnCounter].GetValidRotation();

            var spawnedPlayer = sandbox.NetworkInstantiate(PlayerPrefab, position, rotation, player);

            spawnedPlayer.GetComponent<PlayerCharacterController>().Teleport(position, rotation);

            _spawnCounter++;

            if (_spawnCounter >= _spawnPoints.Count)
            {
                _spawnCounter = 0;
            }

            GameObject gameObject = spawnedPlayer.gameObject;

            PlayerNetworkData data = gameObject.GetComponent<PlayerNetworkData>();

            if (Steam.Initialized)
            {
                if (player.PlayerId == 0)
                {
                    data.SteamId = SteamUser.GetSteamID().m_SteamID;
                }
                else
                {
                    data.SteamId = SteamworksTransport.GetPlayerSteamID(player);
                }
            }
            else
            {
                if (player.PlayerId == 0)
                {
                    data.Name = NetworkManager.Instance.GetDeviceName(false);
                }
                else
                {
                    data.Name = name;
                }

                data.SteamId = 0;
            }

            player.PlayerObject = gameObject;
        }

        private bool OnLevelLoaded(LevelEvent data)
        {
            if (data.Type != LevelEventType.StartNetwork)
            {
                return true;
            }

            var sandbox = NetworkManager.Instance.Sandbox;

            if (sandbox.LocalPlayer.PlayerId == 0)
            {
                SpawnPlayer(sandbox, sandbox.LocalPlayer);
            }
            else
            {
                ClientState state = State.Client;
                string ourName = state.Replicated.Name;
                PlayerManagerNetwork.Instance.SendSpawnInfo(ourName, state.Replicated.Health);
            }

            return true;
        }

        public void AddPlayer(PlayerNetworkData player)
        {
            if (player.IsInputSource)
            {
                LocalPlayer = player;
            }

            _players.Add(player);

            PlayerChangedEvent.Instance.Publish(true, player.InputSource, player);
        }

        public void RemovePlayer(PlayerNetworkData player)
        {
            if (player.IsInputSource)
            {
                LocalPlayer = null;
            }

            _players.Remove(player);
            PlayerChangedEvent.Instance.Publish(false, player.InputSource, player);
        }
    }
}
