#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace Core.Editor
{
    [CustomPropertyDrawer(typeof(SerializableGuid))]
    public class SerializableGuidDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            using (new EditorGUI.PropertyScope(position, label, property))
            {
                position = EditorGUI.PrefixLabel(position, label);

                SerializableGuid target = (SerializableGuid)property.boxedValue;

                if (target == null)
                {
                    return;
                }

                EditorGUI.LabelField(position, target.Value.ToString());
            }
        }
    }
}

#endif
