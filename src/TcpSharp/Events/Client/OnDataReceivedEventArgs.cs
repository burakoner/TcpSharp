using System;

namespace TcpSharp.Events.Client
{
    public class OnDataReceivedEventArgs : EventArgs
    {
        public byte[] Data { get; internal set; }
    }
}
