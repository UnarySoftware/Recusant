using Unary.Core;
using TMPro;
using UnityEngine;

namespace Unary.Recusant
{
    public class PlayerOverhead : NetworkBehaviourExtended
    {
        [SerializeField]
        private GameObject _canvas;

        [SerializeField]
        private TMP_Text _label;

        private CameraManager Manager = null;

        public override void NetworkStart()
        {
            if (IsInputSource)
            {
                _canvas.SetActive(false);
            }
            else
            {
                _canvas.SetActive(true);

                if (Steam.Initialized)
                {
                    Steam.Instance.OnIdentityUpdate.Subscribe(OnIdentityUpdate, this);
                }
                else
                {
                    PlayerIdentity identity = GetComponent<PlayerIdentity>();
                    _label.text = identity.OfflineName;
                }
            }

            _canvas.transform.localScale = new Vector3(-1.0f, 1.0f, 1.0f);

            Manager = CameraManager.Instance;
        }

        public override void NetworkDestroy()
        {
            if (!IsInputSource && Steam.Initialized)
            {
                Steam.Instance.OnIdentityUpdate.Unsubscribe(this);
            }
        }

        private bool OnIdentityUpdate(ref Steam.PersonaStateChangeData data)
        {
            if (InputSourcePlayerId != data.PlayerId)
            {
                return true;
            }

            if (data.OnlineName != null)
            {
                _label.text = data.OnlineName;
            }

            return true;
        }

        public override void NetworkUpdate()
        {
            if (Manager.CurrentCamera != null)
            {
                _canvas.transform.LookAt(Manager.CurrentCamera.transform.position);
            }
        }
    }
}
