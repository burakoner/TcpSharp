namespace TcpSharp;

public class OnServerDataReceivedEventArgs : EventArgs, IDisposable
{
    public TcpClient Client { get; internal set; }
    public string ConnectionId { get; internal set; }
    public byte[] Data { get; internal set; }

    ~OnServerDataReceivedEventArgs()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Free any other managed objects here.
            Client?.Dispose();
            Data = null;
        }

        // Free any unmanaged objects here.
    }
}
