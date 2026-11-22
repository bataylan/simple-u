using System;
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

        private class FilteredInventory : AInventoryManager
        {
            public Func<IGridSlot, IGridSlot, IItemAsset, int, bool> Filter { get; set; }

            public FilteredInventory(int capacity) : base(1, 1, capacity)
            {
            }

            protected override bool CanAddItem_Internal(IGridSlot sourceSlot, IGridSlot targetSlot,
                IItemAsset itemAsset, int quantity)
            {
                return Filter?.Invoke(sourceSlot, targetSlot, itemAsset, quantity) ?? true;
            }
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

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_testItem);
            UnityEngine.Object.DestroyImmediate(_testItemNonStackable);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Swap_DryRunAndCommitPreserveStacksAndNotifyAfterBothMoves(bool acrossInventories)
        {
            var source = (GridSlot)_inventoryManager.Slots[0];
            var target = acrossInventories ? (GridSlot)new FilteredInventory(5).Slots[0]
                : (GridSlot)_inventoryManager.Slots[1];
            var payload = new object();
            var otherPayload = new object();
            FillSlot(source, _testItem, 5, payload);
            FillSlot(target, _testItemNonStackable, 1, otherPayload);

            int itemChanges = 0;
            int quantityChanges = 0;
            int emptinessChanges = 0;
            void AssertBothStacksSwapped(GridSlot _)
            {
                AssertStack(source, _testItemNonStackable, 1, otherPayload);
                AssertStack(target, _testItem, 5, payload);
            }
            foreach (var slot in new[] { source, target })
            {
                slot.OnItemChange += changedSlot =>
                {
                    AssertBothStacksSwapped(changedSlot);
                    itemChanges++;
                };
                slot.OnQuantityChange += changedSlot =>
                {
                    AssertBothStacksSwapped(changedSlot);
                    quantityChanges++;
                };
                slot.OnEmptinessChange += _ => emptinessChanges++;
            }

            Assert.IsTrue(InventoryService.CheckSwapItems(source, target, apply: false));
            AssertStack(source, _testItem, 5, payload);
            AssertStack(target, _testItemNonStackable, 1, otherPayload);
            Assert.AreEqual(0, itemChanges + quantityChanges + emptinessChanges);

            Assert.IsTrue(InventoryService.CheckSwapItems(source, target, apply: true));
            AssertBothStacksSwapped(source);
            Assert.AreEqual(2, itemChanges);
            Assert.AreEqual(2, quantityChanges);
            Assert.AreEqual(0, emptinessChanges);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Swap_RejectsInsufficientCapacityInEitherDirection(bool reverse)
        {
            var largerSlot = (GridSlot)_inventoryManager.Slots[0];
            var smallerSlot = (GridSlot)new FilteredInventory(4).Slots[0];
            FillSlot(largerSlot, _testItem, 5);
            FillSlot(smallerSlot, _testItemNonStackable, 1);

            AssertRejectedSwap(reverse ? smallerSlot : largerSlot, reverse ? largerSlot : smallerSlot);
        }

        [TestCase(true, true)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(false, false)]
        public void Swap_RejectsWhenEitherFilterBlocksAdditionOrRemoval(bool filterSource, bool blockAddition)
        {
            var sourceInventory = new FilteredInventory(10);
            var targetInventory = new FilteredInventory(10);
            var source = (GridSlot)sourceInventory.Slots[0];
            var target = (GridSlot)targetInventory.Slots[0];
            FillSlot(source, _testItem, 5);
            FillSlot(target, _testItemNonStackable, 1);

            var filteredInventory = filterSource ? sourceInventory : targetInventory;
            filteredInventory.Filter = (otherSlot, filteredSlot, item, quantity) =>
            {
                Assert.AreSame(filterSource ? target : source, otherSlot);
                Assert.AreSame(filterSource ? source : target, filteredSlot);
                Assert.AreSame(quantity > 0 ? otherSlot.ItemAsset : filteredSlot.ItemAsset, item);
                Assert.AreEqual(quantity > 0 ? otherSlot.Quantity : -filteredSlot.Quantity, quantity);
                return blockAddition ? quantity < 0 : quantity > 0;
            };

            AssertRejectedSwap(source, target);
        }

        [Test]
        public void Swap_RejectsOversizedNonStackableStack()
        {
            var source = (GridSlot)_inventoryManager.Slots[0];
            var target = (GridSlot)_inventoryManager.Slots[1];
            FillSlot(source, _testItem, 5);
            FillSlot(target, _testItemNonStackable, 2);

            AssertRejectedSwap(source, target);
        }

        [Test]
        public void Swap_RejectsEmptyMatchingAndIdenticalSlots()
        {
            var source = (GridSlot)_inventoryManager.Slots[0];
            var target = (GridSlot)_inventoryManager.Slots[1];
            FillSlot(source, _testItem, 8);

            AssertRejectedSwap(source, target);
            AssertRejectedSwap(target, source);
            AssertRejectedSwap(source, source);
            FillSlot(target, _testItem, 4);
            AssertRejectedSwap(source, target);

            // Normal transfer still merges as much as fits into a matching occupied slot.
            Assert.IsTrue(InventoryService.CheckAddItem(source, target, _testItem, 8, true,
                out int leftQuantity, returnCompletedAllQuantity: false));
            Assert.AreEqual(2, leftQuantity);
            AssertStack(source, _testItem, 2);
            AssertStack(target, _testItem, 10);
        }

        private static void FillSlot(GridSlot slot, IItemAsset item, int quantity, object extraData = null)
        {
            ((IManagedGridSlot)slot).AddQuantity(null, new QuantityItem
            {
                itemAsset = item, quantity = quantity, ExtraData = extraData
            });
        }

        private static void AssertStack(GridSlot slot, IItemAsset item, int quantity, object extraData = null)
        {
            Assert.AreSame(item, slot.ItemAsset);
            Assert.AreEqual(quantity, slot.Quantity);
            Assert.AreSame(extraData, slot.QuantityItem.ExtraData);
        }

        private static void AssertRejectedSwap(GridSlot source, GridSlot target)
        {
            var sourceStack = source.QuantityItem;
            var targetStack = target.QuantityItem;
            int sourceQuantity = source.Quantity;
            int targetQuantity = target.Quantity;
            source.OnQuantityChange += _ => Assert.Fail("Rejected swap changed the source slot.");
            target.OnQuantityChange += _ => Assert.Fail("Rejected swap changed the target slot.");
            try
            {
                Assert.IsFalse(InventoryService.CheckSwapItems(source, target, apply: false));
                Assert.IsFalse(InventoryService.CheckSwapItems(source, target, apply: true));
            }
            finally
            {
                source.OnQuantityChange = null;
                target.OnQuantityChange = null;
            }
            Assert.AreSame(sourceStack, source.QuantityItem);
            Assert.AreSame(targetStack, target.QuantityItem);
            Assert.AreEqual(sourceQuantity, source.Quantity);
            Assert.AreEqual(targetQuantity, target.Quantity);
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
            bool success = InventoryService.CheckAddItem(null, _inventoryManager, 
                _testItem, 5, true, out int leftOver, false);

            Assert.IsTrue(success);
            Assert.AreEqual(0, leftOver);
            Assert.AreEqual(5, _inventoryManager.Slots[0].Quantity);
            Assert.AreEqual(_testItem, _inventoryManager.Slots[0].ItemAsset);
        }

        [Test]
        public void TryAdd_StackableItem_Merges()
        {
            // Add 5 initially
            InventoryService.CheckAddItem(null, _inventoryManager, 
                _testItem, 5, true, out _, false);
            
            // Add 3 more
            bool success = InventoryService.CheckAddItem(null, _inventoryManager, 
                _testItem, 3, true, out int leftOver, false);

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
            bool success = InventoryService.CheckAddItem(null, _inventoryManager, 
                _testItem, 15, true, out int leftOver, false);

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
            InventoryService.CheckAddItem(null, _inventoryManager, 
                _testItem, 40, true, out _, false);

            // Try adding 5 more
            bool success = InventoryService.CheckAddItem(null, _inventoryManager, 
                _testItem, 5, true, out int leftOver, false);

            Assert.IsFalse(success);
            Assert.AreEqual(5, leftOver);
        }

        [Test]
        public void TryAdd_NonStackable_Splits()
        {
            // Add 2 non-stackable items
            bool success = InventoryService.CheckAddItem(null, _inventoryManager, 
                _testItemNonStackable, 2, true, out int leftOver, false);

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
            bool canAdd = InventoryService.CheckAddItem(null, _inventoryManager, 
                _testItem, 15, false, out int leftOver, false);

            Assert.IsTrue(canAdd);
            Assert.AreEqual(0, leftOver);

            // Ensure inventory is STILL empty
            Assert.IsTrue(_inventoryManager.Slots[0].IsEmpty);
        }

        [Test]
        public void GetQuantity_MultipleSlots()
        {
            // Add 10 to slot 0, 5 to slot 1
            InventoryService.CheckAddItem(null, _inventoryManager, 
                _testItem, 15, true, out int leftOver, false);

            int total = _inventoryManager.GetQuantity(_testItem);
            Assert.AreEqual(15, total);
        }

        [Test]
        public void HasEnoughQuantity_Checks()
        {
            InventoryService.CheckAddItem(null, _inventoryManager, 
                _testItem, 10, true, out int leftOver, false);

            Assert.IsTrue(_inventoryManager.HasEnoughQuantity(_testItem, 5));
            Assert.IsTrue(_inventoryManager.HasEnoughQuantity(_testItem, 10));
            Assert.IsFalse(_inventoryManager.HasEnoughQuantity(_testItem, 11));
        }
    }
}
