using SimpleU.DataContainer;
using UnityEngine;

namespace SimpleU.Inventory
{
    public static class InventoryManagerService
    {
        public static bool CanAddItemQuantity(IManagedInventoryManager inventory, IItemAsset inventoryItem,
            int quantity, out int leftQuantity, bool returnCompletedAll = true)
        {
            CheckAddItem_Internal(inventory, inventoryItem, quantity, false, out leftQuantity);
            return returnCompletedAll ? leftQuantity == 0 : leftQuantity != quantity;
        }

        public static bool TryAddItemQuantity(IManagedInventoryManager inventory, IItemAsset inventoryItem, int quantity,
            out int leftQuantity, bool returnCompletedAll = true)
        {
            CheckAddItem_Internal(inventory, inventoryItem, quantity, true, out leftQuantity);
            return returnCompletedAll ? leftQuantity == 0 : leftQuantity != quantity;
        }

        public static bool CanAddItemToSlot(IGridSlot slot, IItemAsset itemAsset,
            int quantity, out int leftQuantity, bool returnCompletedAll = true)
        {
            int refLeftQuantity = quantity;
            CanAddItemToSlot_Internal(slot as IManagedGridSlot, itemAsset, quantity > 0, false, ref refLeftQuantity);
            leftQuantity = refLeftQuantity;
            return returnCompletedAll ? leftQuantity == 0 : leftQuantity != quantity;
        }

        public static bool TryAddItemToSlot(IGridSlot slot, IItemAsset itemAsset,
            int quantity, out int leftQuantity, bool returnCompletedAll = true)
        {
            int refLeftQuantity = quantity;
            CanAddItemToSlot_Internal(slot as IManagedGridSlot, itemAsset, quantity > 0, true, ref refLeftQuantity);
            leftQuantity = refLeftQuantity;
            return returnCompletedAll ? leftQuantity == 0 : leftQuantity != quantity;
        }

        //current, multiple slot support
        private static void CheckAddItem_Internal(IManagedInventoryManager inventory, IItemAsset itemAsset,
            int quantity, bool doAdd, out int leftQuantity)
        {
            leftQuantity = quantity;

            if (itemAsset == null || quantity == 0)
                return;

            bool isAdd = quantity > 0;

            for (int i = 0; i < inventory.SlotCount; i++)
            {
                var slot = inventory.ManagedGridSlots[i];

                CanAddItemToSlot_Internal(slot, itemAsset, isAdd, doAdd, ref leftQuantity);

                if (leftQuantity == 0)
                    break;
            }
        }

        private static void CanAddItemToSlot_Internal(IManagedGridSlot slot, IItemAsset itemAsset,
            bool isAdd, bool doAdd, ref int leftQuantity)
        {
            if (slot.HasItem)
            {
                CheckStack_Internal(slot, itemAsset, isAdd, doAdd, ref leftQuantity);
            }
            else
            {
                if (!isAdd)
                    return;

                CheckSet_Internal(slot, itemAsset, doAdd, ref leftQuantity);
            }
        }

        private static void CheckStack_Internal(IManagedGridSlot slot, IItemAsset itemAsset, bool isAdd, bool doAdd, ref int leftQuantity)
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

        internal static void CheckSet_Internal(IManagedGridSlot slot, IItemAsset itemAsset, bool doAdd, ref int leftQuantity)
        {
            //try add on empty slotq
            int usedCapacity = itemAsset != null && !itemAsset.IsStackable ? Mathf.Min(leftQuantity, 1) : Mathf.Min(leftQuantity, slot.Capacity);
            leftQuantity -= usedCapacity;

            if (doAdd)
            {
                slot.SetItem(itemAsset, usedCapacity);
            }
        }

        public static bool HasEnoughQuantity(IManagedInventoryManager inventory, IItemAsset itemAsset, int quantity)
        {
            return GetQuantity(inventory, itemAsset) >= quantity;
        }

        public static int GetQuantity(IManagedInventoryManager inventory, IItemAsset itemAsset)
        {
            if (itemAsset == null)
                return 0;

            int quantity = 0;
            for (int i = 0; i < inventory.SlotCount; i++)
            {
                var slot = inventory.ManagedGridSlots[i];
                if (slot.HasItem && slot.ItemAsset.Equals(itemAsset))
                {
                    quantity += slot.Quantity;
                }
            }

            return quantity;
        }

        public static int GetIndexByRowColumnIndex(int rowIndex, int columnIndex, int columnCount)
            => IManagedGridSlot.GetIndexByRowColumnIndex(rowIndex, columnIndex, columnCount);
    }
}
