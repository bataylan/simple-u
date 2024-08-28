using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleU.Logger;
using UnityEngine;

namespace SimpleU.Collections
{
    [Serializable]
    public class PersistentList<T> : IList<T>, IEnumerable<T>, IList where T : new()    
    {
        [SerializeReference] private List<T> _items;
        [SerializeField, HideInInspector] private List<int> _ids;
        [SerializeField, HideInInspector] private int _currentIndex = -1;

        public T this[int id]
        {
            get => _items[_ids[id]];
            set => _items[_ids[id]] = value;
        }

        public int Count => _items.Count;
        public bool IsReadOnly => false;

        public bool IsFixedSize => false;

        public bool IsSynchronized => false;

        public object SyncRoot => false;

        object IList.this[int index]
        {
            get => this[_ids[index]];
            set => this[_ids[index]] = (T)value;
        }

        public PersistentList()
        {
            _items = new List<T>();
            _ids = new List<int>();
            _currentIndex = -1;
        }

        public void Add(T item)
        {
            _currentIndex++;
            _items.Add(item);
            _ids.Add(_currentIndex);
        }

        public void Clear()
        {
            _items.Clear();
            _ids.Clear();
            _currentIndex = -1;
        }

        public bool Contains(T item)
        {
            return _items.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (_currentIndex >= 0 && arrayIndex > 0)
                throw new Exception("Can't copy to existing persistent list!");

            for (int i = 0; i < array.Length; i++)
            {
                Insert(arrayIndex, array[i]);
                arrayIndex++;
            }
        }

        public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

        public int IndexOf(T item)
        {
            if (!Contains(item))
                return -1;

            return _ids[_items.IndexOf(item)];
        }

        public void Insert(int index, T item)
        {
            if (index != (_currentIndex + 1))
                throw new Exception("Can't insert to persistent list!");

            Add(item);
        }

        public bool Remove(T item)
        {
            if (!Contains(item))
                return false;

            var index = _items.IndexOf(item);
            _items.RemoveAt(index);
            _ids.RemoveAt(index);

            return true;
        }

        public void RemoveAt(int index)
        {
            _items.RemoveAt(_ids[index]);
        }

        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

        public int Add(object value)
        {
            if (value == default)
            {
                Add(default(T));
                return _currentIndex;
            }

            if (value is T t)
            {
                Add(t);
                return _currentIndex;
            }
            else
            {
                throw new Exception("Can't cast to " + typeof(T).Name);
            }
        }

        public bool Contains(object value)
        {
            if (value is T t)
            {

                return Contains(t);
            }
            else
            {
                throw new Exception("Can't cast to " + typeof(T).Name);
            }
        }

        public int IndexOf(object value)
        {
            if (value is T t)
            {
                return IndexOf(t);
            }
            else
            {
                throw new Exception("Can't cast to " + typeof(T).Name);
            }
        }

        public void Insert(int index, object value)
        {
            if (value is T t)
            {
                Insert(index, t);
            }
            else
            {
                throw new Exception("Can't cast to " + typeof(T).Name);
            }
        }

        public void Remove(object value) => Remove((T)value);

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }
    }

    [Serializable]
    public struct IdValue<T>
    {
        public int id;
        [SerializeReference] public T value;
    }
}
