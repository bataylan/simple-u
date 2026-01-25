using SimpleU.DataContainer;
using UnityEngine;

namespace SimpleU.Inventory
{
    public static class InventoryManagerService<T> where T : IItemAsset
    {
        public static bool TryAddItemQuantityToSlot(IInventoryManager inventory, IItemAsset inventoryItem, int quantity,
            int slotIndex, out int leftCount)
        {
            leftCount = quantity;
            if (slotIndex >= inventory.SlotCount)
                return false;

            var gridSlot = inventory.GridSlots[slotIndex];

            bool canAddItem = CanAddItemQuantityToSlot_Internal(inventoryItem, quantity, gridSlot, out leftCount,
                out int completedQuantity);

            if (!canAddItem)
                return false;

            AddItem_Internal(inventory, gridSlot, (T)inventoryItem, completedQuantity);

            return leftCount != quantity;
        }

        public static bool TryAddItemToSingleSlot(IInventoryManager inventory, IItemAsset inventoryItem, int quantity, out int leftCount,
            out IGridSlot gridSlot, bool stackItems = true)
        {
            bool canAddItem = CanAddItemToSlot(inventory, inventoryItem, quantity, out leftCount,
                out int completedQuantity, out gridSlot, stackItems);

            if (!canAddItem)
                return false;

            AddItem_Internal(inventory, gridSlot, (T)inventoryItem, completedQuantity);

            return leftCount != quantity;
        }

        public static bool TryAddItemQuantitySmart(IInventoryManager inventory, IItemAsset inventoryItem, int quantity,
            out int leftQuantity)
        {
            leftQuantity = quantity;

            if (inventoryItem == null || quantity == 0)
                return false;

            if (inventoryItem is not T safeItem)
                return false;

            CheckAddItem_Internal(inventory, safeItem, quantity, true, out leftQuantity);
            return leftQuantity != quantity;
        }

        private static void AddItem_Internal(IInventoryManager inventory, IGridSlot slotToAdd, T itemAsset,
            int completedQuantity)
        {
            if (slotToAdd.IsEmpty)
            {
                var quantityItem = new QuantityItem<T>
                {
                    itemAsset = itemAsset,
                    quantity = completedQuantity
                };

                SetItemToGridSlot(inventory, slotToAdd, quantityItem);
            }
            else //stash
            {
                slotToAdd.AddQuantity(completedQuantity);
            }
        }

        private static void SetItemToGridSlot(IInventoryManager inventory, IGridSlot slotToAdd, IQuantityItem<T> quantityItem)
        {
            slotToAdd.SetItem(quantityItem.ItemAsset, quantityItem.Quantity);

            var relativeSlotIndexes = quantityItem.ItemAsset.RelativeSlotIndexes;

            for (int i = 1; i < relativeSlotIndexes.Length; i++)
            {
                var relativeRowColumn = relativeSlotIndexes[i];
                int indexAddition = GetIndexByRowColumnIndex(relativeRowColumn.rowIndex,
                    relativeRowColumn.columnIndex, inventory.ColumnCount);
                int index = slotToAdd.Index + indexAddition;
                var relativeSlot = inventory.GridSlots[index];

                relativeSlot.SetItem(quantityItem.ItemAsset, quantityItem.Quantity, slotToAdd.Index);
            }
        }

        public static bool CanAddItemQuantity(IInventoryManager inventory, IItemAsset inventoryItem, 
            int quantity, out int leftQuantity)
        {
            leftQuantity = quantity;

            if (inventoryItem == null || quantity == 0)
                return false;

            if (inventoryItem is not T safeItem)
                return false;

            CheckAddItem_Internal(inventory, safeItem, quantity, false, out leftQuantity);
            return leftQuantity != quantity;
        }

        public static void CheckAddItem_Internal(IInventoryManager inventory, T itemAsset, int quantity,
            bool doAdd, out int leftQuantity)
        {
            leftQuantity = quantity;
            bool isAdd = quantity > 0;

            for (int i = 0; i < inventory.SlotCount; i++)
            {
                var slot = inventory.GridSlots[i];
                if (!slot.HasOriginalItem)
                    continue;

                if (!slot.ItemAsset.Equals(itemAsset))
                    continue;

                int slotLeftCapacity = isAdd ? slot.LeftCapacity() : slot.Quantity;
                if (slotLeftCapacity != 0)
                {
                    int addedQuantity = 0;
                    if (!isAdd)
                        addedQuantity = -Mathf.Min(Mathf.Abs(leftQuantity), slotLeftCapacity);
                    else
                        addedQuantity = Mathf.Min(leftQuantity, slotLeftCapacity);

                    leftQuantity -= addedQuantity;

                    if (doAdd)
                    {
                        slot.AddQuantity(addedQuantity);
                    }
                }

                if (leftQuantity == 0)
                    break;
            }

            if (leftQuantity == 0 || !isAdd)
                return;

            //do add on empty slots
            for (int i = 0; i < inventory.SlotCount; i++)
            {
                var slot = inventory.GridSlots[i];
                if (slot.HasOriginalItem)
                    continue;

                int usedCapacity = Mathf.Min(leftQuantity, slot.Capacity);
                leftQuantity -= usedCapacity;

                if (doAdd)
                {
                    var quantityITem = new QuantityItem<T>()
                    {
                        itemAsset = itemAsset,
                        quantity = usedCapacity
                    };

                    SetItemToGridSlot(inventory, slot, quantityITem);
                }

                if (leftQuantity == 0)
                    break;
            }
        }

        public static bool CanAddItemQuantityToSlot_Internal(IItemAsset inventoryItem, int quantity, IGridSlot gridSlot,
            out int leftQuantity, out int completedQuantity)
        {
            leftQuantity = quantity;
            completedQuantity = 0;

            if (inventoryItem == null || quantity == 0)
                return false;

            if (inventoryItem is not T)
                return false;

            var slotToAdd = gridSlot;
            if (slotToAdd.IsEmpty)
            {
                if (!CheckAddItemToEmptySlot(quantity, slotToAdd, out completedQuantity, out leftQuantity))
                    return false;
            }
            else //try stack
            {
                if (!CheckAddItemToStackableSlot(quantity, slotToAdd, out completedQuantity, out leftQuantity))
                    return false;
            }

            return true;
        }

        public static bool CanAddItemToSlot(IInventoryManager inventory, IItemAsset inventoryItem,
            int quantity, out int leftQuantity, out int completedQuantity, out IGridSlot gridSlot,
            bool stackItems = true)
        {
            gridSlot = null;
            leftQuantity = quantity;
            completedQuantity = 0;

            if (inventoryItem == null || quantity == 0)
                return false;

            if (inventoryItem is not T safeItem)
                return false;

            if (!TryGetSlotToAdd(inventory, safeItem, quantity, stackItems, out IGridSlot slotToAdd))
                return false;

            if (!CanAddItemQuantityToSlot_Internal(inventoryItem, quantity, slotToAdd, out leftQuantity, out completedQuantity))
                return false;

            gridSlot = slotToAdd;
            return true;
        }

        private static bool CheckAddItemToEmptySlot(int quantity, IGridSlot slotToAdd,
            out int completedQuantity, out int leftQuantity)
        {
            completedQuantity = 0;
            leftQuantity = quantity;

            if (quantity <= 0 || slotToAdd.Capacity <= 0)
                return false;

            completedQuantity = Mathf.Min(quantity, slotToAdd.Capacity);
            leftQuantity = quantity - completedQuantity;

            return leftQuantity != quantity;
        }

        private static bool CheckAddItemToStackableSlot(int quantity, IGridSlot slotToAdd,
            out int completedQuantity, out int leftQuantity)
        {
            completedQuantity = 0;

            int targetQuantity = slotToAdd.Quantity + quantity;
            int safeQuantity = Mathf.Clamp(targetQuantity, 0, slotToAdd.Capacity);
            leftQuantity = targetQuantity - safeQuantity;

            if (leftQuantity == quantity)
                return false;

            completedQuantity = safeQuantity - slotToAdd.Quantity;
            return leftQuantity != quantity;
        }

        private static bool TryGetSlotToAdd(IInventoryManager inventory, T itemAsset, int quantity,
            bool stackItems, out IGridSlot slotToAdd)
        {
            slotToAdd = null;

            if (stackItems)
            {
                slotToAdd = GetStackableSlot(inventory, itemAsset, quantity);
            }

            if (slotToAdd != null)
                return true;

            slotToAdd = GetEmptySuitableSlot(inventory, itemAsset);

            return slotToAdd != null;
        }

        private static IGridSlot GetStackableSlot(IInventoryManager inventory, T itemAsset, int quantity)
        {
            for (int i = 0; i < inventory.SlotCount; i++)
            {
                if (IsStackable(inventory, itemAsset, quantity, i))
                    return inventory.GridSlots[i];
            }

            return null;
        }

        private static bool IsStackable(IInventoryManager inventory, T itemAsset, int quantity, int slotIndex)
        {
            var gridSlot = inventory.GridSlots[slotIndex];
            if (gridSlot.IsEmpty || gridSlot.IsRelativeSlot)
                return false;

            return !gridSlot.IsEmpty && !gridSlot.IsRelativeSlot && gridSlot.IsStackable(itemAsset, quantity);
        }

        private static IGridSlot GetEmptySuitableSlot(IInventoryManager inventory, T itemAsset)
        {
            var relativeSlotIndexes = itemAsset.RelativeSlotIndexes;

            //check for empty slot
            for (int i = 0; i < inventory.SlotCount; i++)
            {
                if (CheckSlotEmptyAndSuitable(inventory, i, relativeSlotIndexes))
                {
                    return inventory.GridSlots[i];
                }
            }

            return null;
        }

        private static bool IsSlotEmpty(IInventoryManager inventory, RowColumnIndex[] relativeSlotIndexes, int slotIndex)
        {
            bool isEmptySlotAvailable = true;

            for (int i = 0; i < relativeSlotIndexes.Length; i++)
            {
                var relativeRowColumn = relativeSlotIndexes[i];

                int indexAddition = GetIndexByRowColumnIndex(relativeRowColumn.rowIndex,
                    relativeRowColumn.columnIndex, inventory.ColumnCount);
                int index = slotIndex + indexAddition;

                if (!inventory.GridSlots[index].IsEmpty)
                {
                    isEmptySlotAvailable = false;
                    break;
                }
            }

            return isEmptySlotAvailable;
        }

        private static bool CheckSlotEmptyAndSuitable(IInventoryManager inventory, int slotIndex, RowColumnIndex[] relativeSlotIndexes)
        {
            bool isSlotInsideInventory = IsSlotInsideInventory(inventory, relativeSlotIndexes, slotIndex);
            bool isEmptySlotAvailable = IsSlotEmpty(inventory, relativeSlotIndexes, slotIndex);

            return isSlotInsideInventory && isEmptySlotAvailable;
        }

        private static bool IsSlotInsideInventory(IInventoryManager inventory, RowColumnIndex[] relativeSlotIndexes, int slotIndex)
        {
            var slot = inventory.GridSlots[slotIndex];

            for (int j = 0; j < relativeSlotIndexes.Length; j++)
            {
                var relativeRowColumn = relativeSlotIndexes[j];

                if (slot.ColumnIndex + relativeRowColumn.columnIndex >= inventory.ColumnCount
                    || slot.RowIndex + relativeRowColumn.rowIndex >= inventory.RowCount)
                {
                    return false;
                }
            }

            return true;
        }
        
        public static int TryGetSlotIndex(IInventoryManager inventory, IItemAsset item)
        {
            for (int i = 0; i < inventory.SlotCount; i++)
            {
                if (inventory.GridSlots[i].ItemAsset != null && inventory.GridSlots[i].ItemAsset.Equals(item))
                    return i;
            }

            return -1;
        }
        
        public static bool HasEnoughQuantity(IInventoryManager inventory,IItemAsset itemAsset, int quantity)
        {
            if (itemAsset is not T safeItem)
                return false;

            return HasEnoughQuantityTyped(inventory, safeItem, quantity);
        }

        public static bool HasEnoughQuantityTyped(IInventoryManager inventory, T itemAsset, int quantity)
        {
            return GetQuantityTyped(inventory, itemAsset) >= quantity;
        }

        public static int GetQuantity(IInventoryManager inventory, IItemAsset itemAsset)
        {
            if (itemAsset is not T safeItem)
                return 0;

            return GetQuantityTyped(inventory, safeItem);
        }

        public static int GetQuantityTyped(IInventoryManager inventory, T itemAsset)
        {
            if (itemAsset == null)
                return 0;

            int quantity = 0;
            for (int i = 0; i < inventory.SlotCount; i++)
            {
                var slot = inventory.GridSlots[i];
                if (slot.HasOriginalItem && slot.ItemAsset.Equals(itemAsset))
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
