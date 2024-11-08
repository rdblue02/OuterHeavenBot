using Discord;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using OuterHeavenBot.Lavalink.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using OuterHeavenBot.Lavalink.Entities;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
namespace OuterHeavenBot.Lavalink
{

    public class LavalinkNode : IDisposable
    { 
        public LavalinkEndpoint RestEndpoint { get; private set; } 
        public LavalinkEndpoint WebSocketEndpoint { get; private set; } 
        public string NodeName { get; set; }
        public event Func<LavalinkNode,LavalinkNodeConnectedEventArgs,Task> OnConnected;
        public event Func<LavalinkNode, LavalinkClientExceptionEventArgs, Task> OnException;
        public event Func<LavalinkNode,LavalinkNodeReadyEventArgs, Task> OnReady;
        public event Func<LavalinkNode, LavalinkNodeDisconnectedEventArgs, Task> OnDisconnected;
         
        public string ResumeKey { get; set; }
        public string SessionId { get; private set; } 
        public bool IsReady { get; set; } 
        public LavalinkStats Stats { get; set; }  
        internal DiscordSocketClient DiscordClient { get; set; }  
        internal LavalinkRestNode Rest { get; private set; }
        internal LavalinkGuildConnection Connection { get; private set; }
      
        private LavalinkWebsocket _websocket;
        private Queue< TaskCompletionSource<VoiceServerUpdateEventArgs>> _voiceServerUpdateTasks = new();
        private Queue<TaskCompletionSource<VoiceStateUpdateEventArgs>> _voiceStateUpdateTasks =  new();
        private bool _previouslyConnected = false;
        private int _reconnectAttempts = 0;
        private ILogger _logger;

        public LavalinkNode(ILogger<LavalinkNode> logger)
        {
            this._logger = logger;
        }
 
        public void Initialize(LavalinkEndpoint restEndpoint, LavalinkEndpoint webSocketEndpoint, string nodeName = "Lava Node", string resumeKey = null)
        {
            RestEndpoint = restEndpoint ?? throw new ArgumentNullException(nameof(restEndpoint));
            WebSocketEndpoint = webSocketEndpoint ?? throw new ArgumentNullException(nameof(webSocketEndpoint));

            _websocket = new LavalinkWebsocket(WebSocketEndpoint.ToWebSocketString());
            NodeName = nodeName;
            ResumeKey = resumeKey;
            Rest = new LavalinkRestNode(this);

            _websocket.AddHeader("Authorization", WebSocketEndpoint.Password);
            _websocket.AddHeader("Client-Name", "DHCPCD9/Nomia");

            if (ResumeKey != null)
            {
                _websocket.AddHeader("Resume-Key", ResumeKey);
            }

            _websocket.OnConnected += websocketOnOnConnected;
            _websocket.OnDisconnected += websocketOnOnDisconnected;

            _websocket.RegisterOp<LavalinkNodeReadyPayload>("ready", Websocket_NodeReady);
            _websocket.RegisterOp("event", Websocket_LavalinkEvent);
            _websocket.RegisterOp("playerUpdate", Websocket_PlayerUpdate);
            _websocket.RegisterOp<LavalinkStats>("stats", Websocket_Stats);
        }

        public async Task<LavalinkGuildConnection> ConnectAsync(IVoiceChannel channel)
        {
            var channelType = channel?.GetChannelType() ?? throw new ArgumentNullException(nameof(ChannelType));

            if (channelType != ChannelType.Voice && channelType != ChannelType.Stage) throw new ArgumentException("VoiceState must be a voice channel.", nameof(channel)); 
           
            if (Connection.VoiceState.VoiceChannel.Guild.Id != channel.GuildId)
                throw new InvalidOperationException("Cannot connect to more than one guild at a time!");

            if (Connection?.VoiceState?.VoiceChannel.Id == channel.Id) return Connection; 
           
            var vstu = new TaskCompletionSource<VoiceStateUpdateEventArgs>();
            var vsru = new TaskCompletionSource<VoiceServerUpdateEventArgs>();

            _voiceStateUpdateTasks.Enqueue(vstu);
            _voiceServerUpdateTasks.Enqueue(vsru);

            var vsd = new VoiceDispatch
            {
                OpCode = 4,
                Payload = new VoiceStateUpdatePayload
                {
                    GuildId = channel.Guild.Id,
                    ChannelId = channel.Id,
                    Deafened = false,
                    Muted = false,
                }
            };

            var vsj = JsonConvert.SerializeObject(vsd);

            DiscordWsSendAsync(vsj);

            if (!vstu.Task.Wait(TimeSpan.FromSeconds(10)) && !vsru.Task.Wait(TimeSpan.FromSeconds(10)))
            {
                throw new TimeoutException("Voice state update timed out.");
            }

            var vsu = await _voiceStateUpdateTasks.Dequeue().Task.ConfigureAwait(false);
            var vsr = await _voiceServerUpdateTasks.Dequeue().Task.ConfigureAwait(false);

            var connection = new LavalinkGuildConnection(this, vsr, vsu);  
            var sessionId = vsu.Channel.VoiceSessionId;
            var endpoint = vsr.Endpoint;
            var token = vsr.VoiceToken;

            //Creating player
            await Rest.UpdatePlayer(channel.Guild.Id, new LavalinkPlayerUpdatePayload
            {
                VoiceState = new LavalinkVoiceState
                {
                    SessionId = sessionId,
                    Token = token,
                    Endpoint = endpoint,
                }
            });

            Connection = connection;
            return connection;
        }

        internal void DiscordWsSendAsync(string payload)
        {
             DiscordClient?.Guilds?.FirstOrDefault()?.Channels?.OfType<IVoiceChannel>()?.FirstOrDefault()?.DisconnectAsync()?.ConfigureAwait(false).GetAwaiter().GetResult();
            //var method = DiscordClient.GetType().GetMethod("SendRawPayloadAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            //method.Invoke(DiscordClient, new object[] { payload });
        }

    
        public override string ToString() => $"{NodeName} ({RestEndpoint})";
        public string ToWebSocketString() => $"{NodeName} ({WebSocketEndpoint.ToWebSocketString()})";

        public async Task DestroyPlayer(IVoiceChannel voiceChannel)
        {
            if(Connection == null ) return;
            if (voiceChannel.GetChannelType() != ChannelType.Voice && voiceChannel.GetChannelType() != ChannelType.Stage) throw new ArgumentException("VoiceState must be a voice channel.", nameof(voiceChannel));
             
            await Connection.DisconnectAsync();
        }

        public async Task<LavalinkLoadable> LoadTrackAsync(string query, LavalinkSearchType searchType = LavalinkSearchType.Youtube)
        {
            var prefix = searchType switch
            {
                LavalinkSearchType.Youtube => "ytsearch:",
                LavalinkSearchType.Soundcloud => "scsearch:",
                LavalinkSearchType.Raw => "",
                _ => throw new ArgumentOutOfRangeException(nameof(searchType), searchType, null)
            };
            var queryRaw = $"{prefix}{query}";
            return await Rest.ResolveTracks(queryRaw); ;
        }

        public void Disconnect()
        {
            _websocket.Dispose();
        }
         
        private void websocketOnOnDisconnected(object? sender, WebsocketDisconnectedEventArgs e)
        {
             this._logger.LogWarning( "Node {NodeName} disconnected, reconnecting in 5 seconds", NodeName);
            OnDisconnected.Invoke(this, new LavalinkNodeDisconnectedEventArgs());
        }
        private async Task Websocket_Stats(LavalinkStats? stats)
        {
            Stats = stats;
        }

        private async Task Websocket_PlayerUpdate(object? arg)
        {
            if (Connection == null)
            {
                return;
            }

            var jObject = JObject.Parse(JsonConvert.SerializeObject(arg)); 
            var state = jObject["state"].ToObject<LavalinkPlayerState>();

            Connection.State = state;
        }

        private async Task Websocket_LavalinkEvent(object arg)
        {
            if (Connection == null)
            {
                throw new InvalidOperationException("Connection not found");
            }

            var jObject = JObject.Parse(JsonConvert.SerializeObject(arg));

            var guildId = jObject["guildId"].Value<ulong>();  
            var typeRaw = jObject["type"].Value<string>();

            Enum.TryParse(typeRaw, true, out LavalinkEventType type);

            if (type == LavalinkEventType.TrackStartEvent)
            {
                var track = jObject.GetValue("track")!.ToObject<LavalinkTrack>();

              await  Connection.HandleLavalinkEvent(new PlaybackStartedEventArgs(track));
            }

            if (type == LavalinkEventType.TrackEndEvent)
            {
                var track = jObject.GetValue("track")!.ToObject<LavalinkTrack>();
                var reasonRaw = jObject["reason"]!.Value<string>();
                Enum.TryParse(reasonRaw, true, out LavalinkTrackEndReason reason);

              await  Connection.HandleLavalinkEvent(new PlaybackFinishedEventArgs(track, reason));
            }

            if (type == LavalinkEventType.TrackExceptionEvent)
            {
                var track = jObject.GetValue("track")!.ToObject<LavalinkTrack>();
                var error = jObject["exception"].Value<LavalinkException>();
              await  Connection.HandleLavalinkEvent(new PlaybackExceptionEventArgs(track, error));
            }

            if (type == LavalinkEventType.TrackStuckEvent)
            {
                var track = jObject.GetValue("track")!.ToObject<LavalinkTrack>();
                var threshold = jObject["thresholdMs"].Value<int>();
              await  Connection.HandleLavalinkEvent(new PlaybackStuckEventArgs(track, threshold));
            }

            if (type == LavalinkEventType.WebSocketClosedEvent)
            {
                var code = jObject.GetValue("code")!.Value<int>();
                var reason = jObject.GetValue("reason")!.Value<string>();
                var byRemote = jObject.GetValue("byRemote")!.ToObject<bool>();
              await  Connection.HandleLavalinkEvent(new PlayerWebsocketClosedEventArgs(code, reason, byRemote));
            }
        }

        private async Task Websocket_NodeReady(LavalinkNodeReadyPayload payload)
        {
            SessionId = payload.SessionId;
            IsReady = true; 
            await HandleEventAsync<LavalinkClientExceptionEventArgs>(OnReady.GetInvocationList(), new LavalinkNodeReadyEventArgs(payload.Resumed, payload.SessionId));
            this._logger.LogInformation(LavalinkEvent.Lavalink, "Node {NodeName} is ready ({ClientShardId})", NodeName, DiscordClient.ShardId); 
        }

        private void websocketOnOnConnected(object? sender, WebsocketConnectedEventArgs e)
        {
            this.OnConnected.Invoke(this, new LavalinkNodeConnectedEventArgs(this));
        }

        private async Task InternalHandleException(Exception exception)
        {
            await HandleEventAsync<LavalinkClientExceptionEventArgs>(OnException.GetInvocationList(), new LavalinkClientExceptionEventArgs(exception));
        }

        internal async Task ConnectNodeAsync(DiscordSocketClient client)
        { 
            if (IsReady) return; 
            if (client is null) throw new ArgumentNullException(nameof(client));
            if (_websocket is null) throw new ArgumentNullException(nameof(_websocket));
            if (client.CurrentUser is null) throw new InvalidOperationException("Client is not ready yet."); 
            DiscordClient = client;

            if (!_websocket.Headers.ContainsKey("User-Id"))
                _websocket.AddHeader("User-Id", DiscordClient.CurrentUser.Id.ToString());

            client.VoiceServerUpdated += Client_VoiceServerUpdated;
            client.UserVoiceStateUpdated += Client_UserVoiceStateUpdated;

            if (_websocket.IsConnected) return;

            //Running connection in background to prevent blocking.
            await Task.Run(async () =>
            {
                try
                {
                    if(_websocket is null) throw new ArgumentNullException(nameof(_websocket));
                    await _websocket.ConnectAsync();
                }
                catch
                {
                     _logger.LogError(LavalinkEvent.Lavalink, "Failed to connect to node {NodeName} ({ClientShardId})", NodeName, DiscordClient.ShardId);
                }
            });
        }

        private Task Client_UserVoiceStateUpdated(SocketUser arg1, SocketVoiceState arg2, SocketVoiceState arg3)
        {
            var channel = (arg2.VoiceChannel ?? arg3.VoiceChannel) as IVoiceState;

            if (channel?.VoiceChannel?.Guild is null) return Task.CompletedTask;

            if (arg1.Username != DiscordClient.CurrentUser.Username) return Task.CompletedTask; ;

            if (_voiceStateUpdateTasks.TryDequeue(out var task))
            {
                task.SetResult(new VoiceStateUpdateEventArgs()
                {
                    Guild = channel.VoiceChannel.Guild,
                    Channel = channel
                });
            }

            return Task.CompletedTask;
        }

        private Task Client_VoiceServerUpdated(SocketVoiceServer arg)
        {
            if (arg?.Guild.Id is null) return Task.CompletedTask;

            if (_voiceServerUpdateTasks.TryDequeue(out var task))
            {
                task.SetResult(new VoiceServerUpdateEventArgs()
                {
                    Endpoint = arg.Endpoint,
                    VoiceToken = arg.Token
                });
            }

            return Task.CompletedTask;
        } 

        async Task HandleEventAsync<T>(Delegate[] delegates, object args)
        {
            var tasks = new Task[delegates.Length];
            for (int i = 0; i < tasks.Length; i++)
            {
                var task = ((Func<object, T, Task>)delegates[i])(this, (T)args);
                tasks[i] = task;
            }

            await Task.WhenAll(tasks);
        }

        public void Dispose()
        {
            _websocket?.Dispose();
        } 
    }
}