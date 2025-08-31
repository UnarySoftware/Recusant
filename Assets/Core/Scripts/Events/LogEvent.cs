
namespace Core
{
    public sealed class LogEvent : BaseEvent<LogEvent>
    {
        public Logger.LogType Type;
        public string Message;
        public string StackTrace;
        public Logger.LogReciever Reciever;

        public void Publish(Logger.LogType type, string message, string stackTrace, Logger.LogReciever reciever)
        {
            Type = type;
            Message = message;
            StackTrace = stackTrace;
            Reciever = reciever;
            Publish();
        }
    }
}
