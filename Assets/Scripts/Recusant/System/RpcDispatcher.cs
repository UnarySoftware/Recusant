using Core;
using Netick;
using Netick.Unity;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Recusant
{
    public class RpcDispatcher : System<RpcDispatcher>
    {
        public GameObject RpcRelayPrefab;

        private readonly Dictionary<int, RpcRelay> _idToRelay = new();

        public override void Initialize()
        {
            NetworkPlayerConnectedEvent.Instance.Subscribe(PlayerConnected, this);
            NetworkPlayerDisconnectedEvent.Instance.Subscribe(PlayerDisconnected, this);
        }

        public override void PostInitialize()
        {

        }

        public override void Deinitialize()
        {
            NetworkPlayerConnectedEvent.Instance.Unsubscribe(this);
            NetworkPlayerDisconnectedEvent.Instance.Unsubscribe(this);
        }

        private bool PlayerConnected(NetworkPlayerConnectedEvent data)
        {
            if (data.Player.PlayerId == 0)
            {
                return true;
            }

            var spawnedRelay = data.Sandbox.NetworkInstantiate(RpcRelayPrefab, Vector3.zero, Quaternion.identity, data.Player);
            _idToRelay[data.Player.PlayerId] = spawnedRelay.gameObject.GetComponent<RpcRelay>();

            return true;
        }

        private bool PlayerDisconnected(NetworkPlayerDisconnectedEvent data)
        {
            if (data.Player.PlayerId == 0)
            {
                return true;
            }

            RpcRelay relay = _idToRelay[data.Player.PlayerId];
            _idToRelay.Remove(data.Player.PlayerId);
            data.Sandbox.Destroy(relay.GetComponent<NetworkObject>());

            return true;
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
}
