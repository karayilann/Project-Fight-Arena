using System.Diagnostics;

namespace Utilities
{
    /// <summary>
    /// For better build performance, this Debug class wraps UnityEngine.Debug class.
    /// </summary>
    public static class Debug 
    {
        [Conditional("UNITY_EDITOR")]
        public static void Log(object log)
        {
            UnityEngine.Debug.Log(log);
        }

        [Conditional("UNITY_EDITOR")]
        public static void LogWarning(object log)
        {
            UnityEngine.Debug.LogWarning(log);
        }

        [Conditional("UNITY_EDITOR")]
        public static void LogError(object log)
        {
            UnityEngine.Debug.LogError(log);
        }
    }
}