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

        private readonly Dictionary<NetworkPlayerId, RpcRelay> _idToRelay = new();

        public override void Initialize()
        {
            NetworkPlayerJoinedEvent.Instance.Subscribe(PlayerJoined, this);
            NetworkPlayerLeftEvent.Instance.Subscribe(PlayerLeft, this);
        }

        public override void PostInitialize()
        {

        }

        public override void Deinitialize()
        {
            NetworkPlayerJoinedEvent.Instance.Unsubscribe(this);
            NetworkPlayerLeftEvent.Instance.Unsubscribe(this);
        }

        private bool PlayerJoined(NetworkPlayerJoinedEvent data)
        {
            if (data.Id == 0)
            {
                return true;
            }

            var spawnedRelay = data.Sandbox.NetworkInstantiate(RpcRelayPrefab, Vector3.zero, Quaternion.identity, data.Sandbox.GetPlayerById(data.Id));
            _idToRelay[data.Id] = spawnedRelay.gameObject.GetComponent<RpcRelay>();

            return true;
        }

        private bool PlayerLeft(NetworkPlayerLeftEvent data)
        {
            if (data.Id == 0)
            {
                return true;
            }

            RpcRelay relay = _idToRelay[data.Id];
            _idToRelay.Remove(data.Id);
            data.Sandbox.Destroy(relay.GetComponent<NetworkObject>());

            return true;
        }

        public RpcRelay GetRelay(NetworkPlayerId id)
        {
            if (_idToRelay.TryGetValue(id, out RpcRelay result))
            {
                return result;
            }

            return null;
        }
    }
}
