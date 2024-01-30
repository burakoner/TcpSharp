namespace TcpSharp;

public class OnClientErrorEventArgs : EventArgs
{
    public Exception Exception { get; internal set; }
}
