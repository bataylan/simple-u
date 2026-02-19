using System;
using SimpleU.DataContainer;

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
            GridSlotService.SetItem(this, itemAsset, quantity, originalSlotIndex);
        }

        public bool TryConsumeQuantity(int quantity)
        {
            if (quantity <= 0 || Quantity < quantity)
                return false;

            GridSlotService.SetQuantity(this, Quantity - quantity);
            return true;
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
}
