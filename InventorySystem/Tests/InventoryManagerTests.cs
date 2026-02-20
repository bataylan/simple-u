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

        [SetUp]
        public void SetUp()
        {
            _inventoryManager = new AInventoryManager(2, 2);
            _testItem = ScriptableObject.CreateInstance<ItemAsset>();
        }
    }
}
