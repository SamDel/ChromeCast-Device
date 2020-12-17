using System.Net.Sockets;
using System.Text;

namespace ChromeCast.Device.Application
{
    public class StateObject
    {
        public Socket workSocket = null;
        public const int bufferSize = 2048;
        public byte[] buffer;
        public StringBuilder receiveBuffer = new StringBuilder();
    }
}
