using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SimpleU.Context
{
    public class UpdateManager : IDisposable
    {
        public static UpdateManager Get(bool isSourceLevelContext = true)
        {
            if (isSourceLevelContext)
            {
                return LevelContext.Instance.UpdateManager;
            }
            else
            {
                return GameContext.Instance.UpdateManager;
            }
        }

        private Dictionary<int, UpdateAction> _updateActions;
        private bool _enabled;
        private int _id;

        public UpdateManager()
        {
            _updateActions = new Dictionary<int, UpdateAction>();
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

            var updateAction = new UpdateAction(GetUpdateActionId(), data, duration, Time.time,
                onFinish, onUpdate, Stop);

            _updateActions[updateAction.id] = updateAction;

            if (!_enabled)
                _enabled = true;

            return updateAction;
        }

        private int GetUpdateActionId()
        {
            _id++;

            if (_id > int.MaxValue)
                _id = 0;

            return _id;
        }

        internal void Update()
        {
            if (!_enabled)
                return;

            var toRemoveUpdateActions = new List<UpdateAction>();
            var enumerator = _updateActions.Values.ToList().GetEnumerator();

            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;
                if (current.Update())
                {
                    toRemoveUpdateActions.Add(current);
                }
            }

            for (int i = 0; i < toRemoveUpdateActions.Count; i++)
            {
                //action might be removed from list from anywhere by triggering Stop function
                if (_updateActions.ContainsKey(toRemoveUpdateActions[i].id))
                {
                    _updateActions.Remove(toRemoveUpdateActions[i].id);
                    toRemoveUpdateActions[i].Finish(true);
                }
            }

            if (_updateActions.Count <= 0)
                _enabled = false;
        }

        private void Stop(UpdateAction updateAction)
        {
            if (!_updateActions.ContainsKey(updateAction.id))
            {
                return;
            }

            _updateActions.Remove(updateAction.id);
            updateAction.Finish(false);

            if (_updateActions.Count <= 0)
                _enabled = false;
        }

        private void StopAllImmediate()
        {
            for (int i = 0; i < _updateActions.Count;)
            {
                var updateAction = _updateActions[i];
                _updateActions.Remove(updateAction.id);
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
            private object data;
            public ActionUpdate onUpdate;
            public ActionFinish onFinish;
            private float duration;
            private float startTime;
            internal Action<UpdateAction> stopAction;

            public delegate void ActionFinish(bool complete, object data);
            public delegate void ActionUpdate(float progress, object data);

            internal UpdateAction(int id, object data, float duration, float startTime,
                ActionFinish onFinish, ActionUpdate onUpdate,
                Action<UpdateAction> stopAction)
            {
                this.id = id;
                this.data = data;
                this.duration = duration;
                this.startTime = startTime;
                this.onFinish = onFinish;
                this.onUpdate = onUpdate;
                this.stopAction = stopAction;
            }

            /// <summary>
            /// Update action from UpdateManager
            /// </summary>
            /// <returns>Is update action completed</returns>
            internal bool Update()
            {
                if (onUpdate == null && onFinish == null)
                {
                    Debug.LogError("OnUpdate & OnFinish null. ID: " + id);
                    return true;
                }
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

            public UpdateAction Stop()
            {
                var tempStopAction = stopAction;
                stopAction = null;
                onFinish = null;
                onUpdate = null;
                tempStopAction?.Invoke(this);
                return default;
            }

            internal void Finish(bool success)
            {
                var tempData = data;
                var tempFinish = onFinish;
                stopAction = null;
                onFinish = null;
                onUpdate = null;
                tempFinish?.Invoke(success, data);
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
