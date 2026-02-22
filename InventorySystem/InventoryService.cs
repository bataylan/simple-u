using System;
using SimpleU.DataContainer;
using UnityEngine;
using UnityEngine.Assertions;

namespace SimpleU.Inventory
{
    public static class InventoryService
    {
        public static bool CheckAddItem(IInventoryManager sourceInventory,
            IInventoryManager targetInventory, IItemAsset itemAsset, int quantity, bool apply,
            out int leftQuantity, bool returnCompletedAllQuantity = true)
        {
            if (quantity == 0 || itemAsset == null)
                throw new Exception("Quantity and item asset can't be null");

            if (sourceInventory == null && targetInventory == null)
                throw new Exception("Source and target can't be null");

            CheckAddItem_Internal((IManagedInventoryManager)sourceInventory,
                (IManagedInventoryManager)targetInventory, itemAsset, quantity, apply, out leftQuantity);

            return returnCompletedAllQuantity ? leftQuantity == 0 : leftQuantity != quantity;
        }

        private static void CheckAddItem_Internal(IManagedInventoryManager sourceInventory,
            IManagedInventoryManager targetInventory, IItemAsset itemAsset,
            int quantity, bool apply, out int leftQuantity)
        {
            int happenedQuantity = quantity;
            int notHappenedQuantity = 0;

            int totalHappened = 0;
            var inventory = targetInventory != null ? targetInventory : sourceInventory;
            int sign = targetInventory != null ? 1 : -1;

            for (int i = 0; i < inventory.SlotCount; i++)
            {
                var slot = inventory.ManagedGridSlots[i];
                int targetCount = sign * (happenedQuantity - totalHappened);
                CheckAddItem_Internal(sourceInventory, slot, itemAsset, targetCount,
                    false, out notHappenedQuantity);
                totalHappened += targetCount - notHappenedQuantity;

                if (totalHappened == sign * happenedQuantity)
                    break;
            }

            happenedQuantity = sign * totalHappened;
            leftQuantity = quantity - happenedQuantity;

            if (!apply || happenedQuantity == 0)
                return;

            totalHappened = 0;
            for (int i = 0; i < inventory.SlotCount; i++)
            {
                var slot = inventory.ManagedGridSlots[i];
                int targetCount = sign * (happenedQuantity - totalHappened);
                CheckAddItem_Internal(sourceInventory, slot, itemAsset, targetCount,
                    true, out notHappenedQuantity);
                totalHappened += targetCount - notHappenedQuantity;

                if (totalHappened == sign * happenedQuantity)
                    break;
            }

            Assert.AreEqual(totalHappened, happenedQuantity);
        }

        public static bool CheckAddItem(IInventoryManager sourceInventory, IGridSlot targetSlot,
            IItemAsset itemAsset, int quantity, bool apply, out int leftQuantity, bool returnCompletedAllQuantity = true)
        {
            if (quantity == 0 || itemAsset == null)
                throw new Exception("Quantity and item asset can't be null");

            if (sourceInventory == null && targetSlot == null)
                throw new Exception("Source and target can't be null");

            CheckAddItem_Internal((IManagedInventoryManager)sourceInventory,
                (IManagedGridSlot)targetSlot, itemAsset, quantity, apply, out leftQuantity);

            return returnCompletedAllQuantity ? leftQuantity == 0 : leftQuantity != quantity;
        }

        private static void CheckAddItem_Internal(IManagedInventoryManager sourceInventory,
            IManagedGridSlot targetSlot, IItemAsset itemAsset,
            int quantity, bool apply, out int leftQuantity)
        {
            int calculated = quantity;
            int notHappenedQuantity = 0;

            if (targetSlot != null)
            {
                targetSlot.CheckAddQuantity(null, itemAsset, calculated, false,
                    out notHappenedQuantity);
                calculated -= notHappenedQuantity;
            }

            if (sourceInventory != null)
            {
                int sourceCalculated = 0;
                for (int i = 0; i < sourceInventory.SlotCount; i++)
                {
                    int targetQuantity = calculated - sourceCalculated;

                    CheckAddItem_Internal(sourceInventory.ManagedGridSlots[i], targetSlot, itemAsset,
                        targetQuantity, false, out notHappenedQuantity);

                    sourceCalculated += targetQuantity - notHappenedQuantity;

                    if (sourceCalculated == calculated)
                        break;
                }
                calculated = sourceCalculated;
            }

            leftQuantity = quantity - calculated;
            if (!apply || calculated == 0)
                return;

            int totalHappened = 0;
            if (sourceInventory != null)
            {
                for (int i = 0; i < sourceInventory.SlotCount; i++)
                {
                    int target = calculated - totalHappened;
                    CheckAddItem_Internal(sourceInventory.ManagedGridSlots[i], targetSlot, itemAsset,
                        target, true, out notHappenedQuantity);
                    totalHappened += target - notHappenedQuantity;

                    if (totalHappened == calculated)
                        break;
                }
                Assert.AreEqual(totalHappened, calculated);
            }
            else
            {
                CheckAddItem_Internal(default(IManagedGridSlot), targetSlot, itemAsset,
                    calculated, true, out leftQuantity);
                Assert.AreEqual(leftQuantity, 0);
            }
        }

        public static bool CheckAddItem(IGridSlot sourceSlot, IGridSlot targetSlot, IItemAsset itemAsset,
            int quantity, bool apply, out int leftQuantity, bool returnCompletedAllQuantity = true)
        {
            if (quantity == 0 || itemAsset == null)
                throw new Exception("Quantity and item asset can't be null");

            if (targetSlot == null && sourceSlot == null)
                throw new Exception("Source and target can't be null");

            CheckAddItem_Internal((IManagedGridSlot)sourceSlot, (IManagedGridSlot)targetSlot,
                    itemAsset, quantity, apply, out leftQuantity);

            return returnCompletedAllQuantity ? leftQuantity == 0 : leftQuantity != quantity;
        }

        private static void CheckAddItem_Internal(IManagedGridSlot sourceSlot,
            IManagedGridSlot targetSlot, IItemAsset itemAsset, int quantity, bool apply, out int leftQuantity)
        {
            leftQuantity = quantity;
            if (sourceSlot == targetSlot)
                return;

            int happenedQuantity = quantity;
            int notHappenedQuantity = 0;
            if (targetSlot != null)
            {
                if (!targetSlot.ManagedInventoryManager.CanAddItem(sourceSlot, targetSlot, itemAsset, quantity))
                    return;

                targetSlot.CheckAddQuantity(sourceSlot, itemAsset, quantity, false, out notHappenedQuantity);
                happenedQuantity -= notHappenedQuantity;
            }

            if (sourceSlot != null)
            {
                if (!sourceSlot.ManagedInventoryManager.CanAddItem(targetSlot, sourceSlot, itemAsset, -happenedQuantity))
                    return;

                sourceSlot.CheckAddQuantity(targetSlot, itemAsset, -happenedQuantity, false, out notHappenedQuantity);
                happenedQuantity += notHappenedQuantity;
            }

            leftQuantity = quantity - happenedQuantity;
            if (!apply || happenedQuantity == 0)
                return;

            if (targetSlot != null)
            {
                targetSlot.CheckAddQuantity(sourceSlot, itemAsset, happenedQuantity, true, out _);
            }

            if (sourceSlot != null)
            {
                sourceSlot.CheckAddQuantity(targetSlot, itemAsset, -happenedQuantity, true, out _);
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
            => IGridSlot.GetIndexByRowColumnIndex(rowIndex, columnIndex, columnCount);
    }
}
