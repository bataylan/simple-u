using System;
using System.Collections.Generic;
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
                if (value <= _status)
                    return;

                var previousStatus = _status;
                _status = value;
                for (int i = (int)previousStatus + 1; i <= (int)value && onStatusChangeCallbacks != null; i++)
                {
                    var callbacks = onStatusChangeCallbacks[i];
                    onStatusChangeCallbacks[i] = null;
                    callbacks?.Invoke();
                }
            }
        }
        private LevelStatus _status = LevelStatus.Prepare;

        public Action<bool> onLevelFinish;

        public Action[] onStatusChangeCallbacks;

        internal override void EnsureInit(GameObject referenceObject, ScriptableObject[] extraScriptableObjects, GameObject[] extraPrefabs)
        {
            base.EnsureInit(referenceObject, extraScriptableObjects, extraPrefabs);
            var statusValues = Enum.GetValues(typeof(LevelStatus));
            onStatusChangeCallbacks = new Action[statusValues.Length];
        }

        public void StartLevel()
        {
            Status = LevelStatus.Start;
        }

        public void NotifyOnState(LevelStatus status, Action statusChangeCallback)
        {
            if (Status >= status)
            {
                statusChangeCallback?.Invoke();
            }
            else
            {
                var callbacks = onStatusChangeCallbacks[(int)status]; 
                if (callbacks == null)
                {
                    onStatusChangeCallbacks[(int)status] = statusChangeCallback;
                }
                else
                {
                    callbacks += statusChangeCallback;
                    onStatusChangeCallbacks[(int)status] = callbacks;
                }
            }
        }

        public void CancelNotifyOnState(LevelStatus status, Action statusChangeCallback)
        {
            if (onStatusChangeCallbacks != null)
                onStatusChangeCallbacks[(int)status] -= statusChangeCallback;
        }

        protected virtual void FinishLevel(bool success)
        {
            Status = LevelStatus.Finish;
            var temp = onLevelFinish;
            onLevelFinish = null;
            temp?.Invoke(success);
        }

        public void Dispose()
        {
            if (_status == LevelStatus.Unload)
                return;

            // Pending startup callbacks belong to the scene being unloaded.
            _status = LevelStatus.Unload;
            var unloadCallbacks = onStatusChangeCallbacks[(int)LevelStatus.Unload];
            onStatusChangeCallbacks = null;
            unloadCallbacks?.Invoke();
        }

        public void SetStateSpawn()
        {
            Status = LevelStatus.Spawn;
        }

        public void SetStateAfterSpawn()
        {
            Status = LevelStatus.PostSpawn;
        }
    }

    public enum LevelStatus
    {
        Prepare,
        Spawn,
        PostSpawn,
        Start,
        Finish,
        Unload
    }
}
