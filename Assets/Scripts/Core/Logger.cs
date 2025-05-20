using UnityEngine;
using System;
using System.Collections.Generic;

namespace Core
{
    public class Logger : CoreSystem<Logger>
    {
        public enum LogType
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
            public LogType Type;
            public LogReciever Reciever;
        }

        private static bool _collectingMessages = true;
        private static readonly List<CollectedMessage> _messages = new();

        public void StartCollecting()
        {
            _collectingMessages = true;
            _messages.Clear();
        }

        public IReadOnlyList<CollectedMessage> StopCollecting()
        {
            _collectingMessages = false;
            return _messages;
        }

        private static bool CustomTrace = false;
        private static string CustomTraceMessage = string.Empty;

        public static LogReciever CurrentReciever { get; private set; } = LogReciever.Both;

        public void SetCustomTrace(string stackTrace)
        {
            CustomTrace = true;
            CustomTraceMessage = stackTrace;
        }

        private static void LogCallback(string Message, string StackTrace, UnityEngine.LogType Type)
        {
            if (Instance == null)
            {
                return;
            }

            switch (Type)
            {
                default:
                case UnityEngine.LogType.Log:
                    {
                        if (CustomTrace)
                        {
                            LogEvent.Instance.Publish(LogType.Log, Message, CustomTraceMessage, CurrentReciever);
                            CustomTrace = false;
                            break;
                        }

                        LogEvent.Instance.Publish(LogType.Log, Message, StackTrace, CurrentReciever);

                        if (_collectingMessages)
                        {
                            _messages.Add(new CollectedMessage()
                            {
                                Message = Message,
                                StackTrace = StackTrace,
                                Type = LogType.Log,
                                Reciever = CurrentReciever
                            });
                        }

                        break;
                    }
                case UnityEngine.LogType.Warning:
                    {
                        if (CustomTrace)
                        {
                            LogEvent.Instance.Publish(LogType.Warning, Message, CustomTraceMessage, CurrentReciever);
                            CustomTrace = false;
                            break;
                        }

                        LogEvent.Instance.Publish(LogType.Warning, Message, StackTrace, CurrentReciever);

                        if (_collectingMessages)
                        {
                            _messages.Add(new CollectedMessage()
                            {
                                Message = Message,
                                StackTrace = StackTrace,
                                Type = LogType.Warning,
                                Reciever = CurrentReciever
                            });
                        }

                        break;
                    }
                case UnityEngine.LogType.Error:
                case UnityEngine.LogType.Assert:
                case UnityEngine.LogType.Exception:
                    {
                        if (CustomTrace)
                        {
                            LogEvent.Instance.Publish(LogType.Error, Message, CustomTraceMessage, CurrentReciever);
                            CustomTrace = false;
                            break;
                        }

                        LogEvent.Instance.Publish(LogType.Error, Message, StackTrace, CurrentReciever);

                        if (_collectingMessages)
                        {
                            _messages.Add(new CollectedMessage()
                            {
                                Message = Message,
                                StackTrace = StackTrace,
                                Type = LogType.Error,
                                Reciever = CurrentReciever
                            });
                        }

                        break;
                    }
            }

            CurrentReciever = LogReciever.Both;
        }

        public override bool Initialize()
        {
            Application.logMessageReceived += LogCallback;
            return true;
        }

        public override void PostInitialize()
        {
            _collectingMessages = false;

            foreach (var message in _messages)
            {
                switch (message.Type)
                {
                    default:
                    case LogType.Log:
                        {
                            CustomTrace = true;
                            LogCallback(message.Message, message.StackTrace, UnityEngine.LogType.Log);
                            break;
                        }
                    case LogType.Warning:
                        {
                            CustomTrace = true;
                            LogCallback(message.Message, message.StackTrace, UnityEngine.LogType.Warning);
                            break;
                        }
                    case LogType.Error:
                        {
                            CustomTrace = true;
                            LogCallback(message.Message, message.StackTrace, UnityEngine.LogType.Error);
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

        public void Log(string message, LogReciever reciever = LogReciever.Both)
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

        public void Warning(string warning, LogReciever reciever = LogReciever.Both)
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

        public void Error(string error, LogReciever reciever = LogReciever.Both)
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
}
