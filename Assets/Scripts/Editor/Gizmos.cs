using UnityEditor;
using UnityEngine;

public static class DrawArrow
{
    public static void Draw(Vector3 pos, Vector3 direction, Color color, float duration, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20.0f)
    {
        Debug.DrawRay(pos, direction, color, duration);

        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Debug.DrawRay(pos + direction, right * arrowHeadLength, color, duration);
        Debug.DrawRay(pos + direction, left * arrowHeadLength, color, duration);
    }

    public static void DrawWireCapsule(Vector3 point1, Vector3 point2, float radius)
    {
        Vector3 upOffset = point2 - point1;
        Vector3 up = upOffset.Equals(default) ? Vector3.up : upOffset.normalized;
        Quaternion orientation = Quaternion.FromToRotation(Vector3.up, up);
        Vector3 forward = orientation * Vector3.forward;
        Vector3 right = orientation * Vector3.right;
        // z axis
        Handles.DrawWireArc(point2, forward, right, 180, radius);
        Handles.DrawWireArc(point1, forward, right, -180, radius);
        Handles.DrawLine(point1 + right * radius, point2 + right * radius);
        Handles.DrawLine(point1 - right * radius, point2 - right * radius);
        // x axis
        Handles.DrawWireArc(point2, right, forward, -180, radius);
        Handles.DrawWireArc(point1, right, forward, 180, radius);
        Handles.DrawLine(point1 + forward * radius, point2 + forward * radius);
        Handles.DrawLine(point1 - forward * radius, point2 - forward * radius);
        // y axis
        Handles.DrawWireDisc(point2, up, radius);
        Handles.DrawWireDisc(point1, up, radius);
    }
}