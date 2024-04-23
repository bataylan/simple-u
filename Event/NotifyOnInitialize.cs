using System;
using UnityEngine;
using UnityEngine.Events;

namespace SimpleU.Event
{
    [Serializable]
    public struct NotifyOnInitialize<T>
    {
        [SerializeField] private UnityEvent<T> @event;

        private bool Initialized => _initializer != null;
        private T _initializer;

        public void Initialize(T initializer)
        {
            if (_initializer != null || initializer == null)
            {
                Debug.LogError("Can't initialize");
                return;
            }

            _initializer = initializer;
            @event.Invoke(initializer);
            @event.RemoveAllListeners();
        }

        public void RemoveListener(UnityAction<T> action)
        {
            @event.RemoveListener(action);
        }

        public void AddListener(UnityAction<T> action)
        {
            if (Initialized)
                action.Invoke(_initializer);

            @event.AddListener(action);
        }
    }
}
