using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleU.DataContainer
{
    [CreateAssetMenu(fileName = nameof(ItemAsset), menuName = "SimpleU/DataContainer/" + nameof(ItemAsset))]
    public class ItemAsset : ScriptableObject, IItemAsset
    {
        string IItemAsset.GetAssetName(int id) => "IA-" + id;
    }

    public interface IItemAsset
    {
        public string GetAssetName(int id);
    }
}
