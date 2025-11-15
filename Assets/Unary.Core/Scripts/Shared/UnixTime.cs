using System;
using UnityEngine;

namespace Unary.Core
{
    [Serializable]
    public struct UnixTime
    {
        [SerializeField]
        public long Value;

        public void SetNow()
        {
            Value = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
        }

        public void Reset()
        {
            Value = 0;
        }
    }
}
