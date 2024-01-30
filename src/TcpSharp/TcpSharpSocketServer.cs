namespace TcpSharp;

public class TcpSharpSocketServer
{
    #region Public Properties
    public bool Listening
    {
        get { return _isListening; }
        private set { _isListening = value; }
    }
    public int Port
    {
        get { return _port; }
        set
        {
            if (Listening)
                throw (new Exception("Socket Server is already listening. You cant change this property while listening."));

            _port = value;
        }
    }
    public bool NoDelay
    {
        get { return _nodelay; }
        private set { _nodelay = value; }
    }
    public bool KeepAlive
    {
        get { return _keepAlive; }
        set
        {
            if (Listening)
                throw (new Exception("Socket Server is already listening. You cant change this property while listening."));

            _keepAlive = value;
        }
    }

    public int KeepAliveTime
    {
        get { return _keepAliveTime; }
        set
        {
            if (Listening)
                throw (new Exception("Socket Server is already listening. You cant change this property while listening."));

            _keepAliveTime = value;
        }
    }

    /// <summary>
    /// Keep-alive interval in seconds
    /// </summary>
    public int KeepAliveInterval
    {
        get { return _keepAliveInterval; }
        set
        {
            if (Listening)
                throw (new Exception("Socket Server is already listening. You cant change this property while listening."));

            _keepAliveInterval = value;
        }
    }

    public int KeepAliveRetryCount
    {
        get { return _keepAliveRetryCount; }
        set
        {
            if (Listening)
                throw (new Exception("Socket Server is already listening. You cant change this property while listening."));

            _keepAliveRetryCount = value;
        }
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
    private bool _isListening;
    private int _port;
    private bool _nodelay = true;
    private bool _keepAlive = false;
    private int _keepAliveTime = 900;
    private int _keepAliveInterval = 300;
    private int _keepAliveRetryCount = 5;
    private int _receiveBufferSize = 8192;
    private int _receiveTimeout = 0;
    private int _sendBufferSize = 8192;
    private int _sendTimeout = 0;
    private long _bytesReceived = 0;
    private long _bytesSent = 0;
    #endregion

    #region Public Events
    public event EventHandler<OnServerStartedEventArgs> OnStarted = delegate { };
    public event EventHandler<OnServerStoppedEventArgs> OnStopped = delegate { };
    public event EventHandler<OnServerErrorEventArgs> OnError = delegate { };
    public event EventHandler<OnServerConnectionRequestEventArgs> OnConnectionRequest = delegate { };
    public event EventHandler<OnServerConnectedEventArgs> OnConnected = delegate { };
    public event EventHandler<OnServerDisconnectedEventArgs> OnDisconnected = delegate { };
    public event EventHandler<OnServerDataReceivedEventArgs> OnDataReceived = delegate { };
    #endregion

    #region Event Invokers
    internal void InvokeOnStarted(OnServerStartedEventArgs args) => this.OnStarted?.Invoke(this, args);
    internal void InvokeOnStopped(OnServerStoppedEventArgs args) => this.OnStopped?.Invoke(this, args);
    internal void InvokeOnError(OnServerErrorEventArgs args) => this.OnError?.Invoke(this, args);
    internal void InvokeOnConnectionRequest(OnServerConnectionRequestEventArgs args) => this.OnConnectionRequest?.Invoke(this, args);
    internal void InvokeOnConnected(OnServerConnectedEventArgs args) => this.OnConnected?.Invoke(this, args);
    internal void InvokeOnDisconnected(OnServerDisconnectedEventArgs args) => this.OnDisconnected?.Invoke(this, args);
    internal void InvokeOnDataReceived(OnServerDataReceivedEventArgs args) => this.OnDataReceived?.Invoke(this, args);
    #endregion

    #region Readonly Properties
    private TcpListener _listener;
    private readonly SnowflakeGenerator _idGenerator;
    private readonly ConcurrentDictionary<string, ConnectedClient> _clients;
    #endregion

    #region Listener Task
    private CancellationTokenSource _cancellationTokenSource;
    private CancellationToken _cancellationToken;
    #endregion

    #region Constructors
    public TcpSharpSocketServer() : this(1024)
    {
    }

    public TcpSharpSocketServer(int port)
    {
        this.Port = port;

        this._idGenerator = new SnowflakeGenerator();
        this._clients = new ConcurrentDictionary<string, ConnectedClient>();
    }
    #endregion

    #region Public Methods
    public void StartListening()
    {
        this._clients.Clear();

        this._cancellationTokenSource = new CancellationTokenSource();
        this._cancellationToken = this._cancellationTokenSource.Token;
        Task.Factory.StartNew(ListeningTask, TaskCreationOptions.LongRunning);
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
        this.Listening = false;

        // Stop Thread
        this._cancellationTokenSource.Cancel();

        // Invoke OnStopped
        InvokeOnStopped(new OnServerStoppedEventArgs
        {
            IsStopped = true,
        });
    }

    public ConnectedClient GetClient(string connectionId)
    {
        // Check Point
        if (!_clients.ContainsKey(connectionId)) return null;

        // Return Client
        return _clients[connectionId];
    }

    public long SendBytes(string connectionId, byte[] bytes)
    {
        // Get Client
        var client = GetClient(connectionId);
        if (client == null) return 0;

        // Send Bytes
        return client.SendBytes(bytes);
    }

    public async Task<long> SendBytesAsync(string connectionId, byte[] bytes, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        await Task.CompletedTask;

        return SendBytes(connectionId, bytes);
    }

    public long SendString(string connectionId, string data)
    {
        // Get Client
        var client = GetClient(connectionId);
        if (client == null) return 0;

        // Send Bytes
        return client.SendString(data);
    }

    public async Task<long> SendStringAsync(string connectionId, string data, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        await Task.CompletedTask;

        return SendString(connectionId, data);
    }

    public long SendString(string connectionId, string data, Encoding encoding)
    {
        // Get Client
        var client = GetClient(connectionId);
        if (client == null) return 0;

        // Send Bytes
        return client.SendString(data, encoding);
    }

    public async Task<long> SendStringAsync(string connectionId, string data, Encoding encoding, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        await Task.CompletedTask;

        return SendString(connectionId, data, encoding);
    }

    public long SendFile(string connectionId, string fileName)
    {
        // Get Client
        var client = GetClient(connectionId);
        if (client == null) return 0;

        // Send Bytes
        return client.SendFile(fileName);
    }

    public async Task<long> SendFileAsync(string connectionId, string fileName, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        await Task.CompletedTask;

        return SendFile(connectionId, fileName);
    }

    public long SendFile(string connectionId, string fileName, byte[] preBuffer, byte[] postBuffer, TransmitFileOptions flags)
    {
        // Get Client
        var client = GetClient(connectionId);
        if (client == null) return 0;

        // Send Bytes
        return client.SendFile(fileName, preBuffer, postBuffer, flags);
    }

    public async Task<long> SendFileAsync(string connectionId, string fileName, byte[] preBuffer, byte[] postBuffer, TransmitFileOptions flags, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        await Task.CompletedTask;

        return SendFile(connectionId, fileName, preBuffer, postBuffer, flags);
    }

    public void Disconnect(string connectionId, DisconnectReason reason = DisconnectReason.None)
    {
        // Check Point
        if (!_clients.ContainsKey(connectionId)) return;

        // Get Client
        var client = _clients[connectionId];

        // Check Point
        // if (!client.Connected) return;

        // Stop Receiving
        client.StopReceiving();

        // Disconnect
        this.Disconnect(client.Client);

        // Remove From Clients
        _clients.TryRemove(connectionId, out _);

        // Invoke OnDisconnected
        InvokeOnDisconnected(new OnServerDisconnectedEventArgs
        {
            ConnectionId = connectionId,
            Reason = reason,
        });
    }
    #endregion

    #region Internal Methods
    internal void AddReceivedBytes(long bytesCount)
    {
        Interlocked.Add(ref _bytesReceived, bytesCount);
    }

    internal void AddSentBytes(long bytesCount)
    {
        Interlocked.Add(ref _bytesSent, bytesCount);
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

    private void ListeningTask()
    {
        this._listener = new TcpListener(IPAddress.Any, this.Port);

        // NoDelay
        this._listener.Server.NoDelay = this.NoDelay;

        /* Keep Alive */
        if (this.KeepAlive && this.KeepAliveInterval > 0)
        {
#if NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
            _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            _listener.Server.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, this.KeepAliveTime);
            _listener.Server.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, this.KeepAliveInterval);
            _listener.Server.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, this.KeepAliveRetryCount);
#elif NETFRAMEWORK
            // Get the size of the uint to use to back the byte array
            // int size = Marshal.SizeOf((uint)0);
            int size = sizeof(uint);

            // Create the byte array
            byte[] keepAlive = new byte[size * 3];

            // Pack the byte array:
            // Turn keepalive on
            Buffer.BlockCopy(BitConverter.GetBytes((uint)1), 0, keepAlive, 0, size);

            // How long does it take to start the first probe (in milliseconds)
            Buffer.BlockCopy(BitConverter.GetBytes((uint)(KeepAliveTime*1000)), 0, keepAlive, size, size);

            // Detection time interval (in milliseconds)
            Buffer.BlockCopy(BitConverter.GetBytes((uint)(KeepAliveInterval*1000)), 0, keepAlive, size * 2, size);

            // Set the keep-alive settings on the underlying Socket
            _listener.Server.IOControl(IOControlCode.KeepAliveValues, keepAlive, null);
#elif NETSTANDARD
            // Set the keep-alive settings on the underlying Socket
            _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
#endif
        }

        // Start
        this._listener.Start();

        // Listening
        this.Listening = true;

        // Invoke OnStarted Event
        InvokeOnStarted(new OnServerStartedEventArgs
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
            var cr_args = new OnServerConnectionRequestEventArgs
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
                this.Disconnect(tcpClient);
                continue;
            }

            // Accept
            tcpClient.NoDelay = this.NoDelay;
            tcpClient.ReceiveBufferSize = this.ReceiveBufferSize;
            tcpClient.ReceiveTimeout = this.ReceiveTimeout;
            tcpClient.SendBufferSize = this.SendBufferSize;
            tcpClient.SendTimeout = this.SendTimeout;
            var nanoClient = new ConnectedClient(this, tcpClient, this._idGenerator.GenerateId().ToString());
            this._clients[nanoClient.ConnectionId] = nanoClient;

            // Start Receiving
            nanoClient.StartReceiving();

            // Invoke OnConnected Event
            var c_args = new OnServerConnectedEventArgs
            {
                IPEndPoint = ipEndPoint,
                IPAddress = ipAddress,
                Port = port,
                ConnectionId = nanoClient.ConnectionId
            };
            InvokeOnConnected(c_args);
        }

        // Listening
        this.Listening = false;
    }
    #endregion

}