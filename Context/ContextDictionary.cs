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
                Debug.LogError("SetExtra already contains key: " + key);
                return;
            }

            _extras.Add(key, data);
        }

        public bool TryGetExtra<T>(string key, out T value) where T : class
        {
            value = default(T);
            if (!_extras.ContainsKey(key) || !_extras.TryGetValue(key, out var dictValue))
                return false;

            value = dictValue as T;
            if (value == default(T))
                return false;

            return true;
        }

        public void OnNetworkDespawn()
        {
            _extras = null;
        }
    }
}
