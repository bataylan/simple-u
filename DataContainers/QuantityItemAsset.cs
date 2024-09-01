using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleU.DataContainer
{
    [Serializable]
    public struct QuantityItemAsset
    {
        public bool Viable => itemAsset;

        public ItemAsset itemAsset;
        public int quantity;
    }
}
