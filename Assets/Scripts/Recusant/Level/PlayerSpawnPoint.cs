using UnityEngine;

namespace Recusant
{
    public class PlayerSpawnPoint : MonoBehaviour
    {
        private void Awake()
        {
            if (NetworkManager.Instance.IsClient)
            {
                return;
            }

            PlayerManager.Instance.RegisterSpawn(this);
        }

        private void OnDestroy()
        {
            if (NetworkManager.Instance.IsClient)
            {
                return;
            }

            PlayerManager.Instance.UnregisterSpawn(this);
        }

        public Quaternion GetValidRotation()
        {
            return new Quaternion(0.0f, transform.rotation.y, 0.0f, transform.rotation.w); ;
        }

#if UNITY_EDITOR

        public static Color GizmoColor = new(0.25f, 0.85f, 0.45f, 1.0f);

        private void OnDrawGizmos()
        {
            GizmosAddition.DrawCapsule(transform.position + Vector3.up, Quaternion.identity, GizmoColor);
            GizmosAddition.DrawArrow(transform.position + Vector3.up, transform.rotation, GizmoColor);
        }

        private void OnDrawGizmosSelected()
        {
            transform.rotation = new Quaternion(0.0f, transform.rotation.y, 0.0f, transform.rotation.w);
            transform.localScale = Vector3.one;
        }
#endif

    }
}
