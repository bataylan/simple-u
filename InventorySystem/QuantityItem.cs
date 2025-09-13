using System;
using System.Collections;
using System.Collections.Generic;
using SimpleU.DataContainer;
using UnityEngine;

namespace SimpleU.Inventory
{
    public interface IQuantityItem<T> : IQuantityItem where T : IItemAsset
    {
        new T ItemAsset { get; }
    }
    
    public interface IQuantityItem
    {
        bool Viable { get; }
        IItemAsset ItemAsset { get; }
        int Quantity { get; }

        void SetQuantity(int quantity);
    }

    [Serializable]
    public struct QuantityItem<T> : IQuantityItem<T> where T : IItemAsset
    {
        public T itemAsset;
        public int quantity;

        public T ItemAsset => itemAsset;
        public int Quantity => quantity;
        public bool Viable => itemAsset != null && quantity > 0;

        IItemAsset IQuantityItem.ItemAsset => ItemAsset;

        public void SetQuantity(int quantity)
        {
            this.quantity = quantity;
        }
    }
}
