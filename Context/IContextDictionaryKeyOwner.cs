using System.Collections.Generic;
using UnityEngine;

namespace SimpleU.Context
{
    public interface IContextDictionaryKeyOwner
    {
        public abstract string Key { get; }
    }
}
