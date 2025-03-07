using System.Collections.Generic;
using UnityEngine;

namespace SimpleU.Context
{
    public class ContextDictionary
    {
        public static ContextDictionary Get(bool levelScope = true)
        {
            if (levelScope)
            {
                return LevelContext.Get().ExtraData;
            }
            else
            {
                return GameContext.Instance.ExtraData;
            }
        }

        private Dictionary<object, object> _extras;

        public ContextDictionary()
        {
            _extras = new Dictionary<object, object>();
        }

        public void SetExtra(object key, object data)
        {
            if (data == null)
            {
                Debug.LogError("SetExtra run with null data. key: " + key);
                return;
            }

            if (_extras.ContainsKey(key))
            {
                Debug.Log("<color=yellow>SetExtra value updated for key:</color> " + key + " value: " + data.ToString());
                _extras[key] = data;
                return;
            }

            _extras.Add(key, data);
        }

        public bool TryGetExtra<T>(object key, out T value)
        {
            value = default(T);
            if (!_extras.ContainsKey(key) || !_extras.TryGetValue(key, out var dictValue))
            {
                return false;
            }

            if (dictValue is T val)
            {
                value = val;
                return true;
            }

            return false;
        }

        public T GetExtra<T>(object key, T defaultValue)
        {
            if (!_extras.ContainsKey(key) || !_extras.TryGetValue(key, out var dictValue))
                return defaultValue;

            return (T)dictValue;
        }

        public bool RemoveExtra<T>(object key)
        {
            return _extras.Remove(key);
        }

        public void OnNetworkDespawn()
        {
            _extras = null;
        }
    }
}
