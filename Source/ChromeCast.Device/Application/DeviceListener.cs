using ChromeCast.Device.Log.Interfaces;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ChromeCast.Device.Application
{
    public class DeviceListener : IDisposable
    {
        private Socket listener;
        private static readonly ManualResetEvent manualResetEvent = new ManualResetEvent(false);
        private ILogger logger;
        private IPAddress ipAddress;
        private int port;
        private bool Disposed = false;
        private readonly List<DeviceConnection> connections = new();

        public void StartListening(IPAddress ipAddressIn, int portIn, ILogger loggerIn)
        {
            logger = loggerIn;
            ipAddress = ipAddressIn;
            port = portIn;

            DoStartListening();
        }

        private void DoStartListening()
        {
            try
            {
                Disposed = false;

                IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);
                if (localEndPoint != null)
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
                connections.Add(new DeviceConnection(this, handler, logger));
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

        public void SendNewVolume()
        {
            foreach (var connection in connections)
            {
                connection?.SendNewVolume();
            }
        }

        public void StopPlaying()
        {
            foreach (var connection in connections)
            {
                connection?.StopPlaying();
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

        public void Dispose()
        {
            try
            {
                Disposed = true;
                listener.Close();
                listener.Dispose();
                listener = null;
                connections.Clear();
                GC.SuppressFinalize(this);
            }
            catch (Exception ex)
            {
                logger.Log(ex, "DeviceListener.Dispose");
            }
        }
    }
}