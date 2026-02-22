using System;
using SimpleU.DataContainer;

namespace SimpleU.Inventory
{
    public struct TemporaryInventoryManager : IManagedInventoryManager, IDisposable
    {
        public int SlotCount => _slots != null ? _slots.Length : 0;
        public int ColumnCount { get; private set; }

        private IManagedInventoryManager _copiedInventoryManager;

        public int RowCount { get; private set; }

        public IGridSlot[] GridSlots => _slots;
        IManagedGridSlot[] IManagedInventoryManager.ManagedGridSlots => _slots;
        private IManagedGridSlot[] _slots;

        public TemporaryInventoryManager(IInventoryManager inventoryManager)
        {
            _copiedInventoryManager = inventoryManager as IManagedInventoryManager;
            RowCount = inventoryManager.RowCount;
            ColumnCount = inventoryManager.ColumnCount;

            _slots = new IManagedGridSlot[ColumnCount * RowCount];

            int slotIndex = 0;
            for (int i = 0; i < RowCount; i++)
            {
                for (int j = 0; j < ColumnCount; j++)
                {
                    var sourceGridSlot = inventoryManager.GridSlots[slotIndex];
                    var gridSlot = new TemporaryGrid(this, slotIndex, i, j, sourceGridSlot.Capacity);
                    
                    if (!sourceGridSlot.IsEmpty)
                    {
                        gridSlot.SetItem(sourceGridSlot.ItemAsset, sourceGridSlot.Quantity);
                    }
                    
                    _slots[slotIndex] = gridSlot;
                    slotIndex++;
                }
            }
        }

        public TemporaryInventoryManager(int rowCount, int columnCount, int slotCapacity = 0)
        {
            _copiedInventoryManager = null;
            RowCount = rowCount;
            ColumnCount = columnCount;

            _slots = new IManagedGridSlot[ColumnCount * RowCount];

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

        public bool HasEnoughQuantity(IItemAsset itemAsset, int quantity)
        {
            return InventoryService.HasEnoughQuantity(this, itemAsset, quantity);
        }

        public int GetQuantity(IItemAsset itemAsset)
        {
            return InventoryService.GetQuantity(this, itemAsset);
        }

        bool IManagedInventoryManager.CanAddItem(IGridSlot sourceSlot, IGridSlot targetSlot, IItemAsset itemAsset, int quantity)
        {
            if (_copiedInventoryManager != null)
                return _copiedInventoryManager.CanAddItem(sourceSlot, targetSlot, itemAsset, quantity);
            else
                return true;
        }

        public void Dispose()
        {
            _slots = null;
        }
    }

    public struct TemporaryGrid : IManagedGridSlot
    {
        public int Quantity
        {
            get => _quantityItem != null ? _quantityItem.Quantity : 0;
            private set => SetItem(_quantityItem.ItemAsset, value);
        }
        
        public int Index => _index;
        public int RowIndex => _rowIndex;
        public int ColumnIndex => _columnIndex;
        public IInventoryManager InventoryManager => _inventoryManager;
        
        public IItemAsset ItemAsset => _quantityItem != null ? _quantityItem.ItemAsset : default;
        public int Capacity => _capacity;
        public IQuantityItem QuantityItem => _quantityItem;

        private IInventoryManager _inventoryManager;
        private int _index;
        private int _rowIndex;
        private int _columnIndex;
        private int _capacity;
        private IQuantityItem _quantityItem;

        public TemporaryGrid(IInventoryManager inventoryManager, int index, int rowIndex, int columnIndex,
            int capacity = int.MaxValue)
        {
            _inventoryManager = inventoryManager;
            _index = index;
            _rowIndex = rowIndex;
            _columnIndex = columnIndex;
            _capacity = capacity;
            _quantityItem = null;
        }

        public void SetItem(IItemAsset itemAsset, int quantity)
        {
            if (itemAsset == null)
            {
                _quantityItem = default;
            }
            else
            {
                var quantityItem = new QuantityItem()
                {
                    itemAsset = itemAsset,
                    quantity = quantity
                };
                _quantityItem = quantityItem;
            }
        }

        bool IManagedGridSlot.CanApplyQuantity(IGridSlot sourceSlot, IItemAsset itemAsset, int quantity, out int leftQuantity)
        {
            return GridSlotService.CanApplyQuantity(this, sourceSlot, itemAsset, quantity, out leftQuantity);
        }

        void IManagedGridSlot.RemoveQuantity(IGridSlot sourceSlot, int quantity, out IQuantityItem removedItem)
        {
            GridSlotService.RemoveQuantity(this, sourceSlot, quantity, out removedItem);
        }

        void IManagedGridSlot.AddQuantity(IGridSlot sourceSlot, IQuantityItem quantityItem)
        {
            if (_quantityItem == null)
            {
                _quantityItem = quantityItem;
            }
            else
            {
                QuantityItem.SetQuantity(Quantity + quantityItem.Quantity);
            }
        }
    }
}
