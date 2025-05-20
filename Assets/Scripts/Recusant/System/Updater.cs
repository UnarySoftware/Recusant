using Core;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Recusant
{
    public sealed class UpdaterUnit
    {
        private const byte PoolLimit = 3;
        private byte PoolCounter = 0;

        public readonly float Interval;

        private readonly Action[] OnUpdate;
        private readonly float[] OnTime;

        public static void Dummy()
        {

        }

        public UpdaterUnit(float Time, float Interval)
        {
            this.Interval = Interval;

            OnUpdate = new Action[PoolLimit];
            OnTime = new float[PoolLimit];

            float Offset = Interval / PoolLimit;

            for (int i = 0; i < PoolLimit; i++)
            {
                OnUpdate[i] = Dummy;
                OnTime[i] = Time + (Offset * i);
            }
        }

        public void Update(float Time)
        {
            for (int i = 0; i < PoolLimit; i++)
            {
                if (Time > OnTime[i])
                {
                    OnUpdate[i]();
                    OnTime[i] = Time + Interval;
                }
            }
        }

        public void Subscribe(Action Update)
        {
            OnUpdate[PoolCounter] += Update;

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
                if (Target.Interval == Interval)
                {
                    FoundUnit = true;
                    Target.Subscribe(Action);
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

        public void Unsubscribe(ref UpdaterUnit[] Units, Action Action, float Interval)
        {
            for (int i = 0; i < Units.Length; i++)
            {
                UpdaterUnit Target = Units[i];
                if (Target.Interval == Interval)
                {
                    Target.Unsubscribe(Action);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsubscribeFixedUpdate(Action Action, float Interval = 0.0f)
        {
            Unsubscribe(ref FixedUpdateUnits, Action, Interval);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsubscribeUpdate(Action Action, float Interval = 0.0f)
        {
            Unsubscribe(ref UpdateUnits, Action, Interval);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsubscribeLateUpdate(Action Action, float Interval = 0.0f)
        {
            Unsubscribe(ref LateUpdateUnits, Action, Interval);
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
