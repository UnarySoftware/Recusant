using Unary.Core;
using UnityEngine;

namespace Unary.Recusant
{
    [RequireComponent(typeof(PlayerCharacterController))]
    public class PlayerUnstucker : NetworkBehaviourExtended
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
