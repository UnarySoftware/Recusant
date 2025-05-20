using Netick;
using Netick.Unity;
using UnityEngine;

namespace Recusant
{
    public class PlayerNetworkData : NetworkBehaviour
    {
        [Networked]
        [Smooth]
        public Quaternion Rotation { get; set; } = Quaternion.identity;

        [Networked]
        public Vector3 Position { get; set; } = Vector3.zero;

        [Networked]
        public NetworkObjectRef Mover { get; set; }

        [Networked]
        public NetworkBool Teleporting { get; set; }

        [Networked]
        public float Ping { get; set; }

        [Networked]
        public NetworkString32 Name { get; set; }

        [Networked]
        public ulong SteamId { get; set; }

        [Networked]
        public bool Connected { get; set; } = false;

        [OnChanged(nameof(Connected))]
        public void OnConnected(OnChangedData data)
        {
            PlayerConnectedChangedEvent.Instance.Publish(this, InputSource, data, Connected);
        }

        [Networked]
        public bool Leader { get; set; } = false;

        [OnChanged(nameof(Leader))]
        public void OnLeader(OnChangedData data)
        {
            PlayerLeaderChangedEvent.Instance.Publish(this, InputSource, data, Leader);
        }

        public bool IsGrounded { get; set; } = false;

        public override void NetworkStart()
        {
            PlayerManager.Instance.AddPlayer(this);
        }

        public override void OnInputSourceLeft()
        {
            PlayerManager.Instance.RemovePlayer(this);
            Sandbox.Destroy(Object);
        }
    }
}
