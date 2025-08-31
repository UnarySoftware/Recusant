using Core;
using Steamworks;
using TMPro;
using UnityEngine;

namespace Recusant
{
    public class PlayerOverhead : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _label;

        private CameraManager Manager = null;

        private void Start()
        {
            _label.transform.localScale = new Vector3(-1.0f, 1.0f, 1.0f);

            Manager = CameraManager.Instance;
        }

        public void SetName(PlayerNetworkData playerData)
        {
            _label.enabled = true;

            if (Steam.Instance.Initialized)
            {
                _label.text = SteamFriends.GetFriendPersonaName(new(playerData.SteamId));
            }
            else
            {
                _label.text = playerData.Name;
            }
        }

        void Update()
        {
            if (Manager.CurrentCamera != null)
            {
                _label.transform.LookAt(Manager.CurrentCamera.transform.position);
            }
        }
    }
}
