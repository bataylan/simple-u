using System;
using SimpleU.DataContainer;
using UnityEngine;
using UnityEngine.Assertions;

namespace SimpleU.Inventory
{
    public static class InventoryManagerService
    {
        public static bool CanAddItemQuantity(IManagedInventoryManager inventory, IItemAsset inventoryItem,
            int quantity, out int leftQuantity, bool returnCompletedAll = true)
        {
            CheckAddItem_Internal(inventory, inventoryItem, quantity, false, out leftQuantity, out _);
            return returnCompletedAll ? leftQuantity == 0 : leftQuantity != quantity;
        }

        public static bool TryAddItemQuantity(IManagedInventoryManager inventory, IItemAsset inventoryItem, int quantity,
            out int leftQuantity, bool returnCompletedAll = true)
        {
            CheckAddItem_Internal(inventory, inventoryItem, quantity, true, out leftQuantity, out _);
            return returnCompletedAll ? leftQuantity == 0 : leftQuantity != quantity;
        }

        public static bool CanAddItemToSlot(IGridSlot targetSlot, IItemAsset itemAsset,
            int quantity, out int leftQuantity, bool returnCompletedAll = true)
        {
            int refLeftQuantity = quantity;
            CanAddItemToSlot_Internal(targetSlot as IManagedGridSlot, itemAsset, quantity > 0, false, ref refLeftQuantity);
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

        public static bool TryMoveItem(IManagedInventoryManager sourceInventory, IManagedInventoryManager targetInventory, IItemAsset itemAsset,
            int quantity, out int leftQuantity, bool returnCompletedAll = true)
        {
            CheckAddItem_Internal(targetInventory, itemAsset, quantity, false, out int addLeftQuantity, out _);
            int maxAddQuantity = quantity - addLeftQuantity;
            int removeQuantity = -maxAddQuantity;

            CheckAddItem_Internal(sourceInventory, itemAsset, removeQuantity, false, out int removeLeftQuantity, out _);
            int moveQuantity = maxAddQuantity + removeLeftQuantity;

            if (moveQuantity == 0)
            {
                leftQuantity = quantity;
                return false;
            }

            CheckAddItem_Internal(sourceInventory, itemAsset, -moveQuantity, true, out int moveLeftQuantity, out var removedSingleSlot);
            Assert.IsTrue(moveLeftQuantity == 0);
            CheckAddItem_Internal(targetInventory, itemAsset, moveQuantity, false, out moveLeftQuantity, out _, removedSingleSlot);
            Assert.IsTrue(moveLeftQuantity == 0);

            leftQuantity = quantity - moveQuantity;
            return true;
        }

        public static bool CanMoveNonStackedItem(IGridSlot sourceSlot, IGridSlot targetSlot)
        {
            if (sourceSlot.ItemAsset.IsStackable || targetSlot.Quantity != 1)
                return false;

            if (!targetSlot.IsEmpty)
                return false;


        }

        //current, multiple slot support
        private static void CheckAddItem_Internal(IManagedInventoryManager inventory, IItemAsset itemAsset,
            int quantity, bool doAdd, out int leftQuantity, out object dataFromSingleSlot, object setData = null)
        {
            leftQuantity = quantity;
            dataFromSingleSlot = null;

            if (itemAsset == null || quantity == 0)
                return;

            bool isAdd = quantity > 0;
            bool itemAddedFirstTime = false;

            for (int i = 0; i < inventory.SlotCount; i++)
            {
                var slot = inventory.ManagedGridSlots[i];

                CanAddItemToSlot_Internal(slot, itemAsset, isAdd, doAdd, ref leftQuantity, setData);

                if (leftQuantity == 0)
                {
                    if (!itemAddedFirstTime)
                    {
                        dataFromSingleSlot = slot.GetData();
                    }

                    break;
                }

                if (!itemAddedFirstTime && leftQuantity != quantity)
                    itemAddedFirstTime = true;
            }
        }

        private static bool CanAddItemSlotToInventory(IGridSlot sourceSlot, IInventoryManager targetInventory,
            IItemAsset itemAsset, int quantity, bool apply, out int leftQuantity, bool returnCompletedAllQuantity = true)
        {
            if (quantity == 0 || itemAsset == null)
                throw new Exception("Quantity and item asset can't be null");

            if (targetInventory == null && sourceSlot == null)
                throw new Exception("Source and target can't be null");

            if (quantity > 0)
            {
                CanAddItemSlotToInventory_Internal((IManagedGridSlot)sourceSlot,
                    (IManagedInventoryManager)targetInventory, itemAsset, quantity, apply, out leftQuantity);
            }
            else
            {
                CanAddItemInventoryToSlot_Internal((IManagedInventoryManager)targetInventory,
                    (IManagedGridSlot)sourceSlot, itemAsset, quantity, apply, out leftQuantity);
            }

            return returnCompletedAllQuantity ? leftQuantity == 0 : leftQuantity != quantity;
        }

        private static void CanAddItemSlotToInventory_Internal(IManagedGridSlot sourceSlot,
            IManagedInventoryManager targetInventory, IItemAsset itemAsset,
            int quantity, bool apply, out int leftQuantity)
        {
            int removableQuantity = quantity;
            if (sourceSlot != null)
                sourceSlot.CheckRemoveItem(itemAsset, quantity, false, out removableQuantity);

            int addableQuantity = removableQuantity;
            if (targetInventory != null)
            {
                int totalAddedQuantity = 0;
                for (int i = 0; i < targetInventory.SlotCount; i++)
                {
                    var slot = targetInventory.ManagedGridSlots[i];
                    slot.CheckAddItem(sourceSlot, itemAsset, addableQuantity - totalAddedQuantity, false,
                        out int addedQuantity);
                    totalAddedQuantity += addedQuantity;

                    if (totalAddedQuantity >= addableQuantity)
                        break;
                }
            }

            int usableQuantity = Mathf.Max(removableQuantity, addableQuantity);
            if (usableQuantity == 0)
            {
                leftQuantity = quantity;
                return;
            }

            leftQuantity = quantity - usableQuantity;

            if (apply)
            {
                for (int i = 0; i < targetInventory.SlotCount; i++)
                {
                    CanAddItemSlotToSlot_Internal(sourceSlot, targetInventory.ManagedGridSlots[i], itemAsset,
                        usableQuantity, true, out int addedQuantity);
                    usableQuantity -= addedQuantity;

                    if (usableQuantity <= 0)
                        break;
                }

                Assert.AreEqual(usableQuantity, 0);
            }
        }

        private static void CanAddItemInventoryToSlot_Internal(IManagedInventoryManager sourceInventory,
            IManagedGridSlot targetSlot, IItemAsset itemAsset,
            int quantity, bool apply, out int leftQuantity)
        {
            int addableQuantity = quantity;
            if (targetSlot != null)
                targetSlot.CheckAddItem(null, itemAsset, quantity, false, out addableQuantity);

            int removableQuantity = addableQuantity;
            if (sourceInventory != null)
            {
                int totalRemovedQuantity = 0;
                for (int i = 0; i < sourceInventory.SlotCount; i++)
                {
                    var slot = sourceInventory.ManagedGridSlots[i];
                    slot.CheckRemoveItem(itemAsset, removableQuantity - totalRemovedQuantity, false,
                        out int removedQuantity);
                    totalRemovedQuantity += removedQuantity;

                    if (totalRemovedQuantity >= removableQuantity)
                        break;
                }
                removableQuantity = totalRemovedQuantity;
            }

            int usableQuantity = Mathf.Max(removableQuantity, addableQuantity);
            if (usableQuantity == 0)
            {
                leftQuantity = quantity;
                return;
            }


            leftQuantity = quantity - usableQuantity;

            if (apply)
            {
                for (int i = 0; i < sourceInventory.SlotCount; i++)
                {
                    CanAddItemSlotToSlot_Internal(sourceInventory.ManagedGridSlots[i], targetSlot, itemAsset,
                        usableQuantity, true, out int addedQuantity);
                    usableQuantity -= addedQuantity;

                    if (usableQuantity <= 0)
                        break;
                }

                Assert.AreEqual(usableQuantity, 0);
            }
        }

        private static bool CanAddItemSlotToSlot(IGridSlot sourceSlot, IGridSlot targetSlot, IItemAsset itemAsset,
            int quantity, bool apply, out int leftQuantity, bool returnCompletedAllQuantity = true)
        {
            if (quantity == 0 || itemAsset == null)
                throw new Exception("Quantity and item asset can't be null");

            if (targetSlot == null && sourceSlot == null)
                throw new Exception("Source and target can't be null");

            CanAddItemSlotToSlot_Internal((IManagedGridSlot)sourceSlot, (IManagedGridSlot)targetSlot,
                    itemAsset, quantity, apply, out leftQuantity);

            return returnCompletedAllQuantity ? leftQuantity == 0 : leftQuantity != quantity;
        }

        private static void CanAddItemSlotToSlot_Internal(IManagedGridSlot sourceSlot,
            IManagedGridSlot targetSlot, IItemAsset itemAsset, int quantity, bool apply, out int leftQuantity)
        {
            int addableQuantity = quantity;
            if (targetSlot != null)
                targetSlot.CheckAddQuantity(sourceSlot, itemAsset, quantity, false, out addableQuantity);

            int removableQuantity = addableQuantity;
            if (sourceSlot != null)
                sourceSlot.CheckRemoveItem(itemAsset, quantity, false, out removableQuantity);

            int usableQuantity = Mathf.Min(addableQuantity, removableQuantity);
            if (usableQuantity == 0)
            {
                leftQuantity = quantity;
                return;
            }

            leftQuantity = quantity - usableQuantity;

            if (apply)
            {
                if (targetSlot != null)
                {
                    targetSlot.CheckAddItem(sourceSlot, itemAsset, usableQuantity, true, out int addedQuantity);
                    Assert.AreEqual(addedQuantity, usableQuantity);
                }

                if (sourceSlot != null)
                {
                    sourceSlot.CheckRemoveItem(itemAsset, usableQuantity, true, out int removedQuantity);
                    Assert.AreEqual(removedQuantity, usableQuantity);
                }
            }
        }

        private static void CanAddItemToSlot_Internal(IManagedGridSlot slot, IItemAsset itemAsset,
            bool isAdd, bool doAdd, ref int leftQuantity, object setData = null)
        {
            if (slot.HasItem)
            {
                CheckStack_Internal(slot, itemAsset, isAdd, doAdd, ref leftQuantity, setData);
            }
            else
            {
                if (!isAdd)
                    return;

                CheckSet_Internal(slot, itemAsset, doAdd, ref leftQuantity, setData);
            }
        }

        private static void CheckStack_Internal(IManagedGridSlot slot, IItemAsset itemAsset, bool isAdd, bool doAdd,
            ref int leftQuantity, object setData = null)
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
                slot.AddQuantity(addedQuantity, setData);
            }
        }

        internal static void CheckSet_Internal(IManagedGridSlot slot, IItemAsset itemAsset, bool doAdd,
            ref int leftQuantity, object setData = null)
        {
            //try add on empty slotq
            int usedCapacity = itemAsset != null && !itemAsset.IsStackable ? Mathf.Min(leftQuantity, 1) : Mathf.Min(leftQuantity, slot.Capacity);
            leftQuantity -= usedCapacity;

            if (doAdd)
            {
                slot.SetItem(itemAsset, usedCapacity, setData);
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
