using System;
using UnityEngine;

namespace SimpleU.Context
{
    public class GameContextReferenceBehaviour : ContextReferenceBehaviour<GameContext>
    {
        public Action TriggerOnDestroy { get; set; }
        
        void OnApplicationQuit()
        {
            var temp = TriggerOnDestroy;
            TriggerOnDestroy = null;
            temp.Invoke();
        }
    }
}