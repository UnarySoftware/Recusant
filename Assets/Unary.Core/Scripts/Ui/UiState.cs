using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unary.Core
{
    public abstract class UiState : MonoBehaviour
    {
        private bool _open = false;

        public bool IsOpen()
        {
            return _open;
        }

        public static UiManager Instance = null;
        private readonly List<UiUnit> _uiUnits = new();

        public virtual void Initialize()
        {
            UiUnit[] TargetUnits = GetComponentsInChildren<UiUnit>();

            foreach (UiUnit unit in TargetUnits)
            {
                _uiUnits.Add(unit);
                unit.State = this;
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
            _open = true;
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
            _open = false;
        }

        public abstract Type GetBackState();

        public virtual CursorLockMode GetCursorMode()
        {
            return CursorLockMode.None;
        }
    }
}
