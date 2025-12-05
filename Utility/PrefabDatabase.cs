using System;
using System.Collections.Generic;
using SimpleU.Utility;
using UnityEngine;

namespace SimpleU.Utility
{
    public class PrefabDatabase<T> : ScriptableObject where T : Component
    {
        public string parentFolder = "Assets/Game/";
        public Map prefabs;
        
#if UNITY_EDITOR
        public void FetchAllItems()
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Prefab", new string[] { parentFolder });
            var list = new List<T>();

            for (int i = 0; i < guids.Length; i++)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
                var go = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (go.TryGetComponent<T>(out var networkObject))
                {
                    list.Add(networkObject);
                }
            }

            prefabs.FetchWithList(list);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        void OnValidate()
        {
            if (Application.isPlaying || UnityEditor.EditorApplication.isUpdating)
                return;

            FetchAllItems();
        }
#endif
        
        [Serializable]
        public class Map : IdentifiedSet<T> { }
    }
}
