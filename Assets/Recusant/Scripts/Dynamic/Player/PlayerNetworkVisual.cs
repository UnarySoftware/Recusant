using HighlightPlus;
using Netick;
using Netick.Unity;
using UnityEngine;

namespace Recusant
{
    public class PlayerNetworkVisual : NetworkBehaviour
    {
        [SerializeField]
        private ScriptableObjectRef<PlayerNetworkVisualData> _data;

        private PlayerNetworkData _networkData = null;
        private PlayerCharacterController _pawnController = null;

        private void Awake()
        {
            _data.Precache();
        }

        public override void NetworkStart()
        {
            _networkData = GetComponent<PlayerNetworkData>();
            _pawnController = GetComponent<PlayerCharacterController>();

            PlayerConnectedChangedEvent.Instance.Subscribe(OnConnectedChanged, this);

            if (Sandbox.IsServer)
            {
                gameObject.name = "Player_" + InputSource.PlayerId.ToString();
            }

            if (IsInputSource)
            {
                CameraManager.Instance.CurrentCamera = _pawnController.Head.GetComponentInChildren<Camera>();
            }
        }

        public override void NetworkDestroy()
        {
            PlayerConnectedChangedEvent.Instance.Unsubscribe(this);
        }

        public bool OnConnectedChanged(PlayerConnectedChangedEvent data)
        {
            if (data.Value && data.Root != null)
            {
                _pawnController.MeshRoot.SetActive(true);

                if (IsInputSource)
                {
                    UiManager.Instance.GoForward(typeof(GameplayState));
                }
                else
                {
                    GetComponent<PlayerOverhead>().SetName(GetComponent<PlayerNetworkData>());
                }
            }

            return true;
        }

        public override void NetworkRender()
        {
            if (IsInputSource)
            {
                return;
            }

            if (_networkData.Connected)
            {
                Vector3 Euler = _networkData.Rotation.eulerAngles;
                _pawnController.Head.transform.localEulerAngles = new Vector3(Euler.x, 0, 0);
                _pawnController.MeshRoot.transform.rotation = Quaternion.Euler(new Vector3(0, Euler.y, 0));

                Vector3 Target = _networkData.Position;

                NetworkObject TargetMover = _networkData.Mover.GetObject(Sandbox);

                if (TargetMover != null)
                {
                    Target = TargetMover.transform.position + _networkData.Position;
                }

                if (_networkData.Teleporting || Vector3.Distance(_pawnController.transform.position, Target) >= _data.Value.SmoothingTeleportRange)
                {
                    _pawnController.transform.position = Target;
                }
                else
                {
                    _pawnController.transform.position = Vector3.Lerp(_pawnController.transform.position, Target, 0.1f);
                }
            }
        }
    }
}
