using Netick.Unity;
using UnityEngine;

namespace Recusant
{
    [RequireComponent(typeof(PlayerCharacterController))]
    public class PlayerUnstucker : NetworkBehaviour
    {
        private PlayerCharacterController _pawnController = null;

        public override void NetworkStart()
        {
            if (!IsInputSource)
            {
                return;
            }

            _pawnController = GetComponent<PlayerCharacterController>();
        }

        public override void NetworkUpdate()
        {
            if (!IsInputSource)
            {
                return;
            }


        }
    }
}
