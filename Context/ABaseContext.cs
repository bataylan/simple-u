using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleU.Context
{
    public abstract class ABaseContext
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
        
        public EventBusManager EventBusManager
        {
            get
            {
                if (_eventBusManager == null)
                    _eventBusManager = new EventBusManager();

                return _eventBusManager;
            }
        }
        private EventBusManager _eventBusManager;
        protected UpdateManagerBehaviour _updateBehaviour;

        internal virtual void EnsureInit(GameObject referenceObject, ScriptableObject[] extraScriptableObjects, GameObject[] extraPrefabs)
        {
            RegisterInitialExtras(extraScriptableObjects);

#if UNITY_EDITOR
            if (!Application.isPlaying)
                return;
#endif

            InstantiateExtraPrefabs(extraPrefabs);

            _updateBehaviour = referenceObject.AddComponent<UpdateManagerBehaviour>();
            _updateBehaviour.Init(UpdateManager);
        }

        private void RegisterInitialExtras(ScriptableObject[] extraScriptableObjects)
        {
            if (extraScriptableObjects == null)
                return;

            for (int i = 0; i < extraScriptableObjects.Length; i++)
            {
                var scriptableObject = extraScriptableObjects[i];
                if (scriptableObject is IExtraScriptableObject keyOwner)
                {
                    ExtraData.SetExtra(keyOwner.Key, extraScriptableObjects[i]);
                    keyOwner.OnSet();
                }
                else
                {
                    Debug.Log(scriptableObject.name + " is not key owner!");
                }
            }
        }

        private void InstantiateExtraPrefabs(GameObject[] gameObjects)
        {
            if (gameObjects == null)
                return;
                
            for (int i = 0; i < gameObjects.Length; i++)
            {
                GameObject.Instantiate(gameObjects[i]);
            }
        }

        public class UpdateManagerBehaviour : MonoBehaviour
        {
            private UpdateManager _updateManager;

            public void Init(UpdateManager updateManager)
            {
                _updateManager = updateManager;
            }

            void Update()
            {
                _updateManager.Update();
            }
        }
    }
}
