using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
            set
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

        private int _sceneLoadCount = 0;

        void Awake()
        {
            Instance = this;
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
