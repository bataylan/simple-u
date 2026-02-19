using System;
using SimpleU.DataContainer;
using UnityEngine;

namespace SimpleU.Inventory
{
    public static class GridSlotService
    {
        //TODO refactor ref/struct usages
        public static void SetQuantity(GridSlot gridSlot, int value)
        {
            SetQuantityInternal(ref gridSlot, value);
            gridSlot.OnQuantityChange?.Invoke(gridSlot);
        }

        

        public static void SetQuantityInternal<TSlot>(ref TSlot gridSlot, int value)
            where TSlot : IServicableGridSlot
        {
            int safeQuantity = Mathf.Max(value, 0);
            if (safeQuantity == gridSlot.Quantity)
                return;

            if (safeQuantity <= 0)
            {
                gridSlot.SetItem(null, 0);
            }
            else
            {
                if (gridSlot.QuantityItem == null)
                    throw new Exception("QuantityItem is null but trying to set quantity!");

                gridSlot.QuantityItem.SetQuantity(safeQuantity);
            }
        }

        public static bool GetIsDroppableToTargetSlot(IServicableGridSlot gridSlot, IGridSlot targetSlot)
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

        public static bool GetIsStackableToTargetGridSlot(IServicableGridSlot gridSlot, IGridSlot targetSlot)
        {
            var target = targetSlot as GridSlot;
            return !gridSlot.IsEmpty && !target.IsEmpty && target.IsStackable(gridSlot.ItemAsset, gridSlot.Quantity);
        }

        public static bool IsStackable(IServicableGridSlot gridSlot, IItemAsset itemAsset, int count)
        {
            return HasCapacity(gridSlot, count) && (gridSlot.IsEmpty || gridSlot.ItemAsset.Equals(itemAsset));
        }

        public static int LeftCapacity(IServicableGridSlot gridSlot)
        {
            return gridSlot.Capacity - gridSlot.Quantity;
        }

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

        public static bool HasCapacity(IServicableGridSlot gridSlot, int count)
        {
            return (gridSlot.Quantity + count) <= gridSlot.Capacity;
        }
    }
}
