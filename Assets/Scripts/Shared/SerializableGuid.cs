using System;
using UnityEngine;


#if UNITY_EDITOR

using UnityEditor;


[CustomPropertyDrawer(typeof(SerializableGuid))]
public class GuidDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        using (new EditorGUI.PropertyScope(position, label, property))
        {
            position = EditorGUI.PrefixLabel(position, label);

            SerializableGuid target = (SerializableGuid)property.boxedValue;

            EditorGUI.LabelField(position, target.Id.ToString());
        }
    }
}

#endif

[Serializable]
public class SerializableGuid
{
    private Guid _id = Guid.Empty;

    [HideInInspector]
    [SerializeField]
    private byte[] _bytes;

    public static implicit operator SerializableGuid(Guid value)
    {
        SerializableGuid result = new()
        {
            Id = value
        };
        return result;
    }

    public static implicit operator Guid(SerializableGuid value)
    {
        return value.Id;
    }

    private bool IsBytesEmpty()
    {
        if(_bytes == null || _bytes.Length != 16)
        {
            return true;
        }

        bool empty = true;
        foreach (var targetByte in _bytes)
        {
            if(targetByte != 0)
            {
                empty = false;
                break;
            }
        }
        return empty;
    }

    public Guid Id
    {
        get
        {
            if (IsBytesEmpty())
            {
                _bytes = Guid.NewGuid().ToByteArray();
                _id = new Guid(_bytes);
            }

            if (_id == Guid.Empty)
            {
                _id = new Guid(_bytes);
            }

            return _id;
        }
        set
        {
            _id = value;
            _bytes = _id.ToByteArray();
        }
    }

    public void OnAfterDeserialize()
    {
        if (IsBytesEmpty())
        {
            _bytes = Guid.NewGuid().ToByteArray();
            _id = new Guid(_bytes);
        }
        
        if (_id == Guid.Empty)
        {
            _id = new Guid(_bytes);
        }
    }

}
