namespace TcpSharp.Events;

public class OnServerConnectedEventArgs : EventArgs
{
    public IPEndPoint IPEndPoint { get; internal set; }
    public string IPAddress { get; internal set; }
    public int Port { get; internal set; }
    public long ConnectionId { get; internal set; }
}
