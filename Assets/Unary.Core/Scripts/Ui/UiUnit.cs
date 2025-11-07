using System;
using UnityEngine;

namespace Unary.Core
{
    public abstract class UiUnit : MonoBehaviour
    {
        [NonSerialized]
        public UiState State;

        public bool IsOpen()
        {
            if (State == null)
            {
                return false;
            }

            return State.IsOpen();
        }

        public abstract void Initialize();
        public abstract void Deinitialize();
        public abstract void Open();
        public abstract void Close();
    }
}
