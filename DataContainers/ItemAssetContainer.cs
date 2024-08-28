using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleU.DataContainer
{
    [CreateAssetMenu(fileName = nameof(ItemAssetContainer), menuName = "SimpleU/DataContainer/" + nameof(ItemAssetContainer))]
    public class ItemAssetContainer : ScriptableObject
    {
        [SerializeField, HideInInspector] private List<ItemAsset> items;
        [SerializeField, HideInInspector] private int lastIndex;
    }
}
