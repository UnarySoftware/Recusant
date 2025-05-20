using HighlightPlus;
using Netick;
using Netick.Unity;
using UnityEngine;

namespace Recusant
{
    public class PlayerNetworkVisual : NetworkBehaviour
    {
        private PlayerNetworkData _data = null;
        private PlayerCharacterController _pawnController = null;

        public static GameplayVariableRanged<float, float> SmoothingTeleportRange = new(
        GameplayGroup.Server, GameplayFlag.Replicated, 30.0f, 0.0001f, 99999.0f,
        "Range at which proxy player would teleport instead of interpolation");

        public override void NetworkStart()
        {
            _data = GetComponent<PlayerNetworkData>();
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
            if (data.Value && data.Root.gameObject == gameObject)
            {
                _pawnController.MeshRoot.SetActive(true);

                if (IsInputSource)
                {
                    Ui.Instance.GoForward(typeof(GameplayState));
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

            if (_data.Connected)
            {
                Vector3 Euler = _data.Rotation.eulerAngles;
                _pawnController.Head.transform.localEulerAngles = new Vector3(Euler.x, 0, 0);
                _pawnController.MeshRoot.transform.rotation = Quaternion.Euler(new Vector3(0, Euler.y, 0));

                Vector3 Target = _data.Position;

                NetworkObject TargetMover = _data.Mover.GetObject(Sandbox);

                if (TargetMover != null)
                {
                    Target = TargetMover.transform.position + _data.Position;
                }

                if (_data.Teleporting || Vector3.Distance(_pawnController.transform.position, Target) >= SmoothingTeleportRange.Get())
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
