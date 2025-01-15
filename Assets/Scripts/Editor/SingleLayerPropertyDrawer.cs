#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;

[CustomPropertyDrawer(typeof(SingleLayer))]
public class SingleLayerPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        using (new EditorGUI.PropertyScope(position, label, property))
        {
            position = EditorGUI.PrefixLabel(position, label);

            var valueProperty = property.FindPropertyRelative("m_Value");

            valueProperty.intValue = EditorGUI.LayerField(position, valueProperty.intValue);
        }
    }
}

#endif

