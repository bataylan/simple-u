using System.Collections;
using System.Collections.Generic;
using SimpleU.DataContainer;
using UnityEngine;

namespace SimpleU.Inventory
{
    public class AInventoryManager : IInventoryManager
    {
        public AInventoryManager(int rowCount, int columnCount, int slotCapacity = 0)
        {
            RowCount = rowCount;
            ColumnCount = columnCount;

            _slots = new GridSlot[SlotCount];

            if (slotCapacity <= 0)
                slotCapacity = int.MaxValue;

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

        public int ColumnCount { get; private set; }
        public int RowCount { get; private set; }

        public IGridSlot[] Slots => _slots;
        IGridSlot[] IInventoryManager.GridSlots => _slots;

        public int SlotCount => RowCount * ColumnCount;
        protected IGridSlot[] _slots;

        protected virtual IGridSlot CreateGridSlot(int index, int rowIndex, int columnIndex, int capacity)
        {
            return new GridSlot(this, index, rowIndex, columnIndex, capacity);
        }

        public virtual bool TryAddItemQuantityToSlot(IItemAsset inventoryItem, int quantity, int slotIndex, out int leftCount)
        {
            return InventoryManagerService.TryAddItemQuantityToSlot(this, inventoryItem, quantity, slotIndex, 
                out leftCount);
        }

        public virtual bool TryAddItemToSingleSlot(IItemAsset inventoryItem, int quantity, out int leftCount,
            out IGridSlot gridSlot, bool stackItems = true)
        {
            return InventoryManagerService.TryAddItemToSingleSlot(this, inventoryItem, quantity, out leftCount,
                out gridSlot, stackItems);
        }

        public virtual bool CanAddItemQuantity(IItemAsset inventoryItem, int quantity, out int leftQuantity)
        {
            return InventoryManagerService.CanAddItemQuantity(this, inventoryItem, quantity, out leftQuantity);
        }

        public virtual bool TryAddItemQuantitySmart(IItemAsset inventoryItem, int quantity, out int leftQuantity)
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
    }

    public interface IInventoryManager
    {
        int SlotCount { get; }
        int ColumnCount { get; }
        int RowCount { get; }
        IGridSlot[] GridSlots { get; }
        int TryGetSlotIndex(IItemAsset item);
        bool HasEnoughQuantity(IItemAsset itemAsset, int quantity);
        int GetQuantity(IItemAsset itemAsset);
        bool CanAddItemFromSingleSlot(IItemAsset inventoryItem, int quantity, out int leftQuantity,
            out int completedQuantity, out IGridSlot gridSlot, bool stackItems = true);
        bool TryAddItemToSingleSlot(IItemAsset inventoryItem, int quantity, out int leftCount, out IGridSlot gridSlot, bool stackItems = true);
        bool TryAddItemQuantityToSlot(IItemAsset item, int quantity, int slotIndex, out int leftQuantity);
        bool TryAddItemQuantitySmart(IItemAsset inventoryItem, int quantity, out int leftQuantity);
        bool CanAddItemQuantity(IItemAsset inventoryItem, int quantity, out int leftQuantity);
    }
}
