using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleU.DataContainer
{
    public class ItemAsset : ScriptableObject, IItemAsset
    {
        public virtual bool IsStackable => true;
    }

    public interface IItemAsset
    {
        public bool IsStackable { get; }
    }
}
