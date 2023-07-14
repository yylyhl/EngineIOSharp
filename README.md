# New
This is a fork of [ EngineIOSharp:master ](https://github.com/uhm0311/EngineIOSharp) with additional support for socket connection pre-processing!
```csharp
using (EngineIOServer server = new EngineIOServer(new EngineIOServerOption(1009, AllowEIO3: true, VerificationTimeout: 1001)))
{
    Console.WriteLine("Listening on " + server.Option.Port);
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
    });

    server.Start();
}
```

# Added
1.EngineIOSharp/Server/EngineIOServer.Exceptions.cs
```csharp
public static readonly EngineIOException CONNECTIONREFUSED = new EngineIOException("ConnectionRefused");//401/403
private static readonly EngineIOException[] VALUES = new EngineIOException[] { CONNECTIONREFUSED }
```
2.EngineIOSharp/Server/EngineIOServer.Event.cs
```csharp
public EngineIOServer OnConnecting(Action<Tuple<System.Collections.Specialized.NameValueCollection, string>> Callback)
{
	return On(Event.CONNECTING, (Client) => Callback(Client as Tuple<System.Collections.Specialized.NameValueCollection, string>));
}
public static class Event
{
	public static readonly string CONNECTING = "connecting";
}
```
3.EngineIOSharp/Common/Static/EngineIOHttpManager.cs
```csharp
public static void SendErrorMessage(HttpListenerRequest Request, HttpListenerResponse Response, EngineIOException Exception)
{
    using (Response)
    {
        ......
        if (EngineIOServer.Exceptions.Contains(Exception))
        {
            ......
            Response.StatusCode = 400;
            Response.Headers["ServerResponse"] = Message;
            Response.StatusCode = Exception == EngineIOServer.Exceptions.CONNECTIONREFUSED ? 401 : 400;
        }
        else
        {
            ......
        }

        ......
    }
}
```
4.EngineIOSharp/Server/EngineIOServerOption.cs
```csharp
public ulong VerificationTimeout { get; private set; }
public EngineIOServerOption(ulong VerificationTimeout = 1000)
{
	this.VerificationTimeout = VerificationTimeout;
}
```
5.EngineIOSharp/Server/EngineIOServer.cs
```csharp
/// <summary>
/// Verification result: If no entry is made within 1000ms, it is regarded as passed
/// </summary>
public readonly ConcurrentDictionary<string, bool> VerificationResult = new ConcurrentDictionary<string, bool>();
private EngineIOException Verify(NameValueCollection QueryString, NameValueCollection Headers, EngineIOTransportType ExpectedTransportType)
{
    ......
    else if (EngineIOHttpManager.GetTransport(QueryString).Equals(ExpectedTransportType.ToString()))
    {
        ......
        if (IsPolling || IsWebSocket)
        {
            if (EngineIOHttpManager.IsValidHeader(EngineIOHttpManager.GetOrigin(Headers)))
            {
                Exception = null;
                #region connection verification
				if (string.IsNullOrWhiteSpace(EngineIOHttpManager.GetSID(QueryString)))
				{
					var abortId = $"{QueryString}&k={DateTime.Now.Ticks}";
					Emit(Event.CONNECTING, new Tuple<NameValueCollection, string>(Headers, abortId));

#if NET45_OR_GREATER
				//var task = System.Threading.Tasks.Task.Run(() => { while (!VerificationResult.ContainsKey(abortId)) { continue; } });
				//System.Threading.Tasks.Task.WaitAny(new System.Threading.Tasks.Task[] { task }, TimeSpan.FromMilliseconds(Option.VerificationTimeout));

				var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromMilliseconds(Option.VerificationTimeout));
				while (!VerificationResult.ContainsKey(abortId) && !cts.IsCancellationRequested) { continue; }
#else
					var cts = new System.Threading.CancellationTokenSource();
					var waitTimer = new System.Timers.Timer(Option.VerificationTimeout) { AutoReset = false };
					waitTimer.Elapsed += (aa, bb) => cts.Cancel();
					waitTimer.Start();
					while (!VerificationResult.ContainsKey(abortId) && !cts.IsCancellationRequested) { continue; }
#endif
					if (VerificationResult.TryGetValue(abortId, out var result))
					{
						VerificationResult.TryRemove(abortId, out _);
						if (result) Exception = Exceptions.CONNECTIONREFUSED;
					}
				}
				#endregion
            }
            else
            {
                Exception = Exceptions.BAD_REQUEST;
            }
        }
    }

    return Exception;
}
```


# EngineIOSharp
`EngineIOSharp` is a **Engine.IO Protocol revision `4` client and server** library based on `WebSocket` or `Long-Polling` transport. It depends on [WebSocketSharp](https://github.com/uhm0311/websocket-sharp) to use `WebSocket` transport. `Long-Polling` transport is implemented by itself.

# Installation
- [Nuget gallery](https://www.nuget.org/packages/EngineIOSharp2)

- Command `Install-Package EngineIOIOSharp2` in nuget package manager console.

# Usage

## Client

### Namespace

```csharp
using EngineIOSharp.Client;
using EngineIOSharp.Common.Enum;
```

### Constructor

```csharp
EngineIOClient client = new EngineIOClient(new EngineIOClientOption(EngineIOScheme.http, "localhost", 1009)));
```

#### EngineIOClientOption

  - **Essential Parameters**
  
    - `Scheme` : Scheme to connect to. It can be `EngineIOScheme.http` or `EngineIOScheme.https`. Internally, it supports `ws` and `wss`.
    
    - `Host` : Host to connect to.
    
    - `Port` : Port to connect to.
    
  - **Optional Parameters**
  
    - `PolicyPort` : Port the policy server listens on. Defaults to `843`.
    
    - `Path` : Path to connect to. Defaults to `"/engine.io"`.
    
    - `Query` : Parameters that will be passed for each request to the server. Defaults to `null`.
    
    - `Upgrade` : Whether the client should try to upgrade the transport. Defaults to `true`.
    
    - `RemeberUpgrade` : Whether the client should bypass normal upgrade process when previous websocket connection is succeeded. Defaults to `false`.
    
    - `ForceBase64` : Forces base 64 encoding for transport. Defaults to `false`.
    
    - `WithCredentials` : Whether to include credentials such as cookies, authorization headers, TLS client certificates, etc. with polling requests. Defaults to `false`.
    
    - `TimestampRequests` : Whether to add the timestamp with each transport request. Polling requests are always stamped. Defaults to `null`.
    
    - `TimestampParam` : Timestamp parameter. Defaults to `"t"`.
    
    - `Polling` : Whether to include polling transport. Defaults to `true`.
    
    - `PollingTimeout` : Timeout for polling requests in milliseconds. Defaults to `0`, which waits indefinitely.
    
    - `WebSocket` : Whether to include websocket transport. Defaults to `true`.
    
    - `WebSocketSubprotocols` : List of websocket subprotocols. Defaults to `null`.
    
    - `ExtraHeaders` : Headers that will be passed for each request to the server. Defaults to `null`.
    
    - `ClientCertificates` : The collection of security certificates that are associated with each request. Defaults to `null`.
    
    - `ClientCertificateSelectionCallback` : Callback used to select the certificate to supply to the server. Defaults to `null`.
    
    - `ServerCertificateValidationCallback` : Callback method to validate the server certificate. Defaults to `null` and server certificate will be always validated.

### Connect

```csharp
client.Connect();
```

### Disconnect

```csharp
client.Close();
```

or

```csharp
client.Dispose();
```

Since `EngineIOClient` implements `IDisposable` interface, it will be automatically disconnected when `EngineIOClient.Dispose` is called.

### Handlers

For convenient usage, it is implemented to can be used as `Javascript` style.

```csharp
client.OnOpen(() =>
{
  Console.WriteLine("Conencted!");
});

client.OnMessage((Packet) =>
{
  Console.WriteLine("Server : " + Packet.Data);
});

client.OnClose((Exception Description) =>
{
  Console.WriteLine("Disconnected!");
  
  if (Description != null)
  {
    Console.WriteLine(Description);
  }
});

client.On(EngineIOClient.Event.FLUSH, () =>
{
  Console.WriteLine("Flushed!");
});
```

#### EngineIOClient.Event

```csharp
public static class Event
{
  public static readonly string OPEN = "open";
  public static readonly string HANDSHAKE = "handshake";

  public static readonly string ERROR = "error";
  public static readonly string CLOSE = "close";

  public static readonly string PACKET = "packet";
  public static readonly string MESSAGE = "message";

  public static readonly string PACKET_CREATE = "packetCreate";
  public static readonly string FLUSH = "flush";
  public static readonly string DRAIN = "drain";

  public static readonly string UPGRADE = "upgrade";
  public static readonly string UPGRADING = "upgrading";
  public static readonly string UPGRADE_ERROR = "upgradeError";
}
```

These are the common basic `Engine.IO` events.

### Send

```csharp
client.Send("This is string value.");
client.Send("String value with callback.", () => Console.WriteLine("Flushed!"));

client.send(new byte[] { 1, 2, 3, 4, 5 });
client.send(new byte[] { 6, 7, 8, 9, 0 }, () => Console.WriteLine("Bytes flushed!"));
```

## Server

### Namespace

```csharp
using EngineIOSharp.Server;
```

### Constructor
```csharp
EngineIOServer server = new EngineIOServer(new EngineIOServerOption(1009));
```

#### EngineIOServerOption

  - **Essential Parameters**
  
    - `Port` : Port to listen.
  
  - **Optional Parameters**
  
    - `Path` : Path to listen. Defaults to `"/engine.io"`.
    
    - `Secure` : Whether to secure connections. Defatuls to `false`.
    
    - `PingTimeout` : How many ms without a pong packet to consider the connection closed. Defatuls to `5000`.
    
    - `PingInterval` : How many ms before sending a new ping packet. Defatuls to `25000`.
    
    - `UpgradeTimeout` : How many ms before an uncompleted transport upgrade is cancelled. Defatuls to `10000`.
    
    - `Polling` : Whether to accept polling transport. Defatuls to `true`.
    
    - `WebSocket` : Whether to accept websocket transport. Defatuls to `true`.
    
    - `AllowUpgrade` : Whether to allow transport upgrade. Defatuls to `true`.
    
    - `SetCookie` : Whether to use cookie. Defatuls to `true`.
    
    - `SIDCookieName` : Name of sid cookie. Defatuls to `"io"`.
    
    - `Cookies` : Configuration of the cookie that contains the client sid to send as part of handshake response headers. This cookie might be used for sticky-session. Defatuls to `null`.
    
    - `AllowHttpRequest` : A function that receives a given handshake or upgrade http request as its first parameter, and can decide whether to continue or not. Defatuls to `null`.
    
    - `AllowWebSocket` : A function that receives a given handshake or upgrade websocket connection as its first parameter, and can decide whether to continue or not. Defatuls to `null`.
    
    - `InitialData` : An optional packet which will be concatenated to the handshake packet emitted by Engine.IO. Defatuls to `null`.
    
    - `ServerCertificate` : The certificate used to authenticate the server. Defatuls to `null`.
    
    - `ClientCertificateValidationCallback` : Callback used to validate the certificate supplied by the client. Defatuls to `null` and  client certificate will be always validated.
    
### Start

```csharp
server.Start();
```

### Stop

```csharp
server.Stop();
```

or

```csharp
server.Dispose();
```

Since `EngineIOServer` implements `IDisposable` interface, it will be automatically stoped when `EngineIOServer.Dispose` is called.

### Connection

For convenient usage, it is implemented to can be used as `Javascript` style.

```csharp
server.OnConnection((EngineIOSocket socket) =>
{
  Console.WriteLine("Client connected!");

  socket.OnMessage((Packet) =>
  {
    Console.WriteLine(Packet.Data);

    if (Packet.IsText)
    {
      socket.Send(Packet.Data);
    }
    else
    {
      socket.Send(Packet.RawData);
    }
  });

  socket.OnClose(() =>
  {
    Console.WriteLine("Client disconnected!");
  });

  socket.Send("Hello client!");
  socket.Send(new byte[] { 0, 1, 2, 3, 4, 5, 6 });
});
```

#### EngineIOSocket

- `EngineIOSocket` is a type of parameter in `EngineIOServer.OnConnection` event callback. It can be used similarly as `EngineIOClient`.

##### Disconnect

```csharp
socket.Close();
```

or

```csharp
socket.Dispose();
```

Since `EngineIOSocket` implements `IDisposable` interface, it will be automatically disconnected when `EngineIOSocket.Dispose` is called.

##### Handlers

For convenient usage, it is implemented to can be used as `Javascript` style.

```csharp
socket.OnMessage((Packet) =>
{
  Console.WriteLine("Server : " + Packet.Data);
});

socket.OnClose((string Messsage, Exception Description) =>
{
  Console.WriteLine("Disconnected!");
  
  if (Message != null) 
  {
    Console.WriteLine(Message);
  }
  
  if (Description != null)
  {
    Console.WriteLine(Description);
  }
});

socket.On(EngineIOSocket.Event.FLUSH, () =>
{
  Console.WriteLine("Flushed!");
});
```

- There is no `EngineIOSocket.OnOpen` event since it is already opened when `EngineIOServer.OnConnection` event callback is called.

###### EngineIOSocket.Event

```csharp
public static class Event
{
  public static readonly string OPEN = "open";
  public static readonly string HEARTBEAT = "heartbeat";

  public static readonly string ERROR = "error";
  public static readonly string CLOSE = "close";

  public static readonly string PACKET = "packet";
  public static readonly string MESSAGE = "message";

  public static readonly string PACKET_CREATE = "packetCreate";
  public static readonly string FLUSH = "flush";
  public static readonly string DRAIN = "drain";

  public static readonly string UPGRADE = "upgrade";
  public static readonly string UPGRADING = "upgrading";
  public static readonly string UPGRADE_ERROR = "upgradeError";
}
```

##### Send

```csharp
socket.Send("This is string value.");
socket.Send("String value with callback.", () => Console.WriteLine("Flushed!"));

socket.send(new byte[] { 1, 2, 3, 4, 5 });
socket.send(new byte[] { 6, 7, 8, 9, 0 }, () => Console.WriteLine("Bytes flushed!"));
```

### Broadcast

```csharp
server.Braodcast("This is string value.");
server.Braodcast("String value with callback.", () => Console.WriteLine("Braodcasted!"));

server.Braodcast(new byte[] { 1, 2, 3, 4, 5 });
server.Braodcast(new byte[] { 6, 7, 8, 9, 0 }, () => Console.WriteLine("Bytes braodcasted!"));
```

# Maintenance

Welcome to report issue or create pull request. I will check it happily.

# Dependencies
- [WebSocketSharp.CustomHeaders.CustomHttpServer v1.0.2.3](https://github.com/uhm0311/websocket-sharp)
- [SimpleThreadMonitor v1.0.2.1](https://github.com/uhm0311/SimpleThreadMonitor)
- [Newtonsoft.Json v9.0.1](https://github.com/JamesNK/Newtonsoft.Json)
- [EmitterSharp v1.1.1.1](https://github.com/uhm0311/EmitterSharp)

# License
`EngineIOSharp` is under [The MIT License](https://github.com/yylyhldev/EngineIOSharp/blob/master/LICENSE).
