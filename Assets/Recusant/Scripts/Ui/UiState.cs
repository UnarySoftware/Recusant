using System;
using System.Collections.Generic;
using UnityEngine;

namespace Recusant
{
    public abstract class UiState : MonoBehaviour
    {
        public static UiManager Instance = null;

        private readonly List<UiUnit> _uiUnits = new();

        public virtual void Initialize()
        {
            UiUnit[] TargetUnits = GetComponentsInChildren<UiUnit>();

            foreach (UiUnit unit in TargetUnits)
            {
                _uiUnits.Add(unit);
                unit.Initialize();
            }
        }

        public virtual void Deinitialize()
        {
            for (int i = _uiUnits.Count - 1; i >= 0; i--)
            {
                UiUnit unit = _uiUnits[i];
                unit.Deinitialize();
            }
        }

        public virtual void Open()
        {
            foreach (var unit in _uiUnits)
            {
                unit.Open();
            }
        }

        public virtual void Close()
        {
            foreach (var unit in _uiUnits)
            {
                unit.Close();
            }
        }

        public abstract Type GetBackState();
    }
}
