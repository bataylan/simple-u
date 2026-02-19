using System;
using SimpleU.DataContainer;

namespace SimpleU.Inventory
{
    public class GridSlot : IGridSlot
    {
        public bool IsEmpty => ItemAsset == null || Quantity <= 0;
        public bool HasOriginalItem => !IsEmpty && !IsRelativeSlot;
        public int Index => _index;
        public int RowIndex => _rowIndex;
        public int ColumnIndex => _columnIndex;
        public bool IsRelativeSlot => !IsEmpty && _originalSlotIndex >= 0;
        public IInventoryManager InventoryManager => _inventoryManager;
        public bool IsFull => Quantity >= Capacity;
        public int Quantity => _quantityItem != null ? _quantityItem.Quantity : 0;
        public IItemAsset ItemAsset => _quantityItem != null ? _quantityItem.ItemAsset : default;
        public int OriginalSlotIndex => IsRelativeSlot ? _originalSlotIndex : Index;
        public IQuantityItem QuantityItem => _quantityItem;
        public int Capacity => _capacity;

        private int _originalSlotIndex;
        private IInventoryManager _inventoryManager;
        private int _index, _rowIndex, _columnIndex;
        private IQuantityItem _quantityItem;
        private int _capacity;

        public Action<GridSlot> OnEmptinessChange { get; set; }
        public Action<GridSlot> OnQuantityChange { get; set; }

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
            bool wasEmpty = IsEmpty;
            bool wasOriginalItemOwner = HasOriginalItem;
            
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

            SetOriginalSlotIndex(originalSlotIndex);
            
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

        void SetOriginalSlotIndex(int originalSlotIndex)
        {
            _originalSlotIndex = originalSlotIndex;
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
