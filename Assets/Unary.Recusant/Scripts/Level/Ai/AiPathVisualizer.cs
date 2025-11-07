using UnityEngine;

namespace Unary.Recusant
{
    public class AiPathVisualizer : MonoBehaviour
    {
#if UNITY_EDITOR
        public Vector3[] Path = null;

        public void OnDrawGizmos()
        {
            if (Path == null)
            {
                return;
            }

            for (int i = 0; i < Path.Length; i++)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawSphere(Path[i] + Vector3.up, 0.25f);
                Gizmos.DrawLine(Path[i] + Vector3.up, Path[i]);

                if (i + 1 < Path.Length)
                {
                    Gizmos.DrawLine(Path[i] + Vector3.up, Path[i + 1] + Vector3.up);
                }
            }
        }
#endif
    }
}
