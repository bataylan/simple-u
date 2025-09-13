using System;
using SimpleU.DataContainer;
using UnityEngine;

namespace SimpleU.Inventory
{
    public class GridSlot<T> : IGridSlot where T : IItemAsset
    {
        public bool IsEmpty => ItemAsset == null || Quantity <= 0;
        public bool HasOriginalItem => !IsEmpty && !IsRelativeSlot;
        public RowColumnIndex[] RelativeSlotIndexes => ItemAsset.RelativeSlotIndexes;
        public int Index => _index;
        public int RowIndex => _rowIndex;
        public int ColumnIndex => _columnIndex;
        public bool IsRelativeSlot => !IsEmpty && _originalSlotIndex >= 0;
        public IInventoryManager InventoryManager => _inventoryManager;
        public int Quantity
        {
            get => _quantityItem != null ? _quantityItem.Quantity : 0;
            private set
            {
                int safeQuantity = Mathf.Max(value, 0);
                if (safeQuantity == Quantity)
                    return;

                if (safeQuantity <= 0)
                {
                    SetItem(null);
                }
                else
                {
                    _quantityItem.SetQuantity(safeQuantity);
                }

                OnQuantityChange?.Invoke(this);
            }
        }
        public IItemAsset ItemAsset => _quantityItem != null ? _quantityItem.ItemAsset : default;
        public T CastedItem => (T)ItemAsset;
        public int OriginalSlotIndex => IsRelativeSlot ? _originalSlotIndex : Index;
        public IQuantityItem QuantityItem => _quantityItem;
        public int Capacity => _capacity;

        private int _originalSlotIndex;
        private IInventoryManager _inventoryManager;
        private int _index, _rowIndex, _columnIndex;
        private IQuantityItem<T> _quantityItem;
        private int _capacity;

        public Action<IGridSlot> OnEmptinessChange { get; set; }
        public Action<IGridSlot> OnQuantityChange { get; set; }

        public GridSlot(IInventoryManager inventoryManager, int index, int rowIndex, int columnIndex,
            int capacity = int.MaxValue)
        {
            _inventoryManager = inventoryManager;
            _index = index;
            _rowIndex = rowIndex;
            _columnIndex = columnIndex;
            _capacity = capacity;
        }

        public void SetItem(IQuantityItem<T> quantityItem, int originalSlotIndex = -1)
        {
            bool wasEmpty = IsEmpty;
            bool wasOriginalItemOwner = HasOriginalItem;

            _quantityItem = quantityItem;
            _originalSlotIndex = originalSlotIndex;

            if (wasEmpty && HasOriginalItem)
            {
                OnEmptinessChange?.Invoke(this);
            }
            else if (wasOriginalItemOwner && IsEmpty)
            {
                OnEmptinessChange?.Invoke(this);
            }
        }

        public bool TryConsumeQuantity(int quantity)
        {
            if (quantity <= 0 || Quantity < quantity)
                return false;

            Quantity -= quantity;
            return true;
        }

        public void AddQuantity(int quantity)
        {
            Quantity += quantity;
        }

        public bool IsStackable(IItemAsset itemAsset, int count)
        {
            return HasCapacity(count) && (IsEmpty || ItemAsset.Equals(itemAsset));
        }
        
        public bool HasCapacity(int count)
        {
            return !HasOriginalItem || (Quantity + count) <= Capacity;
        }

        public bool GetIsDroppableToTargetSlot(IGridSlot gridSlot)
        {
            if (IsEmpty || gridSlot == this)
                return true;

            if (IsRelativeSlot && _inventoryManager == gridSlot.InventoryManager
                && _originalSlotIndex == gridSlot.OriginalSlotIndex)
            {
                return true;
            }

            return false;
        }

        public bool GetIsStackableToTargetGridSlot(IGridSlot gridSlot)
        {
            var target = gridSlot as GridSlot<T>;
            return !IsEmpty && !target.IsEmpty && target.IsStackable(_quantityItem.ItemAsset, Quantity);
        }
    }
}
