using System.Collections.Generic;
using UnityEngine;

namespace SimpleU.Context
{
    public abstract class ExtraScriptableObject : ScriptableObject
    {
        public abstract string Key { get; }
    }
}
