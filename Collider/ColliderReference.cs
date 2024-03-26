using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleU.Extensions.Colliders
{
    public class ColliderReference : MonoBehaviour
    {
        private Transform _refTr;

        public void Init(Transform refTr)
        {
            _refTr = refTr;
        }

        public T GetComponentSafe<T>()
        {
            if (!_refTr)
                return default(T);

            return _refTr.GetComponent<T>();
        }
    }
}
