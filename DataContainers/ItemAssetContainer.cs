using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleU.DataContainer
{
    [CreateAssetMenu(fileName = nameof(ItemAssetContainer), menuName = "SimpleU/DataContainer/" + nameof(ItemAssetContainer))]
    public class ItemAssetContainer : AssetContainer
    {
        [SerializeField, HideInInspector] protected List<ItemAsset> items;

        public List<ItemAsset> Items => items;
        public virtual string Prefix => "IA{0}";

        public virtual string GetPrefix(int sortingIndex)
        {
            return string.Format(Prefix, sortingIndex);
        }
    }

    public static class ItemAssetContainerExtensions
    {
        public static void GetTypeOfItems<T>(this ItemAssetContainer container, ref List<T> matchedItems)
            where T : ItemAsset
        {
            for (int i = 0; i < container.Items.Count; i++)
            {
                var item = container.Items[i];
                if (item is T matchedItem)
                {
                    matchedItems.Add(matchedItem);
                }
            }
        }

        public static List<T> GetTypeOfItems<T>(this ItemAssetContainer container) where T : ItemAsset
        {
            var items = new List<T>();
            container.GetTypeOfItems(ref items);
            return items;
        }
    }
}
