using System;
using SimpleU.DataContainer;

namespace SimpleU.Inventory
{
    public struct TemporaryInventoryManager : IInventoryManager, IDisposable
    {
        public int SlotCount => _slots != null ? _slots.Length : 0;
        public int ColumnCount { get; private set; }
        public int RowCount { get; private set; }

        private IGridSlot[] _slots;
        public IGridSlot[] GridSlots => _slots;

        public TemporaryInventoryManager(IInventoryManager inventoryManager)
        {
            RowCount = inventoryManager.RowCount;
            ColumnCount = inventoryManager.ColumnCount;

            _slots = new IGridSlot[ColumnCount * RowCount];

            int slotIndex = 0;
            for (int i = 0; i < RowCount; i++)
            {
                for (int j = 0; j < ColumnCount; j++)
                {
                    var sourceGridSlot = inventoryManager.GridSlots[slotIndex];
                    var gridSlot = new TemporaryGrid(this, slotIndex, i, j, sourceGridSlot.Capacity);
                    
                    if (!sourceGridSlot.IsEmpty)
                    {
                        int originalSlotIndex = sourceGridSlot.HasOriginalItem ? -1 : sourceGridSlot.OriginalSlotIndex;
                        gridSlot.SetItem(sourceGridSlot.ItemAsset, sourceGridSlot.Quantity, originalSlotIndex);
                    }
                    
                    _slots[slotIndex] = gridSlot;
                    slotIndex++;
                }
            }
        }

        public TemporaryInventoryManager(int rowCount, int columnCount, int slotCapacity = 0)
        {
            RowCount = rowCount;
            ColumnCount = columnCount;

            _slots = new IGridSlot[ColumnCount * RowCount];

            if (slotCapacity <= 0)
                slotCapacity = int.MaxValue;

            int slotIndex = 0;
            for (int i = 0; i < RowCount; i++)
            {
                for (int j = 0; j < ColumnCount; j++)
                {
                    _slots[slotIndex] = new TemporaryGrid(this, slotIndex, i, j, slotCapacity);
                    slotIndex++;
                }
            }
        }

        public bool TryAddItemQuantityToSlot(IItemAsset inventoryItem, int quantity, int slotIndex, out int leftCount)
        {
            return InventoryManagerService.TryAddItemQuantityToSlot(this, inventoryItem, quantity, slotIndex, 
                out leftCount);
        }

        public bool TryAddItemToSingleSlot(IItemAsset inventoryItem, int quantity, out int leftCount,
            out IGridSlot gridSlot, bool stackItems = true)
        {
            return InventoryManagerService.TryAddItemToSingleSlot(this, inventoryItem, quantity, out leftCount,
                out gridSlot, stackItems);
        }

        public bool CanAddItemQuantity(IItemAsset inventoryItem, int quantity, out int leftQuantity)
        {
            return InventoryManagerService.CanAddItemQuantity(this, inventoryItem, quantity, out leftQuantity);
        }

        public bool TryAddItemQuantitySmart(IItemAsset inventoryItem, int quantity, out int leftQuantity)
        {
            return InventoryManagerService.TryAddItemQuantitySmart(this, inventoryItem, quantity, out leftQuantity);
        }

        public bool CanAddItemFromSingleSlot(IItemAsset inventoryItem, int quantity, out int leftQuantity,
            out int completedQuantity, out IGridSlot gridSlot, bool stackItems = true)
        {
            return InventoryManagerService.CanAddItemToSlot(this, inventoryItem, quantity, out leftQuantity,
                out completedQuantity, out gridSlot, stackItems);
        }
        
        public int TryGetSlotIndex(IItemAsset item)
        {
            return InventoryManagerService.TryGetSlotIndex(this, item);
        }

        public bool HasEnoughQuantity(IItemAsset itemAsset, int quantity)
        {
            return InventoryManagerService.HasEnoughQuantity(this, itemAsset, quantity);
        }

        public int GetQuantity(IItemAsset itemAsset)
        {
            return InventoryManagerService.GetQuantity(this, itemAsset);
        }

        public void Dispose()
        {
            _slots = null;
        }
    }

    public struct TemporaryGrid : IServicableGridSlot
    {
        public int Quantity
        {
            get => _quantityItem != null ? _quantityItem.Quantity : 0;
            private set => SetItem(_quantityItem.ItemAsset, value, OriginalSlotIndex);
        }
        public bool IsEmpty => ItemAsset == null || Quantity <= 0;
        public bool HasOriginalItem => !IsEmpty && !IsRelativeSlot;
        public int Index => _index;
        public int RowIndex => _rowIndex;
        public int ColumnIndex => _columnIndex;
        public bool IsRelativeSlot => !IsEmpty && _originalSlotIndex >= 0;
        public IInventoryManager InventoryManager => _inventoryManager;
        public bool IsFull => Quantity >= Capacity;
        public int OriginalSlotIndex => _originalSlotIndex;
        public IItemAsset ItemAsset => _quantityItem != null ? _quantityItem.ItemAsset : default;
        public int Capacity => _capacity;
        public IQuantityItem QuantityItem => _quantityItem;

        private IInventoryManager _inventoryManager;
        private int _index;
        private int _rowIndex;
        private int _columnIndex;
        private int _capacity;
        private int _originalSlotIndex;
        private IQuantityItem _quantityItem;

        public TemporaryGrid(IInventoryManager inventoryManager, int index, int rowIndex, int columnIndex,
            int capacity = int.MaxValue)
        {
            _inventoryManager = inventoryManager;
            _index = index;
            _rowIndex = rowIndex;
            _columnIndex = columnIndex;
            _capacity = capacity;
            _originalSlotIndex = 0;
            _quantityItem = null;
        }

        public void SetItem(IItemAsset itemAsset, int quantity, int originalSlotIndex = -1)
        {
            GridSlotService.SetItem(ref this, itemAsset, quantity, originalSlotIndex);
        }

        public bool TryConsumeQuantity(int quantity)
        {
            if (quantity <= 0 || Quantity < quantity)
                return false;

            SetItem(_quantityItem.ItemAsset, Quantity - quantity, OriginalSlotIndex);
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
}
