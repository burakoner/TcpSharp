using System;
using TcpSharp.Enums;

namespace TcpSharp.Events.Client
{
    public class OnDisconnectedEventArgs : EventArgs
    {
        public DisconnectReason Reason { get; internal set; }
    }
}
