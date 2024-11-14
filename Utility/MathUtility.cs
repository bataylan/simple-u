using UnityEngine;

namespace SimpleU.Utility
{
    public static class MathUtility
    {
        public static int GetArrayLoopMod(int index, int arrayLength)
        {
            int remainder = index % arrayLength;
            return remainder < 0 ? remainder + arrayLength : remainder;
        }
    }
}