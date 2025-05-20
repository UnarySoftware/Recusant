using UnityEngine;

namespace Recusant
{
    public class MoaiMover : MonoBehaviour
    {

        void Start()
        {

        }

        private GameObject _player = null;

        // Update is called once per frame
        void Update()
        {
            if (PlayerManager.Instance == null)
            {
                return;
            }

            if (PlayerManager.Instance.LocalPlayer == null)
            {
                return;
            }

            if (_player == null)
            {
                _player = PlayerManager.Instance.LocalPlayer.gameObject;
            }

            Vector3 position = _player.transform.position;

            position.y = transform.position.y;

            transform.LookAt(position);
        }
    }
}
