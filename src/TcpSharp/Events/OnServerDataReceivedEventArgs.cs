namespace TcpSharp.Events;

public class OnServerDataReceivedEventArgs : EventArgs
{
    public TcpClient Client { get; internal set; }
    public long ConnectionId { get; internal set; }
    public byte[] Data { get; internal set; }
}
