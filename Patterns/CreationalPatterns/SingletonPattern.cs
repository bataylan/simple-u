using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleU.Pattern.Singleton
{
    //simply: only one instance of a class
    //common and simple
    //easy to use, may cause a lot of trouble
    //too much reference, too powerful
    public class ExampleUsage
    {
        public void Run()
        {
            ConcreteSingleton.Instance.DoSometing();
        }
    }

    public class ConcreteSingleton
    {
        public static ConcreteSingleton Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ConcreteSingleton();

                return _instance;
            }
        }
        private static ConcreteSingleton _instance;

        public void DoSometing()
        {

        }
    }

    public class UnityRuntimeSingleton : MonoBehaviour
    {
        public static UnityRuntimeSingleton Instance
        {
            get
            {
                if (_instance == null)
                {
                    var empty = new GameObject();
                    _instance = empty.AddComponent<UnityRuntimeSingleton>();
                }

                return _instance;
            }
        }
        private static UnityRuntimeSingleton _instance;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}