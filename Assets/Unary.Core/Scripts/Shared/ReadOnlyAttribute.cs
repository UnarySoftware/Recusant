using UnityEngine;

#if UNITY_EDITOR

using UnityEditor;

#endif

namespace Unary.Core
{
    public class ReadOnlyPropertyAttribute : PropertyAttribute
    {

    }

#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(ReadOnlyPropertyAttribute))]
    public class ReadOnlyPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var previousGUIState = GUI.enabled;
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label);
            GUI.enabled = previousGUIState;
        }
    }

#endif

}
