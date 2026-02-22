using System;
using SimpleU.DataContainer;

namespace SimpleU.Inventory
{
    public interface IGridSlot
    {
        public bool IsFull => Quantity >= Capacity;
        public bool IsEmpty => !HasItem;
        public bool HasItem => Quantity > 0 && ItemAsset != null;
        int Index { get; }
        int RowIndex { get; }
        int ColumnIndex { get; }
        IItemAsset ItemAsset { get; }
        IInventoryManager InventoryManager { get; }
        int Quantity { get; }
        int Capacity { get; }
        IQuantityItem QuantityItem { get; }
        public int LeftCapacity => Capacity - Quantity;

        public static int GetIndexByRowColumnIndex(int rowIndex, int columnIndex, int columnCount)
        {
            return (rowIndex * columnCount) + columnIndex;
        }
        
        public bool GetIsDroppableToTargetSlot(IGridSlot targetSlot)
        {
            return IsEmpty || targetSlot == this;
        }

        public bool GetIsStackableToTargetGridSlot(IGridSlot targetSlot)
        {
            return !IsEmpty && !targetSlot.IsEmpty && targetSlot.IsStackable(ItemAsset, Quantity);
        }

        public bool IsStackable(IItemAsset itemAsset, int count)
        {
            return HasCapacity(count) && (IsEmpty || ItemAsset.Equals(itemAsset));
        }

        public bool HasCapacity(int count)
        {
            return (Quantity + count) <= Capacity;
        }
    }
    
    public interface IManagedGridSlot : IGridSlot
    {
        IManagedInventoryManager ManagedInventoryManager => InventoryManager as IManagedInventoryManager;
        bool CanApplyQuantity(IGridSlot sourceSlot, IItemAsset itemAsset, int quantity, out int leftQuantity);
        void RemoveQuantity(IGridSlot sourceSlot, int quantity, out IQuantityItem removedItem);
        void AddQuantity(IGridSlot sourceSlot, IQuantityItem quantityItem);
    }
}
