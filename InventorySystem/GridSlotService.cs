using System;
using SimpleU.DataContainer;
using UnityEngine;

namespace SimpleU.Inventory
{
    public static class GridSlotService
    {
        //TODO refactor ref/struct usages
        public static void SetItem<TSlot>(ref TSlot gridSlot, IItemAsset itemAsset, int quantity,
            int originalSlotIndex = -1) where TSlot : struct, IServicableGridSlot
        {
            SetItemInternal(ref gridSlot, itemAsset, quantity, originalSlotIndex);
        }

        public static void SetItem(GridSlot gridSlot, IItemAsset itemAsset, int quantity,
            int originalSlotIndex = -1)
        {
            bool wasEmpty = gridSlot.IsEmpty;
            bool wasOriginalItemOwner = gridSlot.HasOriginalItem;
            
            SetItemInternal(ref gridSlot, itemAsset, quantity, originalSlotIndex);
            
            if (wasEmpty && gridSlot.HasOriginalItem)
            {
                gridSlot.OnEmptinessChange?.Invoke(gridSlot);
            }
            else if (wasOriginalItemOwner && gridSlot.IsEmpty)
            {
                gridSlot.OnEmptinessChange?.Invoke(gridSlot);
            }
        }

        public static void SetItemInternal<TSlot>(ref TSlot gridSlot, IItemAsset itemAsset, int quantity,
            int originalSlotIndex = -1) where TSlot : IServicableGridSlot
        {
            if (itemAsset == null)
            {
                gridSlot.SetQuantityItem(default);
            }
            else
            {
                var quantityItem = new QuantityItem()
                {
                    itemAsset = itemAsset,
                    quantity = quantity
                };
                gridSlot.SetQuantityItem(quantityItem);
            }

            gridSlot.SetOriginalSlotIndex(originalSlotIndex);
        }
        
        public static bool GetIsDroppableToTargetSlot(this IGridSlot gridSlot, IGridSlot targetSlot)
        {
            if (gridSlot.IsEmpty || targetSlot == gridSlot)
                return true;

            if (gridSlot.IsRelativeSlot && gridSlot.InventoryManager == targetSlot.InventoryManager
                && gridSlot.OriginalSlotIndex == targetSlot.OriginalSlotIndex)
            {
                return true;
            }

            return false;
        }

        public static bool GetIsStackableToTargetGridSlot(this IGridSlot gridSlot, IGridSlot targetSlot)
        {
            var target = targetSlot as GridSlot;
            return !gridSlot.IsEmpty && !target.IsEmpty && target.IsStackable(gridSlot.ItemAsset, gridSlot.Quantity);
        }

        public static bool IsStackable(this IGridSlot gridSlot, IItemAsset itemAsset, int count)
        {
            return HasCapacity(gridSlot, count) && (gridSlot.IsEmpty || gridSlot.ItemAsset.Equals(itemAsset));
        }

        public static int LeftCapacity(this IGridSlot gridSlot)
        {
            return gridSlot.Capacity - gridSlot.Quantity;
        }
        
        public static bool HasCapacity(this IGridSlot gridSlot, int count)
        {
            return (gridSlot.Quantity + count) <= gridSlot.Capacity;
        }
    }
}
