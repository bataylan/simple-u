using System;
using SimpleU.DataContainer;

namespace SimpleU.Inventory
{
    public interface IGridSlot
    {
        public bool IsFull => Quantity >= Capacity;
        public bool IsEmpty => !HasItem;
        public bool HasItem => QuantityItem.IsValid;
        int Index { get; }
        int RowIndex { get; }
        int ColumnIndex { get; }
        IItemAsset ItemAsset { get; }
        IInventoryManager InventoryManager { get; }
        int Quantity { get; }
        int Capacity { get; }
        IQuantityItem QuantityItem { get; }

        bool TryConsumeQuantity(int quantity); //TODO limit access internal
        void SetItem(IItemAsset itemAsset, int quantity); //TODO limit access internal
        void AddQuantity(int quantity); //TODO limit access internal
        bool IsStackable(IItemAsset itemAsset, int count);
        int LeftCapacity();
        bool GetIsDroppableToTargetSlot(IGridSlot gridSlot);
        bool GetIsStackableToTargetGridSlot(IGridSlot gridSlot);

        public static int GetIndexByRowColumnIndex(int rowIndex, int columnIndex, int columnCount)
        {
            return (rowIndex * columnCount) + columnIndex;
        }
    }

}
