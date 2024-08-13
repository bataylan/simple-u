using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleU.Context
{
    public abstract class ABaseContext : MonoBehaviour
    {
        public ContextDictionary ExtraData
        {
            get
            {
                if (_extraData == null)
                    _extraData = new ContextDictionary();

                return _extraData;
            }
        }
        private ContextDictionary _extraData;

        public UpdateManager UpdateManager
        {
            get
            {
                if (_updateManager == null)
                    _updateManager = new UpdateManager();

                return _updateManager;
            }
        }
        private UpdateManager _updateManager;

        [SerializeField] private ExtraScriptableObject[] _extraScriptableObjects;

        protected virtual void Awake()
        {
            RegisterInitialExtras();
        }

        private void RegisterInitialExtras()
        {
            if (_extraScriptableObjects != null)
            {
                for (int i = 0; i < _extraScriptableObjects.Length; i++)
                {
                    ExtraData.SetExtra(_extraScriptableObjects[i].Key, _extraScriptableObjects[i]);
                }
            }
        }

        protected virtual void Update()
        {
            UpdateManager.Update();
        }
    }
}
