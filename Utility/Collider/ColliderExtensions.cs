using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleU.Extensions.Colliders
{
    public static class ColliderExtensions
    {
        public static bool TryGetComponentExtended<T>(this GameObject gameObject, out T component)
        {
            if (gameObject.TryGetComponent(out component))
                return true;

            if (gameObject.TryGetComponent(out ColliderReference refComponent))
            {
                return refComponent.TryGetComponentSafe(out component);
            }

            return false;
        }

        public static bool TryGetComponentExtended<T>(this Collider collider, out T component)
        {
            if (collider.attachedRigidbody && collider.attachedRigidbody.TryGetComponent(out component))
            {
                return true;
            }
            else
            {
                return collider.gameObject.TryGetComponentExtended(out component);
            }
        }
    }
}
