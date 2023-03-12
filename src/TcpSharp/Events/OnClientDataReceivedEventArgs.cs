namespace TcpSharp.Events;

public class OnClientDataReceivedEventArgs : EventArgs
{
    public byte[] Data { get; internal set; }
}
