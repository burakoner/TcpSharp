using System;
using System.Net.Sockets;

namespace TcpSharp.Events.Server
{
    public class OnDataReceivedEventArgs : EventArgs
    {
        public TcpClient Client { get; internal set; }
        public long ConnectionId { get; internal set; }
        public byte[] Data { get; internal set; }
    }
}
