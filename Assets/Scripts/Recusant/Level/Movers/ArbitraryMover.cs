using UnityEngine;
using KinematicCharacterController;
using Netick.Unity;
using Netick;

namespace Recusant
{
    [ExecuteBefore(typeof(PlayerNetworkInput))]
    public abstract class ArbitraryMover : NetworkBehaviour, IMoverController
    {
        protected Vector3 OriginalPosition { get; private set; }
        protected Quaternion OriginalRotation { get; private set; }

        private PhysicsMover _mover;

        public Transform RenderTransform;

        private void Awake()
        {
            _mover = GetComponent<PhysicsMover>();
            _mover.MoverController = this;

            OriginalPosition = _mover.Rigidbody.position;
            OriginalRotation = _mover.Rigidbody.rotation.normalized;
        }

        protected abstract void ProvideMovement(out Vector3 position, out Quaternion rotation, float deltaTime);

        void IMoverController.UpdateMovement(out Vector3 position, out Quaternion rotation, float deltaTime)
        {
            if (Sandbox == null)
            {
                position = OriginalPosition;
                rotation = OriginalRotation;
                return;
            }

            if (IsOwner)
            {
                ProvideMovement(out position, out rotation, deltaTime);
            }
            else
            {
                position = RenderTransform.position;
                rotation = RenderTransform.rotation;
            }
        }
    }
}
