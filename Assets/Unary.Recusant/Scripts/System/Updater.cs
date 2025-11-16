using Unary.Core;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Unary.Recusant
{
    public class UpdaterUnit
    {
        private const byte PoolLimit = 3;
        private byte PoolCounter = 0;

        public readonly float Interval;

        private readonly Action[] OnUpdate;
        private readonly float[] OnTime;

        public UpdaterUnit(float Time, float Interval)
        {
            this.Interval = Interval;

            OnUpdate = new Action[PoolLimit];
            OnTime = new float[PoolLimit];

            float Offset = Interval / PoolLimit;

            for (int i = 0; i < PoolLimit; i++)
            {
                OnTime[i] = Time + (Offset * i);
            }
        }

        public void Update(float Time)
        {
            for (int i = 0; i < PoolLimit; i++)
            {
                if (Time > OnTime[i])
                {
                    OnUpdate[i]?.Invoke();
                    OnTime[i] = Time + Interval;
                }
            }
        }

        public void Subscribe(Action Update)
        {
            if (OnUpdate[PoolCounter] == null)
            {
                OnUpdate[PoolCounter] = Update;
            }
            else
            {
                OnUpdate[PoolCounter] += Update;
            }

            PoolCounter++;
            if (PoolCounter == PoolLimit)
            {
                PoolCounter = 0;
            }
        }

        public void Unsubscribe(Action Update)
        {
            for (int i = 0; i < PoolLimit; i++)
            {
                OnUpdate[i] -= Update;
            }
        }
    }

    public class Updater : System<Updater>
    {
        private UpdaterUnit[] FixedUpdateUnits = new UpdaterUnit[0];
        private UpdaterUnit[] UpdateUnits = new UpdaterUnit[0];
        private UpdaterUnit[] LateUpdateUnits = new UpdaterUnit[0];

        private float Timer = 0.0f;

        public override void Initialize()
        {

        }

        public override void PostInitialize()
        {

        }

        public override void Deinitialize()
        {

        }

        private void Subscribe(ref UpdaterUnit[] Units, Action Action, float Interval)
        {
            bool FoundUnit = false;

            for (int i = 0; i < Units.Length; ++i)
            {
                UpdaterUnit Target = Units[i];
                if (Mathf.Abs(Target.Interval - Interval) < Mathf.Epsilon)
                {
                    FoundUnit = true;
                    Target.Subscribe(Action);
                    break;
                }
            }

            if (!FoundUnit)
            {
                UpdaterUnit[] NewUnits = new UpdaterUnit[Units.Length + 1];
                Array.Copy(Units, NewUnits, Units.Length);
                NewUnits[^1] = new UpdaterUnit(Timer, Interval);
                Units = NewUnits;
                Units[^1].Subscribe(Action);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SubscribeFixedUpdate(Action Action, float Interval = 0.0f)
        {
            Subscribe(ref FixedUpdateUnits, Action, Interval);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SubscribeUpdate(Action Action, float Interval = 0.0f)
        {
            Subscribe(ref UpdateUnits, Action, Interval);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SubscribeLateUpdate(Action Action, float Interval = 0.0f)
        {
            Subscribe(ref LateUpdateUnits, Action, Interval);
        }

        private void Unsubscribe(ref UpdaterUnit[] Units, Action Action)
        {
            for (int i = 0; i < Units.Length; i++)
            {
                UpdaterUnit Target = Units[i];
                Target.Unsubscribe(Action);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsubscribeFixedUpdate(Action Action)
        {
            Unsubscribe(ref FixedUpdateUnits, Action);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsubscribeUpdate(Action Action)
        {
            Unsubscribe(ref UpdateUnits, Action);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsubscribeLateUpdate(Action Action)
        {
            Unsubscribe(ref LateUpdateUnits, Action);
        }

        private void FixedUpdate()
        {
            Timer += Time.fixedDeltaTime;

            for (int i = 0; i < FixedUpdateUnits.Length; ++i)
            {
                FixedUpdateUnits[i].Update(Timer);
            }
        }

        private void Update()
        {
            for (int i = 0; i < UpdateUnits.Length; ++i)
            {
                UpdateUnits[i].Update(Time.time);
            }
        }

        private void LateUpdate()
        {
            for (int i = 0; i < LateUpdateUnits.Length; ++i)
            {
                LateUpdateUnits[i].Update(Time.time);
            }
        }
    }
}
