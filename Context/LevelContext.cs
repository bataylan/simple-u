using UnityEngine;
using UnityEngine.Events;

namespace SimpleU.Context
{
    [DefaultExecutionOrder(-99)]
    public class LevelContext : ABaseContext
    {
        public static LevelContext Get() => GameContext.Instance.LevelContext;

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
