using System;
using System.Runtime.CompilerServices;
using Unary.Core;
using UnityEngine;

namespace Unary.Recusant
{
    public class UpdaterUnit
    {
        private const byte _poolLimit = 3;
        private byte _poolCounter = 0;

        public readonly float Interval;

        private readonly Action[] _onUpdate;
        private readonly float[] _onTime;

        public UpdaterUnit(float time, float interval)
        {
            Interval = interval;

            _onUpdate = new Action[_poolLimit];
            _onTime = new float[_poolLimit];

            float Offset = interval / _poolLimit;

            for (int i = 0; i < _poolLimit; i++)
            {
                _onTime[i] = time + (Offset * i);
            }
        }

        public void Update(float time)
        {
            for (int i = 0; i < _poolLimit; i++)
            {
                if (time > _onTime[i])
                {
                    _onUpdate[i]?.Invoke();
                    _onTime[i] = time + Interval;
                }
            }
        }

        public void Subscribe(Action update)
        {
            if (_onUpdate[_poolCounter] == null)
            {
                _onUpdate[_poolCounter] = update;
            }
            else
            {
                _onUpdate[_poolCounter] += update;
            }

            _poolCounter++;
            if (_poolCounter == _poolLimit)
            {
                _poolCounter = 0;
            }
        }

        public void Unsubscribe(Action update)
        {
            for (int i = 0; i < _poolLimit; i++)
            {
                _onUpdate[i] -= update;
            }
        }
    }

    public class Updater : System<Updater>
    {
        private UpdaterUnit[] _fixedUpdateUnits = new UpdaterUnit[0];
        private UpdaterUnit[] _updateUnits = new UpdaterUnit[0];
        private UpdaterUnit[] _lateUpdateUnits = new UpdaterUnit[0];

        private float _timer = 0.0f;

        public override void Initialize()
        {

        }

        public override void PostInitialize()
        {

        }

        public override void Deinitialize()
        {

        }

        private void Subscribe(ref UpdaterUnit[] units, Action action, float interval)
        {
            bool foundUnit = false;

            for (int i = 0; i < units.Length; ++i)
            {
                UpdaterUnit target = units[i];
                if (Mathf.Abs(target.Interval - interval) < Mathf.Epsilon)
                {
                    foundUnit = true;
                    target.Subscribe(action);
                    break;
                }
            }

            if (!foundUnit)
            {
                UpdaterUnit[] newUnits = new UpdaterUnit[units.Length + 1];
                Array.Copy(units, newUnits, units.Length);
                newUnits[^1] = new UpdaterUnit(_timer, interval);
                units = newUnits;
                units[^1].Subscribe(action);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SubscribeFixedUpdate(Action action, float interval = 0.0f)
        {
            Subscribe(ref _fixedUpdateUnits, action, interval);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SubscribeUpdate(Action action, float interval = 0.0f)
        {
            Subscribe(ref _updateUnits, action, interval);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SubscribeLateUpdate(Action action, float interval = 0.0f)
        {
            Subscribe(ref _lateUpdateUnits, action, interval);
        }

        private void Unsubscribe(ref UpdaterUnit[] units, Action action)
        {
            for (int i = 0; i < units.Length; i++)
            {
                UpdaterUnit target = units[i];
                target.Unsubscribe(action);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsubscribeFixedUpdate(Action action)
        {
            Unsubscribe(ref _fixedUpdateUnits, action);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsubscribeUpdate(Action action)
        {
            Unsubscribe(ref _updateUnits, action);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsubscribeLateUpdate(Action action)
        {
            Unsubscribe(ref _lateUpdateUnits, action);
        }

        private void FixedUpdate()
        {
            _timer += Time.fixedDeltaTime;

            for (int i = 0; i < _fixedUpdateUnits.Length; ++i)
            {
                _fixedUpdateUnits[i].Update(_timer);
            }
        }

        private void Update()
        {
            for (int i = 0; i < _updateUnits.Length; ++i)
            {
                _updateUnits[i].Update(Time.time);
            }
        }

        private void LateUpdate()
        {
            for (int i = 0; i < _lateUpdateUnits.Length; ++i)
            {
                _lateUpdateUnits[i].Update(Time.time);
            }
        }
    }
}
