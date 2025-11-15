using Unary.Core;
using Netick;
using UnityEngine;

namespace Unary.Recusant
{
    public class PlayerNetworkInfo : NetworkBehaviourExtended
    {
        [Networked]
        public int Fps { get; set; } = 0;

        [Networked]
        public int Ping { get; set; } = 0;

        [Networked]
        public float BandwithIn { get; set; } = 0.0f;

        [Networked]
        public float BandwithOut { get; set; } = 0.0f;

        [Networked]
        public float PacketLossIn { get; set; } = 0.0f;

        [Networked]
        public float PacketLossOut { get; set; } = 0.0f;

        [Networked]
        public NetworkBool Connected { get; set; } = false;

        [OnChanged(nameof(Connected))]
        public void ConnectedChanged(OnChangedData _)
        {
            OnConnected.Publish(Connected.ToBool());
        }

        public EventFunc<bool> OnConnected { get; } = new();

        private ServerConnection _connection;
        private NetworkPeer _peer;

        public override void OnInputSourceLeft()
        {
            _connection = null;
            _peer = null;
            Sandbox.Destroy(Object);
        }

        public override void NetworkDestroy()
        {
            PlayerManager.Instance.RemovePlayer(InputSourcePlayerId, NetworkObject);
        }

        private bool _added = false;

        public override void NetworkUpdate()
        {
            if (!_added)
            {
                _added = true;
                PlayerManager.Instance.AddPlayer(InputSourcePlayerId, NetworkObject);
            }

            if (GetInput(out PlayerNetworkInput input))
            {
                input.Fps = Mathf.RoundToInt(PerformanceManager.Instance.Fps);
                SetInput(input);
            }
        }

        public override void NetworkFixedUpdate()
        {
            if (FetchInputServer(out PlayerNetworkInput input))
            {
                if (!Connected)
                {
                    Connected = true;
                }

                if (_connection == null && InputSource is ServerConnection Connection)
                {
                    _connection = Connection;
                }

                if (_peer == null && InputSource is NetworkPeer Peer)
                {
                    _peer = Peer;
                }

                Fps = input.Fps;

                if (_connection != null)
                {
                    Ping = Mathf.RoundToInt((float)_connection.RTT.Average * 1000.0f) / 2;
                    PacketLossIn = _connection.InPacketLoss;
                    PacketLossOut = _connection.OutPacketLoss;
                    // These are intentionnaly reversed.
                    // Netick always considers that "out" means FROM SERVER, 
                    // while in my use case it means that it comes FROM CURRENT PLAYER
                    BandwithIn = _connection.BytesOut.Avg / 1000.0f;
                    BandwithOut = _connection.BytesIn.Avg / 1000.0f;
                }
                else
                {
                    Ping = 0;
                    PacketLossIn = 0.0f;
                    PacketLossOut = 0.0f;
                    BandwithIn = 0.0f;
                    BandwithOut = 0.0f;
                }

                if (_peer != null)
                {
                    BandwithIn = _peer.InKBps;
                    BandwithOut = _peer.OutKBps;
                }
            }
        }
    }
}
