using ChromeCast.Device.Application.Interfaces;
using ChromeCast.Device.Log.Interfaces;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ChromeCast.Device.Application
{
    public class BaseListener : IBaseListener, IDisposable
    {
        public ManualResetEvent allDone = new ManualResetEvent(false);
        private Action<Socket, string> onConnectCallback;
        private Socket listener;
        private string ip;
        private int port;
        private ILogger logger;


        /// <summary>
        /// Start listening for new streaming connections.
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="onConnectCallbackIn"></param>
        public void StartListening(IPAddress ipAddress, int portIn, Action<Socket, string> onConnectCallbackIn, ILogger loggerIn)
        {
            if (ipAddress == null || onConnectCallbackIn == null)
                return;

            logger = loggerIn;
            onConnectCallback = onConnectCallbackIn;
            port = portIn;

            try
            {
                var localEndPoint = new IPEndPoint(ipAddress, port);
                listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                listener.Bind(localEndPoint);
                listener.Listen(100);
                var endPoint = (IPEndPoint)listener.LocalEndPoint;
                if (endPoint != null)
                {
                    ip = endPoint.Address?.ToString();
                    port = endPoint.Port;
                    logger.Log(string.Format("Listening {0}:{1}", ip, port));

                    while (true)
                    {
                        allDone.Reset();
                        listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
                        allDone.WaitOne();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log(ex, "BaseListener.StartListening");
            }
        }

        /// <summary>
        /// Stop listening.
        /// </summary>
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

        /// <summary>
        /// Accept the connection.
        /// </summary>
        /// <param name="asyncResult"></param>
        private void AcceptCallback(IAsyncResult asyncResult)
        {
            if (asyncResult == null || asyncResult.AsyncState == null)
                return;

            try
            {
                allDone.Set();

                var listener = (Socket)asyncResult.AsyncState;
                var handlerSocket = listener.EndAccept(asyncResult);
                var state = new StateObject { workSocket = handlerSocket };

                state.buffer = new byte[StateObject.bufferSize];
                handlerSocket.BeginReceive(state.buffer, 0, StateObject.bufferSize, 0, new AsyncCallback(ReadCallback), state);
            }
            catch (Exception ex)
            {
                logger.Log(ex, "BaseListener.AcceptCallback");
            }
        }

        /// <summary>
        /// Read incoming bytes.
        /// </summary>
        private void ReadCallback(IAsyncResult asyncResult)
        {
            if (asyncResult == null || asyncResult.AsyncState == null || onConnectCallback == null)
                return;

            try
            {
                var state = (StateObject)asyncResult.AsyncState;
                var handlerSocket = state.workSocket;

                var bytesRead = handlerSocket.EndReceive(asyncResult);
                if (bytesRead > 0)
                {
                    state.receiveBuffer.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
                    if (state.receiveBuffer.ToString().Contains("\r\n\r\n", StringComparison.CurrentCulture))
                    {
                        onConnectCallback?.Invoke(handlerSocket, state.receiveBuffer.ToString());
                    }
                    else
                    {
                        // Not all data received. Get more.  
                        handlerSocket.BeginReceive(state.buffer, 0, StateObject.bufferSize, 0, new AsyncCallback(ReadCallback), state);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log(ex, "BaseListener.ReadCallback");
            }
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        protected virtual void Dispose(bool cleanupAll)
        {
            allDone?.Dispose();
            listener?.Dispose();
        }
    }
}
