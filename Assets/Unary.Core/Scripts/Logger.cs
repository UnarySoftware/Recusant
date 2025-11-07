using UnityEngine;
using System;

namespace Unary.Core
{
    public class Logger : CoreSystem<Logger>
    {
        public enum LogType
        {
            Log,
            Warning,
            Error
        };

        public struct LogEventData
        {
            public LogType Type;
            public string Message;
            public string StackTrace;
        }

        public EventFunc<LogEventData> OnLog { get; } = new();

        public int LogCount { get; private set; } = 0;
        public int WarningCount { get; private set; } = 0;
        public int ErrorCount { get; private set; } = 0;

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
                        Instance.LogCount++;

                        Instance.OnLog.Publish(new()
                        {
                            Type = LogType.Log,
                            Message = Message,
                            StackTrace = StackTrace,
                        });
                        break;
                    }
                case UnityEngine.LogType.Warning:
                    {
                        Instance.WarningCount++;

                        Instance.OnLog.Publish(new()
                        {
                            Type = LogType.Warning,
                            Message = Message,
                            StackTrace = StackTrace,
                        });
                        break;
                    }
                case UnityEngine.LogType.Error:
                case UnityEngine.LogType.Assert:
                case UnityEngine.LogType.Exception:
                    {
                        Instance.ErrorCount++;

                        Instance.OnLog.Publish(new()
                        {
                            Type = LogType.Error,
                            Message = Message,
                            StackTrace = StackTrace,
                        });
                        break;
                    }
            }
        }

        private void OnCleanupStaticState()
        {
            Application.logMessageReceivedThreaded -= LogCallback;
            Bootstrap.Instance.OnCleanupStaticState -= OnCleanupStaticState;
        }

        public override bool Initialize()
        {
            Bootstrap.Instance.OnCleanupStaticState += OnCleanupStaticState;
            Application.logMessageReceivedThreaded += LogCallback;
            return true;
        }

        public override void PostInitialize()
        {

        }

        public override void Deinitialize()
        {
            OnCleanupStaticState();
        }

        private Color _logColor = Color.white;
        private Color _warningColor = Color.yellow;
        private Color _errorColor = Color.red;

        public void Log(string message)
        {
            Debug.Log(message);
        }

        public void Warning(string warning)
        {
            Debug.LogWarning(warning);
        }

        public void Error(string error)
        {
            Debug.LogError(error);
        }
    }
}
