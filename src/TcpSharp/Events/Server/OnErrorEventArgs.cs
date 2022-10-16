using System;
using System.Net.Sockets;

namespace TcpSharp.Events.Server
{
    public class OnErrorEventArgs : EventArgs
    {
        public TcpClient Client { get; internal set; }
        public long ConnectionId { get; internal set; }
        public Exception Exception { get; internal set; }
    }
}
