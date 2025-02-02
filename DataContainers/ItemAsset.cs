using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleU.DataContainer
{
    public class ItemAsset : ScriptableObject, IItemAsset
    {
        [SerializeField] private RowColumnIndex[] relativeSlotIndexes = new RowColumnIndex[1] { new RowColumnIndex(0, 0) };

        public RowColumnIndex[] RelativeSlotIndexes => relativeSlotIndexes;
    }

    public interface IItemAsset
    {
        public RowColumnIndex[] RelativeSlotIndexes { get; }
    }

    [Serializable]
    public struct RowColumnIndex
    {
        public int rowIndex;
        public int columnIndex;

        public RowColumnIndex(int rowIndex, int columnIndex)
        {
            this.rowIndex = rowIndex;
            this.columnIndex = columnIndex;
        }
    }
}
