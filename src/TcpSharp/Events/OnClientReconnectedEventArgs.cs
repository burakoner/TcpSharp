namespace TcpSharp;

public class OnClientReconnectedEventArgs : EventArgs
{
    public IPAddress ServerIPAddress => IPAddress.Parse(ServerHost);
    public string ServerHost { get; internal set; }
    public int ServerPort { get; internal set; }
}
