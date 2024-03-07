namespace TcpSharp;

public class OnClientDataReceivedEventArgs : EventArgs, IDisposable
{
    public byte[] Data { get; internal set; }

    ~OnClientDataReceivedEventArgs()
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
            Data = null;
        }

        // Free any unmanaged objects here.
    }
}
