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

            if (inventoryItem == null || leftCount == 0)
                return false;

            if (inventoryItem is not T safeItem)
                return false;

            var gridSlot = _slots[slotIndex];

            if (quantity > 0)
            {
                if (!CheckSlotEmptyAndSuitable(slotIndex, safeItem.RelativeSlotIndexes) 
                    && !IsStackable(safeItem, quantity, slotIndex))
                    return false;
            }
            else
            {
                if (!gridSlot.HasOriginalItem)
                    return false;
            }

            AddItem(quantity, safeItem, gridSlot, out leftCount);
            return leftCount != quantity;
        }

        public bool TryAddItemQuantity(IItemAsset inventoryItem, int quantity, out int leftCount, 
            out IGridSlot gridSlot, bool stackItems = true)
        {
            gridSlot = null;
            leftCount = quantity;

            if (inventoryItem == null || leftCount == 0)
                return false;

            if (inventoryItem is not T safeItem)
                return false;

            if (!TryGetSlotToAdd(safeItem, quantity, stackItems, out GridSlot<T> slotToAdd))
                return false;

            gridSlot = slotToAdd;

            AddItem(quantity, safeItem, slotToAdd, out leftCount);

            return leftCount != quantity;
        }

        protected virtual void AddItem(int quantity, T itemAsset, GridSlot<T> slotToAdd, out int leftQuantity)
        {
            leftQuantity = quantity;

            int completedQuantity = 0;

            if (slotToAdd.IsEmpty)
            {
                if (quantity <= 0 || slotToAdd.Capacity <= 0)
                    return;

                completedQuantity = Mathf.Min(quantity, slotToAdd.Capacity);
                leftQuantity = quantity - completedQuantity;

                var quantityItem = new QuantityItem<T>
                {
                    itemAsset = itemAsset,
                    quantity = completedQuantity
                };

                SetItemToGridSlot(slotToAdd, quantityItem);
            }
            else
            {
                int targetQuantity = slotToAdd.Quantity + quantity;
                int safeQuantity = Mathf.Clamp(targetQuantity, 0, slotToAdd.Capacity);
                leftQuantity = targetQuantity - safeQuantity;
                if (leftQuantity == quantity)
                    return;

                completedQuantity = safeQuantity - slotToAdd.Quantity;
                slotToAdd.AddQuantity(completedQuantity);
            }

            OnItemAdd(itemAsset, completedQuantity);
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
        
        public bool HasEnoughQuantity(T itemAsset, int quantity)
        {
            return GetQuantity(itemAsset) >= quantity;
        }

        public int GetQuantity(T itemAsset)
        {
            if (itemAsset == null)
                return 0;
                
            for (int i = 0; i < _slots.Length; i++)
            {
                var slot = _slots[i];
                if (slot.HasOriginalItem && slot.ItemAsset.Equals(itemAsset))
                {
                    return slot.Quantity;
                }
            }

            return 0;
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
        bool TryAddItemQuantity(IItemAsset inventoryItem, int quantity, out int leftCount, out IGridSlot gridSlot, bool stackItems = true);
        bool TryAddItemQuantityToSlot(IItemAsset item, int quantity, int slotIndex, out int leftQuantity);
    }
}
