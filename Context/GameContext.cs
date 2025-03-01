using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace SimpleU.Context
{
    [DefaultExecutionOrder(-100)]
    public class GameContext : ABaseContext
    {
        public static GameContext Instance
        {
            get
            {
                CheckSetContext(ref _instance);
                return _instance;
            }
        }
        private static GameContext _instance;

        public LevelContext LevelContext
        {
            get
            {
                CheckSetContext(ref _levelContext);
                return _levelContext;
            }
        }
        private LevelContext _levelContext;

        public virtual T GetLevelContext<T>() where T : LevelContext => _levelContext as T;

        internal override void EnsureInit(GameObject referenceObject, ScriptableObject[] extraScriptableObjects, GameObject[] extraPrefabs)
        {
            base.EnsureInit(referenceObject, extraScriptableObjects, extraPrefabs);

            if (!Application.isPlaying)
                return;

            GameObject.DontDestroyOnLoad(referenceObject);
            var behaviour = referenceObject.GetComponent<GameContextReferenceBehaviour>();
            behaviour.TriggerOnDestroy += ClearStaticContext;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        protected static void CheckSetContext<T>(ref T currentContext) where T : ABaseContext, new()
        {
            if (currentContext != null)
                return;

            IContextReference reference = default;
            var newContext = new T();
            GameObject referenceInstance = null;

            var behaviour = GameObject.FindAnyObjectByType<ContextReferenceBehaviour<T>>();
            if (behaviour)
            {
                reference = behaviour;
                referenceInstance = behaviour.gameObject;
            }
            else
            {
                string behaviourName = newContext is GameContext ? "GameContext" : "LevelContext";
                var behaviourObj = Resources.Load<GameObject>(behaviourName);
                var defaultBehaviour = behaviourObj.GetComponent<IContextReference>();
                    
                if (defaultBehaviour != null)
                {
                    reference = defaultBehaviour;
                }
            }

            if (Application.isPlaying && !referenceInstance)
            {
                referenceInstance = new GameObject();
                referenceInstance.name = typeof(T).Name;
                if (newContext is GameContext)
                {
                    referenceInstance.AddComponent<GameContextReferenceBehaviour>();
                }
                else
                {
                    referenceInstance.AddComponent<LevelContextReferenceBehaviour>();
                }
            }
            
            currentContext = newContext;

            if (reference != null)
            {
                newContext.EnsureInit(referenceInstance, reference.ExtraScriptableObjects, reference.ExtraPrefabs);
            }
            else
            {
                newContext.EnsureInit(referenceInstance, null, null);
            }
            
            Debug.Log($"{typeof(T).Name} registered!");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ClearStaticContext()
        {
            _instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void SetContext()
        {
            CheckSetContext(ref _instance);
        }

        protected virtual void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            CheckSetContext(ref _levelContext);
        }

        protected virtual void OnSceneUnloaded(Scene scene)
        {
            _levelContext = null;
        }
    }
}
