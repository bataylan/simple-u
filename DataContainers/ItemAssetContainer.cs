using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleU.DataContainer
{
    [CreateAssetMenu(fileName = nameof(ItemAssetContainer), menuName = "SimpleU/DataContainer/" + nameof(ItemAssetContainer))]
    public class ItemAssetContainer : ItemAssetContainer<ItemAsset>
    {

    }

    public class ItemAssetContainer<T> : ScriptableObject where T : ScriptableObject, IItemAsset
    {
        [SerializeField, HideInInspector] protected List<T> items;
        [SerializeField, HideInInspector] protected int lastIndex;

        public List<T> Items => items;
        public virtual string Prefix => "IA{0}";

        public virtual string GetPrefix(int sortingIndex)
        {
            return string.Format(Prefix, sortingIndex);
        }

        
    }
}
