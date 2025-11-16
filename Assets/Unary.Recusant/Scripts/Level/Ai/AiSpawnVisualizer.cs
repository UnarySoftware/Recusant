using UnityEngine;

namespace Unary.Recusant
{
    public class AiSpawnVisualizer : MonoBehaviour
    {
#if UNITY_EDITOR

        public void OnDrawGizmos()
        {
            GizmosAddition.DrawCapsule(transform.position, Quaternion.identity, Color.lightPink);
        }

        public void OnDrawGizmosSelected()
        {
            GizmosAddition.DrawCapsule(transform.position, Quaternion.identity, Color.antiqueWhite);
        }
#endif
    }
}
