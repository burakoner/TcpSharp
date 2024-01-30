namespace TcpSharp;

public enum DisconnectReason : byte
{
    None = 0,
    Exception = 1,
    ServerAborted = 2,
    ServerStopped = 3,
}
