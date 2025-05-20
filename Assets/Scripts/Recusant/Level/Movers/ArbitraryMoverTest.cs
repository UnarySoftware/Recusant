using UnityEngine;

namespace Recusant
{
    public class ArbitraryMoverTest : ArbitraryMover
    {
        public Vector3 TranslationAxis = Vector3.right;
        public float TranslationPeriod = 10;
        public float TranslationSpeed = 1;
        public Vector3 RotationAxis = Vector3.up;
        public float RotSpeed = 10;
        public Vector3 OscillationAxis = Vector3.zero;
        public float OscillationPeriod = 10;
        public float OscillationSpeed = 10;

        private float Time = 0.0f;

        protected override void ProvideMovement(out Vector3 position, out Quaternion rotation, float deltaTime)
        {
            Time += deltaTime;

            position = (OriginalPosition + (Mathf.Sin(Time * TranslationSpeed) * TranslationPeriod * TranslationAxis.normalized));

            Quaternion targetRotForOscillation = Quaternion.Euler(OscillationAxis.normalized * (Mathf.Sin(Time * OscillationSpeed) * OscillationPeriod)) * OriginalRotation;

            Quaternion resulting = Quaternion.Euler(RotSpeed * Time * RotationAxis) * targetRotForOscillation;

            rotation = resulting.normalized;
        }
    }
}
