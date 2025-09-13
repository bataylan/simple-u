using System;
using System.Collections;
using System.Collections.Generic;
using SimpleU.Extensions.Colliders;
using UnityEngine;

namespace SimpleU.TWOD.Collider
{
    public class TriggerEnhancedHandler : MonoBehaviour
    {
        private Action<Collider2D> _handleTriggerEnter;
        private Action<Collider2D> _handleTriggerExit;

        public void SetListeners(Action<Collider2D> triggerEnter,
            Action<Collider2D> triggerExit)
        {
            _handleTriggerEnter = triggerEnter;
            _handleTriggerExit = triggerExit;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_handleTriggerEnter != null)
                _handleTriggerEnter.Invoke(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (_handleTriggerExit != null)
                _handleTriggerExit.Invoke(other);
        }
    }

    public class TriggerEnhancedHandler<T>
        where T : MonoBehaviour, ITrackDeathOwner<T>
    {
        public Action<T> handleTriggerEnter;
        public Action<T> handleTriggerExit;
        public Action<T> onFirstItemEntered;
        public Action<T> onLastItemLeft;
        public Func<T, bool> enterCondition;
        public string targetTag;

        public List<T> ItemList => _itemList;
        private List<T> _itemList = new List<T>();
        private TriggerEnhancedHandler _triggerHandler;

        public bool Enabled
        {
            get { return _triggerHandler.enabled; }
            set
            {
                _triggerHandler.enabled = value;
            }
        }

        public TriggerEnhancedHandler(TriggerEnhancedHandler triggerHandler, string targetTag)
        {
            if (triggerHandler == null)
                return;

            this.targetTag = targetTag;
            _triggerHandler = triggerHandler;
            _triggerHandler.SetListeners(OnTriggerEnter2D, OnTriggerExit2D);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.gameObject.CompareTag(targetTag))
                return;

            if (!other.gameObject.TryGetComponentExtended(out T item))
                return;

            AddItem(item);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.gameObject.CompareTag(targetTag))
                return;

            var item = other.gameObject.GetComponent<T>();
            if (item == null)
            {
                var colRef = other.gameObject.GetComponent<ColliderReference>();
                if (colRef == null)
                    return;

                if (!colRef.gameObject.TryGetComponentExtended(out item))
                    return;
            }
            RemoveItem(item);
        }

        private void AddItem(T item)
        {
            if (item == null)
                return;

            if (_itemList.Contains(item))
                return;

            if (enterCondition != null && !enterCondition.Invoke(item))
                return;

            item.DeathSender.RegisterOnDeath(RemoveItem);

            if (_itemList.Count <= 0 && onFirstItemEntered != null)
                onFirstItemEntered.Invoke(item);

            _itemList.Add(item);

            if (handleTriggerEnter != null)
                handleTriggerEnter.Invoke(item);
        }

        private void RemoveItem(T item)
        {
            if (item == null)
                return;

            if (!_itemList.Contains(item))
                return;

            item.DeathSender.UnRegisterOnDeath(RemoveItem);

            _itemList.Remove(item);

            if (_itemList.Count <= 0 && onLastItemLeft != null)
                onLastItemLeft.Invoke(item);

            if (handleTriggerExit != null)
                handleTriggerExit.Invoke(item);
        }

        private void RemoveAll()
        {
            if (_itemList == null)
                return;

            for (int i = 0; i < _itemList.Count; i++)
            {
                _itemList[i].DeathSender.UnRegisterOnDeath(RemoveItem);
            }

            _itemList = null;
        }

        // public void Disable()
        // {
        //     _triggerHandler.enabled = false;
        // }

        public void ResetActions()
        {
            handleTriggerEnter = null;
            handleTriggerExit = null;
        }

        public void Remove()
        {
            ResetActions();
            _triggerHandler.SetListeners(null, null);
            _triggerHandler = null;
            RemoveAll();
        }
    }

    public interface ITrackDeathOwner<T> where T : MonoBehaviour
    {
        DeathSender<T> DeathSender { get; }
        public void OnDeath();
    }

    public class DeathSender<T> where T : MonoBehaviour
    {
        private Action<T> _onDeath;

        public void RegisterOnDeath(Action<T> onDeath)
        {
            if (onDeath != null)
                _onDeath += onDeath;
        }

        public void UnRegisterOnDeath(Action<T> onDeath)
        {
            if (onDeath != null)
                _onDeath -= onDeath;
        }

        public void InvokeOnDeath(T obj, bool reset = false)
        {
            if (_onDeath != null)
                _onDeath.Invoke(obj);

            if (reset)
                Reset();
        }

        public void Reset()
        {
            _onDeath = null;
        }
    }
}
