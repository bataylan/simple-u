using System;
using SimpleU.DataContainer;

namespace SimpleU.Inventory
{
    public class GridSlot : IGridSlot
    {
        public bool IsEmpty => !HasItem;
        public bool HasItem => QuantityItem.IsValid;
        public int Index => _slotIndex;
        public int RowIndex => _rowIndex;
        public int ColumnIndex => _columnIndex;
        public IInventoryManager InventoryManager => _inventoryManager;
        public bool IsFull => Quantity >= Capacity;
        public int Quantity => _quantityItem != null ? _quantityItem.Quantity : 0;
        public IItemAsset ItemAsset => _quantityItem != null ? _quantityItem.ItemAsset : default;
        public IQuantityItem QuantityItem => _quantityItem;
        public int Capacity => _capacity;

        private IInventoryManager _inventoryManager;
        private int _slotIndex, _rowIndex, _columnIndex;
        private IQuantityItem _quantityItem;
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

        public virtual void SetItem(IItemAsset itemAsset, int quantity)
        {
            bool wasEmpty = IsEmpty;

            SetItem_Internal(itemAsset, quantity);

            if (wasEmpty)
            {
                OnEmptinessChange?.Invoke(this);
            }
            else if (IsEmpty)
            {
                OnEmptinessChange?.Invoke(this);
            }
        }

        protected virtual void SetItem_Internal(IItemAsset itemAsset, int quantity)
        {
            if (itemAsset == null)
            {
                SetQuantityItem(default);
            }
            else
            {
                var quantityItem = new QuantityItem()
                {
                    itemAsset = itemAsset,
                    quantity = quantity
                };
                SetQuantityItem(quantityItem);
            }
        }

        public bool TryConsumeQuantity(int quantity)
        {
            if (quantity <= 0 || Quantity < quantity)
                return false;

            SetQuantity(this, Quantity - quantity);
            return true;
        }

        public void AddQuantity(int quantity)
        {
            SetQuantity(this, Quantity + quantity);
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

        void SetQuantityItem(QuantityItem quantityItem)
        {
            _quantityItem = quantityItem;
        }

        public static void SetQuantity(GridSlot gridSlot, int value)
        {
            int safeQuantity = Math.Max(value, 0);
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
    }
}
