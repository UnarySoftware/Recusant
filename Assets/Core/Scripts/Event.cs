using System;
using System.Collections.Generic;

namespace Core
{
    public abstract class BaseEvent<T> where T : BaseEvent<T>, new()
    {
        private readonly List<Type> _subscriberTypes = new();
        private readonly List<Func<T, bool>> _subscriberFuncs = new();

        private static T _instance = null;
        public static T Instance
        {
            get
            {
                _instance ??= new();
                return _instance;
            }
            private set { }
        }

        private readonly T _casted = null;

        protected BaseEvent()
        {
            _casted = (T)this;
            Bootstrap.Instance.OnCleanupStaticState += OnCleanupStaticState;
        }

        private void OnCleanupStaticState()
        {
            _instance = null;
            _subscriberTypes.Clear();
            _subscriberFuncs.Clear();
        }

        protected void Publish()
        {
            foreach (Func<T, bool> handler in _subscriberFuncs)
            {
                if (!handler(_casted))
                {
                    break;
                }
            }
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

        private void SubscribeIndexed(int insertIndex, Func<T, bool> func, Type subscriberType)
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

        public void SubscribeBefore(Type before, Func<T, bool> func, object subscriber)
        {
            int index = FindIndexByType(before, true);

            if (index != -1)
            {
                SubscribeIndexed(index - 1, func, subscriber.GetType());
            }
        }

        public void SubscribeAfter(Type after, Func<T, bool> func, object subscriber)
        {
            int index = FindIndexByType(after, false);

            if (index != -1)
            {
                SubscribeIndexed(index + 1, func, subscriber.GetType());
            }
        }

        public void Subscribe(Func<T, bool> func, object subscriber)
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
}
