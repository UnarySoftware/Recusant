using System;
using UnityEngine;

[Serializable]
public struct SingleLayer
{
    [SerializeField]
    private int m_Value;

    public SingleLayer(int value)
    {
        m_Value = value;
    }

    public SingleLayer(CodeGenerated.GameObjectLayerMask value)
    {
        m_Value = (int)value;
    }

    public static implicit operator int(SingleLayer layer)
    {
        return layer.m_Value;
    }

    public static implicit operator SingleLayer(int value)
    {
        return new SingleLayer(value);
    }

    public static implicit operator SingleLayer(CodeGenerated.GameObjectLayerMask value)
    {
        return new SingleLayer(value);
    }
}
