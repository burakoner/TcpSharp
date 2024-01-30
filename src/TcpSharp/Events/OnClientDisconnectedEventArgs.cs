namespace TcpSharp;

public class OnClientDisconnectedEventArgs : EventArgs
{
    public DisconnectReason Reason { get; internal set; }
}
