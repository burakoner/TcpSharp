namespace TcpSharp.Events;

public class OnServerErrorEventArgs : EventArgs
{
    public TcpClient Client { get; internal set; }
    public long ConnectionId { get; internal set; }
    public Exception Exception { get; internal set; }
}
