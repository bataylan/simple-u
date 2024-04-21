using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SimpleU.Context
{
    [DefaultExecutionOrder(-99)]
    public class LevelContext : MonoBehaviour
    {
        public static LevelContext Instance
        {
            get
            {
                if (_instance)
                    return _instance;

                var gameObject = new GameObject();
                var levelContext = gameObject.AddComponent<LevelContext>();

                Instance = levelContext;
                return _instance;
            }
            private set
            {
                if (value == null)
                {
                    if (_instance)
                        Destroy(_instance.gameObject);

                    _instance = null;
                    return;
                }

                if (_instance != null)
                {
                    if (_instance == value)
                    {
                        return;
                    }
                    else
                    {
                        Debug.Log("LevelContext already exist!");
                        Destroy(_instance.gameObject);
                        return;
                    }
                }

                value.gameObject.name = nameof(LevelContext);
                Debug.Log("LevelContext registered!");
            }
        }

        private static LevelContext _instance;
        

        void Awake()
        {
            Instance = this;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnSceneUnloaded(Scene scene)
        {
            Instance = null;

        }

        void OnEnable()
        {
            Debug.Log("LevelContext OnEnable");
        }

        void Start()
        {
            Debug.Log("LevelContext Start");
        }

        void OnDisable()
        {
            Debug.Log("LevelContext OnDisable");
        }

        void OnDestroy()
        {
            Debug.Log("LevelContext OnDestroy");
        }
    }
}
