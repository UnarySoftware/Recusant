using Netick;
using Netick.Unity;
using UnityEngine;

namespace Recusant
{
    [Networked]
    public struct PlayerNetworkSentInput : INetworkInput
    {
        // Movement
        [Networked]
        public int AiTriangle { get; set; }
        [Networked]
        public Quaternion Rotation { get; set; }
        [Networked]
        public Vector3 Position { get; set; }
        [Networked]
        public NetworkObjectRef Mover { get; set; }
        [Networked]
        public NetworkBool Teleporting { get; set; }
        [Networked]
        public NetworkBool Grounded { get; set; }
    }

    public class PlayerNetworkInput : NetworkBehaviour
    {
        private PlayerCharacterController _pawnController = null;
        private PlayerFlow _flow = null;

        private Vector3 _aoiVector;

        private ServerConnection _connection = null;

        private PlayerNetworkData _data = null;

        public override void NetworkStart()
        {
            _data = GetComponent<PlayerNetworkData>();

            int cellSize = NetworkManager.AreaOfInterestCellSize;

            _aoiVector = new Vector3(cellSize * 3.0f, cellSize * 3.0f, cellSize * 3.0f);

            _pawnController = GetComponent<PlayerCharacterController>();
            _flow = GetComponent<PlayerFlow>();
        }

        public override void OnInputSourceLeft()
        {
            _connection = null;
        }

        public override void NetworkUpdate()
        {
            if (!IsInputSource)
            {
                return;
            }

            var Input = Sandbox.GetInput<PlayerNetworkSentInput>();
            Input.Rotation = _pawnController.Head.transform.rotation;

            if (_pawnController.Mover != null)
            {
                Input.Mover = _pawnController.Mover.GetRef();
                Input.Position = _pawnController.transform.position - _pawnController.Mover.transform.position;
            }
            else
            {
                Input.Mover = new NetworkObjectRef(null);
                Input.Position = _pawnController.transform.position;
            }

            Input.Teleporting |= _pawnController.CheckTeleportNetwork();
            Input.Grounded = _pawnController.Motor.GroundingStatus.FoundAnyGround;

            Sandbox.SetInput(Input);
        }

        public override void NetworkFixedUpdate()
        {
            if (Sandbox.IsServer && FetchInput(out PlayerNetworkSentInput input))
            {
                if (!_data.Connected)
                {
                    _data.Connected = true;
                    _data.Teleporting = true;
                }
                else
                {
                    _data.Teleporting = input.Teleporting;
                }

                AiTriangleData[] triangles = LevelManager.Instance.LevelData.AiTriangles;

                _flow.AiTriangle = Mathf.Clamp(input.AiTriangle, 0, triangles.Length - 1);
                _data.Rotation = input.Rotation;
                _data.Position = input.Position;
                _data.Mover = input.Mover;

                _data.IsGrounded = input.Grounded;

                if (_connection == null)
                {
                    if (InputSource is ServerConnection Connection)
                    {
                        _connection = Connection;
                    }
                }

                if (_connection != null)
                {
                    _data.Ping = (float)_connection.RTT.Average * 1000.0f;
                }
                else
                {
                    _data.Ping = 0.0f;
                }

                InputSource.AddInterestBoxArea(new Bounds(transform.position, _aoiVector));
            }
        }
    }
}
