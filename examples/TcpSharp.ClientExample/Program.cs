using System;
using System.Diagnostics;
using System.Text;
using TcpSharp.Events.Client;

namespace TcpSharp.ClientExample
{
    internal class Program
    {
        static TcpSharpSocketClient client;
        static void Main(string[] args)
        {
            client = new TcpSharpSocketClient("127.0.0.1", 1024);
            client.OnConnected += Client_OnConnected;
            client.OnDisconnected += Client_OnDisconnected;
            client.OnDataReceived += Client_OnDataReceived;
            client.OnError += Client_OnError;

            Console.Write("Press <ENTER> to connect");
            Console.ReadLine();
            client.Connect();

            for (var i = 0; i < 10; i++)
            {
                Console.WriteLine($"[{i}] Press <ENTER> to send Hello World!");
                Console.ReadLine();

                client.SendString("Hello World!");
            }

            while (true)
            {
                Console.WriteLine("Press <ENTER> to start speed test");
                Console.ReadLine();
                Console.WriteLine("Starting 1GB test speed test");
                client.OnDataReceived -= Client_OnDataReceived;

                var bytes = Encoding.UTF8.GetBytes("1234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234");
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < 1024 * 1024; i++)
                {
                    client.SendBytes(bytes);
                }
                sw.Stop();
                Console.WriteLine("Finished in " + sw.Elapsed);
            }
        }

        private static void Client_OnError(object sender, OnErrorEventArgs e)
        {
            Console.WriteLine("Client_OnError");
        }

        private static void Client_OnDataReceived(object sender, OnDataReceivedEventArgs e)
        {
            Console.WriteLine("Client_OnDataReceived: " + Encoding.UTF8.GetString(e.Data));
        }

        private static void Client_OnDisconnected(object sender, OnDisconnectedEventArgs e)
        {
            Console.WriteLine("Client_OnDisconnected");
        }

        private static void Client_OnConnected(object sender, OnConnectedEventArgs e)
        {
            Console.WriteLine("Client_OnConnected");
        }
    }
}