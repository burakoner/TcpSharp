using System;
using System.Text;

namespace TcpSharp.ServerExample
{

    internal class Program
    {
        static TcpSharpSocketServer server;
        static void Main(string[] args)
        {
            server = new TcpSharpSocketServer();
            server.OnStarted += Server_OnStarted;
            server.OnStopped += Server_OnStopped;
            server.OnConnectionRequest += Server_OnConnectionRequest;
            server.OnConnected += Server_OnConnected;
            server.OnDisconnected += Server_OnDisconnected;
            server.OnDataReceived += Server_OnDataReceived;
            server.OnError += Server_OnError;
            server.StartListening();

            Console.WriteLine("TCP Server is listening on port " + server.Port);


            System.Timers.Timer timer = new System.Timers.Timer(1000);
            timer.Elapsed += Timer_Elapsed;
            //timer.Enabled = true;

            Console.ReadLine();
        }

        private static void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Console.WriteLine($"Received Bytes: {bytesReceived}");
        }

        private static void Server_OnStarted(object sender, OnServerStartedEventArgs e)
        {
            Console.WriteLine("Server_OnStarted");
        }

        private static void Server_OnStopped(object sender, OnServerStoppedEventArgs e)
        {
            Console.WriteLine("Server_OnStopped");
        }
        private static void Server_OnConnectionRequest(object sender, OnServerConnectionRequestEventArgs e)
        {
            Console.WriteLine($"Server_OnConnectionRequest. IPEndPoint: {e.IPEndPoint} Address {e.IPAddress}:{e.Port}");
            //e.Accept = false;
        }

        private static void Server_OnConnected(object sender, OnServerConnectedEventArgs e)
        {
            Console.WriteLine($"Server_OnConnected. ConnectionId: {e.ConnectionId} Address {e.IPAddress}:{e.Port}");
        }

        private static void Server_OnDisconnected(object sender, OnServerDisconnectedEventArgs e)
        {
            Console.WriteLine("Server_OnDisconnected");
        }


        static long bytesReceived = 0;
        private static void Server_OnDataReceived(object sender, OnServerDataReceivedEventArgs e)
        {
            // bytesReceived += e.Data.Length;
            // server.SendBytes(e.ConnectionId, Encoding.UTF8.GetBytes("Sana da selam!"));
            // Console.WriteLine("Server_OnDataReceived: "+ Encoding.UTF8.GetString(e.Data));
            // Console.WriteLine("Server_OnDataReceived: Packet Size: "+ e.Data.Length);
            if (e.Data.Length < 20)
            {
                var data = Encoding.UTF8.GetString(e.Data);
                Console.WriteLine("Server_OnDataReceived: " + data);
                server.SendString(e.ConnectionId, "Echo: " + data);
            }
        }

        private static void Server_OnError(object sender, OnServerErrorEventArgs e)
        {
            Console.WriteLine("Server_OnError");
        }
    }
}