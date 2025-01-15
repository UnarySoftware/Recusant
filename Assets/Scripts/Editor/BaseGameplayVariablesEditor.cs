#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

public class BaseGameplayVariablesEditor<T> : Editor
{
    protected string[] FieldNames;
    private AbstractVariable[] _values;

    private GUIContent[] _contentKeys;
    private GUIContent _contentValue = null;

    private static readonly GUILayoutOption[] _options = new[] { GUILayout.ExpandWidth(true) };

    virtual protected void OnEnable()
    {
        var Type = typeof(T);

        _values = new AbstractVariable[FieldNames.Length];

        _contentKeys = new GUIContent[FieldNames.Length];

        for (int i = 0; i < FieldNames.Length; i++)
        {
            _values[i] = (AbstractVariable)Type.GetField(FieldNames[i]).GetValue(null);

            _contentKeys[i] = new(" " + FieldNames[i], EditorGUIUtility.IconContent("d_Animation.Record").image);
        }
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        for (int i = 0; i < FieldNames.Length; i++)
        {
            EditorGUILayout.BeginHorizontal();

            if (_values[i].IsRanged())
            {
                _contentValue = new(string.Format("{0,-20} {1}", _values[i].GetRanges(), _values[i].GetObject().ToString()));
            }
            else
            {
                _contentValue = new(_values[i].GetObject().ToString());
            }

            EditorGUILayout.LabelField(_contentKeys[i], _contentValue, _options);

            EditorGUILayout.EndHorizontal();
        }
    }
}

#endif
