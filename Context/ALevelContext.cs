using UnityEngine;

namespace SimpleU.Context
{
    [DefaultExecutionOrder(-99)]
    public abstract class ALevelContext<T> : LevelContext where T : LevelContext
    {
        public static new T Instance
        {
            get
            {
                var levelContext = GameContext.Instance.LevelContext;
                if (levelContext == null)
                    return null;

                return levelContext as T;
            }
        }
    }
}
