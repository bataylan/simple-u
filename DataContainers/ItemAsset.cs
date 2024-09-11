using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleU.DataContainer
{
    public class ItemAsset : ScriptableObject, IItemAsset
    {
        [SerializeField] private Vector2Int[] relativeSlotIndexes = new Vector2Int[1] { new Vector2Int(0, 0) };

        public Vector2Int[] RelativeSlotIndexes => relativeSlotIndexes;
    }

    public interface IItemAsset
    {
        public Vector2Int[] RelativeSlotIndexes { get; }
    }
}
