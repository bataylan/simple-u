using System.Collections.Generic;
using UnityEngine;

namespace SimpleU.Context
{
    public interface IExtraScriptableObject
    {
        public abstract string Key { get; }

        public void OnSet();
    }
}
