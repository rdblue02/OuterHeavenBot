
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OuterHeavenBot.Lavalink.Entities;
using OuterHeavenBot.Lavalink.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Websocket.Client;

namespace OuterHeavenBot.Lavalink
{
    public interface IHandlerEntry
    {
        Type Type { get; }
        Func<object, Task> Handler { get; }
    }
    public class HandlerEntry<T> : IHandlerEntry
    {
        public Func<object, Task> Handler { get; set; }
        public Type Type { get; } = typeof(T);
    }

    public class LavalinkWebsocket
    { 
        public event EventHandler<WebsocketConnectedEventArgs> OnConnected; 
        public event EventHandler<WebsocketDisconnectedEventArgs> OnDisconnected; 
        public event EventHandler<WebsocketMessageEventArgs> OnMessage; 
        public bool IsConnected => websocket is not null && websocket.IsRunning;
    
        public string OpParam { get; set; } = "op";
 
        public Uri Uri { get; private set; } 
        public Dictionary<string, string> Headers { get; } = new(); 
        private readonly Dictionary<string, IHandlerEntry> _handlers = new();
        private WebsocketClient websocket;
        private ILogger<LavalinkWebsocket> logger;
       
        public LavalinkWebsocket(ILogger<LavalinkWebsocket> logger)
        {
            this.logger = logger;
            this.OnConnected += (object? sender, WebsocketConnectedEventArgs e) => { logger.LogInformation("Websocket Connected"); };
            this.OnDisconnected += (object? sender, WebsocketDisconnectedEventArgs e) => { logger.LogInformation("Websocket Disconnected"); }; ;
        }
        
        public void AddHeader(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(nameof(value));

            Headers.Add(key, value);
        }
         
        public void RemoveHeader(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));

            Headers.Remove(key);
        }
         
        public void ClearHeaders()
        {
            Headers.Clear();
        }
 
        public override string ToString() => Uri.ToString();
         
        public async Task ConnectAsync()
        {
            if(Uri == null) throw new ArgumentNullException(nameof(Uri));

            var factory = new Func<ClientWebSocket>(() =>
            {
                var client = new ClientWebSocket();
                foreach (var (key, value) in Headers)
                {
                    client.Options.SetRequestHeader(key, value);
                }
                return client;
            });

            websocket = new WebsocketClient(Uri, factory);
            websocket.ErrorReconnectTimeout = TimeSpan.FromSeconds(10);
            websocket.ReconnectTimeout = null; //Disable weird reconnect timeout
            websocket.ReconnectionHappened.Subscribe(InternalOnReconnected);
            websocket.DisconnectionHappened.Subscribe(InternalOnDisconnected);
            websocket.MessageReceived.Subscribe(InternalOnMessage);

            await websocket.Start().ConfigureAwait(false);
        } 

        private void InternalOnDisconnected(DisconnectionInfo disconnectionInfo)
        {
             OnDisconnected?.Invoke(this, new WebsocketDisconnectedEventArgs(disconnectionInfo.CloseStatusDescription)); 
        }
        private void InternalOnReconnected(ReconnectionInfo reconnectionInfo)
        {
            OnConnected?.Invoke(this,new WebsocketConnectedEventArgs());
        }

        private void InternalOnMessage(ResponseMessage responseMessage)
        {
            if (responseMessage.MessageType == WebSocketMessageType.Text)
            {
                //Handling op code
                var jObject = JObject.Parse(responseMessage.Text);

                if (!jObject.TryGetValue(OpParam, out _))
                {
                    return;
                } 

                var op = jObject[OpParam].ToString(); 
                 
                if (_handlers.ContainsKey(op))
                {
                    var handler = _handlers[op];
                    var obj = jObject.ToObject(handler.Type);
                    handler.Handler(obj);
                }
            }

            OnMessage?.Invoke(this, new WebsocketMessageEventArgs(responseMessage.MessageType == WebSocketMessageType.Binary, responseMessage.Text));
        }
 
        public void RegisterOp<T>(string op, Func<T, Task> handler)
        {
            _handlers.Add(op, new HandlerEntry<T>
            {
                Handler = (obj) => handler((T)obj)
            });
        }
         
        public void RegisterOp(string op, Func<object, Task> handler)
        {
            _handlers.Add(op, new  HandlerEntry<object>
            {
                Handler = handler
            });
        }
         
        public void UnregisterOp(string op)
        {
            _handlers.Remove(op);
        }
 
        public void UnregisterAllOps()
        {
            _handlers.Clear();
        }

        public void Dispose()
        {
            websocket?.Dispose();
        }
              

        public void Initialize(string uri, string resumeKey, string password, string clientUserId)
        {
            this.Uri = new Uri(uri);
            this.AddHeader("Authorization", password);
            this.AddHeader("client-Name", "DHCPCD9/OuterHeaven");

            if (resumeKey != null)
                this.AddHeader("Resume-Key", resumeKey);

            if (!this.Headers.ContainsKey("User-Id"))
                this.AddHeader("User-Id", clientUserId);
        }
    }
}
