using System.Collections;
using System.Collections.Generic;
using SimpleU.DataContainer;
using UnityEngine;

namespace SimpleU.Inventory
{
    public class AInventoryManager<T> : IInventoryManager where T : IItemAsset
    {
        public AInventoryManager(int rowCount, int columnCount, int slotCapacity = 0)
        {
            RowCount = rowCount;
            ColumnCount = columnCount;

            _slots = new GridSlot<T>[SlotCount];

            if (slotCapacity <= 0)
                slotCapacity = int.MaxValue;

            int slotIndex = 0;
            for (int i = 0; i < RowCount; i++)
            {
                for (int j = 0; j < ColumnCount; j++)
                {
                    _slots[slotIndex] = new GridSlot<T>(this, slotIndex, i, j, slotCapacity);
                    slotIndex++;
                }
            }
        }

        public int ColumnCount { get; private set; }
        public int RowCount { get; private set; }

        public GridSlot<T>[] Slots => _slots;
        IGridSlot[] IInventoryManager.GridSlots => _slots;

        public int SlotCount => RowCount * ColumnCount;
        protected GridSlot<T>[] _slots;

        public bool TryAddItemQuantityToSlot(IItemAsset inventoryItem, int quantity, int slotIndex, out int leftCount)
        {
            leftCount = quantity;
            if (slotIndex >= SlotCount)
                return false;
                
            var gridSlot = _slots[slotIndex];

            //old control
            // if (quantity > 0)
            // {
            //     if (!CheckSlotEmptyAndSuitable(slotIndex, safeItem.RelativeSlotIndexes) 
            //         && !IsStackable(safeItem, quantity, slotIndex))
            //         return false;
            // }
            // else
            // {
            //     if (!gridSlot.HasOriginalItem)
            //         return false;
            // }

            bool canAddItem = CanAddItemQuantity(inventoryItem, quantity, gridSlot, out leftCount,
                out int completedQuantity);

            if (!canAddItem)
                return false;

            AddItem_Internal(gridSlot, (T)inventoryItem, completedQuantity);

            return leftCount != quantity;
        }

        public bool TryAddItemToSingleSlot(IItemAsset inventoryItem, int quantity, out int leftCount,
            out IGridSlot gridSlot, bool stackItems = true)
        {
            bool canAddItem = CanAddItemFromSingleSlot(inventoryItem, quantity, out leftCount, out int completedQuantity,
                out gridSlot, stackItems);

            if (!canAddItem)
                return false;

            AddItem_Internal(gridSlot as GridSlot<T>, (T)inventoryItem, completedQuantity);

            return leftCount != quantity;
        }

        public bool CanAddItemQuantity(IItemAsset inventoryItem, int quantity, bool stash, out int leftQuantity)
        {
            leftQuantity = quantity;

            if (inventoryItem == null || quantity == 0)
                return false;

            if (inventoryItem is not T safeItem)
                return false;

            CheckAddItem_Internal(safeItem, quantity, stash, false, out leftQuantity);
            return leftQuantity != quantity;
        }

        public bool TryAddItemQuantity(IItemAsset inventoryItem, int quantity, bool stash, out int leftQuantity)
        {
            leftQuantity = quantity;

            if (inventoryItem == null || quantity == 0)
                return false;

            if (inventoryItem is not T safeItem)
                return false;

            CheckAddItem_Internal(safeItem, quantity, stash, true, out leftQuantity);
            return leftQuantity != quantity;
        }

        protected virtual void CheckAddItem_Internal(T itemAsset, int quantity, bool stashed, bool doAdd,
            out int leftQuantity)
        {
            leftQuantity = quantity;
            bool isAdd = quantity > 0;

            //find same item owner slots
            //ignore stash if removing
            if (stashed || !isAdd)
            {
                for (int i = 0; i < Slots.Length; i++)
                {
                    var slot = _slots[i];
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
                for (int i = 0; i < _slots.Length; i++)
                {
                    var slot = _slots[i];
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

                        SetItemToGridSlot(slot, quantityITem);
                    }

                    if (leftQuantity == 0)
                        break;
                }
            }
        }

        protected virtual void AddItem_Internal(GridSlot<T> slotToAdd, T itemAsset, int completedQuantity)
        {
            if (slotToAdd.IsEmpty)
            {
                var quantityItem = new QuantityItem<T>
                {
                    itemAsset = itemAsset,
                    quantity = completedQuantity
                };

                SetItemToGridSlot(slotToAdd, quantityItem);
            }
            else //stash
            {
                slotToAdd.AddQuantity(completedQuantity);
            }

            OnItemAdd(itemAsset, completedQuantity);
        }

        public bool CanAddItemFromSingleSlot(IItemAsset inventoryItem, int quantity, out int leftQuantity,
            out int completedQuantity, out IGridSlot gridSlot, bool stackItems = true)
        {
            gridSlot = null;
            leftQuantity = quantity;
            completedQuantity = 0;

            if (inventoryItem == null || quantity == 0)
                return false;

            if (inventoryItem is not T safeItem)
                return false;

            if (!TryGetSlotToAdd(safeItem, quantity, stackItems, out GridSlot<T> slotToAdd))
                return false;

            if (!CanAddItemQuantity(inventoryItem, quantity, slotToAdd, out leftQuantity, out completedQuantity, stackItems))
                return false;

            gridSlot = slotToAdd;
            return true;
        }

        public bool CanAddItemQuantity(IItemAsset inventoryItem, int quantity, IGridSlot gridSlot, out int leftQuantity,
            out int completedQuantity, bool stackItems = true)
        {
            leftQuantity = quantity;
            completedQuantity = 0;

            if (inventoryItem == null || quantity == 0)
                return false;

            if (inventoryItem is not T safeItem)
                return false;

            var slotToAdd = gridSlot as GridSlot<T>;
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

        private bool CheckAddItemToEmptySlot(int quantity, GridSlot<T> slotToAdd,
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

        private bool CheckAddItemToStackableSlot(int quantity, GridSlot<T> slotToAdd,
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

        private void SetItemToGridSlot(GridSlot<T> slotToAdd, IQuantityItem<T> quantityItem)
        {
            slotToAdd.SetItem(quantityItem);

            var relativeSlotIndexes = quantityItem.ItemAsset.RelativeSlotIndexes;

            for (int i = 1; i < relativeSlotIndexes.Length; i++)
            {
                var relativeRowColumn = relativeSlotIndexes[i];
                int indexAddition = GetIndexByRowColumnIndex(relativeRowColumn.rowIndex, relativeRowColumn.columnIndex);
                int index = slotToAdd.Index + indexAddition;
                var relativeSlot = _slots[index];

                relativeSlot.SetItem(quantityItem, slotToAdd.Index);
            }
        }
        
        protected virtual void OnItemAdd(T itemAsset, int quantity) { }

        public int TryGetSlotIndex(IItemAsset item)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].ItemAsset != null && _slots[i].ItemAsset.Equals(item))
                    return i;
            }

            return -1;
        }

        public bool HasEnoughQuantity(IItemAsset itemAsset, int quantity)
        {
            if (itemAsset is not T safeItem)
                return false;

            return HasEnoughQuantity(safeItem, quantity);
        }

        public bool HasEnoughQuantity(T itemAsset, int quantity)
        {
            return GetQuantity(itemAsset) >= quantity;
        }

        public int GetQuantity(IItemAsset itemAsset)
        {
            if (itemAsset is not T safeItem)
                return 0;

            return GetQuantity(safeItem);
        }

        public int GetQuantity(T itemAsset)
        {
            if (itemAsset == null)
                return 0;

            int quantity = 0;
            for (int i = 0; i < _slots.Length; i++)
            {
                var slot = _slots[i];
                if (slot.HasOriginalItem && slot.ItemAsset.Equals(itemAsset))
                {
                    quantity += slot.Quantity;
                }
            }

            return quantity;
        }

        private bool TryGetSlotToAdd(T itemAsset, int quantity, bool stackItems, out GridSlot<T> slotToAdd)
        {
            slotToAdd = null;

            if (stackItems)
            {
                slotToAdd = GetStackableSlot(itemAsset, quantity);
            }

            if (slotToAdd != null)
                return true;

            slotToAdd = GetEmptySuitableSlot(itemAsset);

            return slotToAdd != null;
        }

        private GridSlot<T> GetStackableSlot(T itemAsset, int quantity)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (IsStackable(itemAsset, quantity, i))
                    return _slots[i];
            }

            return null;
        }

        private bool IsStackable(T itemAsset, int quantity, int slotIndex)
        {
            GridSlot<T> gridSlot = _slots[slotIndex];
            if (gridSlot.IsEmpty || gridSlot.IsRelativeSlot)
                return false;

            return !gridSlot.IsEmpty && !gridSlot.IsRelativeSlot && gridSlot.IsStackable(itemAsset, quantity);
        }

        private GridSlot<T> GetEmptySuitableSlot(T itemAsset)
        {
            var relativeSlotIndexes = itemAsset.RelativeSlotIndexes;

            //check for empty slot
            for (int i = 0; i < _slots.Length; i++)
            {
                if (CheckSlotEmptyAndSuitable(i, relativeSlotIndexes))
                {
                    return _slots[i];
                }
            }

            return null;
        }

        private bool CheckSlotEmptyAndSuitable(int slotIndex, RowColumnIndex[] relativeSlotIndexes)
        {
            bool isSlotInsideInventory = IsSlotInsideInventory(relativeSlotIndexes, slotIndex);
            bool isEmptySlotAvailable = IsSlotEmpty(relativeSlotIndexes, slotIndex);

            return isSlotInsideInventory && isEmptySlotAvailable;
        }

        private bool IsSlotEmpty(RowColumnIndex[] relativeSlotIndexes, int slotIndex)
        {
            bool isEmptySlotAvailable = true;

            for (int i = 0; i < relativeSlotIndexes.Length; i++)
            {
                var relativeRowColumn = relativeSlotIndexes[i];

                int indexAddition = GetIndexByRowColumnIndex(relativeRowColumn.rowIndex, relativeRowColumn.columnIndex);
                int index = slotIndex + indexAddition;

                if (!_slots[index].IsEmpty)
                {
                    isEmptySlotAvailable = false;
                    break;
                }
            }

            return isEmptySlotAvailable;
        }

        private bool IsSlotInsideInventory(RowColumnIndex[] relativeSlotIndexes, int slotIndex)
        {
            var slot = _slots[slotIndex];

            for (int j = 0; j < relativeSlotIndexes.Length; j++)
            {
                var relativeRowColumn = relativeSlotIndexes[j];

                if (slot.ColumnIndex + relativeRowColumn.columnIndex >= ColumnCount
                    || slot.RowIndex + relativeRowColumn.rowIndex >= RowCount)
                {
                    return false;
                }
            }

            return true;
        }

        public int GetIndexByRowColumnIndex(int rowIndex, int columnIndex)
            => IGridSlot.GetIndexByRowColumnIndex(rowIndex, columnIndex, ColumnCount);
    }

    public interface IInventoryManager
    {
        int ColumnCount { get; }
        int RowCount { get; }
        IGridSlot[] GridSlots { get; }
        int TryGetSlotIndex(IItemAsset item);
        bool HasEnoughQuantity(IItemAsset itemAsset, int quantity);
        int GetQuantity(IItemAsset itemAsset);
        bool CanAddItemFromSingleSlot(IItemAsset inventoryItem, int quantity, out int leftQuantity,
            out int completedQuantity, out IGridSlot gridSlot, bool stackItems = true);
        bool TryAddItemToSingleSlot(IItemAsset inventoryItem, int quantity, out int leftCount, out IGridSlot gridSlot, bool stackItems = true);
        bool TryAddItemQuantityToSlot(IItemAsset item, int quantity, int slotIndex, out int leftQuantity);
        bool TryAddItemQuantity(IItemAsset inventoryItem, int quantity, bool stash, out int leftQuantity);
        bool CanAddItemQuantity(IItemAsset inventoryItem, int quantity, bool stash, out int leftQuantity);
    }
}
