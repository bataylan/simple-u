using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace SimpleU.Context
{
    [DefaultExecutionOrder(-99)]
    public abstract class ALevelContext<T> : MonoBehaviour where T : MonoBehaviour
    {
        //level based singleton integration, support scene placed and runtime created
        public static T Instance
        {
            get
            {
                if (_instance)
                    return _instance;

                var levelContext = FindObjectOfType<T>();
                if (levelContext)
                {
                    Instance = levelContext;
                    return _instance;
                }

                var gameObject = new GameObject();
                levelContext = gameObject.AddComponent<T>();

                Instance = levelContext;
                return _instance;
            }
            private set
            {
                if (value == null)
                {
                    if (_instance)
                        Destroy(_instance.gameObject);
                        
                    Debug.Log("LevelContext set to null");
                    _instance = null;
                    return;
                }
                else if (_instance == value)
                {
                    return;
                }

                if (_instance != null)
                {
                    Debug.Log("LevelContext already exist!");
                    Destroy(_instance.gameObject);
                }

                _instance = value;
                _instance.gameObject.name = nameof(T);
                Debug.Log("LevelContext registered!");
            }
        }
        private static T _instance;

        public LevelStatus Status
        {
            get
            {
                return _status;
            }
            protected set
            {
                if (value == _status)
                    return;

                _status = value;
                onStatusChange.Invoke(_status);
            }
        }
        private LevelStatus _status = LevelStatus.Prepare;

        public UnityEvent<LevelStatus> onStatusChange;

        void Awake()
        {
            Instance = this as T;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnSceneUnloaded(Scene scene)
        {
            Instance = default;
        }

        public enum LevelStatus
        {
            Prepare,
            Start,
            Finish
        }
    }
}
