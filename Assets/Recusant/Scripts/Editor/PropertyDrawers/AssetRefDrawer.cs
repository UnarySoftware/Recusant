#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using Core;
using System.Threading;

namespace Recusant.Editor
{
    [CustomPropertyDrawer(typeof(AssetRef<>))]
    [CustomPropertyDrawer(typeof(ScriptableObjectRef<>))]
    [CustomPropertyDrawer(typeof(PrefabRef<>))]
    public class AssetRefDrawer : PropertyDrawer
    {
        private static Type componentType = typeof(Component);
        private static Type gameObjectType = typeof(GameObject);

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
            foreach (var targetObject in property.serializedObject.targetObjects)
            {
                EditorUtility.SetDirty(targetObject);
            }
        }

        AssetRefBase _objectBase;
        UnityEngine.Object _selection;
        SerializedProperty _property;

        private void OnSelected(bool prefab)
        {
            if (prefab)
            {
                _selection = PrefabSearchWindow.SelectedObject;
            }

            string assetPath = AssetDatabase.GetAssetPath(_selection).Replace("\\", "/");
            _objectBase.AssetId.Value = AssetDatabase.GUIDFromAssetPath(assetPath).ToSystem();
            _objectBase.SetValueInternal(_selection);
            _property.boxedValue = _objectBase;
            MarkDirty(_property);

            if (prefab)
            {
                _property.serializedObject.ApplyModifiedProperties();
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

            _objectBase = (AssetRefBase)property.boxedValue;

            if (_objectBase == null)
            {
                return;
            }

            _property = property;

            object internalObject = _objectBase.GetValueInternal();

            if (internalObject != null)
            {
                targetObject = (UnityEngine.Object)internalObject;
            }
            else if (internalObject == null && !_objectBase.AssetId.IsDefault())
            {
                Type loadType = targetType;

                if (targetType.IsSubclassOf(componentType))
                {
                    loadType = gameObjectType;
                }

                targetObject = AssetDatabase.LoadAssetByGUID(_objectBase.AssetId.Value.ToUnity(), loadType);

                if (targetObject == null)
                {
                    Debug.LogError("We have failed to resolve ScriptingObject with GUID \"" + _objectBase.AssetId.Value + "\"");
                    _objectBase.AssetId.Value = Guid.Empty;
                    property.boxedValue = _objectBase;
                    MarkDirty(property);
                    return;
                }

                _objectBase.SetValueInternal(targetObject);
            }

            _selection = null;
            bool changesCheck = false;

            if (targetType.IsSubclassOf(componentType))
            {
                if(PrefabSearchWindow.IsOpened)
                {
                    ReadonlyObjectField.Draw(position, targetObject, new(property.displayName), true);
                }
                else
                {
                    ReadonlyObjectField.Draw(position, targetObject, new(property.displayName), false, () =>
                    {
                        PrefabSearchWindow.OnSelected = OnSelected;
                        PrefabSearchWindow.TargetType = targetType;
                        PrefabSearchWindow window = EditorWindow.GetWindow<PrefabSearchWindow>("Prefab Search");
                    });
                }
            }
            else
            {
                changesCheck = true;
                EditorGUI.BeginChangeCheck();
                _selection = EditorGUI.ObjectField(position, property.displayName, targetObject, targetType, false);
            }

            if (changesCheck && EditorGUI.EndChangeCheck())
            {
                OnSelected(false);
            }
        }
    }
}

#endif
