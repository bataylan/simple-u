using UnityEngine;

namespace SimpleU.Logger
{
    public static class LogUtility
    {
        public static bool isLogLevelSet = false;
        public static LogLevel logLevel = LogLevel.Default;

        public static void Log(this object instance, string message, 
            Color color = default, LogLevel logLevel = LogLevel.Default)
        {
            Log(instance.GetType().Name + ":" + GetColoredMessage(message, color));
        }

        public static void Log(string message, 
            Color color = default, LogLevel logLevel = LogLevel.Default)
        {
            Log(GetColoredMessage(message, color));
        }

        public static void LogError(this object instance, string message, 
            Color color = default, LogLevel logLevel = LogLevel.Default)
        {
            Log(instance.GetType().Name + ":" + GetColoredMessage(message, color));
        }

        public static void LogError(string message, 
            Color color = default, LogLevel logLevel = LogLevel.Default)
        {
            LogError(GetColoredMessage(message, color));
        }

        public static string GetColoredMessage(string message,
            Color color = default)
        {
            if (color == default)
                return message;
            else
                return $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>" + message + "</color>";
        }

        public static void Log(string message)
        {
            Debug.Log(message);
        }

        public static void LogError(string message)
        {
            Debug.LogError(message);
        }
    }

    public enum LogLevel
    {
        Default = 0,
        Development = 1,
        Debug = 2
    }
}
