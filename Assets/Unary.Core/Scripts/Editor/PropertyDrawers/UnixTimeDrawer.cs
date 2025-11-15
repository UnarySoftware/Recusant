#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

namespace Unary.Core.Editor
{
    [CustomPropertyDrawer(typeof(UnixTime))]
    public class UnixTimeDrawer : PropertyDrawer
    {
        private void MarkDirty(SerializedProperty property)
        {
            foreach (var targetObject in property.serializedObject.targetObjects)
            {
                EditorUtility.SetDirty(targetObject);
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            using (new EditorGUI.PropertyScope(position, label, property))
            {
                position = EditorGUI.PrefixLabel(position, label);

                UnixTime target = (UnixTime)property.boxedValue;

                DateTimeOffset fromUnixSeconds = DateTimeOffset.FromUnixTimeSeconds(target.Value);

                EditorGUI.LabelField(position, fromUnixSeconds.ToString("HH:mm:ss dd.MM.yyyy"));

                Rect setNowRect = new()
                {
                    x = position.xMax - 60,
                    y = position.y,
                    width = 60,
                    height = position.height
                };

                if (GUI.Button(setNowRect, "Set Now"))
                {
                    target.SetNow();
                    property.boxedValue = target;
                    MarkDirty(property);
                    property.serializedObject.ApplyModifiedProperties();
                }

                Rect resetRect = new()
                {
                    x = position.xMax - 120,
                    y = position.y,
                    width = 60,
                    height = position.height
                };

                if (GUI.Button(resetRect, "Reset"))
                {
                    target.Reset();
                    property.boxedValue = target;
                    MarkDirty(property);
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
        }
    }
}

#endif
