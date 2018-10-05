using ASM_FORM_CLIENT.Model;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ASM_FORM_CLIENT.SocketHandlerClient
{
    /// <summary>
    /// Implements the connection logic for the socket client.
    /// </summary>
    internal sealed class SocketClient : IDisposable
    {
        /// <summary>
        /// Constants for socket operations.
        /// </summary>
        private const Int32 ReceiveOperation = 1, SendOperation = 0;

        /// <summary>
        /// The socket used to send/receive messages.
        /// </summary>
        private Socket clientSocket;

        /// <summary>
        /// Flag for connected socket.
        /// </summary>
        private Boolean connected = false;

        private bool connecting = false;

        /// <summary>
        /// Listener endpoint.
        /// </summary>
        private IPEndPoint hostEndPoint;

        /// <summary>
        /// Signals a connection.
        /// </summary>
        private static AutoResetEvent autoConnectEvent = new AutoResetEvent(false);

        /// <summary>
        /// Signals the send/receive operation.
        /// </summary>
        private static AutoResetEvent[] autoSendReceiveEvents = new AutoResetEvent[]
        {
            new AutoResetEvent(false),
            new AutoResetEvent(false)
        };

        //private SocketAsyncEventArgs connectArgs = new SocketAsyncEventArgs();
        //private SocketAsyncEventArgs dataTransferArgs = new SocketAsyncEventArgs();
        private readonly Int16 SAEABufferSize = Int16.MaxValue;

        public event EventHandler ConnectivityChanged;

        /// <summary>
        /// Create an uninitialized client instance.  
        /// To start the send/receive processing
        /// call the Connect method followed by SendReceive method.
        /// </summary>
        /// <param name="hostName">Name of the host where the listener is running.</param>
        /// <param name="port">Number of the TCP port from the listener.</param>
        ////internal SocketClient(String hostName, Int32 port)
        ////{
        ////    // Get host related information.
        ////    IPHostEntry host = Dns.GetHostEntry(hostName);

        ////    // Addres of the host.
        ////    IPAddress[] addressList = host.AddressList;

        ////    // Instantiates the endpoint and socket.
        ////    this.hostEndPoint = new IPEndPoint(addressList[addressList.Length - 1], port);
        ////    this.clientSocket = new Socket(this.hostEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        ////}

        internal SocketClient(String ipAddress, Int32 port)
        {
            hostEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
            clientSocket = new Socket(hostEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            var startTimeSpan = TimeSpan.Zero;
            var periodTimeSpan = TimeSpan.FromSeconds(2);

            var timer = new Timer((e) =>
            {
                AutoCheckAndReconnect();
            }, null, startTimeSpan, periodTimeSpan);

            // Instantiates the endpoint and socket.
            //this.ConnectivityChanged += OnConnectivityChanged;

            //Thread AutoCheckConnectivity = new Thread(AutoCheckAndReconnect)
            //{
            //    IsBackground = true
            //};
            //AutoCheckConnectivity.Start();
        }

        private void ReNewSocket()
        {
            Console.WriteLine("------------------->[START] ReNewSocket for RECONNECT");
            clientSocket.Dispose();
            clientSocket = null;
            clientSocket = new Socket(hostEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            Console.WriteLine("------------------->[END] ReNewSocket for RECONNECT");
        }

        /// <summary>
        /// Connect to the host.
        /// </summary>
        /// <returns>True if connection has succeded, else false.</returns>
        internal void Connect()
        {
            Console.WriteLine("------------------->[START] connect");
            
            SocketAsyncEventArgs connectArgs = new SocketAsyncEventArgs
            {
                UserToken = clientSocket,
                RemoteEndPoint = hostEndPoint
            };
            connectArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnConnect);
            clientSocket.ConnectAsync(connectArgs);

            Console.WriteLine("------------------->[WAIT] for connect!");
            autoConnectEvent.WaitOne();

            connecting = false;
            Console.WriteLine("------------------->[END] connect, connected: {0}", this.connected);
        }

        void OnConnectivityChanged(object sender, EventArgs e)
        {
            Console.WriteLine("OnConnectivityChanged: {0}", (Boolean)sender);
            Dispose();
            Connect();
        }

        private void AutoCheckAndReconnect()
        {
            if (clientSocket != null)
            {
                Console.WriteLine("===========================================================================");
                Console.WriteLine("(1)---------------->[START] Check socket CONNECTIVITY, current status: {0}", this.connected);
                Console.WriteLine("(1)---------------->[CHECKING]<-------------------");
                this.connected = SocketConnected(clientSocket);
                Console.WriteLine("(1)---------------->[END] Check socket CONNECTIVITY, current status: {0}", this.connected);
                if (this.connected == false && !connecting)
                {
                    connecting = true;
                    Console.WriteLine("===========================================================================");
                    Console.WriteLine("(2)---------------->[START] RE-CONNECT, current status: {0}", this.connected);
                    Console.WriteLine("(2)---------------->[RE-CONNECTING]<-------------------");
                    Connect();
                    Console.WriteLine("(2)---------------->[END] RE-CONNECT, current status: {0}", this.connected);
                }
            }
        }

        bool SocketConnected(Socket s)
        {
            try
            {
                String text = "P";
                byte[] data = Encoding.ASCII.GetBytes(text);
                s.Send(data);
                return true;
            }
            catch (ArgumentNullException ex)
            {
                Console.WriteLine("ArgumentNullException: {0}", ex.Message);
                return false;
            }
            catch (SocketException ex)
            {
                Console.WriteLine("(1)---------------->[CHECKING] Has ERROR ...");
                Console.WriteLine("===========================================================================");
                Console.WriteLine("SocketException: {0}", ex.Message);
                Console.WriteLine("===========================================================================");
                ReNewSocket();
                Console.WriteLine("===========================================================================");
                return false;
            }
            catch (ObjectDisposedException ex)
            {
                Console.WriteLine("ObjectDisposedException: {0}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Disconnect from the host.
        /// </summary>
        internal void Disconnect()
        {
            clientSocket.Disconnect(false);
        }

        private void OnConnect(object sender, SocketAsyncEventArgs e)
        {
            // Signals the end of connection.
            autoConnectEvent.Set();

            // Set the flag for socket connected.
            this.connected = (e.SocketError == SocketError.Success);
            connecting = false;
        }

        private void OnReceive(object sender, SocketAsyncEventArgs e)
        {
            // Signals the end of receive.
            autoSendReceiveEvents[SendOperation].Set();
        }

        private void OnSend(object sender, SocketAsyncEventArgs e)
        {
            // Signals the end of send.
            autoSendReceiveEvents[ReceiveOperation].Set();

            if (e.SocketError == SocketError.Success)
            {
                if (e.LastOperation == SocketAsyncOperation.Send)
                {
                    // Prepare receiving.
                    Socket s = e.UserToken as Socket;

                    byte[] receiveBuffer = new byte[SAEABufferSize];
                    e.SetBuffer(receiveBuffer, 0, receiveBuffer.Length);
                    e.Completed += new EventHandler<SocketAsyncEventArgs>(OnReceive);
                    s.ReceiveAsync(e);
                }
            }
            else
            {
                this.ProcessError(e);
            }
        }

        /// <summary>
        /// Close socket in case of failure and throws a SockeException according to the SocketError.
        /// </summary>
        /// <param name="e">SocketAsyncEventArg associated with the failed operation.</param>
        private void ProcessError(SocketAsyncEventArgs e)
        {
            Socket s = e.UserToken as Socket;
            this.connected = this.connecting = false;
            if (s.Connected)
            {
                // close the socket associated with the client
                try
                {
                    s.Shutdown(SocketShutdown.Both);
                }
                catch (Exception)
                {
                    // throws if client process has already closed
                }
                finally
                {
                    if (s.Connected)
                    {
                        s.Close();
                    }
                }
            }

            // Throw the SocketException
            throw new SocketException((Int32)e.SocketError);
        }

        internal BaseModel SendRequest(BaseModel baseModel)
        {
            if (this.connected)
            {
                // Create a buffer to send.
                Byte[] sendBuffer = baseModel.ObjectToByteArray();

                // Prepare arguments for send/receive operation.
                SocketAsyncEventArgs dataTransferArgs = new SocketAsyncEventArgs();
                dataTransferArgs.SetBuffer(sendBuffer, 0, sendBuffer.Length);
                dataTransferArgs.UserToken = this.clientSocket;
                dataTransferArgs.RemoteEndPoint = this.hostEndPoint;
                dataTransferArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSend);

                // Start sending asyncronally.
                clientSocket.SendAsync(dataTransferArgs);

                // Wait for the send/receive completed.
                AutoResetEvent.WaitAll(autoSendReceiveEvents);

                // Return data from SocketAsyncEventArgs buffer.
                return BaseModel.ByteArrayToObject(dataTransferArgs.Buffer);// (completeArgs.Buffer, completeArgs.Offset, completeArgs.BytesTransferred);
            }
            return null;
        }

        #region IDisposable Members

        /// <summary>
        /// Disposes the instance of SocketClient.
        /// </summary>
        public void Dispose()
        {
            autoConnectEvent.Close();
            autoSendReceiveEvents[SendOperation].Close();
            autoSendReceiveEvents[ReceiveOperation].Close();
            if (this.clientSocket.Connected)
            {
                this.clientSocket.Close();
            }
        }

        #endregion
    }
}
