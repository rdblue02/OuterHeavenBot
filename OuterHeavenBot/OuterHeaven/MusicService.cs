using Discord;
using Discord.Commands;
using Discord.WebSocket;
using OuterHeavenBot.Clients;
using OuterHeavenBot.Commands;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;
using Victoria.Responses.Search;

namespace OuterHeavenBot.Services
{
    public class MusicService
    {
        public ulong? CurrentChannelId { get; private set; } = null;
        public MusicBotPlayerState MusicBotPlayerState { get; private set; } = MusicBotPlayerState.Disconnected;

        ILogger logger;
        OuterHeavenDiscordClient discordClient;
        OuterHeavenCommandHandler outerHeavenCommandHandler;

        LavaNode lavaNode;
        public MusicService(ILogger<MusicService> logger,
                            OuterHeavenDiscordClient outerHeavenDiscordClient,
                            OuterHeavenCommandHandler outerHeavenCommandHandler,
                            LavaNode lavaNode)
        {
            this.discordClient = outerHeavenDiscordClient;
            this.outerHeavenCommandHandler = outerHeavenCommandHandler;

            this.logger = logger;
            this.lavaNode = lavaNode;
            this.discordClient.Connected += DiscordClient_Connected;
            this.discordClient.Ready += DiscordClient_Ready;
            this.discordClient.MessageReceived += DiscordClient_MessageReceived;

            this.lavaNode.OnLog += Lavanode_OnLog;
            this.lavaNode.OnTrackStuck += Lavanode_OnTrackStuck;
            this.lavaNode.OnTrackException += Lavanode_OnTrackException;
            this.lavaNode.OnTrackStarted += Lavanode_OnTrackStarted;
            this.lavaNode.OnTrackEnded += Lavanode_OnTrackEnded;
            this.lavaNode.OnWebSocketClosed += Lavanode_OnWebSocketClosed;
            this.lavaNode.OnPlayerUpdated += Lavanode_OnPlayerUpdated;

        }

        public async Task InitializeAsync() => await this.discordClient.InitializeAsync();

        public async Task RequestSong(string searchArgument, IVoiceChannel userChannel, ITextChannel commandChannel)
        {
            try
            {
                //local file requested
                var searchResponse = searchArgument.Contains("C:\\") &&
                                     File.Exists(searchArgument) ?
                                                                 await lavaNode.SearchAsync(SearchType.Direct, searchArgument) :
                                                                 await lavaNode.SearchAsync(SearchType.YouTube, searchArgument);

                if (searchResponse.Tracks.Any())
                {
                    var player = GetCurrentPlayer(userChannel, commandChannel);
                    if (player == null) player = await lavaNode.JoinAsync(userChannel, commandChannel) ?? throw new ArgumentNullException(nameof(player));

                    CurrentChannelId = player?.VoiceChannel?.Id;
                    if (!string.IsNullOrWhiteSpace(searchResponse.Playlist.Name))
                    {
                        await ProcessPlayList(searchResponse, player, commandChannel);
                    }
                    else
                    {
                        await ProcessTrack(searchResponse, player, commandChannel);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                await commandChannel.SendMessageAsync($"Error joining channel {userChannel.Name}");
            }

        }

        #region discordEvents
        private async Task DiscordClient_MessageReceived(SocketMessage messageParam)
        {
            var userMessage = messageParam as SocketUserMessage;
            if (userMessage == null) return;

            var requestedCommand = outerHeavenCommandHandler.GetCommandInfoFromMessage(userMessage);
            if (requestedCommand == null) return;

            await outerHeavenCommandHandler.HandleCommandAsync(discordClient, userMessage);
        }

        private async Task DiscordClient_Connected()
        {
            this.MusicBotPlayerState = MusicBotPlayerState.Connecting;
           
            await this.outerHeavenCommandHandler.ApplyCommands();           
        }
        private async Task DiscordClient_Ready()
        {
            MusicBotPlayerState = MusicBotPlayerState.Available;
            
            if (!lavaNode.IsConnected)
            {
                await lavaNode.ConnectAsync();
            }
        }
        #endregion

        #region lavanodeEvents 
        private Task Lavanode_OnPlayerUpdated(PlayerUpdateEventArgs arg)
        {
            //I think this would be used to track fast forward or skips.
            return Task.CompletedTask;
        }

        private async Task Lavanode_OnWebSocketClosed(WebSocketClosedEventArgs arg)
        {
            try
            {
                MusicBotPlayerState = MusicBotPlayerState.Disconnected;
                logger.LogInformation($"Websocket Closed reason: {arg.Reason} {arg.Code}. Attempting to reconnect");
              
                //reconnect logic maybe? not sure what would cause this or if it will automatically try to reconnect.
                await lavaNode.ConnectAsync();
                MusicBotPlayerState = MusicBotPlayerState.Available;
              
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
            }
        }

        private async Task Lavanode_OnTrackEnded(TrackEndedEventArgs arg)
        {
            if (arg.Reason == Victoria.Enums.TrackEndReason.Finished &&
               arg.Player != null &&
               arg.Player.IsConnected &&
               arg.Player.Queue.Any() &&
               arg.Player.Queue.TryDequeue(out LavaTrack nextTrack))
            {
                await arg.Player.PlayAsync(nextTrack);
            }
            else
            {
                this.MusicBotPlayerState = MusicBotPlayerState.Available;

                if (arg.Reason == Victoria.Enums.TrackEndReason.Stopped)
                {
                    logger.LogInformation($"Stop requested. Clearing {arg.Player?.Queue?.Count() ?? 0} songs from the queue");
                    arg.Player?.Queue.Clear();
                }
                await Task.Run(() => CheckForIdelDisconnect(arg.Player));
            }
        }

        private async Task Lavanode_OnTrackStarted(TrackStartEventArgs arg)
        {
            this.MusicBotPlayerState = MusicBotPlayerState.Playing;
            await arg.Player.TextChannel.SendMessageAsync($"Now playing: {arg.Track.Title} - {arg.Track.Author} - {arg.Track.Duration}");
        }

        private async Task Lavanode_OnTrackException(TrackExceptionEventArgs arg)
        {
            logger.LogError($"Error playing {arg.Track.Title} from {arg.Track.Source}. Error:\n{arg.Exception}");
            if (CurrentChannelId.HasValue)
            {
                var currentChannel = arg.Player?.TextChannel;
                if (currentChannel != null)
                {
                    await currentChannel.SendMessageAsync($"Error playing {arg.Track.Title} from {arg.Track.Source}.");
                }
                else
                {
                    logger.LogError($"Unable to find channel for channel Id{currentChannel?.Id} to notify user of the error.");
                }
            }
        }

        private async Task Lavanode_OnTrackStuck(TrackStuckEventArgs arg)
        {
            if (arg.Threshold > TimeSpan.FromSeconds(5))
            {
                await arg.Player.TextChannel.SendMessageAsync($"track {arg.Track.Title} is stuck. Skipping track...");
                if (arg.Player.Queue.Any())
                {
                    await arg.Player.SkipAsync();
                }
                else
                {
                    await arg.Player.StopAsync();
                }
            }
        }
        private Task Lavanode_OnLog(LogMessage arg)
        {

            logger.Log(Helpers.ToMicrosoftLogLevel(arg.Severity), $"{arg.Message}\n{arg.Exception}");
            return Task.CompletedTask;
        }
        #endregion


        public async Task GoTo(TimeSpan time, IVoiceChannel userChannel, ITextChannel commandChannel)
        {
            var player = GetCurrentPlayer(userChannel, commandChannel);
            if (player == null || player.Track == null)
            {
                await commandChannel.SendMessageAsync($"Nothing is playing");
            }
            else if (!player.Track.CanSeek)
            {
                await commandChannel.SendMessageAsync($"Cannot go to time in track");
            }
            else if (time > player.Track.Duration)
            {
                await commandChannel.SendMessageAsync($"Seek time must be smaller than the track duration {player.Track.Duration}");
            }
            else
            {
                await player.SeekAsync(time);
            }
        }
        public async Task RequestQueueClear(int? index, IVoiceChannel userChannel, ITextChannel commandChannel)
        {
            var player = GetCurrentPlayer(userChannel, commandChannel);
            if (player == null || !player.Queue.Any())
            {
                await commandChannel.SendMessageAsync("Que is currently empty!");
                return;
            }

            if (index.HasValue)
            {
                if (index.Value > 0 && index.Value - 1 < player.Queue.Count)
                {
                    var trackToKill = player.Queue.ElementAt(index.Value - 1);

                    player.Queue.Remove(trackToKill);
                    await commandChannel.SendMessageAsync($"Clearing {trackToKill.Title} from the queue");
                }
                else
                {
                    await commandChannel.SendMessageAsync($"Invalid index. Please enter a value between 1 - {player?.Queue.Count}");
                }
            }
            else
            {
                await commandChannel.SendMessageAsync($"Clearing {player.Queue.Count} songs from the queue");
                player.Queue.Clear();
            }
        }
        public async Task RequestRewind(int seconds, IVoiceChannel userChannel, ITextChannel commandChannel)
        {
            var player = GetCurrentPlayer(userChannel, commandChannel);
            if (player == null || player.Track == null)
            {
                await commandChannel.SendMessageAsync($"Nothing is playing");
            }
            else if (!player.Track.CanSeek)
            {
                await commandChannel.SendMessageAsync($"Cannot go to time in track");
            }
            else
            {
                var time = player.Track.Position - TimeSpan.FromSeconds(seconds) > TimeSpan.FromSeconds(0) ? player.Track.Position - TimeSpan.FromSeconds(seconds) :
                                                                                                             TimeSpan.FromSeconds(0);
                await commandChannel.SendMessageAsync($"Rewinding {time}");
                await player.SeekAsync(time);
            }
        }
        public async Task FastForward(int seconds, IVoiceChannel userChannel, ITextChannel commandChannel)
        {
            var player = GetCurrentPlayer(userChannel, commandChannel);
            if (player == null || player.Track == null)
            {
                await commandChannel.SendMessageAsync($"Nothing is playing");
            }
            else if (!player.Track.CanSeek)
            {
                await commandChannel.SendMessageAsync($"Cannot go to time in track");
            }
            else
            {
                var time = player.Track.Position + TimeSpan.FromSeconds(seconds) < player.Track.Duration ? player.Track.Position + TimeSpan.FromSeconds(seconds) :
                                                                                                               player.Track.Duration - TimeSpan.FromMilliseconds(500);
                await commandChannel.SendMessageAsync($"Fast forwarding {time.ToString("hh:mm:ss")}");
                await player.SeekAsync(time);
            }
        }
        public string GetCurrentTrackInfo(IVoiceChannel userChannel, ITextChannel commandChannel)
        {
            var player = GetCurrentPlayer(userChannel, commandChannel);
            return player?.Track == null ? "Nothing is playing" : $"Current track: {ConvertTrackToTrackInfo(player.Track)}";
        }      
       
        public List<LavaTrack> GetAllTracks(IVoiceChannel userChannel, ITextChannel commandChannel)
        {
            var tracks = new List<LavaTrack>(); 
            var player = GetCurrentPlayer(userChannel, commandChannel);
            if(player == null || player.Track == null) return tracks;

            tracks.Add(player.Track);
            if(player.Queue.Any()) tracks.AddRange(player.Queue);

            return tracks;
        }
        public async Task RequestSkip(IVoiceChannel userChannel, ITextChannel commandChannel)
        {
            try
            {
                var player = GetCurrentPlayer(userChannel, commandChannel) ?? throw new ArgumentNullException(nameof(LavaPlayer));

                if (player.Queue.Any())
                {
                    await commandChannel.SendMessageAsync($"Skipping - {player.Track.Title}");
                    await player.SkipAsync(TimeSpan.FromSeconds(0));
                }
                else if (player.Track != null)
                {
                    await commandChannel.SendMessageAsync($"Skipping - {player.Track.Title}");
                    await player.StopAsync();
                    this.CurrentChannelId = null;
                }
                else
                {
                    await commandChannel.SendMessageAsync($"There's nothing to skip!");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }

        }
        public async Task ChangePauseState(IVoiceChannel userChannel, ITextChannel commandChannel)
        {
            var player = GetCurrentPlayer(userChannel, commandChannel);
            if (player == null) return;

            if (player.PlayerState == PlayerState.Paused)
            {
                await player.ResumeAsync();
                MusicBotPlayerState = MusicBotPlayerState.Playing;
                return;
            }

            await player.PauseAsync();
            MusicBotPlayerState = MusicBotPlayerState.Paused;

        }
        public async Task RequestDisconnect()
        {
            foreach(var player in lavaNode.Players)
            {
                if(player.VoiceChannel != null)
                {
                    await player.StopAsync();
                    await lavaNode.LeaveAsync(player.VoiceChannel);                    
                }
            }
            this.CurrentChannelId = null;
        }

        private async Task ProcessTrack(SearchResponse searchResponse, LavaPlayer? player, ITextChannel commandChannel)
        {
            var track = searchResponse.Tracks.FirstOrDefault();
            if (track == null || player == null) return;

            if (MusicBotPlayerState == MusicBotPlayerState.Playing)
            {
                player.Queue.Enqueue(track);
                await commandChannel.SendMessageAsync($"Track {track.Title} has been queued!");
                return;
            } 
            await player.PlayAsync(track);
        }
        private async Task ProcessPlayList(SearchResponse searchResponse, LavaPlayer? player, ITextChannel commandChannel)
        {
            var firstTrack = searchResponse.Tracks.FirstOrDefault();
            if (player == null || firstTrack == null) return;

            if (MusicBotPlayerState == MusicBotPlayerState.Playing)
            {
                player.Queue.Enqueue(firstTrack);
                await commandChannel.SendMessageAsync($"Queing playlist {searchResponse.Playlist.Name}");
            }
            else
            {
                await player.PlayAsync(firstTrack);
                await commandChannel.SendMessageAsync($"Play list {searchResponse.Playlist.Name} started");
            }

            foreach (var track in searchResponse.Tracks.Skip(1))
            {
                player.Queue.Enqueue(track);
            }
        }
        private async Task CheckForIdelDisconnect(LavaPlayer? lavaPlayer)
        {
            if (lavaPlayer == null)
                return;

            //todo make this a setting
            var disconnectTime = DateTime.UtcNow.AddMinutes(5);

            while (this.MusicBotPlayerState == MusicBotPlayerState.Available)
            {
                if (DateTime.UtcNow > disconnectTime && lavaPlayer.VoiceChannel != null)
                {
                    logger.LogInformation("Idle time limit has been reached. Disconnecting");
                    await lavaNode.LeaveAsync(lavaPlayer.VoiceChannel);
                    this.CurrentChannelId = null;
                }
            }
        }
        private string ConvertTrackToTrackInfo(LavaTrack track) => $"{track.Title} - {track.Author} - {track.Duration - track.Position} - {track.Url}";
        private LavaPlayer? GetCurrentPlayer(IVoiceChannel userChannel, ITextChannel commandChannel)
        {
            if (lavaNode.TryGetPlayer(userChannel.Guild, out LavaPlayer player) && player != null)
            {
                return player;
            }
            logger.LogError($"Unable to find player for channel {userChannel.Name} with command fired in  channel {commandChannel.Name}");
            return null;
        }

    }

}
