using System;
using SimpleU.DataContainer;

namespace SimpleU.Inventory
{
    public interface IGridSlot
    {
        bool IsEmpty { get; }
        bool HasOriginalItem { get; }
        RowColumnIndex[] RelativeSlotIndexes { get; }
        int Index { get; }
        int RowIndex { get; }
        int ColumnIndex { get; }
        bool IsRelativeSlot { get; }
        int OriginalSlotIndex { get; }
        IItemAsset ItemAsset { get; }
        IInventoryManager InventoryManager { get; }
        int Quantity { get; }
        int Capacity { get; }
        IQuantityItem QuantityItem {get;}
        Action<IGridSlot> OnEmptinessChange { get; set; }
        Action<IGridSlot> OnQuantityChange { get; set; }

        bool GetIsDroppableToTargetSlot(IGridSlot gridSlot);
        bool GetIsStackableToTargetGridSlot(IGridSlot gridSlot);
        bool TryConsumeQuantity(int quantity);

        public static int GetIndexByRowColumnIndex(int rowIndex, int columnIndex, int columnCount)
        {
            return (rowIndex * columnCount) + columnIndex;
        }
    }
    
}
