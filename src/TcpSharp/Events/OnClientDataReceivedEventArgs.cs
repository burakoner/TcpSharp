namespace TcpSharp;

public class OnClientDataReceivedEventArgs : EventArgs
{
    public byte[] Data { get; internal set; }
}
