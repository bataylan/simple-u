using UnityEngine;
using UnityEngine.Events;

namespace SimpleU.Context
{
    [DefaultExecutionOrder(-99)]
    public class LevelContext : MonoBehaviour
    {
        public static LevelContext Instance => GameContext.Instance.LevelContext;

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

        public UpdateManager UpdateManager
        {
            get
            {
                if (_updateManager == null)
                    _updateManager = new UpdateManager();

                return _updateManager;
            }
        }
        private UpdateManager _updateManager;

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

                onStatusChange?.Invoke(_status);
            }
        }
        private LevelStatus _status = LevelStatus.Prepare;

        public UnityEvent<LevelStatus> onStatusChange = new UnityEvent<LevelStatus>();
        public UnityEvent<bool> onLevelFinish = new UnityEvent<bool>();

        void Update()
        {
            UpdateManager.Update();
        }

        public static T GetInstance<T>() where T : LevelContext
        {
            return GameContext.Instance.LevelContext as T;
        }

        protected virtual void FinishLevel(bool success)
        {
            Status = LevelStatus.Finish;
            onLevelFinish?.Invoke(success);
            onLevelFinish.RemoveAllListeners();
        }
    }

    public enum LevelStatus
        {
            Prepare,
            Start,
            Finish
        }
}
