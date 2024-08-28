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

        [SerializeField, HideInInspector] private List<IdValue<T>> _items;
        [SerializeField, HideInInspector] private int _currentIndex;

        public T this[int id]
        {
            get => _items[GetIndexById(id)].value;
            set
            {
                SetById(id, value);
            }
        }

        private void SetById(int id, T value)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].id == id)
                {
                    _items[i] = new IdValue<T>()
                    {
                        id = _items[i].id,
                        value = value
                    };
                    return;
                }
            }
        }

        private int GetIndexById(int id)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].id == id)
                    return i;
            }

            return -1;
        }

        public int Count => _items.Count;
        public bool IsReadOnly => false;

        public bool IsFixedSize => false;

        public bool IsSynchronized => false;

        public object SyncRoot => false;

        object IList.this[int index]
        {
            get => this[index];
            set => SetById(index, (T)value);
        }

        public PersistentList()
        {
            _items = new List<IdValue<T>>();
            _currentIndex = -1;
        }

        public void Add(T item)
        {
            var idValue = new IdValue<T>()
            {
                id = _currentIndex,
                value = item
            };
            _items.Add(idValue);
            _currentIndex++;
        }

        public void Clear()
        {
            _items.Clear();
            _currentIndex = 0;
        }

        public bool Contains(T item)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].value.Equals(item))
                    return true;
            }

            return false;
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

        public IEnumerator<T> GetEnumerator() => _items.Select(x => x.value).GetEnumerator();

        public int IndexOf(T item)
        {
            if (!Contains(item))
                return -1;

            int id = -1;
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].value.Equals(item))
                {
                    id = _items[i].id;
                    break;
                }
            }

            return id;
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

            int indexToRemove = -1;
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].value.Equals(item))
                {
                    indexToRemove = i;
                    break;
                }
            }

            if (indexToRemove >= 0)
            {
                _items.RemoveAt(indexToRemove);
                return true;
            }

            return false;
        }

        public void RemoveAt(int id)
        {
            _items.RemoveAt(GetIndexById(id));
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
        public T value;
    }
}
