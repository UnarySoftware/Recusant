using System;
using UnityEngine;

namespace Recusant
{
    [Serializable]
    public struct SingleLayer
    {
        [SerializeField]
        private int m_Value;

        public SingleLayer(int value)
        {
            m_Value = value;
        }

        public SingleLayer(GameObjectLayerMask value)
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

        public static implicit operator SingleLayer(GameObjectLayerMask value)
        {
            return new SingleLayer(value);
        }
    }
}
