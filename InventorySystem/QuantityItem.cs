using System;
using System.Collections;
using System.Collections.Generic;
using SimpleU.DataContainer;
using UnityEngine;

namespace SimpleU.Inventory
{
    public interface IQuantityItem
    {
        bool IsValid { get; }
        IItemAsset ItemAsset { get; }
        int Quantity { get; }
        object ExtraData { get; set; }

        void SetQuantity(int quantity);
    }

    [Serializable]
    public struct QuantityItem : IQuantityItem
    {
        public IItemAsset itemAsset;
        public int quantity;

        public IItemAsset ItemAsset => itemAsset;
        public int Quantity => quantity;
        public bool IsValid => itemAsset != null && quantity > 0;
        public object ExtraData { get; set; }

        public void SetQuantity(int quantity)
        {
            this.quantity = quantity;
        }
    }
}
