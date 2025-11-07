using Unary.Core;
using Netick.Unity;
using UnityEngine;

namespace Unary.Recusant
{
    public class PlayerNetworkVisual : NetworkBehaviourExtended
    {
        [SerializeField]
        private ScriptableObjectRef<PlayerNetworkVisualData> _data;

        private PlayerNetworkInfo _networkInfo = null;
        private PlayerCharacterController _pawnController = null;

        private void Awake()
        {
            _data.Precache();
        }

        public override void NetworkStart()
        {
            _networkInfo = GetComponent<PlayerNetworkInfo>();
            _pawnController = GetComponent<PlayerCharacterController>();

            GetComponent<PlayerNetworkInfo>().OnConnected.Subscribe(OnConnectedChanged, this);

            if (Sandbox.IsServer)
            {
                gameObject.name = "Player_" + InputSource.PlayerId.ToString();
            }

            if (IsInputSource)
            {
                CameraManager.Instance.CurrentCamera = _pawnController.Head.GetComponentInChildren<Camera>();
                gameObject.AddComponent<AudioListener>();
            }
        }

        public override void NetworkDestroy()
        {
            if (IsInputSource)
            {
                Destroy(gameObject.GetComponent<AudioListener>());
            }

            GetComponent<PlayerNetworkInfo>().OnConnected.Unsubscribe(this);
        }

        public bool OnConnectedChanged(ref bool data)
        {
            if (data)
            {
                _pawnController.MeshRoot.SetActive(true);

                if (IsInputSource)
                {
                    LoadingManager.Instance.HideLoading(typeof(GameplayState));
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

            if (_networkInfo.Connected)
            {
                Vector3 Euler = _pawnController.Rotation.eulerAngles;
                _pawnController.Head.transform.localEulerAngles = new Vector3(Euler.x, 0, 0);
                _pawnController.MeshRoot.transform.rotation = Quaternion.Euler(new Vector3(0, Euler.y, 0));

                Vector3 Target = _pawnController.Position;

                NetworkObject TargetMover = _pawnController.NetworkedMover.GetObject(Sandbox);

                if (TargetMover != null)
                {
                    Target = TargetMover.transform.position + _pawnController.Position;
                }

                if (_pawnController.Teleporting || Vector3.Distance(_pawnController.transform.position, Target) >= _data.Value.SmoothingTeleportRange)
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
