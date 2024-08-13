using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace SimpleU.Context
{
    [DefaultExecutionOrder(-100)]
    public class GameContext : MonoBehaviour
    {
        public static GameContext Instance
        {
            get
            {
                _instance = CheckGetGameContext(_instance);
                return _instance;
            }
            private set
            {
                _instance = SetGameContext(_instance, value);
            }
        }
        private static GameContext _instance;

        public LevelContext LevelContext
        {
            get
            {
                _levelContext = CheckGetLevelContext(_levelContext);
                return _levelContext;
            }
            private set
            {
                _levelContext = SetLevelContext(_levelContext, value);
            }
        }
        private LevelContext _levelContext;

        public virtual T GetLevelContext<T>() where T : LevelContext => _levelContext as T;

        protected static T CheckGetGameContext<T>(T currentContext) where T : GameContext
        {
            if (currentContext)
                return currentContext;

            var newContext = FindObjectOfType<T>();
            if (newContext)
            {
                SetGameContext(currentContext, newContext);
                return newContext;
            }

            var gameObject = new GameObject();
            newContext = gameObject.AddComponent<T>();

            return SetGameContext(currentContext, newContext);
        }

        protected static T SetGameContext<T>(T context, T newContext) where T : GameContext
        {
            if (context != null)
            {
                if (context == newContext)
                {
                    return context;
                }
                else
                {
                    Debug.Log($"{typeof(T)} already exist!");
                    Destroy(newContext);
                    return context;
                }
            }

            context = newContext;
            newContext.gameObject.name = typeof(T).Name;
            DontDestroyOnLoad(newContext.gameObject);
            Debug.Log($"{typeof(T)} registered!");
            return context;
        }

        protected virtual T CheckGetLevelContext<T>(T currentContext) where T : LevelContext
        {
            if (currentContext)
                return currentContext;

            var levelContext = FindObjectOfType<T>();
            if (levelContext)
            {
                SetLevelContext(currentContext, levelContext);
                return levelContext;
            }

            var gameObject = new GameObject();
            levelContext = gameObject.AddComponent<T>();

            SetLevelContext(currentContext, levelContext);
            return levelContext;
        }

        protected virtual T SetLevelContext<T>(T context, T newContext) where T : LevelContext
        {
            if (newContext == null)
            {
                if (context)
                    Destroy(context.gameObject);

                Debug.Log($"{typeof(T)} set to null");
                context = null;
                return context;
            }
            else if (context == newContext)
            {
                return context;
            }

            if (context != null)
            {
                Debug.LogError($"{typeof(T)} already exist!");
                return context;
            }

            context = newContext;
            context.gameObject.name = typeof(T).Name;
            Debug.Log($"{typeof(T)} registered!");
            return context;
        }

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
        public UnityEvent<LevelStatus> onLevelStatusChange;

        private int _sceneIndex = 0;

        protected virtual void Awake()
        {
            Instance = this;
            RegisterInitialExtras();
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        void Update()
        {
            UpdateManager.Update();
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

        protected virtual void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            LevelContext.onStatusChange.AddListener(OnLevelContextStatusChange);
        }

        protected virtual void OnLevelContextStatusChange(LevelStatus status)
        {
            onLevelStatusChange.Invoke(status);
        }

        protected virtual void OnSceneUnloaded(Scene scene)
        {
            SetLevelContext(LevelContext, null);
        }

        public void TryChangeScene(LevelContext levelContext, int sceneIndex)
        {
            if (levelContext == null)
                return;

            if (sceneIndex >= SceneManager.sceneCountInBuildSettings)
            {
                Debug.Log("LastLevelFinish");
                return;
            }

            SceneManager.LoadScene(sceneIndex);
            _sceneIndex = sceneIndex;
        }

        protected static T Get<T>() where T : GameContext
        {
            return Instance as T;
        }
    }
}
