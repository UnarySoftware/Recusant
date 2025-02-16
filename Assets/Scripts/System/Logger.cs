using UnityEngine;
using System;
using System.Collections.Generic;

public class Logger : CoreSystem<Logger>
{
    public enum Type
    {
        None,
        Log,
        Warning,
        Error
    };

    public enum LogReciever
    {
        None,
        ServerOnly,
        Both
    }

    public struct CollectedMessage
    {
        public string Message;
        public string StackTrace;
        public Type Type;
        public LogReciever Reciever;
    }

    private static bool _collectingMessages = true;
    private static readonly List<CollectedMessage> _messages = new();

    public void StartCollecting()
    {
        _collectingMessages = true;
        _messages.Clear();
    }

    public IEnumerable<CollectedMessage> StopCollecting()
    {
        _collectingMessages = false;
        return _messages;
    }

    // First - message
    // Second - stack trace
    public Action<string, string, LogReciever> OnLog;
    public Action<string, string, LogReciever> OnWarning;
    public Action<string, string, LogReciever> OnError;

    private static bool CustomTrace = false;
    private static string CustomTraceMessage = string.Empty;

    public static LogReciever CurrentReciever { get; private set; } = LogReciever.Both;

    public void SetCustomTrace(string stackTrace)
    {
        CustomTrace = true;
        CustomTraceMessage = stackTrace;
    }

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
                    if (CustomTrace)
                    {
                        Instance.OnLog?.Invoke(Message, CustomTraceMessage, CurrentReciever);
                        CustomTrace = false;
                        break;
                    }

                    Instance.OnLog?.Invoke(Message, StackTrace, CurrentReciever);

                    if (_collectingMessages)
                    {
                        _messages.Add(new CollectedMessage()
                        {
                            Message = Message,
                            StackTrace = StackTrace,
                            Type = Logger.Type.Log,
                            Reciever = CurrentReciever
                        });
                    }

                    break;
                }
            case LogType.Warning:
                {
                    if (CustomTrace)
                    {
                        Instance.OnWarning?.Invoke(Message, CustomTraceMessage, CurrentReciever);
                        CustomTrace = false;
                        break;
                    }

                    Instance.OnWarning?.Invoke(Message, StackTrace, CurrentReciever);

                    if (_collectingMessages)
                    {
                        _messages.Add(new CollectedMessage()
                        {
                            Message = Message,
                            StackTrace = StackTrace,
                            Type = Logger.Type.Warning,
                            Reciever = CurrentReciever
                        });
                    }

                    break;
                }
            case LogType.Error:
            case LogType.Assert:
            case LogType.Exception:
                {
                    if (CustomTrace)
                    {
                        Instance.OnError?.Invoke(Message, CustomTraceMessage, CurrentReciever);
                        CustomTrace = false;
                        break;
                    }

                    Instance.OnError?.Invoke(Message, StackTrace, CurrentReciever);

                    if (_collectingMessages)
                    {
                        _messages.Add(new CollectedMessage()
                        {
                            Message = Message,
                            StackTrace = StackTrace,
                            Type = Logger.Type.Error,
                            Reciever = CurrentReciever
                        });
                    }

                    break;
                }
        }

        CurrentReciever = LogReciever.Both;
    }

    [InitDependency()]
    public override void Initialize()
    {
        Application.logMessageReceived += LogCallback;
    }

    public override void PostInitialize()
    {
        _collectingMessages = false;

        foreach(var message in _messages)
        {
            switch (message.Type)
            {
                default:
                case Type.Log:
                    {
                        LogCallback(message.Message, message.StackTrace, LogType.Log);
                        break;
                    }
                case Type.Warning:
                    {
                        LogCallback(message.Message, message.StackTrace, LogType.Warning);
                        break;
                    }
                case Type.Error:
                    {
                        LogCallback(message.Message, message.StackTrace, LogType.Error);
                        break;
                    }
            }
        }

        _messages.Clear();
    }

    public override void Deinitialize()
    {
        Application.logMessageReceived -= LogCallback;
        _messages.Clear();
    }

    public void Log(object message, LogReciever reciever = LogReciever.Both)
    {
        CurrentReciever = reciever;
        Debug.Log(message);
    }

    public void Log(Exception e, LogReciever reciever = LogReciever.Both)
    {
        CurrentReciever = reciever;
        SetCustomTrace(e.StackTrace);
        Debug.Log(e.Message);
    }

    public void Warning(object warning, LogReciever reciever = LogReciever.Both)
    {
        CurrentReciever = reciever;
        Debug.LogWarning(warning);
    }

    public void Warning(Exception e, LogReciever reciever = LogReciever.Both)
    {
        CurrentReciever = reciever;
        SetCustomTrace(e.StackTrace);
        Debug.LogWarning(e.Message);
    }

    public void Error(object error, LogReciever reciever = LogReciever.Both)
    {
        CurrentReciever = reciever;
        Debug.LogError(error);
    }

    public void Error(Exception e, LogReciever reciever = LogReciever.Both)
    {
        CurrentReciever = reciever;
        SetCustomTrace(e.StackTrace);
        Debug.LogError(e.Message);
    }
}
