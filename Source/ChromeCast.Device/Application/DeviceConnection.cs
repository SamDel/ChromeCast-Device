using ChromeCast.Device.Application.Interfaces;
using ChromeCast.Device.Classes;
using ChromeCast.Device.Log.Interfaces;
using ChromeCast.Device.ProtocolBuffer;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace ChromeCast.Device.Application
{
    public class DeviceConnection : IDisposable
    {
        private readonly Socket handler;
        private readonly SslStream sslStream;
        private readonly DeviceListener listener;
        private X509Certificate cert = null;
        private readonly bool Disposed = false;
        private readonly ILogger logger;
        private readonly StateObjectSsl stateObject;
        private readonly IDeviceReceiveBuffer deviceReceiveBuffer;
        private readonly DeviceCommunication deviceCommunication;

        public DeviceConnection(DeviceListener listenerIn, Socket handlerIn, ILogger loggerIn)
        {
            listener = listenerIn;
            handler = handlerIn;
            logger = loggerIn;
            GetCertificate();
            deviceReceiveBuffer = new DeviceReceiveBuffer();
            deviceCommunication = new DeviceCommunication(logger);
            deviceReceiveBuffer.SetCallback(OnReceive);
            sslStream = new SslStream(new NetworkStream(handler, true));
            sslStream.AuthenticateAsServer(cert, false, SslProtocols.Tls12, true);
            stateObject = new StateObjectSsl
            {
                workStream = sslStream
            };
            sslStream.BeginRead(stateObject.buffer, 0, StateObjectSsl.BufferSize, ReceiveCallback, stateObject);
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            if (Disposed)
                return;

            try
            {
                var state = (StateObjectSsl)ar.AsyncState;
                var stream = state.workStream;
                var byteCount = stream.EndRead(ar);

                if (byteCount > 0)
                {
                    deviceReceiveBuffer.OnReceive(state.buffer.Take(byteCount).ToArray());
                    sslStream.BeginRead(stateObject.buffer, 0, StateObjectSsl.BufferSize, ReceiveCallback, stateObject);
                }
            }
            catch (Exception ex)
            {
                logger.Log(ex, "DeviceConnection.ReceiveCallback");
                Dispose();
                Thread.Sleep(1000);
            }
        }

        public void Write(CastMessage message, DeviceState state)
        {
            if (Disposed)
                return;

            var endPoint = (IPEndPoint)handler.RemoteEndPoint;
            logger.Log($"out [{DateTime.Now.ToLongTimeString()}] [{state}] [{endPoint.Address}:{endPoint.Port}] {message.PayloadUtf8}");
            var byteArray = ChromeCastMessages.MessageToByteArray(message);
            sslStream.BeginWrite(byteArray, 0, byteArray.Length, WriteAsyncCallback, sslStream);
        }

        public void OnNewLoad()
        {
            listener.StopPlaying();
        }

        public void StopPlaying()
        {
            deviceCommunication.Stop(this);
        }

        public void SendNewVolume()
        {
            deviceCommunication?.SendNewVolume(this);
        }

        private void WriteAsyncCallback(IAsyncResult ar)
        {
            if (Disposed)
                return;

            SslStream sslStream = (SslStream)ar.AsyncState;

            try
            {
                sslStream.EndWrite(ar);
            }
            catch (IOException ex)
            {
                logger.Log(ex, $"DeviceListener.WriteAsyncCallback {ar}");
            }
        }

        private void GetCertificate()
        {
            byte[] pfxData = EmbeddedResource.GetResource("ChromeCast.Device.SamDel4321.pfx");
            cert = new X509Certificate2(pfxData, "SamDel4321", X509KeyStorageFlags.EphemeralKeySet);
        }

        private void OnReceive(CastMessage message)
        {
            if (Disposed)
                return;

            logger.Log($"in [{DateTime.Now.ToLongTimeString()}] [{((IPEndPoint)handler.RemoteEndPoint).Address}:8009]: {message.PayloadUtf8}");
            deviceCommunication?.ProcessMessage(this, message);
        }

        public void Dispose()
        {
            try
            {
                sslStream.Close();
                sslStream.Dispose();
                GC.SuppressFinalize(this);
            }
            catch (Exception ex)
            {
                logger.Log(ex, "DeviceListener.Dispose");
            }
        }
    }

    public class StateObjectSsl
    {
        public SslStream workStream = null;
        public const int BufferSize = 1024;
        public byte[] buffer = new byte[BufferSize];
    }
}
