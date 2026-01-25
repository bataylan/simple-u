using System;
using SimpleU.DataContainer;
using UnityEngine;

namespace SimpleU.Inventory
{
    public class GridSlot<T> : IServicableGridSlot<T> where T : IItemAsset
    {
        public bool IsEmpty => ItemAsset == null || Quantity <= 0;
        public bool HasOriginalItem => !IsEmpty && !IsRelativeSlot;
        public int Index => _index;
        public int RowIndex => _rowIndex;
        public int ColumnIndex => _columnIndex;
        public bool IsRelativeSlot => !IsEmpty && _originalSlotIndex >= 0;
        public IInventoryManager InventoryManager => _inventoryManager;
        public bool IsFull => Quantity >= Capacity;
        public int Quantity
        {
            get => _quantityItem != null ? _quantityItem.Quantity : 0;
            private set => GridSlotService<T>.SetQuantity(this, value);
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

        public void SetItem(IItemAsset itemAsset, int quantity, int originalSlotIndex = -1)
        {
            GridSlotService<T>.SetItem(this, itemAsset, quantity, originalSlotIndex);
        }

        public bool TryConsumeQuantity(int quantity)
        {
            return GridSlotService<T>.TryConsumeQuantity(this, quantity);
        }

        public void AddQuantity(int quantity)
        {
            Quantity += quantity;
        }

        public bool IsStackable(IItemAsset itemAsset, int count)
        {
            return GridSlotService<T>.IsStackable(this, itemAsset, count);
        }

        public bool HasCapacity(int count)
        {
            return GridSlotService<T>.HasCapacity(this, count);
        }

        public int LeftCapacity() => GridSlotService<T>.LeftCapacity(this);

        public bool GetIsDroppableToTargetSlot(IGridSlot gridSlot)
        {
            return GridSlotService<T>.GetIsDroppableToTargetSlot(this, gridSlot);
        }

        public bool GetIsStackableToTargetGridSlot(IGridSlot gridSlot)
        {
            return GridSlotService<T>.GetIsStackableToTargetGridSlot(this, gridSlot);
        }

        void IServicableGridSlot<T>.SetQuantityItem(QuantityItem<T> quantityItem)
        {
            _quantityItem = quantityItem;
        }

        void IServicableGridSlot<T>.SetOriginalSlotIndex(int originalSlotIndex)
        {
            _originalSlotIndex = originalSlotIndex;
        }
    }

    public interface IServicableGridSlot<T> : IGridSlot where T : IItemAsset
    {
        void SetQuantityItem(QuantityItem<T> quantityItem);
        void SetOriginalSlotIndex(int originalSlotIndex);
    }

    public static class GridSlotService<T> where T : IItemAsset
    {
        public static void SetQuantity(IServicableGridSlot<T> gridSlot, int value)
        {
            SetQuantityInternal(ref gridSlot, value);
        }

        public static void SetQuantity<TSlot>(ref TSlot slot, int value)
            where TSlot : struct, IServicableGridSlot<T>
        {
            SetQuantityInternal(ref slot, value);
        }

        private static void SetQuantityInternal<TSlot>(ref TSlot gridSlot, int value)
            where TSlot : IServicableGridSlot<T>
        {
            int safeQuantity = Mathf.Max(value, 0);
            if (safeQuantity == gridSlot.Quantity)
                return;

            if (safeQuantity <= 0)
            {
                gridSlot.SetItem(null, 0);
            }
            else
            {
                if (gridSlot.QuantityItem == null)
                    throw new Exception("QuantityItem is null but trying to set quantity!");

                gridSlot.QuantityItem.SetQuantity(safeQuantity);
            }

            gridSlot.OnQuantityChange?.Invoke(gridSlot);
        }

        public static bool GetIsDroppableToTargetSlot(IServicableGridSlot<T> gridSlot, IGridSlot targetSlot)
        {
            if (gridSlot.IsEmpty || targetSlot == gridSlot)
                return true;

            if (gridSlot.IsRelativeSlot && gridSlot.InventoryManager == targetSlot.InventoryManager
                && gridSlot.OriginalSlotIndex == targetSlot.OriginalSlotIndex)
            {
                return true;
            }

            return false;
        }

        public static bool GetIsStackableToTargetGridSlot(IServicableGridSlot<T> gridSlot, IGridSlot targetSlot)
        {
            var target = targetSlot as GridSlot<T>;
            return !gridSlot.IsEmpty && !target.IsEmpty && target.IsStackable(gridSlot.ItemAsset, gridSlot.Quantity);
        }

        public static bool IsStackable(IServicableGridSlot<T> gridSlot, IItemAsset itemAsset, int count)
        {
            return HasCapacity(gridSlot, count) && (gridSlot.IsEmpty || gridSlot.ItemAsset.Equals(itemAsset));
        }

        public static int LeftCapacity(IServicableGridSlot<T> gridSlot)
        {
            return gridSlot.Capacity - gridSlot.Quantity;
        }

        public static void SetItem<TSlot>(ref TSlot gridSlot, IItemAsset itemAsset, int quantity,
            int originalSlotIndex = -1) where TSlot : struct, IServicableGridSlot<T>
        {
            SetItemInternal(ref gridSlot, itemAsset, quantity, originalSlotIndex);
        }

        public static void SetItem(IServicableGridSlot<T> gridSlot, IItemAsset itemAsset, int quantity,
            int originalSlotIndex = -1)
        {
            SetItemInternal(ref gridSlot, itemAsset, quantity, originalSlotIndex);
        }

        public static void SetItemInternal<TSlot>(ref TSlot gridSlot, IItemAsset itemAsset, int quantity,
            int originalSlotIndex = -1) where TSlot : IServicableGridSlot<T>
        {
            bool wasEmpty = gridSlot.IsEmpty;
            bool wasOriginalItemOwner = gridSlot.HasOriginalItem;

            if (itemAsset == null)
            {
                gridSlot.SetQuantityItem(default);
            }
            else
            {
                var quantityItem = new QuantityItem<T>()
                {
                    itemAsset = (T)itemAsset,
                    quantity = quantity
                };
                gridSlot.SetQuantityItem(quantityItem);
            }

            gridSlot.SetOriginalSlotIndex(originalSlotIndex);

            if (wasEmpty && gridSlot.HasOriginalItem)
            {
                gridSlot.OnEmptinessChange?.Invoke(gridSlot);
            }
            else if (wasOriginalItemOwner && gridSlot.IsEmpty)
            {
                gridSlot.OnEmptinessChange?.Invoke(gridSlot);
            }
        }

        public static bool TryConsumeQuantity<TSlot>(ref TSlot gridSlot, int quantity)
            where TSlot : struct,IServicableGridSlot<T>
        {
            return TryConsumeQuantityInternal(ref gridSlot, quantity);
        }

        public static bool TryConsumeQuantity(IServicableGridSlot<T> gridSlot, int quantity)
        {
            return TryConsumeQuantityInternal(ref gridSlot, quantity);
        }

        private static bool TryConsumeQuantityInternal<TSlot>(ref TSlot gridSlot, int quantity)
            where TSlot : IServicableGridSlot<T>
        {
            if (quantity <= 0 || gridSlot.Quantity < quantity)
                return false;

            SetQuantity(gridSlot, gridSlot.Quantity - quantity);
            return true;
        }

        public static bool HasCapacity(IServicableGridSlot<T> gridSlot, int count)
        {
            return (gridSlot.Quantity + count) <= gridSlot.Capacity;
        }
    }
}
