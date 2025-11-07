using System.Collections.Generic;
using UnityEngine;

namespace Unary.Recusant
{
    public class AiBoundVisualizer : MonoBehaviour
    {
#if UNITY_EDITOR
        public int Index = -1;

        public AiBoundData Data = null;

        public static Vector3 SlightlyUp = new(0.0f, 0.25f, 0.0f);

        private HashSet<Triangle.TriangleGizmoDrawOrder> _drawOrders = null;
        private static Vector3 _size = new(AiBoundData.Size - 0.05f, AiBoundData.Size - 0.05f, AiBoundData.Size - 0.05f);

        public void OnDrawGizmos()
        {
            CompiledLevelData data = CompiledLevelDataEditor.Instance.Data;

            if (data == null || Index == -1)
            {
                return;
            }

            Data = data.AiBounds[Index];

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(Data.Position, _size);
            Gizmos.DrawSphere(Data.Position, 0.2f);
        }

        public void OnDrawGizmosSelected()
        {
            CompiledLevelData data = CompiledLevelDataEditor.Instance.Data;

            if (data == null || Index == -1)
            {
                return;
            }

            Triangle.BuildDrawOrders(ref data.AiTriangles, ref data.AiTriangleVertices, Data.RootTriangle, ref _drawOrders, Data.Triangles);

            if (_drawOrders == null)
            {
                return;
            }

            foreach (var drawOrder in _drawOrders)
            {
                Gizmos.color = drawOrder.Color;
                Gizmos.DrawLine(drawOrder.Start, drawOrder.End);
            }
        }
#endif
    }
}
