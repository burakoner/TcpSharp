using System;
using System.Net;

namespace TcpSharp.Events.Server
{
    public class OnConnectionRequestEventArgs : EventArgs
    {
        public IPEndPoint IPEndPoint { get; internal set; }
        public string IPAddress { get; internal set; }
        public int Port { get; internal set; }
        public bool Accept { get; set; } = true;
    }
}
