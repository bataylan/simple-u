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
        private bool _deferQuantityEvents;
        protected bool AreQuantityEventsDeferred => _deferQuantityEvents;

        public Action<GridSlot> OnEmptinessChange { get; set; }
        public event Action<GridSlot> OnItemChange;
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
            bool wasEmpty = ChangeQuantity(item, safeQuantity);
            InvokeQuantityEvents(wasEmpty);
        }

        protected bool ChangeQuantity(IQuantityItem item, int safeQuantity)
        {
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

            return wasEmpty;
        }
        
        protected void InvokeQuantityEvents(bool wasEmpty)
        {
            if (_deferQuantityEvents)
                return;

            if (wasEmpty || IsEmpty)
            {
                OnEmptinessChange?.Invoke(this);
                OnItemChange?.Invoke(this);
            }
            OnQuantityChange?.Invoke(this);
        }

        internal void SwapContents(GridSlot other)
        {
            // Move both payloads before inventory and UI listeners can observe either slot.
            _deferQuantityEvents = true;
            other._deferQuantityEvents = true;
            try
            {
                var item = RemoveStackForSwap(other);
                var otherItem = other.RemoveStackForSwap(this);
                AddQuantity(other, otherItem);
                other.AddQuantity(this, item);
            }
            finally
            {
                _deferQuantityEvents = false;
                other._deferQuantityEvents = false;
            }

            try
            {
                NotifySwappedItem();
            }
            finally
            {
                other.NotifySwappedItem();
            }
        }

        private IQuantityItem RemoveStackForSwap(GridSlot other)
        {
            var extraData = QuantityItem.ExtraData;
            RemoveQuantity(other, Quantity, out var item);
            item.ExtraData ??= extraData;
            return item;
        }

        protected virtual void NotifySwappedItem()
        {
            OnItemChange?.Invoke(this);
            OnQuantityChange?.Invoke(this);
        }

        void IManagedGridSlot.AddQuantity(IGridSlot sourceSlot, IQuantityItem item)
        {
            AddQuantity(sourceSlot, item);
        }

        public virtual void RemoveQuantity(IGridSlot sourceSlot, int quantity,
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
        internal static int GetEmptySlotCapacity(IGridSlot slot, IItemAsset itemAsset)
        {
            int capacity = Math.Max(slot.Capacity, 0);
            return itemAsset != null && !itemAsset.IsStackable ? Math.Min(capacity, 1) : capacity;
        }

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

                int usedCapacity = Mathf.Min(leftQuantity, GetEmptySlotCapacity(slot, itemAsset));
                leftQuantity -= usedCapacity;
            }
            return leftQuantity != quantity;
        }
    }
}
