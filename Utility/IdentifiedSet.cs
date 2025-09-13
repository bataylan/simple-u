using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleU.Utility
{
    [Serializable]
    public class IdentifiedSet<T>
    {
        [SerializeField, HideInInspector] private List<int> _ids;
        [SerializeField, HideInInspector] private List<T> _items;

        public int Count => _items.Count;

        public void Add(T item)
        {
            if (_items.Contains(item))
                return;

            int index = GetLastIndex() + 1;
            _ids.Add(index);
            _items.Add(item);
        }

        public void Remove(T item)
        {
            if (!_items.Contains(item))
                return;

            int localIndex = GetIndex(item);
            _items.RemoveAt(localIndex);
            _ids.RemoveAt(localIndex);
        }

        public bool TryGetItemId(T item, out int id)
        {
            if (_items.Contains(item))
            {
                int localIndex = GetIndex(item);
                id = _ids[localIndex];
                return true;
            }

            id = -1;
            return false;
        }

        public bool TryGetItemId<K>(K item, out int index)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].Equals(item))
                {
                    index = _ids[i];
                    return true;
                }
            }

            index = -1;
            return false;
        }

        public bool TryGetItem(int id, out T value)
        {
            if (_ids.Contains(id))
            {
                int localIndex = GetIndex(id);
                value = _items[localIndex];
                return true;
            }

            value = default;
            return false;
        }

        public bool TryGetItemAndIdByIndex(int index, out KeyValuePair<int, T> pair)
        {
            pair = default;

            if (_ids.Count <= index)
                return false;

            int id = _ids[index];
            T item = _items[index];
            pair = new KeyValuePair<int, T>(id, item);
            
            return true;
        }

        public void FetchWithList(List<T> values)
        {
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                var item = _items[i];
                if (item == null || !values.Contains(item))
                    Remove(item);
            }

            for (int i = 0; i < values.Count; i++)
            {
                Add(values[i]);
            }
        }

        private int GetLastIndex()
        {
            if (_ids == null || _ids.Count == 0)
                return -1;

            return _ids[^1];
        }

        private int GetIndex(T item)
        {
            return _items.IndexOf(item);
        }

        private int GetIndex(int index)
        {
            return _ids.IndexOf(index);
        }
    }
}
