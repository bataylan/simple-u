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

        
    }

    public interface IContextReference
    {
        public ScriptableObject[] ExtraScriptableObjects { get; }
        public GameObject[] ExtraPrefabs { get; }
    }
}
