using System;
using UnityEngine;

namespace SimpleU.Context
{
    public class ContextReferenceBehaviour<T> : MonoBehaviour, IContextReference where T : ABaseContext
    {
        [SerializeField] private ScriptableObject[] extraScriptableObjects;
        [SerializeField] private GameObject[] extraPrefabs;

        public ScriptableObject[] ExtraScriptableObjects => extraScriptableObjects;
        public GameObject[] ExtraPrefabs => extraPrefabs;

        public Action TriggerOnDestroy { get; set; }

        protected virtual void OnDestroy()
        {
            var temp = TriggerOnDestroy;
            TriggerOnDestroy = null;
            temp.Invoke();
        }
    }

    public interface IContextReference
    {
        public ScriptableObject[] ExtraScriptableObjects { get; }
        public GameObject[] ExtraPrefabs { get; }
        public Action TriggerOnDestroy { get; }
    }
}
