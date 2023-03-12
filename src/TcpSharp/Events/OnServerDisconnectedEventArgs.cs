namespace TcpSharp.Events;

public class OnServerDisconnectedEventArgs : EventArgs
{
    public long ConnectionId { get; internal set; }
    public DisconnectReason Reason { get; internal set; }
}
