using System;
using System.Collections;
using System.Collections.Generic;
using SimpleU.DataContainer;
using UnityEngine;

namespace SimpleU.Inventory
{
    public interface IQuantityItem
    {
        bool Valid { get; }
        IItemAsset ItemAsset { get; }
        int Quantity { get; }

        void SetQuantity(int quantity);
    }

    [Serializable]
    public struct QuantityItem : IQuantityItem
    {
        public IItemAsset itemAsset;
        public int quantity;

        public IItemAsset ItemAsset => itemAsset;
        public int Quantity => quantity;
        public bool Valid => itemAsset != null && quantity > 0;

        public void SetQuantity(int quantity)
        {
            this.quantity = quantity;
        }
    }
}
