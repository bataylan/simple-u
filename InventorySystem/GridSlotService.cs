using System;
using SimpleU.DataContainer;
using UnityEngine;

namespace SimpleU.Inventory
{
    internal static class GridSlotService
    {
        public static bool GetIsDroppableToTargetSlot(IGridSlot gridSlot, IGridSlot targetSlot)
        {
            return gridSlot.IsEmpty || targetSlot == gridSlot;
        }

        public static bool GetIsStackableToTargetGridSlot(IGridSlot gridSlot, IGridSlot targetSlot)
        {
            var target = targetSlot as GridSlot;
            return !gridSlot.IsEmpty && !target.IsEmpty && target.IsStackable(gridSlot.ItemAsset, gridSlot.Quantity);
        }

        public static bool IsStackable(IGridSlot gridSlot, IItemAsset itemAsset, int count)
        {
            return HasCapacity(gridSlot, count) && (gridSlot.IsEmpty || gridSlot.ItemAsset.Equals(itemAsset));
        }

        public static int LeftCapacity(IGridSlot gridSlot)
        {
            return gridSlot.Capacity - gridSlot.Quantity;
        }
        
        public static bool HasCapacity(IGridSlot gridSlot, int count)
        {
            return (gridSlot.Quantity + count) <= gridSlot.Capacity;
        }
    }
}
