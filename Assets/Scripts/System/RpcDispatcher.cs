using Netick;
using Netick.Unity;
using System;
using System.Collections.Generic;
using UnityEngine;

public class RpcDispatcher : CoreSystem<RpcDispatcher>
{
    public GameObject RpcRelayPrefab;

    private readonly Dictionary<int, RpcRelay> _idToRelay = new();

    [InitDependency(typeof(Networking))]
    public override void Initialize()
    {
        Networking.Instance.PlayerConnected += PlayerConnected;
        Networking.Instance.PlayerDisconnected += PlayerDisconnected;
    }

    public override void PostInitialize()
    {

    }

    public override void Deinitialize()
    {

    }

    private void PlayerConnected(NetworkSandbox sandbox, NetworkPlayer player)
    {
        if (player.PlayerId == 0)
        {
            return;
        }

        var spawnedRelay = sandbox.NetworkInstantiate(RpcRelayPrefab, Vector3.zero, Quaternion.identity, player);
        _idToRelay[player.PlayerId] = spawnedRelay.gameObject.GetComponent<RpcRelay>();
    }

    private void PlayerDisconnected(NetworkSandbox sandbox, NetworkPlayer player, TransportDisconnectReason reason)
    {
        if (player.PlayerId == 0)
        {
            return;
        }

        _idToRelay.Remove(player.PlayerId);
    }

    public RpcRelay GetRelay(int id)
    {
        if (_idToRelay.TryGetValue(id, out RpcRelay result))
        {
            return result;
        }

        return null;
    }
}
