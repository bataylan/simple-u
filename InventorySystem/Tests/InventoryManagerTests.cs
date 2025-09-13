using NUnit.Framework;
using UnityEngine;
using SimpleU.Inventory;
using SimpleU.DataContainer;

namespace SimpleU.Tests.Inventory
{
    public class InventoryManagerTests
    {
        private AInventoryManager<ItemAsset> _inventoryManager;
        private ItemAsset _testItem;

        [SetUp]
        public void SetUp()
        {
            _inventoryManager = new AInventoryManager<ItemAsset>(2, 2);
            _testItem = ScriptableObject.CreateInstance<ItemAsset>();
        }

        [Test]
        public void Init_ShouldInitializeSlots()
        {
            Assert.AreEqual(4, _inventoryManager.Slots.Length);
        }

        [Test]
        public void TryAddItem_ShouldAddItemToInventory()
        {
            AddItem();
        }

        private void AddItem()
        {
            int addQuantity = 1;
            bool result = _inventoryManager.TryAddItemQuantity(_testItem, addQuantity, out int leftCount,
                out IGridSlot gridSlot);
            Assert.IsTrue(result);
            Assert.AreEqual(0, leftCount);
            Assert.IsNotNull(gridSlot);

            int currentQuantity = _inventoryManager.GetQuantity(_testItem);
            Assert.AreEqual(addQuantity, currentQuantity);
        }

        [Test]
        public void TryAddRemoveItem_ShouldAddItemToInventory()
        {
            AddItem();
            RemoveItem();
        }

        private void RemoveItem()
        {
            int quantity = _inventoryManager.GetQuantity(_testItem);
            bool removeResult = _inventoryManager.TryAddItemQuantity(_testItem, -1, out int leftCount,
                out IGridSlot gridSlot);
            Assert.IsTrue(removeResult);
            Assert.AreEqual(0, leftCount);
            Assert.IsNotNull(gridSlot);

            int currentQuantity = _inventoryManager.GetQuantity(_testItem);
            Assert.AreEqual(currentQuantity, quantity - 1);
        }
    }
}
