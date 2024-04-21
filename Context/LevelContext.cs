using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
                        Debug.Log("LevelContext already exist!");
                        Destroy(value);
                        return;
                    }
                }

                value.gameObject.name = nameof(LevelContext);
                Debug.Log("GameContext registered!");
            }
        }

        private static LevelContext _instance;
        

        void Awake()
        {
            Debug.Log("LevelContext Awake");
            if (Instance != null && Instance != this)
            {
                Debug.Log("LevelContext already exist!");
                Destroy(this);
                return;
            }

            _instance = this;
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
