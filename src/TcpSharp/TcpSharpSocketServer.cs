using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TcpSharp.Enums;
using TcpSharp.Events.Server;

namespace TcpSharp
{
    public class TcpSharpSocketServer
    {
        #region Public Properties
        public bool IsListening
        {
            get { return _isListening; }
            private set { _isListening = value; }
        }
        public IPAddress IPAddress
        {
            get { return IPAddress.Parse(this.Host); }
        }
        public string Host
        {
            get { return _host; }
            set
            {
                if (IsListening)
                    throw (new Exception("Socket Server is already listening. You cant change this property while listening."));

                _host = value;
            }
        }
        public int Port
        {
            get { return _port; }
            set
            {
                if (IsListening)
                    throw (new Exception("Socket Server is already listening. You cant change this property while listening."));



                _port = value;
            }
        }
        public bool NoDelay
        {
            get { return _nodelay; }
            private set { _nodelay = value; }
        }
        public int ReceiveBufferSize
        {
            get { return _receiveBufferSize; }
            set { _receiveBufferSize = value; }
        }
        public int ReceiveTimeout
        {
            get { return _receiveTimeout; }
            set { _receiveTimeout = value; }
        }
        public int SendBufferSize
        {
            get { return _sendBufferSize; }
            set { _sendBufferSize = value; }
        }
        public int SendTimeout
        {
            get { return _sendTimeout; }
            set { _sendTimeout = value; }
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
        #endregion

        #region Private Properties
        private bool _isListening { get; set; }
        private IPAddress _ipAddress { get; set; }
        private string _host { get; set; }
        private int _port { get; set; }
        private bool _nodelay { get; set; } = true;
        private int _receiveBufferSize { get; set; } = 8192;
        private int _receiveTimeout { get; set; } = 0;
        private int _sendBufferSize { get; set; } = 8192;
        private int _sendTimeout { get; set; } = 0;
        private long _bytesReceived { get; set; }
        private long _bytesSent { get; set; }
        #endregion

        #region Public Events
        public event EventHandler<OnStartedEventArgs> OnStarted;
        public event EventHandler<OnStoppedEventArgs> OnStopped;
        public event EventHandler<OnErrorEventArgs> OnError;
        public event EventHandler<OnConnectionRequestEventArgs> OnConnectionRequest;
        public event EventHandler<OnConnectedEventArgs> OnConnected;
        public event EventHandler<OnDisconnectedEventArgs> OnDisconnected;
        public event EventHandler<OnDataReceivedEventArgs> OnDataReceived;
        #endregion

        #region Readonly Properties
        private TcpListener _listener;
        private readonly SnowflakeGenerator _idGenerator;
        private readonly ConcurrentDictionary<long, ConnectedClient> _clients;
        #endregion

        #region Listener Thread
        private Thread _thread;
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationToken _cancellationToken;
        #endregion

        #region Constructors
        public TcpSharpSocketServer() : this("0.0.0.0", 1024)
        {
        }

        public TcpSharpSocketServer(string host, int port)
        {
            this.Host = host;
            this.Port = port;

            this._idGenerator = new SnowflakeGenerator();
            this._clients = new ConcurrentDictionary<long, ConnectedClient>();
        }
        #endregion

        #region Public Methods
        public void StartListening()
        {
            this._clients.Clear();

            this._cancellationTokenSource = new CancellationTokenSource();
            this._cancellationToken = this._cancellationTokenSource.Token;
            this._thread = new Thread(ListeningThreadAction);
            this._thread.Start();
        }

        public void StopListening()
        {
            // Disconnect All Clients
            var connectionIds = _clients.Keys.ToList();
            foreach (var connectionId in connectionIds)
            {
                Disconnect(connectionId, DisconnectReason.ServerStopped);
            }

            // Stop Listener
            this._listener.Stop();

            // Stop Thread
            this._cancellationTokenSource.Cancel();

            // Invoke OnStopped
            InvokeOnStopped(new OnStoppedEventArgs
            {
                IsStopped = true,
            });
        }

        public ConnectedClient GetClient(long connectionId)
        {
            // Check Point
            if (!_clients.ContainsKey(connectionId)) return null;

            // Return Client
            return _clients[connectionId];
        }

        public int SendBytes(long connectionId, byte[] bytes)
        {
            // Get Client
            var client = GetClient(connectionId);
            if (client == null) return 0;

            // Send Bytes
            return client.SendBytes(bytes);
        }

        public int SendString(long connectionId, string data)
        {
            // Get Client
            var client = GetClient(connectionId);
            if (client == null) return 0;

            // Send Bytes
            return client.SendString(data);
        }

        public int SendString(long connectionId, string data, Encoding encoding)
        {
            // Get Client
            var client = GetClient(connectionId);
            if (client == null) return 0;

            // Send Bytes
            return client.SendString(data, encoding);
        }

        public void SendFile(long connectionId, string fileName)
        {
            // Get Client
            var client = GetClient(connectionId);
            if (client == null) return;

            // Send Bytes
            client.SendFile(fileName);
        }

        public void SendFile(long connectionId, string fileName, byte[] preBuffer, byte[] postBuffer, TransmitFileOptions flags)
        {
            // Get Client
            var client = GetClient(connectionId);
            if (client == null) return;

            // Send Bytes
            client.SendFile(fileName, preBuffer, postBuffer, flags);
        }

        public void Disconnect(long connectionId, DisconnectReason reason = DisconnectReason.None)
        {
            // Check Point
            if (!_clients.ContainsKey(connectionId)) return;

            // Get Client
            var client = _clients[connectionId];

            // Check Point
            if (!client.Connected) return;

            // Stop Receiving
            client.StopReceiving();

            // Disconnect
            this.Disconnect(client.Client);

            // Remove From Clients
            _clients.TryRemove(connectionId, out _);

            // Invoke OnDisconnected
            InvokeOnDisconnected(new OnDisconnectedEventArgs
            {
                ConnectionId = connectionId,
                Reason = reason,
            });
        }
        #endregion

        #region Private Methods
        private void Disconnect(TcpClient client)
        {
            try
            {
                client.GetStream().Close();
                client.Close();
                client.Dispose();
            }
            catch { }
        }

        private void ListeningThreadAction()
        {
            this._listener = new TcpListener(IPAddress.Parse(this.Host), this.Port);
            this._listener.Start();
            this.IsListening = true;

            // Invoke OnStarted Event
            InvokeOnStarted(new OnStartedEventArgs
            {
                IsStarted = true
            });

            // Loop for new connections
            while (!this._cancellationToken.IsCancellationRequested)
            {
                // Getting new connections
                var tcpClient = this._listener.AcceptTcpClient();
                var ipEndPoint = (IPEndPoint)tcpClient.Client.RemoteEndPoint;
                var ipAddress = ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString();
                var port = ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Port;
                var cr_args = new OnConnectionRequestEventArgs
                {
                    IPEndPoint = ipEndPoint,
                    IPAddress = ipAddress,
                    Port = port,
                    Accept = true
                };

                // Decide Accept or Reject
                // Invoke OnConnectionRequest Event
                InvokeOnConnectionRequest(cr_args);

                // Reject
                if (!cr_args.Accept)
                {
                    Disconnect(tcpClient);
                    continue;
                }

                // Accept
                tcpClient.NoDelay = this.NoDelay;
                tcpClient.ReceiveBufferSize = this.ReceiveBufferSize;
                tcpClient.ReceiveTimeout = this.ReceiveTimeout;
                tcpClient.SendBufferSize = this.SendBufferSize;
                tcpClient.SendTimeout = this.SendTimeout;
                var nanoClient = new ConnectedClient(this, tcpClient, this._idGenerator.GenerateId());
                this._clients[nanoClient.ConnectionId] = nanoClient;

                // Start Receiving
                nanoClient.StartReceiving();

                // Invoke OnConnected Event
                var c_args = new OnConnectedEventArgs
                {
                    IPEndPoint = ipEndPoint,
                    IPAddress = ipAddress,
                    Port = port,
                    ConnectionId = nanoClient.ConnectionId
                };
                InvokeOnConnected(c_args);
            }
        }
        #endregion

        #region Event Invokers
        internal void InvokeOnStarted(OnStartedEventArgs args)
        {
            this.OnStarted?.Invoke(this, args);
        }

        internal void InvokeOnStopped(OnStoppedEventArgs args)
        {
            this.OnStopped?.Invoke(this, args);
        }

        internal void InvokeOnError(OnErrorEventArgs args)
        {
            this.OnError?.Invoke(this, args);
        }

        internal void InvokeOnConnectionRequest(OnConnectionRequestEventArgs args)
        {
            this.OnConnectionRequest?.Invoke(this, args);
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