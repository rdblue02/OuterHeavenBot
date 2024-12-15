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
using System.Threading.Channels;
using System.Net;
using OuterHeavenBot.Lavalink.Constants;
using System.Collections.Concurrent;
namespace OuterHeavenBot.Lavalink
{
    public class LavalinkNode : IDisposable
    {
        public event Func<LavalinkNode, LavalinkNodeConnectedEventArgs, Task>? OnConnected = null;
        public event Func<LavalinkNode, LavalinkClientExceptionEventArgs, Task>? OnException = null;
        public event Func<LavalinkNode, LavalinkNodeReadyEventArgs, Task>? OnReady = null;
        public event Func<LavalinkNode, LavalinkNodeDisconnectedEventArgs, Task>? OnDisconnected = null;

        public LavalinkEndpoint RestEndpoint { get; } = new LavalinkEndpoint("localhost", 2333, "youshallnotpass", "/v4");
        public LavalinkEndpoint WebSocketEndpoint { get; } = new LavalinkEndpoint("localhost", 2333, "youshallnotpass", "/v4/websocket");
        public string NodeName { get; } = "Lava Node";
        public string? ResumeKey { get; private set; } = null;
        public string? SessionId { get; private set; } = null;
        public bool IsReady { get; private set; } = false; 
        public LavalinkStats? Stats { get; private set; } = null;
        public LavalinkRestNode Rest { get; } 
      
        private LavalinkGuildConnection? lavalinkConnection = null;
        private DiscordSocketClient? client = null;  
        private bool nodePreviouslyConnected = false;
        private bool nodeConnected = false;
        private LavalinkVoiceState lavalinkVoiceState = new LavalinkVoiceState(); 
        private IVoiceState? userVoiceState = null;
        private LavalinkWebsocket websocket;
        private ILogger logger;

        public LavalinkNode(ILogger<LavalinkNode> logger,
                            LavalinkWebsocket lavalinkWebsocket)
        {
            this.logger = logger;
            this.websocket = lavalinkWebsocket;
            this.Rest = new LavalinkRestNode(this);
        }

        public async Task Initialize(DiscordSocketClient client,  
                                     string? resumeKey = null,
                                     CancellationToken cancellationToken = default)
        {
            
            this.client = client;  
            this.client.VoiceServerUpdated += this.OnVoiceServerUpdatedAsync;
            this.client.UserVoiceStateUpdated += this.OnUserVoiceStateUpdatedAsync;
            ResumeKey = resumeKey;
            websocket.Initialize(WebSocketEndpoint.ToWebSocketString(), resumeKey, WebSocketEndpoint.Password, client.CurrentUser.Id.ToString());
            
            websocket.RegisterOp<LavalinkStats>("stats", (stats) => { this.Stats = stats; return Task.CompletedTask; });
            websocket.RegisterOp<LavalinkNodeReadyPayload>("ready", Websocket_NodeReady);           
            websocket.RegisterOp("event", Websocket_LavalinkEvent);
            websocket.RegisterOp("playerUpdate", Websocket_PlayerUpdate);             
           
            if (websocket.IsConnected || IsReady) return;

            //Running lavalinkConnection in background to prevent blocking.
            await Task.Run(async () =>
            {
                try
                {
                    if (websocket is null) throw new ArgumentNullException(nameof(websocket));
                    await websocket.ConnectAsync();
                }
                catch
                {
                    logger.LogError(LavalinkEventId.Lavalink, "Failed to connect to node {NodeName} ({ClientShardId})", NodeName, this.client.ShardId);
                }
            }, cancellationToken);
        }

        public async Task<LavalinkGuildConnection?> ConnectPlayerAsync(IVoiceChannel channel, 
                                                                       CancellationToken cancellationToken = default)
        {
            if (!this.IsReady)
            {
                return null;
            }

            if (this.lavalinkConnection != null && 
                this.lavalinkConnection.IsConnected)
            {
                return this.lavalinkConnection;
            }
 
            this.lavalinkVoiceState = new LavalinkVoiceState();
            var connectionTask = channel.ConnectAsync(true, true, true, false);
            
            //make sure we're connected before updating lavalink
            while ((string.IsNullOrWhiteSpace(lavalinkVoiceState.SessionId) || string.IsNullOrWhiteSpace(lavalinkVoiceState.Token)) && 
                   !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(100, cancellationToken);
            }

            await connectionTask;

            await Rest.UpdatePlayer(channel.GuildId, new LavalinkPlayerUpdatePayload()
            {
                VoiceState = lavalinkVoiceState,
            });

            return this.lavalinkConnection;
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

        public LavalinkGuildConnection? GetConntection()
        {
            return this.lavalinkConnection;
        }

        private async Task Websocket_NodeReady(LavalinkNodeReadyPayload payload)
        { 
            this.logger.LogInformation(LavalinkEventId.Lavalink, $"Lavalink node is ready for client {client.CurrentUser.Id} | Session {payload.SessionId} | resumed {payload.Resumed}");
            this.SessionId = payload.SessionId;
            this.IsReady = true;

            await OnReady.FireEventAsync(this, new LavalinkNodeReadyEventArgs(payload.Resumed, payload.SessionId));        
        }

        private Task Websocket_PlayerUpdate(object? arg)
        {
            if (lavalinkConnection == null) return Task.CompletedTask;

            var deserializedArgs = JsonConvert.SerializeObject(arg);
            if (string.IsNullOrWhiteSpace(deserializedArgs)) return Task.CompletedTask;

            var jObject = JObject.Parse(deserializedArgs);
            if (jObject == null) return Task.CompletedTask;

            var state = jObject["state"]?.ToObject<LavalinkPlayerState>();
            if (state == null) return Task.CompletedTask;

            lavalinkConnection.State = state;
            return Task.CompletedTask;
        }

        private async Task Websocket_LavalinkEvent(object arg)
        {
            if (lavalinkConnection == null) throw new InvalidOperationException("lavalinkConnection not found");

            var jObject = JObject.Parse(JsonConvert.SerializeObject(arg)) ?? throw new InvalidOperationException("Invalid lavalink event");

            var guildId = jObject["guildId"].Value<ulong>();
            var typeRaw = jObject["type"].Value<string>();

            Enum.TryParse(typeRaw, true, out LavalinkEventType type);

            if (type == LavalinkEventType.TrackStartEvent)
            {
                var track = jObject.GetValue("track")!.ToObject<LavalinkTrack>();

                await lavalinkConnection.HandleLavalinkEvent(new PlaybackStartedEventArgs(track));
            }
            else if (type == LavalinkEventType.TrackEndEvent)
            {
                var track = jObject.GetValue("track")!.ToObject<LavalinkTrack>();
                var reasonRaw = jObject["reason"]!.Value<string>();
                Enum.TryParse(reasonRaw, true, out LavalinkTrackEndReason reason);

                await lavalinkConnection.HandleLavalinkEvent(new PlaybackFinishedEventArgs(track, reason));
            }
            else if (type == LavalinkEventType.TrackExceptionEvent)
            {
                var track = jObject.GetValue("track")!.ToObject<LavalinkTrack>();
                var error = jObject["exception"].Value<LavalinkException>();
                await lavalinkConnection.HandleLavalinkEvent(new PlaybackExceptionEventArgs(track, error));
            }
            else if (type == LavalinkEventType.TrackStuckEvent)
            {
                var track = jObject.GetValue("track")!.ToObject<LavalinkTrack>();
                var threshold = jObject["thresholdMs"].Value<int>();
                await lavalinkConnection.HandleLavalinkEvent(new PlaybackStuckEventArgs(track, threshold));
            }
            else if (type == LavalinkEventType.WebSocketClosedEvent)
            {
                var code = jObject.GetValue("code")!.Value<int>();
                var reason = jObject.GetValue("reason")!.Value<string>();
                var byRemote = jObject.GetValue("byRemote")!.ToObject<bool>();
                await lavalinkConnection.HandleLavalinkEvent(new PlayerWebsocketClosedEventArgs(code, reason, byRemote));
            }
            else
            {
                this.logger.LogError("Invalid lavalink event type {type}", type);
            }
        }
                
        private Task OnUserVoiceStateUpdatedAsync(SocketUser user,
                                                  SocketVoiceState pastState,
                                                  SocketVoiceState currentState)
        {
            
            //only update when the bot enters or leaves a channel.
            if (user.Id != this.client?.CurrentUser.Id ||    
                (currentState.VoiceSessionId == lavalinkVoiceState.SessionId && currentState.VoiceSessionId != null) ||
                 currentState.VoiceSessionId == null) 
            {
                return Task.CompletedTask;
            }

            lavalinkVoiceState.SessionId = currentState.VoiceSessionId;

            if (user is IVoiceState voice && (this.lavalinkConnection == null || !this.lavalinkConnection.IsConnected))
            {
                logger.LogInformation("Creating lavalink guild connection");
                this.lavalinkConnection = new LavalinkGuildConnection(this, voice);
            } 
          
            logger.LogInformation("Complete voice state task");
            return Task.CompletedTask;
        }

        private Task OnVoiceServerUpdatedAsync(SocketVoiceServer voiceServer)
        {
            lavalinkVoiceState.Endpoint = voiceServer.Endpoint;
            lavalinkVoiceState.Token = voiceServer.Token; 
            logger.LogInformation("Complete voice server task");
            return Task.CompletedTask;
        } 

        public void Disconnect()
        {
            this.lavalinkConnection?.RemoveActiveTrackAsync().GetAwaiter().GetResult();
            if (lavalinkConnection?.VoiceState?.VoiceChannel != null)
            {
                this.lavalinkConnection.LeaveAsync().GetAwaiter().GetResult(); 
                this.lavalinkConnection = null;
            }

            this.lavalinkVoiceState = new LavalinkVoiceState();
            websocket.Dispose(); 
        }

        public void Dispose()
        {
            try
            {
                  Disconnect(); 
              
            }
            catch (Exception ex) 
            {
                logger.LogError(ex.ToString());
            }
            finally
            {
                websocket?.Dispose();
            } 
        }

        public override string ToString() => $"{NodeName} ({RestEndpoint})";
        public string ToWebSocketString() => $"{NodeName} ({WebSocketEndpoint.ToWebSocketString()})";

    }
}