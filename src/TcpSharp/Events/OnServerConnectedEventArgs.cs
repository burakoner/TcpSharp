namespace TcpSharp;

public class OnServerConnectedEventArgs : EventArgs
{
    public IPEndPoint IPEndPoint { get; internal set; }
    public string IPAddress { get; internal set; }
    public int Port { get; internal set; }
    public string ConnectionId { get; internal set; }
}
