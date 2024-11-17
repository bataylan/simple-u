using System;
using UnityEngine;

namespace SimpleU.Context
{
    public class GenericEventBus<T> : IEventBus
    {
        private Action<T> _listeners;

        public void Subscribe(Action<T> receive)
        {
            _listeners += receive;
        }

        public void Unsubscribe(Action<T> receive)
        {
            _listeners -= receive;
        }

        public void Invoke(T value)
        {
            
        }
    }

    public interface IEventBus { }
}
