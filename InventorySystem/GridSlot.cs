using System;
using SimpleU.DataContainer;
using UnityEngine;

namespace SimpleU.Inventory
{
    public class GridSlot : IServicableGridSlot
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
            private set => GridSlotService.SetQuantity(this, value);
        }
        public IItemAsset ItemAsset => _quantityItem != null ? _quantityItem.ItemAsset : default;
        public int OriginalSlotIndex => IsRelativeSlot ? _originalSlotIndex : Index;
        public IQuantityItem QuantityItem => _quantityItem;
        public int Capacity => _capacity;

        private int _originalSlotIndex;
        private IInventoryManager _inventoryManager;
        private int _index, _rowIndex, _columnIndex;
        private IQuantityItem _quantityItem;
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
            GridSlotService.SetItem(this, itemAsset, quantity, originalSlotIndex);
        }

        public bool TryConsumeQuantity(int quantity)
        {
            return GridSlotService.TryConsumeQuantity(this, quantity);
        }

        public void AddQuantity(int quantity)
        {
            Quantity += quantity;
        }

        public bool IsStackable(IItemAsset itemAsset, int count)
        {
            return GridSlotService.IsStackable(this, itemAsset, count);
        }

        public bool HasCapacity(int count)
        {
            return GridSlotService.HasCapacity(this, count);
        }

        public int LeftCapacity() => GridSlotService.LeftCapacity(this);

        public bool GetIsDroppableToTargetSlot(IGridSlot gridSlot)
        {
            return GridSlotService.GetIsDroppableToTargetSlot(this, gridSlot);
        }

        public bool GetIsStackableToTargetGridSlot(IGridSlot gridSlot)
        {
            return GridSlotService.GetIsStackableToTargetGridSlot(this, gridSlot);
        }

        void IServicableGridSlot.SetQuantityItem(QuantityItem quantityItem)
        {
            _quantityItem = quantityItem;
        }

        void IServicableGridSlot.SetOriginalSlotIndex(int originalSlotIndex)
        {
            _originalSlotIndex = originalSlotIndex;
        }
    }

    public interface IServicableGridSlot : IGridSlot
    {
        void SetQuantityItem(QuantityItem quantityItem);
        void SetOriginalSlotIndex(int originalSlotIndex);
    }

    public static class GridSlotService
    {
        public static void SetQuantity(IServicableGridSlot gridSlot, int value)
        {
            SetQuantityInternal(ref gridSlot, value);
        }

        public static void SetQuantity<TSlot>(ref TSlot slot, int value)
            where TSlot : struct, IServicableGridSlot
        {
            SetQuantityInternal(ref slot, value);
        }

        private static void SetQuantityInternal<TSlot>(ref TSlot gridSlot, int value)
            where TSlot : IServicableGridSlot
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

        public static bool GetIsDroppableToTargetSlot(IServicableGridSlot gridSlot, IGridSlot targetSlot)
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

        public static bool GetIsStackableToTargetGridSlot(IServicableGridSlot gridSlot, IGridSlot targetSlot)
        {
            var target = targetSlot as GridSlot;
            return !gridSlot.IsEmpty && !target.IsEmpty && target.IsStackable(gridSlot.ItemAsset, gridSlot.Quantity);
        }

        public static bool IsStackable(IServicableGridSlot gridSlot, IItemAsset itemAsset, int count)
        {
            return HasCapacity(gridSlot, count) && (gridSlot.IsEmpty || gridSlot.ItemAsset.Equals(itemAsset));
        }

        public static int LeftCapacity(IServicableGridSlot gridSlot)
        {
            return gridSlot.Capacity - gridSlot.Quantity;
        }

        public static void SetItem<TSlot>(ref TSlot gridSlot, IItemAsset itemAsset, int quantity,
            int originalSlotIndex = -1) where TSlot : struct, IServicableGridSlot
        {
            SetItemInternal(ref gridSlot, itemAsset, quantity, originalSlotIndex);
        }

        public static void SetItem(IServicableGridSlot gridSlot, IItemAsset itemAsset, int quantity,
            int originalSlotIndex = -1)
        {
            SetItemInternal(ref gridSlot, itemAsset, quantity, originalSlotIndex);
        }

        public static void SetItemInternal<TSlot>(ref TSlot gridSlot, IItemAsset itemAsset, int quantity,
            int originalSlotIndex = -1) where TSlot : IServicableGridSlot
        {
            bool wasEmpty = gridSlot.IsEmpty;
            bool wasOriginalItemOwner = gridSlot.HasOriginalItem;

            if (itemAsset == null)
            {
                gridSlot.SetQuantityItem(default);
            }
            else
            {
                var quantityItem = new QuantityItem()
                {
                    itemAsset = itemAsset,
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
            where TSlot : struct,IServicableGridSlot
        {
            return TryConsumeQuantityInternal(ref gridSlot, quantity);
        }

        public static bool TryConsumeQuantity(IServicableGridSlot gridSlot, int quantity)
        {
            return TryConsumeQuantityInternal(ref gridSlot, quantity);
        }

        private static bool TryConsumeQuantityInternal<TSlot>(ref TSlot gridSlot, int quantity)
            where TSlot : IServicableGridSlot
        {
            if (quantity <= 0 || gridSlot.Quantity < quantity)
                return false;

            SetQuantity(gridSlot, gridSlot.Quantity - quantity);
            return true;
        }

        public static bool HasCapacity(IServicableGridSlot gridSlot, int count)
        {
            return (gridSlot.Quantity + count) <= gridSlot.Capacity;
        }
    }
}
