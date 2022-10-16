using System;

namespace TcpSharp.Events.Server
{
    public class OnStartedEventArgs : EventArgs
    {
        public bool IsStarted { get; internal set; }
    }
}
