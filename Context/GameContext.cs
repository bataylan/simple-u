using SimpleU.Logger;
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
            newContext.gameObject.name = typeof(T).Name;

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
                    LogUtility.Log($"{typeof(T).Name} already exist!");
                    Destroy(newContext);
                    return context;
                }
            }

            context = newContext;
            DontDestroyOnLoad(newContext.gameObject);
            LogUtility.Log($"{typeof(T).Name} registered!");
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
            levelContext.gameObject.name = typeof(T).Name;

            SetLevelContext(currentContext, levelContext);
            return levelContext;
        }

        protected virtual T SetLevelContext<T>(T context, T newContext) where T : LevelContext
        {
            if (newContext == null)
            {
                if (context)
                    Destroy(context.gameObject);

                this.Log($"{typeof(T).Name} set to null", Color.yellow);
                context = null;
                return context;
            }
            else if (context == newContext)
            {
                return context;
            }

            if (context != null)
            {
                this.LogError($"{typeof(T).Name} already exist!");
                return context;
            }

            context = newContext;
            this.Log($"{typeof(T).Name} registered!");
            return context;
        }

        
        public UnityEvent<LevelStatus> onLevelStatusChange;

        private int _sceneIndex = 0;

        protected override void Awake()
        {
            base.Awake();
            Instance = this;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
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
                this.Log("LastLevelFinish");
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
