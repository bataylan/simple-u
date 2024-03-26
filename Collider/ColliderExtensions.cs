using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleU.Extensions.Colliders
{
    public static class ColliderExtensions
    {
        public static T GetComponentExtended<T>(this Component other)
        {
            var item = other.gameObject.GetComponent<T>();
            if (item != null)
                return item;

            var colRef = other.gameObject.GetComponent<ColliderReference>();
            if (colRef == null)
                return default;

            return colRef.GetComponentSafe<T>();
        }
    }
}
