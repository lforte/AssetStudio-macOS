using System;
using AssetStudio;

namespace AssetStudioCore
{
    public class AppLogger : ILogger
    {
        private readonly Action<string> action;
        private readonly Action<string> errorAction;

        public AppLogger(Action<string> action, Action<string> errorAction)
        {
            this.action = action;
            this.errorAction = errorAction;
        }

        public void Log(LoggerEvent loggerEvent, string message)
        {
            switch (loggerEvent)
            {
                case LoggerEvent.Error:
                    errorAction(message);
                    break;
                default:
                    action(message);
                    break;
            }
        }
    }
}
