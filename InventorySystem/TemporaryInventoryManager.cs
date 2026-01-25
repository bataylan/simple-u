using System;
using SimpleU.DataContainer;

namespace SimpleU.Inventory
{
    public struct TemporaryInventoryManager<T> : IInventoryManager, IDisposable where T : IItemAsset
    {
        public int SlotCount => _slots.Length;
        public int ColumnCount { get; private set; }
        public int RowCount { get; private set; }

        private IGridSlot[] _slots;
        public IGridSlot[] GridSlots => _slots;

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
                    _slots[slotIndex] = new TemporaryGrid<T>(this, slotIndex, i, j, slotCapacity);
                    slotIndex++;
                }
            }
        }

        public bool TryAddItemQuantityToSlot(IItemAsset inventoryItem, int quantity, int slotIndex, out int leftCount)
        {
            return InventoryManagerService<T>.TryAddItemQuantityToSlot(this, inventoryItem, quantity, slotIndex, 
                out leftCount);
        }

        public bool TryAddItemToSingleSlot(IItemAsset inventoryItem, int quantity, out int leftCount,
            out IGridSlot gridSlot, bool stackItems = true)
        {
            return InventoryManagerService<T>.TryAddItemToSingleSlot(this, inventoryItem, quantity, out leftCount,
                out gridSlot, stackItems);
        }

        public bool CanAddItemQuantity(IItemAsset inventoryItem, int quantity, out int leftQuantity)
        {
            return InventoryManagerService<T>.CanAddItemQuantity(this, inventoryItem, quantity, out leftQuantity);
        }

        public bool TryAddItemQuantitySmart(IItemAsset inventoryItem, int quantity, out int leftQuantity)
        {
            return InventoryManagerService<T>.TryAddItemQuantitySmart(this, inventoryItem, quantity, out leftQuantity);
        }

        public bool CanAddItemFromSingleSlot(IItemAsset inventoryItem, int quantity, out int leftQuantity,
            out int completedQuantity, out IGridSlot gridSlot, bool stackItems = true)
        {
            return InventoryManagerService<T>.CanAddItemToSlot(this, inventoryItem, quantity, out leftQuantity,
                out completedQuantity, out gridSlot, stackItems);
        }
        
        public int TryGetSlotIndex(IItemAsset item)
        {
            return InventoryManagerService<T>.TryGetSlotIndex(this, item);
        }

        public bool HasEnoughQuantity(IItemAsset itemAsset, int quantity)
        {
            return InventoryManagerService<T>.HasEnoughQuantity(this, itemAsset, quantity);
        }

        public int GetQuantity(IItemAsset itemAsset)
        {
            return InventoryManagerService<T>.GetQuantity(this, itemAsset);
        }

        public void Dispose()
        {
            _slots = null;
        }
    }

    public struct TemporaryGrid<T> : IServicableGridSlot<T> where T : IItemAsset
    {
        public int Quantity
        {
            get => _quantityItem != null ? _quantityItem.Quantity : 0;
            private set => GridSlotService<T>.SetQuantity(this, value);
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

        public Action<IGridSlot> OnEmptinessChange
        {
            get => null;
            set => throw new NotImplementedException();
        }
        public Action<IGridSlot> OnQuantityChange 
        {
            get => null;
            set => throw new NotImplementedException();
        }

        private IInventoryManager _inventoryManager;
        private int _index;
        private int _rowIndex;
        private int _columnIndex;
        private int _capacity;
        private int _originalSlotIndex;
        private IQuantityItem<T> _quantityItem;

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
}
