using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleU.DataContainer
{
    public class ItemAsset : ScriptableObject, IItemAsset
    {
        string IItemAsset.GetAssetName(int id) => "IA-" + id;
    }

    public interface IItemAsset
    {
        public string GetAssetName(int id);
    }
}
