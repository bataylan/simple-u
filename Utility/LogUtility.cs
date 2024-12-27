using UnityEngine;

namespace SimpleU.Logger
{
    public static class LogUtility
    {
        public static string GetColoredMessage(string message,
            Color color)
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>" + message + "</color>";
        }
    }

    public enum LogLevel
    {
        Default = 0,
        Development = 1,
        Debug = 2
    }
}
