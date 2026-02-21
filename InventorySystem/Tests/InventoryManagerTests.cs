using NUnit.Framework;
using UnityEngine;
using SimpleU.Inventory;
using SimpleU.DataContainer;

namespace SimpleU.Tests.Inventory
{
    public class InventoryManagerTests
    {
        private AInventoryManager _inventoryManager;
        private ItemAsset _testItem;
        private ItemAsset _testItemNonStackable;

        // A concrete mock for testing since ItemAsset is abstract or might need specific settings
        private class MockItemAsset : ItemAsset
        {
            public bool _stackable = true;
            public override bool IsStackable => _stackable;
        }

        [SetUp]
        public void SetUp()
        {
            // Create a 2x2 inventory (4 slots) with a capacity of 10 per slot
            _inventoryManager = new AInventoryManager(2, 2, 10);
            
            _testItem = ScriptableObject.CreateInstance<MockItemAsset>();
            ((MockItemAsset)_testItem)._stackable = true;

            _testItemNonStackable = ScriptableObject.CreateInstance<MockItemAsset>();
            ((MockItemAsset)_testItemNonStackable)._stackable = false;
        }

        [Test]
        public void Initialize_CorrectSlotCount()
        {
            Assert.AreEqual(4, _inventoryManager.SlotCount);
            Assert.AreEqual(4, _inventoryManager.Slots.Length);
        }

        [Test]
        public void Initialize_SlotsAreEmpty()
        {
            foreach (var slot in _inventoryManager.Slots)
            {
                Assert.IsTrue(slot.IsEmpty);
                Assert.AreEqual(0, slot.Quantity);
            }
        }

        [Test]
        public void TryAdd_SingleItem_Success()
        {
            bool success = _inventoryManager.TryAddItemQuantity(_testItem, 5, out int leftOver, false);

            Assert.IsTrue(success);
            Assert.AreEqual(0, leftOver);
            Assert.AreEqual(5, _inventoryManager.Slots[0].Quantity);
            Assert.AreEqual(_testItem, _inventoryManager.Slots[0].ItemAsset);
        }

        [Test]
        public void TryAdd_StackableItem_Merges()
        {
            // Add 5 initially
            _inventoryManager.TryAddItemQuantity(_testItem, 5, out _, false);
            
            // Add 3 more
            bool success = _inventoryManager.TryAddItemQuantity(_testItem, 3, out int leftOver, false);

            Assert.IsTrue(success);
            Assert.AreEqual(0, leftOver);
            // Should still be in the first slot, total 8
            Assert.AreEqual(8, _inventoryManager.Slots[0].Quantity);
            Assert.IsTrue(_inventoryManager.Slots[1].IsEmpty);
        }

        [Test]
        public void TryAdd_StackableItem_Overflows()
        {
            // Slot capacity is 10. Add 15.
            bool success = _inventoryManager.TryAddItemQuantity(_testItem, 15, out int leftOver, false);

            Assert.IsTrue(success);
            Assert.AreEqual(0, leftOver);
            
            // Slot 0 should be full (10)
            Assert.AreEqual(10, _inventoryManager.Slots[0].Quantity);
            // Slot 1 should have remainder (5)
            Assert.AreEqual(5, _inventoryManager.Slots[1].Quantity);
        }

        [Test]
        public void TryAdd_InventoryFull_ReturnsLeftover()
        {
            // Fill all 4 slots with 10 items each = 40 capacity
            _inventoryManager.TryAddItemQuantity(_testItem, 40, out _, false);

            // Try adding 5 more
            bool success = _inventoryManager.TryAddItemQuantity(_testItem, 5, out int leftOver, false);

            Assert.IsFalse(success);
            Assert.AreEqual(5, leftOver);
        }

        [Test]
        public void TryAdd_NonStackable_Splits()
        {
            // Add 2 non-stackable items
            bool success = _inventoryManager.TryAddItemQuantity(_testItemNonStackable, 2, out int leftOver, false);

            Assert.IsTrue(success);
            Assert.AreEqual(0, leftOver);

            // Should occupy slot 0 and slot 1 with quantity 1 each
            Assert.AreEqual(1, _inventoryManager.Slots[0].Quantity);
            Assert.AreEqual(1, _inventoryManager.Slots[1].Quantity);
            Assert.AreEqual(_testItemNonStackable, _inventoryManager.Slots[0].ItemAsset);
        }

        [Test]
        public void CanAdd_Success_DoesNotModify()
        {
            // Check if we can add 5 items
            bool canAdd = _inventoryManager.CanAddItemQuantity(_testItem, 5, out int leftOver, false);

            Assert.IsTrue(canAdd);
            Assert.AreEqual(0, leftOver);

            // Ensure inventory is STILL empty
            Assert.IsTrue(_inventoryManager.Slots[0].IsEmpty);
        }

        [Test]
        public void GetQuantity_MultipleSlots()
        {
            // Add 10 to slot 0, 5 to slot 1
            _inventoryManager.TryAddItemQuantity(_testItem, 15, out _, false);

            int total = _inventoryManager.GetQuantity(_testItem);
            Assert.AreEqual(15, total);
        }

        [Test]
        public void HasEnoughQuantity_Checks()
        {
            _inventoryManager.TryAddItemQuantity(_testItem, 10, out _, false);

            Assert.IsTrue(_inventoryManager.HasEnoughQuantity(_testItem, 5));
            Assert.IsTrue(_inventoryManager.HasEnoughQuantity(_testItem, 10));
            Assert.IsFalse(_inventoryManager.HasEnoughQuantity(_testItem, 11));
        }
    }
}
