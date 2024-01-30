namespace TcpSharp;

public class OnServerDisconnectedEventArgs : EventArgs
{
    public string ConnectionId { get; internal set; }
    public DisconnectReason Reason { get; internal set; }
}
