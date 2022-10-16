using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TcpSharp.Enums;
using TcpSharp.Events.Server;

namespace TcpSharp
{
    public class ConnectedClient
    {
        /* Public Properties */
        public TcpClient Client { get; internal set; }
        public long ConnectionId { get; internal set; }
        public bool Connected { get { return this.Client != null && this.Client.Connected; } }
        public bool AcceptData { get; internal set; } = true;
        public long BytesReceived { get; private set; }
        public long BytesSent { get; private set; }

        /* Reference Fields */
        private readonly TcpSharpSocketServer _server;

        /* Private Fields */
        private readonly Thread _thread;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly CancellationToken _cancellationToken;

        internal ConnectedClient(TcpSharpSocketServer server, TcpClient client, long connectionId)
        {
            this.Client = client;
            this.ConnectionId = connectionId;

            this._server = server;

            this._thread = new Thread(async () => await ConnectionHandler());
            this._cancellationTokenSource = new CancellationTokenSource();
            this._cancellationToken = this._cancellationTokenSource.Token;
        }

        internal void StartReceiving()
        {
            this._thread.Start();
        }

        internal void StopReceiving()
        {
            this._cancellationTokenSource.Cancel();
        }

        internal async Task ConnectionHandler()
        {
            var stream = this.Client.GetStream();
            var buffer = new byte[this.Client.ReceiveBufferSize];
#if RELEASE
            try
            {
#endif
                var bytesCount = 0;
                while ((bytesCount = await stream.ReadAsync(buffer, 0, buffer.Length, this._cancellationToken)) != 0)
                {
                    // Increase BytesReceived
                    BytesReceived += bytesCount;
                    _server.BytesReceived += bytesCount;

                    // Invoke OnDataReceived
                    if (this.AcceptData)
                    {
                        var bytesReceived = new byte[bytesCount];
                        Array.Copy(buffer, bytesReceived, bytesCount);
                        _server.InvokeOnDataReceived(new OnDataReceivedEventArgs
                        {
                            Client = this.Client,
                            ConnectionId = this.ConnectionId,
                            Data = bytesReceived
                        });
                    }
                }
#if RELEASE
            }
            catch (Exception ex)
            {
                // Invoke OnError
                _server.InvokeOnError(new OnErrorEventArgs
                {
                    Client = this.Client,
                    ConnectionId = this.ConnectionId,
                    Exception = ex
                });

                // Disconnect
                _server.Disconnect(this.ConnectionId, DisconnectReason.Exception);
            }
#endif
        }

        public int SendBytes(byte[] bytes)
        {
            if (!this.Connected) return 0;

            this.BytesSent += bytes.Length;
            _server.BytesSent += bytes.Length;

            return this.Client.Client.Send(bytes);
        }

        public int SendString(string data)
        {
            if (!this.Connected) return 0;

            var bytes = Encoding.UTF8.GetBytes(data);
            this.BytesSent += bytes.Length;
            _server.BytesSent += bytes.Length;

            return this.Client.Client.Send(bytes);
        }

        public int SendString(string data, Encoding encoding)
        {
            if (!this.Connected) return 0;

            var bytes = encoding.GetBytes(data);
            this.BytesSent += bytes.Length;
            _server.BytesSent += bytes.Length;

            return this.Client.Client.Send(bytes);
        }

        public void SendFile(string fileName)
        {
            if (!this.Connected) return;

            this.Client.Client.SendFile(fileName);
        }

        public void SendFile(string fileName, byte[] preBuffer, byte[] postBuffer, TransmitFileOptions flags)
        {
            if (!this.Connected) return;

            this.Client.Client.SendFile(fileName, preBuffer, postBuffer, flags);
        }
    }
}
