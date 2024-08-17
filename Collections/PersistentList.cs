using System;
using System.Collections;
using System.Collections.Generic;
using SimpleU.Logger;
using UnityEngine;

namespace SimpleU.Collections
{
    [Serializable]
    public class PersistentList<T> : IList<T>, IEnumerable<T>
    {
        private Dictionary<int, T> _items;
        private int _lastIndex;

        public T this[int index]
        {
            get => _items[index];
            set => _items[index] = value;
        }

        public int Count => _items.Count;
        public bool IsReadOnly => false;

        public PersistentList()
        {
            _items = new Dictionary<int, T>();
            _lastIndex = -1;
        }

        public void Add(T item)
        {
            _lastIndex++;
            _items.Add(_lastIndex, item);
        }

        public void Clear()
        {
            _items.Clear();
            _lastIndex = -1;
        }

        public bool Contains(T item)
        {
            return _items.ContainsValue(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (_lastIndex >= 0 && arrayIndex > 0)
                throw new Exception("Can't copy to existing persistent list!");

            for (int i = 0; i < array.Length; i++)
            {
                Insert(arrayIndex, array[i]);
                arrayIndex++;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _items.Values.GetEnumerator();
        }

        public int IndexOf(T item)
        {
            if (!_items.ContainsValue(item))
                return -1;

            var en = _items.GetEnumerator();
            while (en.MoveNext())
            {
                if (en.Current.Value.Equals(item))
                {
                    return en.Current.Key;
                }
            }

            LogUtility.LogError("IndexOf can't return key!");
            return -1;
        }

        public void Insert(int index, T item)
        {
            if (index != (_lastIndex + 1))
                throw new Exception("Can't insert to persistent list!");

            Add(item);
        }

        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index < 0)
                return false;

            RemoveAt(index);
            return true;
        }

        public void RemoveAt(int index)
        {
            _items.Remove(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.Values.GetEnumerator();
        }
    }
}
