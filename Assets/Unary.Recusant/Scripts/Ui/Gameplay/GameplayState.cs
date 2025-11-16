using System;
using UnityEngine;

namespace Unary.Recusant
{
    public class GameplayState : Core.UiState
    {
        public override void Initialize()
        {
            base.Initialize();
        }

        public override void Deinitialize()
        {
            base.Deinitialize();
        }

        public override void Open()
        {
            base.Open();
        }

        public override void Close()
        {
            base.Close();
        }

        public override Type GetBackState()
        {
            return typeof(GameplayPauseState);
        }

        public override CursorLockMode GetCursorMode()
        {
            return CursorLockMode.Locked;
        }
    }
}
