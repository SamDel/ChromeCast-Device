using System;

namespace ChromeCast.Device.Log.Interfaces
{
    public interface ILogger
    {
        void Log(string message);
        void Log(Exception ex, string message = null);
        void SetCallback(Action<string> logCallbackIn);
    }
}