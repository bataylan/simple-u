using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleU.DataContainer
{
    [CreateAssetMenu(fileName = nameof(ItemAsset), menuName = "SimpleU/DataContainer/" + nameof(ItemAsset))]
    public class ItemAsset : ScriptableObject
    {
        public virtual string GetAssetName(int id) => "IA-" + id;
    }
}
