using System;
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
            _slots[index].SetItem(itemAsset, quantity, null);
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
            return CanAddItem_Internal(sourceSlot, targetSlot, itemAsset, quantity);
        }

        protected virtual bool CanAddItem_Internal(IGridSlot sourceSlot, IGridSlot targetSlot, IItemAsset itemAsset, int quantity)
        {
            return true;
        }
    }

    public interface IInventoryManager
    {
        int SlotCount { get; }
        int ColumnCount { get; }
        int RowCount { get; }
        IGridSlot[] GridSlots { get; }
        bool HasEnoughQuantity(IItemAsset itemAsset, int quantity);
        int GetQuantity(IItemAsset itemAsset);
    }

    public interface IManagedInventoryManager : IInventoryManager
    {
        IManagedGridSlot[] ManagedGridSlots { get; }
        bool CanAddItem(IGridSlot sourceSlot, IGridSlot targetSlot, IItemAsset itemAsset, int quantity);
    }
}
