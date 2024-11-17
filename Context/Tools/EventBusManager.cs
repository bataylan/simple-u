using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleU.Context
{
    public class EventBusManager
    {
        private Dictionary<Type, IEventBus> _dictionary;

        public void Subscribe<T>(Action<T> receive)
        {
            var eventBus = GetOrCreateEventBus<T>();
            eventBus.Subscribe(receive);
        }

        public void Unsubscribe<T>(Action<T> receive)
        {
            if (!_dictionary.TryGetValue(typeof(T), out var eventBus))
            {
                return;
            }

            var genericEventBus = eventBus as GenericEventBus<T>;
            genericEventBus.Unsubscribe(receive);
        }
        
        public void Invoke<T>(T value)
        {
            if (!_dictionary.TryGetValue(typeof(T), out var eventBus))
            {
                return;
            }
            
            var genericEventBus = eventBus as GenericEventBus<T>;
            genericEventBus.Invoke(value);
        }

        private GenericEventBus<T> GetOrCreateEventBus<T>()
        {
            if (_dictionary.TryGetValue(typeof(T), out var eventBus))
            {
                return eventBus as GenericEventBus<T>;
            }
            else
            {
                var genericEventBus = new GenericEventBus<T>();
                _dictionary.Add(typeof(T), genericEventBus);
                return genericEventBus;
            }
        }
    }
}
