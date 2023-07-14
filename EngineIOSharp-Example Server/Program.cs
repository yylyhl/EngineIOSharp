using EngineIOSharp.Server;
using System;

namespace EngineIOSharp.Example.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            //EngineIOLogger.DoWrite = false;
            using (EngineIOServer server = new EngineIOServer(new EngineIOServerOption(1009, AllowEIO3: true, VerificationTimeout: 1001)))
            {
                Console.WriteLine("On Connecting: " + server.Option.Port);
                server.OnConnecting((obj) =>
                {
                    var token = obj.Item1["token2"];
                    if (string.IsNullOrWhiteSpace(token))
                    {
                        Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} The authentication fails and the connection is denied");
                    }
                    server.VerificationResult.TryAdd(obj.Item2, string.IsNullOrWhiteSpace(token));
                });
                server.OnConnection((socket) =>
                {
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} Client connected!");

                    socket.OnMessage((packet) =>
                    {
                        Console.WriteLine(packet.Data);

                        if (packet.IsText)
                        {
                            socket.Send(packet.Data);
                        }
                        else
                        {
                            socket.Send(packet.RawData);
                        }
                    });

                    socket.OnClose(() =>
                    {
                        Console.WriteLine("Client disconnected!");
                    });

                    socket.Send(new byte[] { 0, 1, 2, 3, 4, 5, 6 });
                });

                server.Start();

                Console.WriteLine("Input /exit to exit program.");
                string line;

                while (!(line = Console.ReadLine())?.Trim()?.ToLower()?.Equals("/exit") ?? false)
                {
                    server.Broadcast(line);
                }
            }

            Console.WriteLine("Press enter to continue...");
            Console.Read();
        }
    }
}
