using System.Collections;
using System.Collections.Generic;
using SimpleU.DataContainer;
using UnityEngine;

namespace SimpleU.Inventory
{
    public class AInventoryManager : IManagedInventoryManager
    {
        public AInventoryManager(int rowCount, int columnCount, int slotCapacity = 0)
        {
            RowCount = rowCount;
            ColumnCount = columnCount;
            if (slotCapacity <= 0)
                slotCapacity = int.MaxValue;

            InitializeGridSlots(slotCapacity);
        }

        public int ColumnCount { get; private set; }
        public int RowCount { get; private set; }

        public IGridSlot[] Slots => _slots;
        IGridSlot[] IInventoryManager.GridSlots => _slots;
        IManagedGridSlot[] IManagedInventoryManager.ManagedGridSlots => _slots;

        public int SlotCount => RowCount * ColumnCount;
        protected IManagedGridSlot[] _slots;

        protected virtual IManagedGridSlot CreateGridSlot(int index, int rowIndex, int columnIndex, int capacity)
        {
            return new GridSlot(this, index, rowIndex, columnIndex, capacity);
        }
        
        protected virtual void InitializeGridSlots(int slotCapacity)
        {
            _slots = new GridSlot[SlotCount];

            int slotIndex = 0;
            for (int i = 0; i < RowCount; i++)
            {
                for (int j = 0; j < ColumnCount; j++)
                {
                    _slots[slotIndex] = CreateGridSlot(slotIndex, i, j, slotCapacity);
                    slotIndex++;
                }
            }
        }

        protected void SetItem(int index, IItemAsset itemAsset, int quantity)
        {
            int leftQuantity = quantity;
            InventoryManagerService.CheckSet_Internal(_slots[index], itemAsset, true, ref leftQuantity);
            if (leftQuantity != 0)
            {
                Debug.Log("Failed to set item to slot!");
            }
        }

        public virtual bool CanAddItemQuantity(IItemAsset inventoryItem, int quantity, out int leftQuantity,
            bool returnCompletedAll)
        {
            return InventoryManagerService.CanAddItemQuantity(this, inventoryItem, quantity,
                out leftQuantity, returnCompletedAll);
        }

        public virtual bool TryAddItemQuantity(IItemAsset inventoryItem, int quantity, out int leftQuantity,
            bool returnCompletedAll)
        {
            return InventoryManagerService.TryAddItemQuantity(this, inventoryItem, quantity,
                out leftQuantity, returnCompletedAll);
        }
        
        public virtual bool TryAddItemToSlot(IGridSlot slot, IItemAsset itemAsset, int quantity, 
            out int leftQuantity, bool returnCompletedAll = true)
        {
            return InventoryManagerService.TryAddItemToSlot(slot, itemAsset, quantity,  
                out leftQuantity, returnCompletedAll);
        }
        public virtual bool CanAddItemToSlot(IGridSlot slot, IItemAsset itemAsset, int quantity, 
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
    }

    public interface IInventoryManager
    {
        int SlotCount { get; }
        int ColumnCount { get; }
        int RowCount { get; }
        IGridSlot[] GridSlots { get; }
        bool TryAddItemQuantity(IItemAsset inventoryItem, int quantity, out int leftQuantity, bool returnCompletedAll = true);
        bool CanAddItemQuantity(IItemAsset inventoryItem, int quantity, out int leftQuantity, bool returnCompletedAll = true);
        bool TryAddItemToSlot(IGridSlot slot, IItemAsset itemAsset, int quantity, 
            out int leftQuantity, bool returnCompletedAll = true);
        bool CanAddItemToSlot(IGridSlot slot, IItemAsset itemAsset, int quantity, 
            out int leftQuantity, bool returnCompletedAll = true);
        bool HasEnoughQuantity(IItemAsset itemAsset, int quantity);
        int GetQuantity(IItemAsset itemAsset);
    }

    public interface IManagedInventoryManager : IInventoryManager
    {
        IManagedGridSlot[] ManagedGridSlots { get; }
    }
}
