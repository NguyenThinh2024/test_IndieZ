using System;
using UnityEngine;
using Object = UnityEngine.Object;

public static class DebugLog
{
    public static void Log(object message)
    {
#if UNITY_EDITOR
        Debug.Log(message);
#endif
    }

    public static void Log(object message, Object context)
    {
#if UNITY_EDITOR
        Debug.Log(message, context);
#endif
    }

    public static void LogWarning(object message)
    {
#if UNITY_EDITOR
        Debug.LogWarning(message);
#endif
    }

    public static void LogWarning(object message, Object context)
    {
#if UNITY_EDITOR
        Debug.LogWarning(message, context);
#endif
    }

    public static void LogError(object message)
    {
#if UNITY_EDITOR
        Debug.LogError(message);
#endif
    }

    public static void LogError(object message, Object context)
    {
#if UNITY_EDITOR
        Debug.LogError(message, context);
#endif
    }

    public static void LogException(Exception exception)
    {
#if UNITY_EDITOR
        Debug.LogException(exception);
#endif
    }

    public static void LogException(Exception exception, Object context)
    {
#if UNITY_EDITOR
        Debug.LogException(exception, context);
#endif
    }

    public static void Assert(bool condition)
    {
#if UNITY_EDITOR
        Debug.Assert(condition);
#endif
    }

    public static void Assert(bool condition, object message)
    {
#if UNITY_EDITOR
        Debug.Assert(condition, message);
#endif
    }

    public static void Assert(bool condition, object message, Object context)
    {
#if UNITY_EDITOR
        Debug.Assert(condition, message, context);
#endif
    }
}
