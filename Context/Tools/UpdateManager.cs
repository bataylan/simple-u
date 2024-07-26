using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SimpleU.Context
{
    public class UpdateManager : IDisposable
    {
        public static UpdateManager Get(bool levelScope = true)
        {
            if (levelScope)
            {
                return LevelContext.Instance.UpdateManager;
            }
            else
            {
                return GameContext.Instance.UpdateManager;
            }
        }

        private List<UpdateAction> _updateActions;
        private bool _enabled;
        private int _id;

        public UpdateManager()
        {
            _updateActions = new List<UpdateAction>();
        }

        public UpdateAction AddFinish(Action<bool, object> onFinish,
            float duration, object data = null, Action<float, object> onUpdate = null)
        {
            return AddAction(duration, data, onFinish, onUpdate);
        }

        public UpdateAction AddUpdate(Action<float, object> onUpdate,
            float duration = -1, object data = null,
            Action<bool, object> onFinish = null)
        {
            return AddAction(duration, data, onFinish, onUpdate);
        }

        private UpdateAction AddAction(float duration, object data,
            Action<bool, object> onFinish, Action<float, object> onUpdate)
        {
            if (onFinish == null && onUpdate == null)
                return default;

            var updateAction = new UpdateAction
            {
                id = GetUpdateActionId(),
                data = data,
                startTime = Time.time,
                duration = duration,
                onFinish = onFinish,
                onUpdate = onUpdate,
                stopAction = Stop
            };

            _updateActions.Add(updateAction);

            if (!_enabled)
                _enabled = true;

            return updateAction;
        }

        private int GetUpdateActionId()
        {
            int id = _id;
            IncreaseActionId();
            return id;
        }

        private void IncreaseActionId()
        {
            _id++;
            if (_id < int.MaxValue)
            {
                _id++;
            }
            else
            {
                if (_updateActions.Count > 0)
                {
                    _id = _updateActions[_updateActions.Count - 1].id + 1;
                }
                else
                {
                    _id = 0;
                }
            }
        }

        internal void Update()
        {
            if (!_enabled)
                return;

            var actions = _updateActions;
            var toRemoveUpdateActions = new List<UpdateAction>();

            foreach (var current in _updateActions.ToList())
            {
                if (current.Update())
                {
                    toRemoveUpdateActions.Add(current);
                }
            }

            for (int i = 0; i < toRemoveUpdateActions.Count; i++)
            {
                var toRemove = toRemoveUpdateActions[i];
                toRemove.onFinish?.Invoke(true, toRemove.data);
                _updateActions.Remove(toRemove);
            }

            if (_updateActions.Count <= 0)
                _enabled = false;
        }

        private void Stop(UpdateAction updateAction)
        {
            Debug.Log("Stop");
            updateAction.onFinish?.Invoke(false, updateAction.data);
            _updateActions.Remove(updateAction);

            if (_updateActions.Count <= 0)
                _enabled = false;
        }

        private void StopAllImmediate()
        {
            for (int i = 0; i < _updateActions.Count;)
            {
                var updateAction = _updateActions[i];
                _updateActions.Remove(updateAction);
            }

            _enabled = false;
        }

        public void Dispose()
        {
            StopAllImmediate();
        }

        public struct UpdateAction : IEquatable<UpdateAction>
        {
            public int id;
            public object data;
            public Action<bool, object> onFinish;
            public Action<float, object> onUpdate;
            public float duration;
            public float startTime;
            public Action<UpdateAction> stopAction;

            //Return is completed
            public bool Update()
            {
                bool finished = false;
                float progress = Time.time - startTime;
                if (duration >= 0)
                {
                    progress = duration == 0 ? 1 : Mathf.Clamp((Time.time - startTime) / duration, 0, 1);
                    finished = progress >= 1;
                }

                onUpdate?.Invoke(progress, data);
                return finished;
            }

            public void Stop()
            {
                stopAction?.Invoke(this);
            }

            public bool Equals(UpdateAction other)
            {
                return id == other.id;
            }

            public override int GetHashCode()
            {
                return id;
            }
        }
    }
}
