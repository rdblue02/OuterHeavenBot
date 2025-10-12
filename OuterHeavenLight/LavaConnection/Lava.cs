using Discord;
using Discord.WebSocket;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Nodes;
using Websocket.Client;
using OuterHeavenLight.Entities;
using OuterHeavenLight.Entities.Response.Websocket;
using OuterHeavenLight.Entities.Request;
using OuterHeavenLight.Entities.Response.Rest;
using OuterHeavenLight.Music;
using OuterHeaven.LavalinkLight;

namespace OuterHeavenLight.LavaConnection
{
    public class Lava : IDisposable
    {
        public event Func<TrackStartWebsocketEvent, Task>? OnLavaTrackStartEvent;
        public event Func<TrackEndWebsocketEvent, Task>? OnLavaTrackEndEvent;
        public event Func<TrackExceptionWebsocketEvent, Task>? OnLavaTrackExceptionEvent;
        public event Func<TrackStuckWebsocketEvent, Task>? OnLavaTrackStuckEvent;
        public event Func<ClosedWebsocketEvent, Task>? OnLavaConnectionClosed;
        public event Func<PlayerUpdateWebsocketMessage, Task>? OnPlayerUpdate;

        public bool IsConnected => voiceState.VoiceLoaded();

        private ILogger<Lava> logger;
        private LavaStatsWebsocketMessage stats;
        private WebsocketClient? websocket;
        private DiscordSocketClient client;
        private AppSettings settings;
        private LavalinkRestNode restNode;
        private VoiceState voiceState = new VoiceState();
        private LavaPlayer? player;

        private DateTime timeOfLastActivity = DateTime.UtcNow;
        private TimeSpan idleDisconnectWait = TimeSpan.FromMinutes(2);
        private bool isReconnected = false;

        public Lava(ILogger<Lava> logger,
                    MusicDiscordClient client,
                    AppSettings lavaSettings,
                    LavalinkRestNode lavalinkRest)
        {
            this.logger = logger;
            this.client = client;
            this.settings = lavaSettings;
            this.restNode = lavalinkRest;
            this.stats = new LavaStatsWebsocketMessage();
            this.client.VoiceServerUpdated += DiscordClient_VoiceServerUpdated;
            this.client.UserVoiceStateUpdated += DiscordClient_UserVoiceStateUpdated;
            this.client.Ready += async () =>
            {
                logger.LogInformation("Discord client ready.");
                if (websocket?.IsRunning ?? false)
                    return;

                await StartWebsocket();
            };

            OnPlayerUpdate += (update) =>
            {
                if (player != null)
                { 
                    this.player.state = update.state; 
                }

                if (update.state.position > 0 && update.state.connected)
                {
                    timeOfLastActivity = DateTime.UtcNow;
                }

                return Task.CompletedTask;
            }; 
        }

        public async Task Initialize()
        {
            logger.LogInformation("Initializing lavalink");

            var startTasks = new List<Task>()
            {
               client.LoginAsync(TokenType.Bot, settings?.OuterHeavenBotSettings?.DiscordToken ??
                                                throw new ArgumentNullException(settings?.OuterHeavenBotSettings?.DiscordToken)),

               client.SetGameAsync("|~h for more info", null, ActivityType.Playing),
               client.StartAsync()
            };

            await Task.WhenAll(startTasks);
            await Task.Run(async () =>
            {
                await CheckForIdleDisconnect(default);
            });
        }

        public async Task<bool> IsPlaying()
        {
            var track = await this.GetCurrentTrack();

            return track?.info != null && track.info.position < track.info.length;
        }

        public async Task<LavaPlayer?> PauseResume()
        {
            if (this.player == null)
            {
                return null;
            }

            this.player = await this.restNode.UpdatePlayer(this.voiceState.GuildId, this.voiceState.LavaSessionId, new PlayerUpdateRequest() { paused = !player.paused });

            return player;
        }

        public async Task UpdatePlayer(UpdatePlayerTrack track, bool noReplace = false)
        {
            this.timeOfLastActivity = DateTime.UtcNow;
            logger.LogInformation("Updating player with voice state\n" + voiceState.ToString());

            var playerUpdate = new PlayerUpdateRequest()
            {
                voice = voiceState,
                track = track
            };

            await RunActionAsync(async () =>
            {
                var connected = await CheckConnection(TimeSpan.FromMilliseconds(200));

                if (!connected)
                {
                    logger.LogError("lava player must be connected to a voice channel");
                    return;
                }

                player = await restNode.UpdatePlayer(voiceState.GuildId, voiceState.LavaSessionId, playerUpdate, noReplace);
                logger.LogInformation($"Updated player - voice session {player?.voice.DiscordVoiceSessionId} host session {voiceState.LavaSessionId} | connected {player?.state.connected} | current track {player?.track?.info?.title}");
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
            await restNode.DestroyPlayer(voiceState.GuildId, voiceState.LavaSessionId);
            player = null;
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
            player = await restNode.UpdatePlayer(voiceState.GuildId, voiceState.LavaSessionId, playerUpdate);
        }

        public async Task<LavaDataLoadResult> SearchForTracks(string queryRaw, LavalinkSearchType searchType, bool loadPlaylist) =>
                                                              await restNode.SearchForTracks(queryRaw, searchType, loadPlaylist);

        public async Task DisconnectFromChannel()
        {
            if (voiceState.DiscordVoiceLoaded() && ulong.TryParse(voiceState.ChannelId, out var channelId))
            {
                var channel = client.GetChannel(channelId) as IVoiceChannel;

                if (channel != null)
                {
                    logger.LogInformation($"Disconnecting from channel {channel.Name}");
                    await channel.DisconnectAsync();
                }

                player = null;
            }
            else
            {
                logger.LogError($"Unable to disconnect from channel. Current voice state {this.voiceState}");
            }
        }
   
        public async Task<LavaTrack?> GetCurrentTrack()
        {
            if (!this.IsConnected)
            {
                logger.LogError("Unable to search for track while not connected");
                return null;
            }

            var player = await restNode.GetPlayerOrDefaultAsync(this.voiceState.GuildId, this.voiceState.LavaSessionId);

            return player?.track;
        }

        private async Task StartWebsocket()
        {
            try
            {
                logger.LogInformation("Attempting to connect to lavalink");
                var wsEndpoint = LavalinkEndpoint.ToWebSocketEndpoint;

                await RunActionAsync(async () =>
                   {
                       var factory = new Func<ClientWebSocket>(() =>
                       {
                           var client = new ClientWebSocket();
                           client.Options.SetRequestHeader("Authorization", wsEndpoint.Password);
                           client.Options.SetRequestHeader("client-Name", "DHCPCD9/OuterHeaven");

                           if (!string.IsNullOrWhiteSpace(voiceState.LavaSessionId))
                           {
                               this.isReconnected = true;
                               client.Options.SetRequestHeader("Resume-Key", voiceState.LavaSessionId);
                           }

                           client.Options.SetRequestHeader("User-Id", this.client.CurrentUser.Id.ToString());
                           return client;
                       });

                       websocket = new WebsocketClient(new Uri(wsEndpoint.ToWebSocketString()), factory);
                       websocket.ErrorReconnectTimeout = TimeSpan.FromSeconds(10);
                       websocket.ReconnectTimeout = null;
                       websocket.ReconnectionHappened.Subscribe(async (connectionInfo) =>
                       {
                           if (isReconnected)
                           {
                               logger.LogInformation($"Websocket reconnected. Lava session id {voiceState?.LavaSessionId}");
                               if (!string.IsNullOrWhiteSpace(this.voiceState?.LavaSessionId) && this.voiceState.GuildId != default)
                                   this.player = await restNode.GetPlayerOrDefaultAsync(this.voiceState.GuildId, this.voiceState.LavaSessionId);
                           }
                           else
                           {
                               logger.LogInformation($"Websocket connected. Lava session id {voiceState?.LavaSessionId}");
                           }
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
                    voiceState.LavaSessionId = ready.sessionId;
                    logger.LogInformation($"lava link ready fired with {ready.sessionId} resumed {ready.resumed}");
                    break;
                case LavalinkOPCodes.Stats:
                    stats = jsonNode.Deserialize<LavaStatsWebsocketMessage>() ?? throw new Exception("Error reading ready response from lavalink");
                    break;
                case LavalinkOPCodes.PlayerUpdate:
                    var update = jsonNode.Deserialize<PlayerUpdateWebsocketMessage>() ?? throw new Exception("Error reading ready response from lavalink");                   
                    OnPlayerUpdate?.Invoke(update);
                    break;
                case LavalinkOPCodes.LavalinkEvent:
                    var type = jsonNode["type"]?.GetValue<string>() ?? "";

                    logger.LogInformation($"websocketevent type {type}"); 
                    if(type == "TrackEndEvent")
                    {
                        var lavaEvent = jsonNode.Deserialize<TrackEndWebsocketEvent>() ?? throw new Exception("Error reading ready response from lavalink");
                        this.OnLavaTrackEndEvent?.Invoke(lavaEvent);
                    } 
                    break;
                default:
                    logger.LogError($"Invalid websocket opCode [{opCode ?? "null"}]");
                    return;
            }
        }

        private Task DiscordClient_UserVoiceStateUpdated(SocketUser user, SocketVoiceState previousState, SocketVoiceState currentState)
        {
            //Ignore updates from other users.
            if (user.Id != client.CurrentUser.Id)
            {
                return Task.CompletedTask;
            }

            this.timeOfLastActivity = DateTime.UtcNow;
            logger.LogInformation($"Updating voice state for lava session {voiceState.LavaSessionId}.\n" +
                                  $"Discord voice session id from {voiceState.DiscordVoiceSessionId} to {currentState.VoiceSessionId}\n" +
                                  $"Voice channel id from {voiceState.ChannelId} to {currentState.VoiceChannel?.Id.ToString() ?? "null"}\n" +
                                  $"Guild id from {voiceState.GuildId} to {currentState.VoiceChannel?.Guild?.Id.ToString() ?? "null"}");

            voiceState.DiscordVoiceSessionId = currentState.VoiceSessionId;
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
            this.timeOfLastActivity = DateTime.UtcNow;
            logger.LogInformation($"Updating voice server for lava session {voiceState.LavaSessionId}.\n" +
                                   $"Server token from {voiceState.Token} to {server.Token}\n" +
                                   $"Server endpoint from {voiceState.Endpoint} to {server.Endpoint}");

            this.voiceState.Token = server.Token;
            this.voiceState.Endpoint = server.Endpoint;
            this.timeOfLastActivity = DateTime.UtcNow;
 
            return Task.CompletedTask;
        }

        private async Task<bool> CheckConnection(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            var time = DateTime.UtcNow.Add(timeout);
            while (!IsConnected &&
                   DateTime.UtcNow < time &&
                   !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(100);
            }

            logger.LogDebug($"Time waited for connection update {DateTime.UtcNow - time}");
            return IsConnected;
        }

        private Task RunActionAsync(Func<Task> action, CancellationToken cancellationToken = default)
        {
            return Task.Run(async () =>
              {
                  await action();
              }, cancellationToken);
        }

        async Task CheckForIdleDisconnect(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (this.IsConnected &&
                    DateTime.UtcNow - timeOfLastActivity > idleDisconnectWait)
                {
                    logger.LogInformation($"Idle timer has been reached. Disconnecting from channel id [{voiceState.ChannelId}] in guid id [{voiceState.GuildId}]");
                    await DisconnectFromChannel();
                    break;
                }

                await Task.Delay(500);
            }
        }

        public void Dispose()
        {
            websocket?.Dispose();
        }
    }
}