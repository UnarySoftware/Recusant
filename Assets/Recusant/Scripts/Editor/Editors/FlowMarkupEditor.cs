#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace Recusant.Editor
{
    [CustomEditor(typeof(AiMarkup)), CanEditMultipleObjects]
    public class FlowMarkupEditor : UnityEditor.Editor
    {
        private BoxBoundsHandle _handle = null;

        private void SetupHandle()
        {
            _handle = new BoxBoundsHandle
            {
                handleColor = Color.white,
                wireframeColor = Color.clear
            };
        }

        // the OnSceneGUI callback uses the Scene view camera for drawing handles by default
        protected virtual void OnSceneGUI()
        {
            if (_handle == null)
            {
                SetupHandle();
            }

            AiMarkup boundsExample = (AiMarkup)target;

            _handle.center = boundsExample.transform.position;
            _handle.size = boundsExample.Size;

            EditorGUI.BeginChangeCheck();
            _handle.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(boundsExample, "Change Bounds");
                boundsExample.transform.position = _handle.center;
                boundsExample.Size = _handle.size;
            }
        }
    }
}

#endif
