using System;
using System.Collections.Generic;
using System.Text;

namespace Unary.Core
{
    public abstract class BaseEvent<T, K>
        where T : Delegate
    {
        protected readonly List<Type> _subscriberTypes = new();
        protected readonly List<T> _subscriberFuncs = new();

        protected readonly List<K> _initQueue = new();
        protected bool _initialized = false;
        private bool _subscribed = false;
        protected object _owner;
        protected bool _debug = false;
        protected StringBuilder _debugString;
        protected Type _defineType;
        protected string _defineField;

        protected abstract void ProcessQueue();

        protected void PublishInternal(K data)
        {
            if (_initialized)
            {
                return;
            }

            if (Bootstrap.Instance.FinishedInitialization)
            {
                _initialized = true;
                return;
            }

            if (!_subscribed)
            {
                Bootstrap.Instance.OnFinishInitialization += OnFinishInitialization;
                _subscribed = true;
            }

            _initQueue.Add(data);
        }

        private void OnFinishInitialization()
        {
            if (_initialized)
            {
                Bootstrap.Instance.OnFinishInitialization -= OnFinishInitialization;
                return;
            }

            ProcessQueue();

            _initQueue.Clear();
            _initialized = true;
        }


        private int FindIndexByType(Type obj, bool forwardOrder)
        {
            if (forwardOrder)
            {
                for (int i = 0; i < _subscriberTypes.Count; i++)
                {
                    if (_subscriberTypes[i] == obj)
                    {
                        return i;
                    }
                }
            }
            else
            {
                for (int i = _subscriberTypes.Count - 1; i >= 0; i--)
                {
                    if (_subscriberTypes[i] == obj)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        private void SubscribeIndexed(int insertIndex, T func, Type subscriberType)
        {
            if (insertIndex < 0)
            {
                insertIndex = 0;
            }

            if (insertIndex >= _subscriberTypes.Count)
            {
                _subscriberTypes.Add(subscriberType);
                _subscriberFuncs.Add(func);
            }
            else
            {
                _subscriberTypes.Insert(insertIndex, subscriberType);
                _subscriberFuncs.Insert(insertIndex, func);
            }
        }

        public void SubscribeBefore(Type before, T func, object subscriber)
        {
            int index = FindIndexByType(before, true);

            if (index != -1)
            {
                SubscribeIndexed(index - 1, func, subscriber.GetType());
            }
        }

        public void SubscribeAfter(Type after, T func, object subscriber)
        {
            int index = FindIndexByType(after, false);

            if (index != -1)
            {
                SubscribeIndexed(index + 1, func, subscriber.GetType());
            }
        }

        public void Subscribe(T func, object subscriber)
        {
            _subscriberTypes.Add(subscriber.GetType());
            _subscriberFuncs.Add(func);
        }

        public void Unsubscribe(object subscriber)
        {
            Type type = subscriber.GetType();

            int index = FindIndexByType(type, false);

            while (index != -1)
            {
                _subscriberTypes.RemoveAt(index);
                _subscriberFuncs.RemoveAt(index);
                index = FindIndexByType(type, false);
            }
        }
    }

    public delegate bool EmptyEventDelegate();

    public class EventAction : BaseEvent<EmptyEventDelegate, object>
    {
        public EventAction() : base()
        {

        }

        public EventAction(Type defineType, string defineField) : base()
        {
            _debug = true;
            _defineType = defineType;
            _defineField = defineField;
            _debugString = new();
        }

        protected override void ProcessQueue()
        {
            if (_debug)
            {
                _debugString.Append("Processing queue for event ");
                _debugString.Append(_defineType.FullName);
                _debugString.Append('.');
                _debugString.Append(_defineField);
                _debugString.Append(" (void):\n");
            }

            for (int i = 0; i < _initQueue.Count; i++)
            {
                foreach (EmptyEventDelegate handler in _subscriberFuncs)
                {
                    if (_debug)
                    {
                        _debugString.Append(handler.Target.GetType().FullName);
                        _debugString.Append('\n');
                    }
                    if (!handler())
                    {
                        break;
                    }
                }
            }

            if (_debug)
            {
                Logger.Instance.Log(_debugString.ToString());
                _debugString.Clear();
            }
        }

        public void Publish()
        {
            PublishInternal(null);

            if (_debug)
            {
                _debugString.Append("Processing event ");
                _debugString.Append(_defineType.FullName);
                _debugString.Append('.');
                _debugString.Append(_defineField);
                _debugString.Append(" (void):\n");
            }

            foreach (EmptyEventDelegate handler in _subscriberFuncs)
            {
                if (_debug)
                {
                    _debugString.Append(handler.Target.GetType().FullName);
                    _debugString.Append('\n');
                }
                if (!handler())
                {
                    break;
                }
            }

            if (_debug)
            {
                Logger.Instance.Log(_debugString.ToString());
                _debugString.Clear();
            }
        }
    }

    public delegate bool DataEventDelegate<T>(ref T data) where T : struct;

    public class EventFunc<T> : BaseEvent<DataEventDelegate<T>, T>
        where T : struct
    {
        public EventFunc() : base()
        {

        }

        public EventFunc(Type defineType, string defineField) : base()
        {
            if (typeof(T) == typeof(Logger.LogEventData))
            {
                // Drop LogEventData attempt at debugging or we will get stack overflows
                return;
            }

            _debug = true;
            _debugString = new();
            _defineType = defineType;
            _defineField = defineField;
        }

        public void Publish(T data)
        {
            PublishInternal(data);

            if (_debug)
            {
                _debugString.Append("Processing event ");
                _debugString.Append(_defineType.FullName);
                _debugString.Append('.');
                _debugString.Append(_defineField);
                _debugString.Append(" (");
                _debugString.Append(typeof(T).FullName);
                _debugString.Append("):\n");
            }

            foreach (DataEventDelegate<T> handler in _subscriberFuncs)
            {
                if (_debug)
                {
                    _debugString.Append(handler.Target.GetType().FullName);
                    _debugString.Append('\n');
                }

                if (!handler(ref data))
                {
                    break;
                }
            }

            if (_debug)
            {
                Logger.Instance.Log(_debugString.ToString());
                _debugString.Clear();
            }
        }

        protected override void ProcessQueue()
        {
            if (_debug)
            {
                _debugString.Append("Processing queue for event ");
                _debugString.Append(_defineType.FullName);
                _debugString.Append('.');
                _debugString.Append(_defineField);
                _debugString.Append(" (");
                _debugString.Append(typeof(T).FullName);
                _debugString.Append("):\n");
            }

            foreach (var entry in _initQueue)
            {
                T target = entry;

                foreach (DataEventDelegate<T> handler in _subscriberFuncs)
                {
                    if (_debug)
                    {
                        _debugString.Append(handler.Target.GetType().FullName);
                        _debugString.Append('\n');
                    }
                    if (!handler(ref target))
                    {
                        break;
                    }
                }
            }

            if (_debug)
            {
                Logger.Instance.Log(_debugString.ToString());
                _debugString.Clear();
            }
        }
    }
}
