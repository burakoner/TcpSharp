using System;
using System.IO;
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

        private async Task ConnectionHandler()
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
                this._server.AddReceivedBytes(bytesCount);

                // Invoke OnDataReceived
                if (this.AcceptData)
                {
                    var bytesReceived = new byte[bytesCount];
                    Array.Copy(buffer, bytesReceived, bytesCount);
                    this._server.InvokeOnDataReceived(new OnDataReceivedEventArgs
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
              this.   _server.InvokeOnError(new OnErrorEventArgs
                {
                    Client = this.Client,
                    ConnectionId = this.ConnectionId,
                    Exception = ex
                });

                // Disconnect
               this.  _server.Disconnect(this.ConnectionId, DisconnectReason.Exception);
            }
#endif
        }

        public long SendBytes(byte[] bytes)
        {
            if (!this.Connected) return 0;

            this.BytesSent += bytes.Length;
            this._server.AddSentBytes(bytes.Length);

            return this.Client.Client.Send(bytes);
        }

        public long SendString(string data)
        {
            if (!this.Connected) return 0;

            var bytes = Encoding.UTF8.GetBytes(data);
            this.BytesSent += bytes.Length;
            this._server.AddSentBytes(bytes.Length);

            return this.Client.Client.Send(bytes);
        }

        public long SendString(string data, Encoding encoding)
        {
            if (!this.Connected) return 0;

            var bytes = encoding.GetBytes(data);
            this.BytesSent += bytes.Length;
            this._server.AddSentBytes(bytes.Length);

            return this.Client.Client.Send(bytes);
        }

        public long SendFile(string filePath)
        {
            // Check Point
            if (!this.Connected) return 0;
            if (!File.Exists(filePath)) return 0;

            // FileInfo
            var fileInfo = new FileInfo(filePath);
            if (fileInfo == null) return 0;

            // Action
            this.Client.Client.SendFile(filePath);
            this.BytesSent += fileInfo.Length;
            this._server.AddSentBytes(fileInfo.Length);

            // Return
            return fileInfo.Length;
        }

        public long SendFile(string filePath, byte[] preBuffer, byte[] postBuffer, TransmitFileOptions flags)
        {
            // Check Point
            if (!this.Connected) return 0;
            if (!File.Exists(filePath)) return 0;

            // FileInfo
            var fileInfo = new FileInfo(filePath);
            if (fileInfo == null) return 0;

            // Action
            this.Client.Client.SendFile(filePath, preBuffer, postBuffer, flags);
            this.BytesSent += fileInfo.Length;
            this._server.AddSentBytes(fileInfo.Length);

            // Return
            return fileInfo.Length;
        }

    }
}
