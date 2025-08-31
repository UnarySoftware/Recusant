using System;
using System.Collections;
using UnityEngine;

namespace Recusant
{
    public class GameplayState : UiState
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

            StartCoroutine(DelayProfiling());
        }

        IEnumerator DelayProfiling()
        {
            yield return new WaitForSeconds(4.0f);
            PerformanceManager.Instance.Profiling = true;
        }

        public override void Close()
        {
            base.Close();

            PerformanceManager.Instance.Profiling = false;
        }

        public override Type GetBackState()
        {
            return typeof(MainMenuState);
        }
    }
}
