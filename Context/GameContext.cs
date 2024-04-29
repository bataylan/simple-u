using System;
using System.Collections;
using System.Collections.Generic;
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

        public UnityEvent<LevelContext.LevelStatus> onLevelStatusChange;

        private int _sceneLoadCount = 0;

        void Awake()
        {
            Instance = this;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            LevelContext.onStatusChange.AddListener(OnLevelContextStatusChange);
        }

        private void OnLevelContextStatusChange(LevelContext.LevelStatus status)
        {
            onLevelStatusChange.Invoke(status);
        }

        private void OnSceneUnloaded(Scene scene)
        {
            LevelContext = null;
        }

        void Update()
        {
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.W))
            {
                _sceneLoadCount++;
                SceneManager.LoadScene(_sceneLoadCount % SceneManager.sceneCount);
            }
#endif
        }
    }
}
