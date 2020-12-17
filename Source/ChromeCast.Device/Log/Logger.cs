using System;
using ChromeCast.Device.Log.Interfaces;

namespace ChromeCast.Device.Log
{
    public class Logger : ILogger
    {
        private Action<string> logCallback;
        private readonly bool doLog;

        public Logger(bool log)
        {
            doLog = log;
        }

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="message">the message to log</param>
        public void Log(string message)
        {
            if (doLog)
                logCallback?.Invoke(message);
        }

        /// <summary>
        /// Log an exception.
        /// </summary>
        /// <param name="ex">the exception to log</param>
        public void Log(Exception ex, string message = null)
        {
            if (doLog)
                logCallback?.Invoke($"ex : [{message}] {ex.Message}");
        }

        /// <summary>
        /// Set the callback that does the actual logging.
        /// </summary>
        public void SetCallback(Action<string> logCallbackIn)
        {
            logCallback = logCallbackIn;
        }
    }
}