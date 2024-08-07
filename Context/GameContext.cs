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
                if (_instance)
                    return _instance;

                var gameObject = new GameObject();
                var gameContext = gameObject.AddComponent<GameContext>();

                Instance = gameContext;
                return _instance;
            }
            private set
            {
                if (_instance != null)
                {
                    if (_instance == value)
                    {
                        return;
                    }
                    else
                    {
                        Debug.Log("GameContext already exist!");
                        Destroy(value);
                        return;
                    }
                }

                _instance = value;
                value.gameObject.name = nameof(GameContext);
                DontDestroyOnLoad(value.gameObject);
                Debug.Log("GameContext registered!");
            }
        }
        private static GameContext _instance;

        public LevelContext LevelContext
        {
            get
            {
                if (_levelContext)
                    return _levelContext;

                var levelContext = FindObjectOfType<LevelContext>();
                if (levelContext)
                {
                    LevelContext = levelContext;
                    return _levelContext;
                }

                var gameObject = new GameObject();
                levelContext = gameObject.AddComponent<LevelContext>();

                LevelContext = levelContext;
                return _levelContext;
            }
            private set
            {
                if (value == null)
                {
                    if (_levelContext)
                        Destroy(_levelContext.gameObject);
                        
                    Debug.Log("LevelContext set to null");
                    _levelContext = null;
                    return;
                }
                else if (_levelContext == value)
                {
                    return;
                }

                if (_levelContext != null)
                {
                    Debug.Log("LevelContext already exist!");
                    Destroy(_levelContext.gameObject);
                }

                _levelContext = value;
                _levelContext.gameObject.name = nameof(LevelContext);
                Debug.Log("LevelContext registered!");
            }
        }
        private LevelContext _levelContext;

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

        [SerializeField] private ExtraScriptableObject[]  _extraScriptableObjects;
        public UnityEvent<LevelStatus> onLevelStatusChange;

        private int _sceneIndex = 0;

        protected virtual void Awake()
        {
            Instance = this;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            RegisterInitialExtras();
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
                    var extraType = _extraScriptableObjects[i].GetType();
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
            LevelContext = null;
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

        public static T GetInstance<T>() where T : GameContext
        {
            return Instance as T;
        }

        public static T GetLevelContext<T>() where T : LevelContext
        {
            return Instance.LevelContext as T;
        }
    }
}
