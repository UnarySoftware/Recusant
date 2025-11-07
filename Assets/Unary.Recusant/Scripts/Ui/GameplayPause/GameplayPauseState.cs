using System;
using System.Collections;
using UnityEngine;

namespace Unary.Recusant
{
    public class GameplayPauseState : Core.UiState
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

            Cursor.lockState = CursorLockMode.None;
        }

        public override void Close()
        {
            base.Close();
        }

        public override Type GetBackState()
        {
            return typeof(GameplayState);
        }
    }
}
