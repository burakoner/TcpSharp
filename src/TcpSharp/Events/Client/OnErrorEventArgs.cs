using System;

namespace TcpSharp.Events.Client
{
    public class OnErrorEventArgs : EventArgs
    {
        public Exception Exception { get; internal set; }
    }
}
