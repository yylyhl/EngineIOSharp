using EngineIOSharp.Client;
using EngineIOSharp.Common.Enum;
using System;
using System.Text;

namespace EngineIOSharp.Example.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            //EngineIOLogger.DoWrite = false;
            var ExtraHeaders = new System.Collections.Generic.Dictionary<string, string>()
            {
                { "token", "tokenVal-" },
                { "matchId", "123456-" }
            };
            using (EngineIOClient client = new EngineIOClient(new EngineIOClientOption(EngineIOScheme.http, "localhost", 1009, ExtraHeaders: ExtraHeaders)))
            {
                client.OnOpen(() =>
                {
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} Conencted!");
                });

                client.OnMessage((Packet) =>
                {
                    Console.WriteLine("Server : " + Packet.Data);
                });

                client.OnClose(() =>
                {
                    Console.WriteLine("Disconnected!");
                });

                client.Connect();

                Console.WriteLine("Input /exit to close program.");
                string line;

                while (!(line = Console.ReadLine())?.Trim()?.ToLower()?.Equals("/exit") ?? false)
                {
                    client.Send("Client says, ");
                    client.Send(line);

                    client.Send("And this is also with hex decimal, ");
                    client.Send(Encoding.UTF8.GetBytes(line));
                }
            }

            Console.WriteLine("Press enter to continue...");
            Console.Read();
        }
    }
}
