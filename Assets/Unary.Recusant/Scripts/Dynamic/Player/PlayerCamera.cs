using Unary.Core;
using UnityEngine;

namespace Unary.Recusant
{
    public class PlayerCamera : NetworkBehaviourExtended
    {
        public Camera Head;
        public Camera PlayerCameraLayer;

        public ScriptableObjectRef<PlayerCameraData> Data;

        // TODO Move this to game settings
        public float Sensitivity = 1.0f;

        private Vector3 _rotation = Vector3.forward;
        private float _vertical = 0.0f;

        private void Awake()
        {
            Data.Precache();
        }

        public override void NetworkStart()
        {
            if (!IsInputSource)
            {
                return;
            }

            Head.enabled = true;
            Head.farClipPlane = RenderSettings.fogEndDistance + 100.0f;

            PlayerCameraLayer.enabled = true;
        }

        public void UpdateWithMover(Quaternion rotation, Vector3 characterUp)
        {
            _rotation = rotation * _rotation;
            _rotation = Vector3.ProjectOnPlane(_rotation, characterUp).normalized;
        }

        public void UpdateWithRotation(Quaternion rotation)
        {
            var angles = rotation * Vector3.forward;
            _rotation = angles;
            _vertical = Mathf.Clamp(angles.y, Data.Value.MinVerticalAngle, Data.Value.MaxVerticalAngle);
            Head.transform.rotation = rotation;
        }

        public void UpdateWithInput(Vector2 rotationInput)
        {
            Quaternion rotationFromInput = Quaternion.Euler(transform.up * (rotationInput.x * Sensitivity));
            _rotation = rotationFromInput * _rotation;
            _rotation = Vector3.Cross(transform.up, Vector3.Cross(_rotation, transform.up));
            Quaternion planarRot = Quaternion.LookRotation(_rotation, transform.up);

            _vertical -= (rotationInput.y * Sensitivity);
            _vertical = Mathf.Clamp(_vertical, Data.Value.MinVerticalAngle, Data.Value.MaxVerticalAngle);
            Quaternion verticalRot = Quaternion.Euler(_vertical, 0, 0);

            Head.transform.rotation = planarRot * verticalRot;
        }
    }
}
