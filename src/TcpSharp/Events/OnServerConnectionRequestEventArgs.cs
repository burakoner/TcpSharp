namespace TcpSharp;

public class OnServerConnectionRequestEventArgs : EventArgs
{
    public IPEndPoint IPEndPoint { get; internal set; }
    public string IPAddress { get; internal set; }
    public int Port { get; internal set; }
    public bool Accept { get; set; } = true;
}
