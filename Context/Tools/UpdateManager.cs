using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SimpleU.Context
{
    public class UpdateManager
    {
        public static UpdateManager Get(bool isSourceLevelContext = true)
        {
            if (isSourceLevelContext)
            {
                return LevelContext.Get().UpdateManager;
            }
            else
            {
                return GameContext.Instance.UpdateManager;
            }
        }

        private List<UpdateAction> _updateActions;
        private bool _enabled;

        public UpdateManager()
        {
            _updateActions = new List<UpdateAction>();
        }

        public UpdateAction AddFinish(UpdateAction.ActionFinish onFinish,
            float duration, object data = null, UpdateAction.ActionUpdate onUpdate = null)
        {
            return AddAction(duration, data, onFinish, onUpdate);
        }

        public UpdateAction AddUpdate(UpdateAction.ActionUpdate onUpdate,
            float duration = -1, object data = null,
            UpdateAction.ActionFinish onFinish = null)
        {
            return AddAction(duration, data, onFinish, onUpdate);
        }

        private UpdateAction AddAction(float duration, object data,
            UpdateAction.ActionFinish onFinish, UpdateAction.ActionUpdate onUpdate)
        {
            if (onFinish == null && onUpdate == null)
                return default;

            var updateAction = new UpdateAction(data, duration, Time.time,
                onFinish, onUpdate, Stop);

            _updateActions.Add(updateAction);

            if (!_enabled)
                _enabled = true;

            return updateAction;
        }

        internal void Update()
        {
            if (!_enabled)
                return;

            var toRemoveUpdateActions = new List<UpdateAction>();
            var enumerator = _updateActions.ToList().GetEnumerator();

            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;
                if (!current.IsActive || current.Update())
                {
                    toRemoveUpdateActions.Add(current);
                }
            }

            for (int i = 0; i < toRemoveUpdateActions.Count; i++)
            {
                //action might be removed from list from anywhere by triggering Stop function
                if (_updateActions.Contains(toRemoveUpdateActions[i]))
                {
                    _updateActions.Remove(toRemoveUpdateActions[i]);

                    if (toRemoveUpdateActions[i].IsActive)
                        toRemoveUpdateActions[i].Finish(true);
                }
            }

            if (_updateActions.Count <= 0)
                _enabled = false;
        }

        private void Stop(UpdateAction updateAction)
        {
            if (!_updateActions.Contains(updateAction))
            {
                return;
            }

            _updateActions.Remove(updateAction);
            updateAction.Finish(false);

            if (_updateActions.Count <= 0)
                _enabled = false;
        }

        public void StopAllImmediate()
        {
            for (int i = 0; i < _updateActions.Count;)
            {
                var updateAction = _updateActions[i];
                _updateActions.Remove(updateAction);
            }

            _enabled = false;
        }

        public class UpdateAction
        {
            private object _data;
            private ActionUpdate _onUpdate;
            private ActionFinish _onFinish;
            private float _duration;
            private float _startTime;

            internal Action<UpdateAction> stopAction;

            public bool IsActive { get; private set; }
            public delegate void ActionFinish(bool complete, object data);
            public delegate void ActionUpdate(float progress, object data);

            internal UpdateAction(object data, float duration, float startTime,
                ActionFinish onFinish, ActionUpdate onUpdate,
                Action<UpdateAction> stopAction)
            {
                _data = data;
                _duration = duration;
                _startTime = startTime;
                _onFinish = onFinish;
                _onUpdate = onUpdate;
                this.stopAction = stopAction;
                IsActive = true;
            }

            /// <summary>
            /// Update action used by UpdateManager
            /// </summary>
            /// <returns>Is update action completed</returns>
            internal bool Update()
            {
                if (_onUpdate == null && _onFinish == null)
                {
                    Debug.LogError("OnUpdate & OnFinish null.");
                    return true;
                }

                bool finished = false;
                float progress = Time.time - _startTime;

                if (_duration >= 0)
                {
                    progress = _duration == 0 ? 1 : Mathf.Clamp((Time.time - _startTime) / _duration, 0, 1);
                    finished = progress >= 1;
                }

                _onUpdate?.Invoke(progress, _data);
                return finished;
            }

            //used by update manager
            internal void Finish(bool success)
            {
                var tempData = _data;
                var tempFinish = _onFinish;
                Reset();
                tempFinish?.Invoke(success, _data);
                IsActive = false;
            }

            //used by owner
            public void Stop()
            {
                var tempStopAction = stopAction;
                Reset();
                tempStopAction?.Invoke(this);
                IsActive = false;
            }

            private void Reset()
            {
                stopAction = default;
                _onFinish = default;
                _onUpdate = default;
                _data = default;
                _duration = default;
                _startTime = default;
            }
        }
    }
}
