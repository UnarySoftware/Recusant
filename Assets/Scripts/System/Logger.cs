using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Logger : MonoBehaviour, ISystem
{
    public static Logger Instance = null;

    // First - message
    // Second - stack trace

    public Action<string, string> OnError;
    public Action<string, string> OnWarning;
    public Action<string, string> OnLog;

    private static void LogCallback(string Message, string StackTrace, LogType Type)
    {
        if (Instance == null)
        {
            return;
        }

        switch (Type)
        {
            default:
            case LogType.Log:
                {
                    Instance.OnLog(Message, StackTrace);
                    break;
                }
            case LogType.Warning:
                {
                    Instance.OnWarning(Message, StackTrace);
                    break;
                }
            case LogType.Error:
            case LogType.Assert:
            case LogType.Exception:
                {
                    Instance.OnError(Message, StackTrace);
                    break;
                }
        }
    }

    [InitDependency()]
    public void Initialize()
    {
        Application.logMessageReceived += LogCallback;
    }

    public void Deinitialize()
    {
        Application.logMessageReceived -= LogCallback;
    }

    public void Log(object Message)
    {
        Debug.Log(Message);
    }

    public void Warning(object Warning)
    {
        Debug.LogWarning(Warning);
    }

    public void Error(object Error)
    {
        Debug.LogError(Error);
    }

}
