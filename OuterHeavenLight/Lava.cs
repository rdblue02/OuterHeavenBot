using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging; 
using System;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Nodes;
using Websocket.Client; 
using Newtonsoft.Json.Linq; 
using System.Collections.Concurrent;
using OuterHeavenLight.Entities;
using OuterHeavenLight.Entities.Response.Websocket;
using OuterHeavenLight.Entities.Request;
using OuterHeavenLight.Entities.Response.Rest;
using Microsoft.VisualBasic;
using System.Reactive;
using OuterHeavenLight;

namespace OuterHeaven.LavalinkLight
{
    public class Lava : IDisposable
    {
        public event Func<TrackStartWebsocketEvent, Task>? OnLavaTrackStartEvent;
        public event Func<TrackEndWebsocketEvent, Task>? OnLavaTrackEndEvent;
        public event Func<TrackExceptionWebsocketEvent, Task>? OnLavaTrackExceptionEvent;
        public event Func<TrackStuckWebsocketEvent, Task>? OnLavaTrackStuckEvent;
        public event Func<ClosedWebsocketEvent, Task>? OnLavaConnectionClosed;
        public event Func<PlayerUpdateWebsocketMessage, Task>? OnPlayerUpdate; 

        private WebsocketClient? websocket;
        private ILogger<Lava> logger; 
        private LavaStatsWebsocketMessage stats = new LavaStatsWebsocketMessage(); 
        private DiscordSocketClient discordClient;
        private AppSettings settings;
        private LavalinkEndpointProvider endpointProvider;
        private LavalinkRestNode restNode;
        private ConcurrentQueue<Func<Task>> pendingUpdates = new ConcurrentQueue<Func<Task>>(); 
        private VoiceState voiceState = new VoiceState(); 
        private LavaPlayer? player;
        public bool IsPlaying => player?.track != null && !player.paused && this.IsConnected;
        public LavaTrack? ActiveTrack => player?.track;
        public bool IsConnected => voiceState.VoiceLoaded();
        private bool listeningForUpdates = false;
        DateTime timeOfLastActivity = DateTime.UtcNow;
        TimeSpan timeout = TimeSpan.FromSeconds(10);

        public Lava(ILogger<Lava> logger,
                    DiscordClientProvider clientProvider,
                    AppSettings lavaSettings, 
                    LavalinkEndpointProvider lavalinkEndpointProvider,
                    LavalinkRestNode lavalinkRest )
        {
            this.logger = logger; 
            this.discordClient = clientProvider.GetMusicClient() ?? throw new ArgumentNullException(nameof(DiscordSocketClient));
            this.settings = lavaSettings;
            this.endpointProvider = lavalinkEndpointProvider;
            this.restNode = lavalinkRest;
            discordClient.VoiceServerUpdated += DiscordClient_VoiceServerUpdated;
            discordClient.UserVoiceStateUpdated += DiscordClient_UserVoiceStateUpdated;
            this.discordClient.Ready += async () =>
            {
                logger.LogInformation("Discord client ready.");
                if (websocket?.IsRunning ?? false)
                    return;

                await StartWebsocket();
            };

            this.OnPlayerUpdate += (update) =>
            {
                if (update.state.position > 0 && update.state.connected)
                {
                    //logger.LogInformation($"Updating last time of activity {timeOfLastActivity} to {DateTime.UtcNow}");
                    timeOfLastActivity = DateTime.UtcNow; 
                } 

                return Task.CompletedTask;
            };
        }

        public async Task Initialize()
        {
            logger.LogInformation("Initializing lavalink");
            this.pendingUpdates.Clear();
            this.voiceState = new VoiceState(); 

            var startTasks = new List<Task>()
            { 
               discordClient.LoginAsync(TokenType.Bot, settings.OuterHeavenBotSettings.DiscordToken),
               discordClient.SetGameAsync("|~h for more info", null, ActivityType.Playing),
               discordClient.StartAsync()
            }; 
          
            await Task.WhenAll(startTasks); 
        }
           
        public async Task UpdatePlayer(UpdatePlayerTrack track)
        {
            logger.LogInformation("Updating player with voice state\n" + voiceState.ToString()); 
        
            var playerUpdate = new PlayerUpdateRequest()
            {
                voice = voiceState,
                track = track
            };

            await RunActionAsync(async() =>
            {
                if(await CheckConnection(TimeSpan.FromMilliseconds(200)))
                if (!IsConnected)
                {
                    await Task.Delay(300);
                    if (!IsConnected)
                    {
                        logger.LogError("lava player must be connected to a voice channel");
                        return;
                    }
                } 

                player = await this.restNode.UpdatePlayer(this.voiceState.GuildId, this.voiceState.LavaSessionId, playerUpdate);
                logger.LogInformation($"Updated player - voice session {player?.voice.DiscordVoiceSessionId} host session {this.voiceState.LavaSessionId} | connected {player?.state.connected} | current track {player?.track?.info?.title}");
            }); 
        }

        public async Task DestroyPlayer()
        {
            logger.LogInformation("Current voice state\n" + voiceState.ToString());
            if (!IsConnected)
            {
                logger.LogError("lava player must be connected to a voice channel");
                return;
            }

            logger.LogInformation($"Destroying player..\n {voiceState}");
            await this.restNode.DestroyPlayer(this.voiceState.GuildId, this.voiceState.LavaSessionId);
            this.player = null;
        }

        public async Task StopPlayer()
        {
            logger.LogInformation("Current voice state\n" + voiceState.ToString());
            if (!IsConnected)
            {
                logger.LogError("lava player must be connected to a voice channel");
                return;
            }

            var playerUpdate = new PlayerUpdateRequest()
            {
                voice = voiceState,
                track = new UpdatePlayerTrack()
                { 
                     encoded = null
                }
            };
           
             logger.LogInformation($"Stopping player...\n {voiceState}");
            this.player = await this.restNode.UpdatePlayer(this.voiceState.GuildId, this.voiceState.LavaSessionId,playerUpdate);
        }

        public async Task<LavaDataLoadResult> SearchForTracks(string queryRaw, LavalinkSearchType searchType = LavalinkSearchType.ytsearch)
        {
            var result = await restNode.SearchForTracks(queryRaw, searchType); 
            
            if(result.TrackException != null)
            {
                logger.LogError($"Error running query {queryRaw}\nError level: {result.TrackException.Severity}\nError Message: {result.TrackException.Message}\nCause: {result.TrackException.Cause}"); 
            }

            return result;
        }

        public async Task DisconnectFromChannel()
        {
            if(this.voiceState.DiscordVoiceLoaded() && ulong.TryParse(voiceState.ChannelId,out var channelId))
            {
                var channel = this.discordClient.GetChannel(channelId) as IVoiceChannel;
                if (channel != null) 
                {
                    logger.LogInformation($"Disconnecting from channel {channel.Name}");
                    await channel.DisconnectAsync();
                }

                this.player = null;
            }            
        }

        private async Task StartWebsocket()
        {
            try
            {
                logger.LogInformation("Attempting to connect to lavalink");
                var wsEndpoint = this.endpointProvider.WebSocketEndpoint;
                await RunActionAsync(async () =>
                   {
                       var factory = new Func<ClientWebSocket>(() =>
                       {
                           var client = new ClientWebSocket();
                           client.Options.SetRequestHeader("Authorization", wsEndpoint.Password);
                           client.Options.SetRequestHeader("client-Name", "DHCPCD9/OuterHeaven");

                           if (!string.IsNullOrWhiteSpace(this.voiceState.LavaSessionId))
                           {
                               client.Options.SetRequestHeader("Resume-Key", this.voiceState.LavaSessionId);
                           }

                           client.Options.SetRequestHeader("User-Id", this.discordClient.CurrentUser.Id.ToString());
                           return client;
                       });

                       var websocket = new WebsocketClient(new Uri(wsEndpoint.ToWebSocketString()), factory);
                       websocket.ErrorReconnectTimeout = TimeSpan.FromSeconds(10);
                       websocket.ReconnectTimeout = null;
                       websocket.ReconnectionHappened.Subscribe((connectionInfo) =>
                       {
                           logger.LogInformation($"Websocket reconnected. Lava session id {voiceState?.LavaSessionId}");
                       });

                       websocket.DisconnectionHappened.Subscribe((disconnectionInfo) =>
                       {
                           logger.LogInformation($"Websocket Disconnected.\n" +
                           $"DisconnectionType [{disconnectionInfo.Type}] | Disconnection description {disconnectionInfo.CloseStatusDescription}");
                           if (disconnectionInfo.Exception != null)
                           {
                               logger.LogError(disconnectionInfo.Exception.Message);
                           }
                       });

                       websocket.MessageReceived.Subscribe(ProcessWebsocketMessage);
                       await websocket.Start();
                   });
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to connect to Lavalink. Error {ex.Message}");
            }
        }

        private void ProcessWebsocketMessage(ResponseMessage message)
        {
            var jsonNode = message.MessageType == WebSocketMessageType.Text ? JsonNode.Parse(message?.Text ?? "") : JsonNode.Parse(message.Binary);

            if (jsonNode == null)
            {
                logger.LogError("Websocket message Invalid message");
                return;
            }

            var opCode = jsonNode["op"]?.GetValue<string>() ?? "";

            switch (opCode)
            {
                case LavalinkOPCodes.Ready:
                    var ready = jsonNode.Deserialize<ReadyOPWebsocketMessage>() ?? throw new Exception("Error reading ready response from lavalink");
                    this.voiceState.LavaSessionId = ready.sessionId;
                    logger.LogInformation($"lava link ready fired with {ready.sessionId} resumed {ready.resumed}");
                    break;
                case LavalinkOPCodes.Stats:
                    stats = jsonNode.Deserialize<LavaStatsWebsocketMessage>() ?? throw new Exception("Error reading ready response from lavalink");
                    break;
                case LavalinkOPCodes.PlayerUpdate: 
                    var update = jsonNode.Deserialize<PlayerUpdateWebsocketMessage>() ?? throw new Exception("Error reading ready response from lavalink");  
                    if (update.state.position > 0 && update.state.connected) timeOfLastActivity = DateTime.UtcNow;
                    this.OnPlayerUpdate?.Invoke(update);
                    break;
                case LavalinkOPCodes.LavalinkEvent:
                    HandleLavaEvent(jsonNode).GetAwaiter().GetResult();  
                    break;
                default:
                    logger.LogError($"Invalid websocket opCode [{opCode ?? "null"}]");
                    return;
            }
        }

        private async Task HandleLavaEvent(JsonNode jsonNode)
        {
            var lavaEventString = jsonNode["type"]?.GetValue<string>();

            if (!Enum.TryParse<LavalinkWebsocketEventType>(lavaEventString, out var lavaEventType))
            {
                logger.LogError($"Invalid lava event [{lavaEventString ?? "null"}]");
                return;
            }

            switch (lavaEventType)
            {
                case LavalinkWebsocketEventType.TrackStartEvent:
                    var tStart = jsonNode.Deserialize<TrackStartWebsocketEvent>() ?? throw new ArgumentNullException(nameof(TrackStartWebsocketEvent));
                    if (tStart != null && OnLavaTrackStartEvent != null) await OnLavaTrackStartEvent.Invoke(tStart);
                    break;
                case LavalinkWebsocketEventType.TrackEndEvent:
                    var tEnd = jsonNode.Deserialize<TrackEndWebsocketEvent>() ?? throw new ArgumentNullException(nameof(TrackEndWebsocketEvent));
                    if (tEnd != null && OnLavaTrackEndEvent!=null) await OnLavaTrackEndEvent.Invoke(tEnd);
                    break;
                case LavalinkWebsocketEventType.TrackStuckEvent:
                    var tStuck = jsonNode.Deserialize<TrackStuckWebsocketEvent>() ?? throw new ArgumentNullException(nameof(TrackStuckWebsocketEvent));
                    if (tStuck != null && OnLavaTrackStuckEvent != null) await OnLavaTrackStuckEvent.Invoke(tStuck);
                    break;
                case LavalinkWebsocketEventType.TrackExceptionEvent:
                    var tException = jsonNode.Deserialize<TrackExceptionWebsocketEvent>() ?? throw new ArgumentNullException(nameof(TrackExceptionWebsocketEvent));
                    if (tException != null && OnLavaTrackExceptionEvent != null) await OnLavaTrackExceptionEvent.Invoke(tException);
                    break;
                case LavalinkWebsocketEventType.WebSocketClosedEvent:
                    var websocketClosed = jsonNode.Deserialize<ClosedWebsocketEvent>() ?? throw new ArgumentNullException(nameof(ClosedWebsocketEvent));
                    if (websocketClosed != null && OnLavaConnectionClosed != null) await OnLavaConnectionClosed.Invoke(websocketClosed);
                    break;
            }
        }
         
        private Task DiscordClient_UserVoiceStateUpdated(SocketUser user, SocketVoiceState previousState, SocketVoiceState currentState)
        { 
            //Ignore updates from other users.
            if (user.Id != this.discordClient.CurrentUser.Id)
            {
                return Task.CompletedTask;
            }

            logger.LogInformation($"Updating voice state for lava session {voiceState.LavaSessionId}.\n" +
                                  $"Discord voice session id from {this.voiceState.DiscordVoiceSessionId} to {currentState.VoiceSessionId}\n" +
                                  $"Voice channel id from {this.voiceState.ChannelId} to {currentState.VoiceChannel?.Id.ToString() ?? "null"}\n" +
                                  $"Guild id from {this.voiceState.GuildId} to {currentState.VoiceChannel?.Guild?.Id.ToString() ?? "null"}");

            this.voiceState.DiscordVoiceSessionId = currentState.VoiceSessionId;
            if (currentState.VoiceChannel != null)
            {
                voiceState.ChannelId = currentState.VoiceChannel.Id.ToString();
                voiceState.GuildId = currentState.VoiceChannel.Guild.Id.ToString();
            }
            else
            {
                voiceState.ChannelId = "";
                voiceState.GuildId = "";
                
            }

            return Task.CompletedTask;
        }

        private Task DiscordClient_VoiceServerUpdated(SocketVoiceServer server)
        {
            logger.LogInformation($"Updating voice server for lava session {voiceState.LavaSessionId}.\n" +
                                   $"Server token from {this.voiceState.Token} to {server.Token}\n" +
                                   $"Server endpoint from {this.voiceState.Endpoint} to {server.Endpoint}");

            this.voiceState.Token = server.Token;
            this.voiceState.Endpoint = server.Endpoint;

            return Task.CompletedTask;
        }

        async Task<bool> CheckConnection(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            var time = DateTime.UtcNow.Add(timeout);
            while (!this.IsConnected && DateTime.UtcNow < time && !cancellationToken.IsCancellationRequested) 
            {
                await Task.Delay(50);
            }

            return this.IsConnected;
        }

        private Task RunActionAsync(Func<Task> action, CancellationToken cancellationToken = default)
        {
            return Task.Run(async () =>
              {
                 await action();
              }, cancellationToken);
        }

        public void Dispose()
        {
            this.listeningForUpdates = false;
            this.websocket?.Dispose();
        }
    }
}