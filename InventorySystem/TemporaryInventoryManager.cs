using System;
using SimpleU.DataContainer;

namespace SimpleU.Inventory
{
    public struct TemporaryInventoryManager : IManagedInventoryManager, IDisposable
    {
        public int SlotCount => _slots != null ? _slots.Length : 0;
        public int ColumnCount { get; private set; }
        public int RowCount { get; private set; }

        public IGridSlot[] GridSlots => _slots;
        IManagedGridSlot[] IManagedInventoryManager.ManagedGridSlots => _slots;
        private IManagedGridSlot[] _slots;

        public TemporaryInventoryManager(IInventoryManager inventoryManager)
        {
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

        public bool CanAddItemQuantity(IItemAsset inventoryItem, int quantity, out int leftQuantity,
            bool returnCompletedAll)
        {
            return InventoryManagerService.CanAddItemQuantity(this, inventoryItem, quantity, out leftQuantity,
                returnCompletedAll);
        }

        public bool TryAddItemQuantity(IItemAsset inventoryItem, int quantity, out int leftQuantity,
            bool returnCompletedAll = true)
        {
            return InventoryManagerService.TryAddItemQuantity(this, inventoryItem, quantity, out leftQuantity,
                returnCompletedAll);
        }
        
        public bool TryAddItemToSlot(IGridSlot slot, IItemAsset itemAsset, int quantity, 
            out int leftQuantity, bool returnCompletedAll = true)
        {
            return InventoryManagerService.TryAddItemToSlot(slot, itemAsset, quantity,  
                out leftQuantity, returnCompletedAll);
        }
        public bool CanAddItemToSlot(IGridSlot slot, IItemAsset itemAsset, int quantity, 
            out int leftQuantity, bool returnCompletedAll = true)
        {
            return InventoryManagerService.CanAddItemToSlot(slot, itemAsset, quantity,  
                out leftQuantity, returnCompletedAll);
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

        public void SetItem(IItemAsset itemAsset, int quantity)
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

        void SetQuantityItem(QuantityItem quantityItem)
        {
            _quantityItem = quantityItem;
        }
    }
}
