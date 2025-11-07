using Netick;
using Netick.Unity;
using UnityEngine;

namespace Unary.Recusant
{
    [Networked]
    public struct PlayerNetworkInput : INetworkInput
    {
        // PlayerFlow

        [Networked]
        public int AiTriangle { get; set; }

        // PlayerCharacterController

        [Networked]
        public Quaternion Rotation { get; set; }
        [Networked]
        public Vector3 Position { get; set; }
        [Networked]
        public NetworkObjectRef Mover { get; set; }
        [Networked]
        public NetworkBool Teleporting { get; set; }

        // PlayerExploder

        [Networked]
        public NetworkBool Exploding { get; set; }

        // PlayerNetworkInfo

        [Networked]
        public int Fps { get; set; }

        // PlayerFlashlight

        [Networked]
        public NetworkBool Flashlight { get; set; }
    }
}
