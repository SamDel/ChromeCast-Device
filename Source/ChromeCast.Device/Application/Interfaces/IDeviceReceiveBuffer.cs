using ChromeCast.Device.ProtocolBuffer;
using System;

namespace ChromeCast.Device.Application.Interfaces
{
    public interface IDeviceReceiveBuffer
    {
        void OnReceive(byte[] data);
        void SetCallback(Action<CastMessage> onReceiveMessage);
    }
}
