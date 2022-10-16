using System;
using System.Net;

namespace TcpSharp.Events.Client
{
    public class OnConnectedEventArgs : EventArgs
    {
        public IPAddress ServerIPAddress
        {
            get { return IPAddress.Parse(this.ServerHost); }
        }

        public string ServerHost { get; internal set; }

        public int ServerPort{get; internal set;}
    }
}
