using System;
using SimpleU.DataContainer;
using UnityEngine;

namespace SimpleU.Inventory
{
    public class GridSlot : IManagedGridSlot
    {
        public bool IsEmpty => !HasItem;
        public bool HasItem => Quantity > 0;
        public int Index => _slotIndex;
        public int RowIndex => _rowIndex;
        public int ColumnIndex => _columnIndex;
        public IInventoryManager InventoryManager => _inventoryManager;
        public bool IsFull => Quantity >= Capacity;
        public int Quantity => _quantityItem?.ItemAsset != null ? _quantityItem.Quantity : 0;
        public IItemAsset ItemAsset => _quantityItem != null ? _quantityItem.ItemAsset : default;
        public IQuantityItem QuantityItem => _quantityItem;
        public int Capacity => _capacity;

        private IInventoryManager _inventoryManager;
        private int _slotIndex, _rowIndex, _columnIndex;
        protected IQuantityItem _quantityItem;
        private int _capacity;

        public Action<GridSlot> OnEmptinessChange { get; set; }
        public Action<GridSlot> OnQuantityChange { get; set; }

        public GridSlot(IInventoryManager inventoryManager, int index, int rowIndex, int columnIndex,
            int capacity = int.MaxValue)
        {
            _inventoryManager = inventoryManager;
            _slotIndex = index;
            _rowIndex = rowIndex;
            _columnIndex = columnIndex;
            _capacity = capacity;
        }

        protected virtual void AddQuantity(IGridSlot sourceSlot, IQuantityItem item)
        {
            int value = Quantity + item.Quantity;
            int safeQuantity = Math.Max(value, 0);
            if (safeQuantity == Quantity)
                return;

            bool wasEmpty = IsEmpty;

            if (safeQuantity <= 0)
            {
                _quantityItem = default;
            }
            else
            {
                if (IsEmpty)
                {
                    _quantityItem = item;
                }
                else
                {
                    QuantityItem.SetQuantity(safeQuantity);
                }
            }

            if (wasEmpty)
            {
                OnEmptinessChange?.Invoke(this);
            }
            else if (IsEmpty)
            {
                OnEmptinessChange?.Invoke(this);
            }
            OnQuantityChange?.Invoke(this);
        }

        void IManagedGridSlot.AddQuantity(IGridSlot sourceSlot, IQuantityItem item)
        {
            AddQuantity(sourceSlot, item);
        }

        void IManagedGridSlot.RemoveQuantity(IGridSlot sourceSlot, int quantity,
            out IQuantityItem removedItem)
        {
            GridSlotService.RemoveQuantity(this, sourceSlot, quantity, out removedItem);
        }

        bool IManagedGridSlot.CanApplyQuantity(IGridSlot sourceSlot, IItemAsset itemAsset, int quantity,
            out int leftQuantity)
        {
            return GridSlotService.CanApplyQuantity(this, sourceSlot, itemAsset, quantity, out leftQuantity);
        }
    }

    public static class GridSlotService
    {
        public static void RemoveQuantity(IManagedGridSlot slot, IGridSlot sourceSlot, int quantity,
            out IQuantityItem removedItem)
        {
            var itemAsset = slot.ItemAsset;
            var removeItem = new QuantityItem()
            {
                itemAsset = slot.ItemAsset,
                quantity = -quantity
            };
            slot.AddQuantity(sourceSlot, removeItem);
            removedItem = new QuantityItem()
            {
                itemAsset = itemAsset,
                quantity = quantity
            };
        }
        public static bool CanApplyQuantity(IManagedGridSlot slot, IGridSlot sourceSlot, IItemAsset itemAsset, int quantity,
            out int leftQuantity)
        {
            bool isAdd = quantity > 0;
            leftQuantity = quantity;

            if (slot.HasItem)
            {
                if (isAdd && !itemAsset.IsStackable)
                    return false;

                //try stack on same item
                if (!slot.ItemAsset.Equals(itemAsset))
                    return false;

                int slotLeftCapacity = isAdd ? slot.Capacity - slot.Quantity : slot.Quantity;
                if (slotLeftCapacity <= 0)
                    return false;

                int addedQuantity = 0;
                if (!isAdd)
                    addedQuantity = -Mathf.Min(Mathf.Abs(leftQuantity), slotLeftCapacity);
                else
                    addedQuantity = Mathf.Min(leftQuantity, slotLeftCapacity);

                if (addedQuantity == 0)
                    return false;

                leftQuantity -= addedQuantity;
            }
            else
            {
                if (!isAdd)
                    return false;

                int usedCapacity = itemAsset != null && !itemAsset.IsStackable ? Mathf.Min(leftQuantity, 1)
                    : Mathf.Min(leftQuantity, slot.Capacity);
                leftQuantity -= usedCapacity;
            }
            return leftQuantity != quantity;
        }
    }
}
