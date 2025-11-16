using System;
using UnityEngine;

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
            public Color Color;
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
                            Color = _logColor
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
                            Color = _warningColor
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
                            Color = _errorColor
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

        public static readonly Color LogDefault = Color.white;
        private static Color _logColor = Color.white;

        public static readonly Color WarningDefault = Color.yellow;
        private static Color _warningColor = Color.yellow;

        public static readonly Color ErrorDefault = Color.red;
        private static Color _errorColor = Color.red;

        public void Log(string message)
        {
            _logColor = LogDefault;
            Debug.Log(message);
        }

        public void Log(string message, Color logColor)
        {
            _logColor = logColor;
            Debug.Log(message);
        }

        public void Warning(string warning)
        {
            _warningColor = WarningDefault;
            Debug.LogWarning(warning);
        }

        public void Warning(string warning, Color warningColor)
        {
            _warningColor = warningColor;
            Debug.LogWarning(warning);
        }

        public void Error(string error)
        {
            _errorColor = ErrorDefault;
            Debug.LogError(error);
        }

        public void Error(string error, Color errorColor)
        {
            _errorColor = errorColor;
            Debug.LogError(error);
        }

        public void Error(Exception exception)
        {
            _errorColor = ErrorDefault;
            Debug.LogException(exception);
        }

        public void Error(Exception exception, Color errorColor)
        {
            _errorColor = errorColor;
            Debug.LogException(exception);
        }
    }
}
