namespace TcpSharp;

public class OnServerErrorEventArgs : EventArgs
{
    public TcpClient Client { get; internal set; }
    public string ConnectionId { get; internal set; }
    public Exception Exception { get; internal set; }
}
