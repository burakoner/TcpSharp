namespace TcpSharp;

public class OnServerStoppedEventArgs : EventArgs
{
    public bool IsStopped { get; internal set; }
}
