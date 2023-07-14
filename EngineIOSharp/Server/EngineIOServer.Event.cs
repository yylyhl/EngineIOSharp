using EngineIOSharp.Server.Client;
using System;

namespace EngineIOSharp.Server
{
    partial class EngineIOServer
    {
        public EngineIOServer OnConnection(Action<EngineIOSocket> Callback)
        {
            return On(Event.CONNECTION, (Client) => Callback(Client as EngineIOSocket));
        }
        public EngineIOServer OnConnecting(Action<Tuple<System.Collections.Specialized.NameValueCollection, string>> Callback)
        {
            return On(Event.CONNECTING, (Client) => Callback(Client as Tuple<System.Collections.Specialized.NameValueCollection, string>));
        }

        public static class Event
        {
            public static readonly string CONNECTING = "connecting";
            public static readonly string CONNECTION = "connection";
            public static readonly string FLUSH = "flush";
            public static readonly string DRAIN = "drain";
        }
    }
}
