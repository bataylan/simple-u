using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleU.Context
{
    public class ContextDictionary
    {
        private Dictionary<string, object> _extras;

        public ContextDictionary()
        {
            _extras = new Dictionary<string, object>();
        }

        public void SetExtra(string key, object data)
        {
            if (data == null)
            {
                Debug.LogError("SetExtra run with null data. key: " + key);
                return;
            }

            if (_extras.ContainsKey(key))
            {
                Debug.Log("SetExtra value updated for key: " + key + " value: " + data.ToString());
                _extras[key] = data;
                return;
            }

            _extras.Add(key, data);
        }

        public bool TryGetExtra<T>(string key, out T value)
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

        public T GetExtra<T>(string key, T defaultValue)
        {
            if (!_extras.ContainsKey(key) || !_extras.TryGetValue(key, out var dictValue))
                return defaultValue;

            return (T)dictValue;
        }

        public void OnNetworkDespawn()
        {
            _extras = null;
        }
    }
}
