using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SimpleU.Context
{
    [DefaultExecutionOrder(-99)]
    public class LevelContext : MonoBehaviour
    {
        public ContextDictionary ExtraData
        {
            get
            {
                if (_extraData == null)
                    _extraData = new ContextDictionary();

                return _extraData;
            }
        }
        private ContextDictionary _extraData;

        public LevelStatus Status
        {
            get
            {
                return _status;
            }
            protected set
            {
                if (value == _status)
                    return;

                _status = value;
                onStatusChange.Invoke(_status);
            }
        }
        private LevelStatus _status = LevelStatus.Prepare;

        public UnityEvent<LevelStatus> onStatusChange = new UnityEvent<LevelStatus>();

        public enum LevelStatus
        {
            Prepare,
            Start,
            Finish
        }

        public static T GetInstance<T>() where T : LevelContext
        {
            return GameContext.Instance as T;
        }
    }
}
