using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TcpSharp.Enums;
using TcpSharp.Events.Client;

namespace TcpSharp
{
    public class TcpSharpSocketClient
    {
        #region Public Properties
        public IPAddress IPAddress
        {
            get { return IPAddress.Parse(this.Host); }
        }
        public string Host
        {
            get { return _host; }
            set
            {
                if (Connected)
                    throw (new Exception("Socket Server is already listening. You cant change this property while listening."));

                _host = value;
            }
        }
        public int Port
        {
            get { return _port; }
            set
            {
                if (Connected)
                    throw (new Exception("Socket Server is already listening. You cant change this property while listening."));



                _port = value;
            }
        }
        public bool NoDelay
        {
            get { return _nodelay; }
            set
            {
                _nodelay = value;
                if (_socket != null) _socket.NoDelay = value;
            }
        }
        public int ReceiveBufferSize
        {
            get { return _receiveBufferSize; }
            set
            {
                _receiveBufferSize = value;
                _recvBuffer = new byte[value];
                if (_socket != null) _socket.ReceiveBufferSize = value;
            }
        }
        public int ReceiveTimeout
        {
            get { return _receiveTimeout; }
            set
            {
                _receiveTimeout = value;
                if (_socket != null) _socket.ReceiveTimeout = value;
            }
        }
        public int SendBufferSize
        {
            get { return _sendBufferSize; }
            set
            {
                _sendBufferSize = value;
                _sendBuffer = new byte[value];
                if (_socket != null) _socket.SendBufferSize = value;
            }
        }
        public int SendTimeout
        {
            get { return _sendTimeout; }
            set
            {
                _sendTimeout = value;
                if (_socket != null) _socket.SendTimeout = value;
            }
        }
        public long BytesReceived
        {
            get { return _bytesReceived; }
            internal set { _bytesReceived = value; }
        }
        public long BytesSent
        {
            get { return _bytesSent; }
            internal set { _bytesSent = value; }
        }
        public bool Reconnect
        {
            get { return _reconnect; }
            set { _reconnect = value; }
        }
        public int ReconnectDelayInSeconds
        {
            get { return _reconnectDelay; }
            set { _reconnectDelay = value; }
        }
        public bool AcceptData
        {
            get { return _acceptData; }
            set { _acceptData = value; }
        }
        public bool Connected { get { return this._socket != null && this._socket.Connected; } }
        #endregion

        #region Private Properties
        private string _host { get; set; }
        private int _port { get; set; }
        private bool _nodelay { get; set; } = true;
        private int _receiveBufferSize { get; set; } = 8192;
        private int _receiveTimeout { get; set; } = 0;
        private int _sendBufferSize { get; set; } = 8192;
        private int _sendTimeout { get; set; } = 0;
        private long _bytesReceived { get; set; }
        private long _bytesSent { get; set; }
        private bool _reconnect { get; set; } = true;
        private int _reconnectDelay { get; set; } = 5;
        private bool _acceptData { get; set; } = true;
        #endregion

        #region Public Events
        public event EventHandler<OnErrorEventArgs> OnError;
        public event EventHandler<OnConnectedEventArgs> OnConnected;
        public event EventHandler<OnDisconnectedEventArgs> OnDisconnected;
        public event EventHandler<OnDataReceivedEventArgs> OnDataReceived;
        #endregion

        #region Private Fields
        private Socket _socket;
        private byte[] _recvBuffer;
        private byte[] _sendBuffer;
        #endregion

        #region Receiver Thread
        private Thread _thread;
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationToken _cancellationToken;
        #endregion

        #region Constructors
        public TcpSharpSocketClient() : this("127.0.0.1", 1024)
        {
        }

        public TcpSharpSocketClient(string host, int port)
        {
            this.Host = host;
            this.Port = port;
        }
        #endregion

        #region Public Methods
        public void Connect()
        {
#if RELEASE
            try
            {
#endif
            // Buffers
            this._recvBuffer = new byte[ReceiveBufferSize];
            this._sendBuffer = new byte[SendBufferSize];

            // Get Host IP Address that is used to establish a connection  
            // In this case, we get one IP address of localhost that is IP : 127.0.0.1  
            // If a host has multiple addresses, you will get a list of addresses  
            var serverIPHost = Dns.GetHostEntry(Host);
            var serverIPAddress = serverIPHost.AddressList[0];
            var serverIPEndPoint = new IPEndPoint(serverIPAddress, Port);

            // Create a TCP/IP  socket.    
            this._socket = new Socket(serverIPAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // Set Properties
            this._socket.NoDelay = this.NoDelay;
            this._socket.ReceiveBufferSize = this.ReceiveBufferSize;
            this._socket.ReceiveTimeout = this.ReceiveTimeout;
            this._socket.SendBufferSize = this.SendBufferSize;
            this._socket.SendTimeout = this.SendTimeout;

            // Connect to Remote EndPoint
            _socket.Connect(serverIPEndPoint);

            // Start Receiver Thread
            this._cancellationTokenSource = new CancellationTokenSource();
            this._cancellationToken = this._cancellationTokenSource.Token;
            this._thread = new Thread(ReceiverThreadAction);
            this._thread.Start();

            // Invoke OnConnected
            this.InvokeOnConnected(new OnConnectedEventArgs
            {
                ServerHost = this.Host,
                ServerPort = this.Port,
            });
#if RELEASE
            }
            catch (Exception ex)
            {
                // Invoke OnError
                this.InvokeOnError(new OnErrorEventArgs
                {
                    Exception = ex
                });
            }
#endif
        }

        public void Disconnect()
        {
            this.Disconnect(DisconnectReason.None);
        }

        public int SendBytes(byte[] bytes)
        {
            // Check Point
            if (!this.Connected) return 0;

            // Action
            this.BytesSent += bytes.Length;
            return this._socket.Send(bytes);
        }

        public int SendString(string data)
        {
            // Check Point
            if (!this.Connected) return 0;

            // Action
            var bytes = Encoding.UTF8.GetBytes(data);
            this.BytesSent += bytes.Length;
            return this._socket.Send(bytes);
        }

        public int SendString(string data, Encoding encoding)
        {
            // Check Point
            if (!this.Connected) return 0;

            // Action
            var bytes = encoding.GetBytes(data);
            this.BytesSent += bytes.Length;
            return this._socket.Send(bytes);
        }

        public void SendFile(string fileName)
        {
            // Check Point
            if (!this.Connected) return;

            // Action
            this._socket.SendFile(fileName);
        }

        public void SendFile(string fileName, byte[] preBuffer, byte[] postBuffer, TransmitFileOptions flags)
        {
            // Check Point
            if (!this.Connected) return;

            // Action
            this._socket.SendFile(fileName, preBuffer, postBuffer, flags);
        }
        #endregion

        #region Private Methods
        private void ReceiverThreadAction()
        {
            try
            {
                while (!_cancellationToken.IsCancellationRequested)
                {
                    // Receive the response from the remote device.    
                    var bytesCount = _socket.Receive(_recvBuffer);
                    if (bytesCount > 0)
                    {
                        BytesReceived += bytesCount;
                        if (this.AcceptData)
                        {
                            var bytes = new byte[bytesCount];
                            Array.Copy(_recvBuffer, bytes, bytesCount);

                            // Invoke OnDataReceived
                            this.InvokeOnDataReceived(new OnDataReceivedEventArgs
                            {
                                Data = bytes
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Invoke OnError
                this.InvokeOnError(new OnErrorEventArgs
                {
                    Exception = ex
                });

                // Disconnect
                Disconnect(DisconnectReason.Exception);
            }
        }

        private void Disconnect(DisconnectReason reason)
        {
            // Release the socket.    
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();

            // Stop Receiver Thread
            this._cancellationTokenSource.Cancel();

            // Invoke OnDisconnected
            this.InvokeOnDisconnected(new OnDisconnectedEventArgs());

            // Reconnect
            if (this.Reconnect)
            {
                while (!this.Connected)
                {
                    Task.Delay(this.ReconnectDelayInSeconds * 1000);
                    this.Connect();
                }
            }
        }
        #endregion

        #region Event Invokers
        internal void InvokeOnError(OnErrorEventArgs args)
        {
            this.OnError?.Invoke(this, args);
        }

        internal void InvokeOnConnected(OnConnectedEventArgs args)
        {
            this.OnConnected?.Invoke(this, args);
        }

        internal void InvokeOnDisconnected(OnDisconnectedEventArgs args)
        {
            this.OnDisconnected?.Invoke(this, args);
        }

        internal void InvokeOnDataReceived(OnDataReceivedEventArgs args)
        {
            this.OnDataReceived?.Invoke(this, args);
        }
        #endregion
    }
}
