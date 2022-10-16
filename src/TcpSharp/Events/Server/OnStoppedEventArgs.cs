using System;

namespace TcpSharp.Events.Server
{
    public class OnStoppedEventArgs : EventArgs
    {
        public bool IsStopped { get; internal set; }
    }
}
