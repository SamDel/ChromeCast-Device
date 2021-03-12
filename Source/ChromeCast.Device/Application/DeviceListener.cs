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
    public class DeviceListener : IDisposable
    {
        private Socket listener;
        private SslStream sslStream;
        private StateObjectSsl stateObject;
        private static readonly ManualResetEvent manualResetEvent = new ManualResetEvent(false);
        private Action<CastMessage> onReceiveMessage;
        private ILogger logger;
        private IDeviceReceiveBuffer deviceReceiveBuffer;
        private X509Certificate cert = null;
        private IPAddress ipAddress;
        private int port;
        private bool Disposed = false;

        public void StartListening(IPAddress ipAddressIn, int portIn, Action<CastMessage> onReceiveIn, ILogger loggerIn)
        {
            onReceiveMessage = onReceiveIn;
            logger = loggerIn;
            ipAddress = ipAddressIn;
            port = portIn;

            DoStartListening();
        }

        private void DoStartListening()
        {
            try
            {
                deviceReceiveBuffer = new DeviceReceiveBuffer();
                deviceReceiveBuffer.SetCallback(onReceiveMessage);
                GetCertificate();
                Disposed = false;

                IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);
                if (localEndPoint != null && cert != null)
                {
                    listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                    if (listener != null)
                    {
                        listener.Bind(localEndPoint);
                        listener.Listen(100);
                        listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
                        logger.Log($"Listening {ipAddress}:{port}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log(ex, "DeviceListener.DoStartListening");
            }
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            if (ar == null || ar.AsyncState == null || Disposed)
                return;

            try
            {
                manualResetEvent.Set();

                var handler = listener.EndAccept(ar);
                sslStream = new SslStream(new NetworkStream(handler, true));
                sslStream.AuthenticateAsServer(cert, false, SslProtocols.Tls12, true);

                stateObject = new StateObjectSsl
                {
                    workStream = sslStream
                };

                sslStream.BeginRead(stateObject.buffer, 0, StateObjectSsl.BufferSize, ReceiveCallback, stateObject);
                listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
            }
            catch (Exception ex)
            {
                logger.Log(ex, "DeviceListener.AcceptCallback");
                Dispose();
                Thread.Sleep(1000);
                DoStartListening();
            }
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
                logger.Log(ex, "DeviceListener.ReceiveCallback");
                Dispose();
                Thread.Sleep(1000);
                DoStartListening();
            }
        }

        public void Write(CastMessage message, DeviceState state)
        {
            if (Disposed)
                return;

            logger.Log($"out [{DateTime.Now.ToLongTimeString()}] [{state}] [{ipAddress}:{port}] {message.PayloadUtf8}");
            var byteArray = ChromeCastMessages.MessageToByteArray(message);
            sslStream.BeginWrite(byteArray, 0, byteArray.Length, WriteAsyncCallback, sslStream);
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

        public void StopListening()
        {
            try
            {
                listener.Close();
            }
            catch (Exception)
            {
            }
        }

        private void GetCertificate()
        {
            byte[] pfxData = EmbeddedResource.GetResource("ChromeCast.Device.SamDel4321.pfx");
            cert = new X509Certificate2(pfxData, "SamDel4321", X509KeyStorageFlags.EphemeralKeySet);
        }

        public void Dispose()
        {
            try
            {
                Disposed = true;
                sslStream.Close();
                sslStream.Dispose();
                listener.Close();
                listener.Dispose();
                listener = null;
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