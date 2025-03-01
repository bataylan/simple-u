using System;
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

        public Action<LevelStatus> onStatusChange;
        public Action<bool> onLevelFinish;

        public void StartLevel()
        {
            Status = LevelStatus.Start;
        }

        protected virtual void FinishLevel(bool success)
        {
            Status = LevelStatus.Finish;
            var temp = onLevelFinish;
            onLevelFinish = null;
            temp?.Invoke(success);
        }
    }

    public enum LevelStatus
    {
        Prepare,
        Start,
        Finish
    }
}
