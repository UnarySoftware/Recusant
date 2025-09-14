#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using Core;

namespace Recusant.Editor
{
    [CustomPropertyDrawer(typeof(ScriptableObjectRef<>))]
    public class ScriptableObjectRefDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        private Type GetGenericType(SerializedProperty property)
        {
            if (property.boxedValue == null)
            {
                return null;
            }

            Type type = property.boxedValue.GetType();

            if (!type.IsGenericType)
            {
                return null;
            }

            TypeInfo typeInfo = type.GetTypeInfo();

            return typeInfo.GenericTypeArguments[0];
        }

        private void MarkDirty(SerializedProperty property)
        {
            foreach(var targetObject in property.serializedObject.targetObjects)
            {
                EditorUtility.SetDirty(targetObject);
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property == null)
            {
                return;
            }

            Type targetType = GetGenericType(property);

            if (targetType == null)
            {
                return;
            }

            UnityEngine.Object targetObject = null;

            ScriptableObjectBase objectBase = (ScriptableObjectBase)property.boxedValue;

            if (objectBase == null)
            {
                return;
            }

            object internalObject = objectBase.GetValueInternal();

            if (internalObject != null)
            {
                targetObject = (UnityEngine.Object)internalObject;
            }
            else if (internalObject == null && !objectBase.UniqueId.IsDefault())
            {
                targetObject = AssetDatabase.LoadAssetByGUID(objectBase.UniqueId.Value.ToUnity(), targetType);

                if (targetObject == null)
                {
                    Debug.LogError("We have failed to resolve ScriptingObject with GUID \"" + objectBase.UniqueId.Value + "\"");
                    objectBase.UniqueId.Value = Guid.Empty;
                    property.boxedValue = objectBase;
                    MarkDirty(property);
                    return;
                }

                objectBase.SetValueInternal(targetObject);
            }

            EditorGUI.BeginChangeCheck();

            UnityEngine.Object selection = EditorGUI.ObjectField(position, property.displayName, targetObject, targetType, false);

            if (EditorGUI.EndChangeCheck())
            {
                string assetPath = AssetDatabase.GetAssetPath(selection).Replace("\\", "/");
                objectBase.UniqueId.Value = AssetDatabase.GUIDFromAssetPath(assetPath).ToSystem();
                objectBase.SetValueInternal(selection);
                property.boxedValue = objectBase;
                MarkDirty(property);
            }
        }
    }
}

#endif
