using SimpleU.DataContainer;
using UnityEngine;

namespace SimpleU.Inventory
{
    public static class InventoryManagerService
    {
        public static bool CanAddItemQuantity(IInventoryManager inventory, IItemAsset inventoryItem,
            int quantity, out int leftQuantity)
        {
            CheckAddItem_Internal(inventory, inventoryItem, quantity, false, out leftQuantity);
            return leftQuantity != quantity;
        }

        public static bool TryAddItemQuantity(IInventoryManager inventory, IItemAsset inventoryItem, int quantity,
            out int leftQuantity)
        {
            CheckAddItem_Internal(inventory, inventoryItem, quantity, true, out leftQuantity);
            return leftQuantity != quantity;
        }

        //current, multiple slot support
        private static void CheckAddItem_Internal(IInventoryManager inventory, IItemAsset itemAsset, int quantity,
            bool doAdd, out int leftQuantity)
        {
            leftQuantity = quantity;

            if (itemAsset == null || quantity == 0)
                return;

            bool isAdd = quantity > 0;

            for (int i = 0; i < inventory.SlotCount; i++)
            {
                var slot = inventory.GridSlots[i];

                CanAddItemToSlot_Internal(slot, itemAsset, isAdd, doAdd, ref leftQuantity);

                if (leftQuantity == 0)
                    break;
            }
        }

        private static void CanAddItemToSlot_Internal(IGridSlot slot, IItemAsset itemAsset, bool isAdd, bool doAdd, ref int leftQuantity)
        {
            if (slot.HasItem)
            {
                CheckStack_Internal(slot, itemAsset, isAdd, doAdd, ref leftQuantity);
            }
            else
            {
                if (!isAdd)
                    return;

                CheckSet_Internal(slot, itemAsset, ref leftQuantity);
            }
        }

        private static void CheckStack_Internal(IGridSlot slot, IItemAsset itemAsset, bool isAdd, bool doAdd, ref int leftQuantity)
        {
            if (isAdd && !itemAsset.IsStackable)
                return;

            //try stack on same item
            if (!slot.ItemAsset.Equals(itemAsset))
                return;

            int slotLeftCapacity = isAdd ? slot.LeftCapacity() : slot.Quantity;
            if (slotLeftCapacity <= 0)
                return;

            int addedQuantity = 0;
            if (!isAdd)
                addedQuantity = -Mathf.Min(Mathf.Abs(leftQuantity), slotLeftCapacity);
            else
                addedQuantity = Mathf.Min(leftQuantity, slotLeftCapacity);

            if (addedQuantity == 0)
                return;

            leftQuantity -= addedQuantity;

            if (doAdd)
            {
                slot.AddQuantity(addedQuantity);
            }

            return;
        }

        internal static void CheckSet_Internal(IGridSlot slot, IItemAsset itemAsset, ref int leftQuantity)
        {
            //try add on empty slot
            int usedCapacity = itemAsset.IsStackable ? Mathf.Min(leftQuantity, 1) : Mathf.Min(leftQuantity, slot.Capacity);
            leftQuantity -= usedCapacity;

            var quantityItem = new QuantityItem
            {
                itemAsset = itemAsset,
                quantity = usedCapacity
            };

            slot.SetItem(quantityItem.ItemAsset, quantityItem.Quantity);
        }

        public static bool HasEnoughQuantity(IInventoryManager inventory, IItemAsset itemAsset, int quantity)
        {
            return GetQuantity(inventory, itemAsset) >= quantity;
        }

        public static int GetQuantity(IInventoryManager inventory, IItemAsset itemAsset)
        {
            if (itemAsset == null)
                return 0;

            int quantity = 0;
            for (int i = 0; i < inventory.SlotCount; i++)
            {
                var slot = inventory.GridSlots[i];
                if (slot.HasItem && slot.ItemAsset.Equals(itemAsset))
                {
                    quantity += slot.Quantity;
                }
            }

            return quantity;
        }

        public static int GetIndexByRowColumnIndex(int rowIndex, int columnIndex, int columnCount)
            => IGridSlot.GetIndexByRowColumnIndex(rowIndex, columnIndex, columnCount);
    }
}
