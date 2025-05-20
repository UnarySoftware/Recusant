using Netick.Unity;
using UnityEngine;

namespace Recusant
{
    public class PlayerCamera : NetworkBehaviour
    {
        public GameObject Head;

        public static GameplayVariableRanged<float, float> MinVerticalAngle = new(
            GameplayGroup.Client, GameplayFlag.None, -89.0f, -89.9f, 89.9f, "Minimal angle that local player camera can lower down to");

        public static GameplayVariableRanged<float, float> MaxVerticalAngle = new(
            GameplayGroup.Client, GameplayFlag.None, 89.0f, -89.9f, 89.9f, "Maximum angle that local player camera can go up to");

        public static GameplayVariableRanged<float, float> Sensitivity = new(
            GameplayGroup.Client, GameplayFlag.None, 1.0f, 0.0001f, 9999.9f, "Local player camera sensitivity");

        private Vector3 _rotation = Vector3.forward;
        private float _vertical = 0.0f;

        public override void NetworkStart()
        {
            if (!IsInputSource)
            {
                return;
            }

            Head.GetComponent<Camera>().farClipPlane = RenderSettings.fogEndDistance + 200.0f;
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
            _vertical = Mathf.Clamp(angles.y, MinVerticalAngle.Get(), MaxVerticalAngle.Get());
            Head.transform.rotation = rotation;
        }

        public void UpdateWithInput(Vector2 rotationInput)
        {
            Quaternion rotationFromInput = Quaternion.Euler(transform.up * (rotationInput.x * Sensitivity.Get()));
            _rotation = rotationFromInput * _rotation;
            _rotation = Vector3.Cross(transform.up, Vector3.Cross(_rotation, transform.up));
            Quaternion planarRot = Quaternion.LookRotation(_rotation, transform.up);

            _vertical -= (rotationInput.y * Sensitivity.Get());
            _vertical = Mathf.Clamp(_vertical, MinVerticalAngle.Get(), MaxVerticalAngle.Get());
            Quaternion verticalRot = Quaternion.Euler(_vertical, 0, 0);

            Head.transform.rotation = planarRot * verticalRot;
        }
    }
}
