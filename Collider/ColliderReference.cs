using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleU.Extensions.Colliders
{
    public class ColliderReference : MonoBehaviour, IColliderReference
    {
        [SerializeField] private Transform refTr;

        public void SetOwner(Transform refTr)
        {
            this.refTr = refTr;
        }

        public bool TryGetComponentSafe<T>(out T component)
        {
            component = default;

            if (!refTr)
                return false;

            return refTr.TryGetComponent(out component);
        }
    }

    public interface IColliderReference
    {
        public bool TryGetComponentSafe<T>(out T component);
    }
}
