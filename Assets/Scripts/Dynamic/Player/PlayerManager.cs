using Netick;
using Netick.Unity;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class PlayerManagerShared : CoreSystemShared
{

}

public class PlayerManager : CoreSystemNetwork<PlayerManager, PlayerManagerShared>
{
    public GameObject PlayerPrefab;

    [HideInInspector]
    public PlayerNetwork LocalPlayer { get; private set; } = null;

    [HideInInspector]
    private List<PlayerNetwork> _players = new();

    [InitDependency(typeof(Networking))]
    public override void Initialize()
    {
        Networking.Instance.ConnectRequest += ConnectionRequest;
        Networking.Instance.LevelLoaded += LevelLoaded;
    }

    public override void PostInitialize()
    {

    }

    public override void Deinitialize()
    {
        Networking.Instance.ConnectRequest -= ConnectionRequest;
        Networking.Instance.LevelLoaded -= LevelLoaded;
    }

    private void ConnectionRequest(NetworkSandbox sandbox, NetworkConnectionRequest request)
    {
        request.AllowConnection = true;
    }

    public void SpawnPlayer(NetworkSandbox sandbox, NetworkPlayer player, string name = "")
    {
        Vector3 SpawnPos = new();
        var spawnPos = SpawnPos + Vector3.left * ((1 + sandbox.ConnectedPlayers.Count) * 1.1f);
        var spawnedPlayer = sandbox.NetworkInstantiate(PlayerPrefab, spawnPos, Quaternion.identity, player);

        if (player.PlayerId == 0)
        {
            spawnedPlayer.gameObject.GetComponent<PlayerNetwork>().Name = Networking.Instance.GetDeviceName(false);
        }
        else
        {
            spawnedPlayer.gameObject.GetComponent<PlayerNetwork>().Name = name;
        }

        player.PlayerObject = spawnedPlayer.gameObject;
    }

    private void LevelLoaded(string name, LevelRoot root)
    {
        var sandbox = Networking.Instance.Sandbox;

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
    }

    public void AddPlayer(PlayerNetwork player)
    {
        if(player.IsInputSource)
        {
            LocalPlayer = player;
        }

        _players.Add(player);
    }

    public void RemovePlayer(PlayerNetwork player)
    {
        if (player.IsInputSource)
        {
            LocalPlayer = null;
        }

        _players.Remove(player);
    }
}
