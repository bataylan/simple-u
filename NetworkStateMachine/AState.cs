using System;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

namespace SimpleU.NetworkChainedStateMachine
{
    public abstract class AState : NetworkBehaviour
    {
        public bool isDefault;
        public string stateName;
        public StateCondition condition;
        public StateCondition[] effects;


        public bool IsActive => _isActive;
        private bool _isActive;
        public bool IsCurrent => _isCurrent;
        private bool _isCurrent;

        public bool CheckData()
        {
            if (!isDefault && condition == null)
            {
                Debug.LogError(stateName + " condition empty!");
                return false;
            }
            return true;
        }

        public virtual void ForwardEnter()
        {
            if (_isActive)
            {
                Debug.LogError("Loop detected! " + gameObject.name);
            }

            _isActive = true;
            _isCurrent = true;
        }

        public virtual void BackwardEnter()
        {
            _isCurrent = true;
        }

        public virtual void Enter() { }

        public virtual void Exit() { }

        public virtual void ForwardExit()
        {
            _isCurrent = false;
            Exit();
        }

        public virtual void BackwardExit()
        {
            _isActive = false;
            _isCurrent = false;
            Exit();
        }

        void OnDisable()
        {
            if (_isActive)
                Exit();
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (_isCurrent)
                Handles.Label(transform.position + (2 * Vector3.up), stateName);
        }

        void OnValidate()
        {
            if (string.IsNullOrEmpty(stateName))
            {
                var type = GetType();
                stateName = type.Name;
            }
        }
#endif
    }
}