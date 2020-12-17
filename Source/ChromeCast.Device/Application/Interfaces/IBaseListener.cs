using ChromeCast.Device.Log.Interfaces;
using System;
using System.Net;
using System.Net.Sockets;

namespace ChromeCast.Device.Application.Interfaces
{
    public interface IBaseListener
    {
        void StartListening(IPAddress ipAddress, int portIn, Action<Socket, string> onConnectCallbackIn, ILogger logger);
        void StopListening();
        void Dispose();
    }
}
