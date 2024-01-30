namespace TcpSharp;

public class OnServerStartedEventArgs : EventArgs
{
    public bool IsStarted { get; internal set; }
}
