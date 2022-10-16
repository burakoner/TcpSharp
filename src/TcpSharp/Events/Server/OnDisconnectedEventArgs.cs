using System;
using TcpSharp.Enums;

namespace TcpSharp.Events.Server
{
    public class OnDisconnectedEventArgs : EventArgs
    {
        public long ConnectionId { get; internal set; }
        public DisconnectReason Reason { get; internal set; }
    }
}
