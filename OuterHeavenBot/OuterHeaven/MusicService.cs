using Discord;
using Discord.Commands;
using Discord.WebSocket;
using OuterHeavenBot.Clients;
using OuterHeavenBot.Commands;
using OuterHeavenBot.OuterHeaven;
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
        public bool TrackIsPlaying => GetCurrentPlayer()?.PlayerState == PlayerState.Playing;
        public PlayerState CurrentPlayerState => GetCurrentPlayer()?.PlayerState ?? PlayerState.None;
                                        
        ulong? guildId = null;
        ILogger logger;
        OuterHeavenDiscordClient discordClient;
        OuterHeavenCommandHandler outerHeavenCommandHandler;
        LavaNode lavaNode;
        //todo make this a setting
        TimeSpan timeWaitUntilDisconnect = TimeSpan.FromMinutes(5);
        public MusicService(ILogger<MusicService> logger,
                            OuterHeavenDiscordClient outerHeavenDiscordClient,
                            OuterHeavenCommandHandler outerHeavenCommandHandler,
                            LavaNodeProvider lavaNodeProvider)
        {  
            this.discordClient = outerHeavenDiscordClient;
            this.outerHeavenCommandHandler = outerHeavenCommandHandler;

            this.logger = logger;
            this.lavaNode = lavaNodeProvider.GetLavaNode(); 
            this.discordClient.Ready += DiscordClient_Ready;
            this.discordClient.MessageReceived += DiscordClient_MessageReceived; 
            this.lavaNode.OnLog += Lavanode_OnLog;
            this.lavaNode.OnTrackStuck += Lavanode_OnTrackStuck;
            this.lavaNode.OnTrackException += Lavanode_OnTrackException;
            this.lavaNode.OnTrackStarted += Lavanode_OnTrackStarted;
            this.lavaNode.OnTrackEnded += Lavanode_OnTrackEnded;
            this.lavaNode.OnWebSocketClosed += Lavanode_OnWebSocketClosed; 
        }

        public async Task InitializeAsync() 
        {
            await this.discordClient.InitializeAsync();
            await this.outerHeavenCommandHandler.ApplyCommands();
            logger.LogInfo($"Intialization of {GetType().Name} is complete");
        }
 
        public async Task RequestSong(string searchArgument, SocketCommandContext context)
        {

            var textChannel = context.ToTextChannel();
            var voiceChannel = context.ToVoiceChannel();
 
            if (textChannel == null) return;

            try
            {
                LavaPlayer? player = null;

                if (System.Diagnostics.Debugger.IsAttached && !context.InVoiceChannel())
                {
                    voiceChannel = discordClient.Guilds.First(x => x.Id == textChannel.GuildId).Channels.OfType<IVoiceChannel>().First() as IVoiceChannel;
                    player = await JoinPlayer(voiceChannel, textChannel);                    
                }
                else
                {
                    if (voiceChannel == null || !context.InVoiceChannel())
                    {
                        await textChannel.SendMessageAsync("You must be in a voice channel for this command");
                        return;
                    }
                    else if (BotIsInUseInOtherChannel(context))
                    {
                        await textChannel.SendMessageAsync("The bot is busy");
                        var bot = GetCurrentPlayer();

                        logger.LogError($"User channel: {voiceChannel?.Name} | Bot Channel: {bot?.VoiceChannel?.Name} | Bot State: {bot?.PlayerState}");
                        return;
                    }
                    else
                    {
                        player = await JoinPlayer(voiceChannel, textChannel);
                    }

                    if (player == null)
                    {
                        await textChannel.SendMessageAsync("Error creating music player");
                        return;
                    }
                }
 
                var searchResponse = await SearchRequest(searchArgument);
               
                if (!searchResponse.response.Tracks.Any())
                {
                    logger.LogInformation($"No results found for {searchArgument}"); 
                }
                else if (!string.IsNullOrWhiteSpace(searchResponse.response.Playlist.Name))
                {
                    await ProcessPlayList(searchResponse.response, player, textChannel);
                }
                else
                {
                    await ProcessTrack(searchResponse, player, textChannel);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                await textChannel.SendMessageAsync($"Error joining channel {voiceChannel?.Name}");
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

      
        private async Task DiscordClient_Ready()
        { 
            if (!lavaNode.IsConnected)
            {
                await lavaNode.ConnectAsync();
            }
        }
     
        #endregion

        #region lavanodeEvents 
    
        private Task Lavanode_OnWebSocketClosed(WebSocketClosedEventArgs arg)
        {
            logger.LogInformation($"Websocket Closed reason: {arg.Reason} {arg.Code}");
            return Task.CompletedTask;
        }

        private async Task Lavanode_OnTrackEnded(TrackEndedEventArgs arg)
        {
            logger.LogInformation($"{nameof(Lavanode_OnTrackEnded)} event has been raised. Reason {arg.Reason}");
            if (arg.Reason == TrackEndReason.Finished && 
                arg.Player != null && 
                arg.Player.Queue.TryDequeue(out LavaTrack nextTrack))        
            {
                await arg.Player.PlayAsync(nextTrack);
            }

            if (arg.Reason == TrackEndReason.Stopped) 
            {
                logger.LogInformation($"Stop requested. Clearing {arg.Player?.Queue?.Count() ?? 0} songs from the queue");
                arg.Player?.Queue?.Clear();
            }
         
            if(arg.Player?.Track == null && (arg.Player?.Queue == null || !arg.Player.Queue.Any()))
            {
                logger.LogInformation($"Nothing left to play. Will wait {timeWaitUntilDisconnect} before disconnecting");
                await Task.Run(async () => await CheckForIdelDisconnect(arg.Player));
            }
        }

        private async Task Lavanode_OnTrackStarted(TrackStartEventArgs arg)
        { 
            await arg.Player.TextChannel.SendMessageAsync($"Now playing: {arg.Track.Title} - {arg.Track.Author} - {arg.Track.Duration}");
        }

        private async Task Lavanode_OnTrackException(TrackExceptionEventArgs arg)
        {
            logger.LogError($"Error playing {arg.Track.Title} from {arg.Track.Source}. Error:\n{arg.Exception}");
          
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

        private async Task Lavanode_OnTrackStuck(TrackStuckEventArgs arg)
        {
            logger.LogInformation($"{nameof(Lavanode_OnTrackStuck)} event has been raised. Track name {arg.Track?.Title}. Threshold {arg.Threshold}");
            if (arg.Threshold > TimeSpan.FromSeconds(5))
            {
                await arg.Player.TextChannel.SendMessageAsync($"track {arg.Track?.Title} is stuck. Skipping track...");
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


        public async Task GoTo(TimeSpan time,SocketCommandContext context)
        {
            var channel = context.ToTextChannel();
            var voiceChannel = context.ToVoiceChannel();

            if (context == null || channel == null) return;

            if (voiceChannel == null || !context.InVoiceChannel())
            {
                await channel.SendMessageAsync("You must be in a voice channel for this command");
                return;
            }

            var player = GetCurrentPlayer();
            if (player == null || player.Track == null)
            {
                await channel.SendMessageAsync($"Nothing is playing");
            }
            else if (!player.Track.CanSeek)
            {
                await channel.SendMessageAsync($"Cannot go to time in track");
            }
            else if (time > player.Track.Duration)
            {
                await channel.SendMessageAsync($"Seek time must be smaller than the track duration {player.Track.Duration}");
            }
            else
            {
                await player.SeekAsync(time);
            }
        }
        public async Task RequestQueueClear(int? index, SocketCommandContext context)
        {
            var channel = context.ToTextChannel();
            var voiceChannel = context.ToVoiceChannel();

            if (context == null || channel == null) return;

            if (voiceChannel == null || !context.InVoiceChannel())
            {
                await channel.SendMessageAsync("You must be in a voice channel for this command");
                return;
            }

            var player = GetCurrentPlayer();
           
            if (player == null || player.Queue == null ||!player.Queue.Any())
            {
                await channel.SendMessageAsync("Que is currently empty!");
                return;
            }
            else if (!index.HasValue)
            {
                await channel.SendMessageAsync($"Clearing {player?.Queue?.Count} songs from the queue");
                player?.Queue?.Clear();
            }
            else
            {
                if (index.Value > 0 && index.Value - 1 < player.Queue.Count)
                {
                    var trackToKill = player.Queue.ElementAt(index.Value - 1);

                    player.Queue.Remove(trackToKill);
                    await channel.SendMessageAsync($"Clearing {trackToKill.Title} from the queue");
                }
                else
                {
                    await channel.SendMessageAsync($"Invalid index. Please enter a value between 1 - {player.Queue.Count}");
                }
            }
        }
        public async Task RequestRewind(int seconds, SocketCommandContext? context)
        {
            var player = GetCurrentPlayer();
            var channel = context.ToTextChannel();
            var voiceChannel = context.ToVoiceChannel();

            if (context == null || channel == null) return;

            if (voiceChannel == null || !context.InVoiceChannel())
            {
                await channel.SendMessageAsync("You must be in a voice channel for this command");
                return;
            }
            if (player == null || player.Track == null)
            {
                await channel.SendMessageAsync($"Nothing is playing");
            }
            else if (!player.Track.CanSeek)
            {
                await channel.SendMessageAsync($"Cannot go to time in track");
            }
            else
            {
                //make sure the time requested is greater than the start of the song
                var goToTime = player.Track.Position - TimeSpan.FromSeconds(seconds);

                var time = goToTime >= TimeSpan.FromSeconds(0) ? goToTime :
                                                                 TimeSpan.FromSeconds(0);

                await channel.SendMessageAsync($"Rewinding {time}");
                await player.SeekAsync(time);
            }
        }
        public async Task FastForward(int seconds,SocketCommandContext context)
        {
            var player = GetCurrentPlayer();
            var channel = context.ToTextChannel();
            var voiceChannel = context.ToVoiceChannel();

            if (context == null || channel == null) return;

            if (voiceChannel == null || !context.InVoiceChannel())
            {
                await channel.SendMessageAsync("You must be in a voice channel for this command");
                return;
            }

            if (player == null || player.Track == null)
            {
                await channel.SendMessageAsync($"Nothing is playing");
            }
            else if (!player.Track.CanSeek)
            {
                await channel.SendMessageAsync($"Cannot go to time in track");
            }
            else
            {
                //Make sure the time requested is before the end of the song
                var goToTime = player.Track.Position + TimeSpan.FromSeconds(seconds);

                var time = goToTime <= player.Track.Duration ? goToTime :
                                       player.Track.Duration - TimeSpan.FromMilliseconds(100); 
                                                                                                            
                await channel.SendMessageAsync($"Fast Forwarding {time}");
                await player.SeekAsync(time);
            }
        }
        public string GetCurrentTrackInfo()
        {
            var player = GetCurrentPlayer();
            return player?.Track == null ? "Nothing is playing" : $"Current track: {ConvertTrackToTrackInfo(player.Track)}";
        }      
       
        public List<LavaTrack> GetAllTracks()
        {
            var tracks = new List<LavaTrack>(); 
            var player = GetCurrentPlayer();
            if(player == null || player.Track == null) return tracks;

            tracks.Add(player.Track);
            if(player.Queue.Any()) tracks.AddRange(player.Queue);

            return tracks;
        }
        public async Task RequestSkip(SocketCommandContext context)
        {
            try
            {
               
                var channel = context.ToTextChannel();
                var voiceChannel = context.ToVoiceChannel();

                if(channel== null) return;

                if (voiceChannel == null || !context.InVoiceChannel())
                {
                    await channel.SendMessageAsync("You must be in a voice channel for this command");
                    return;
                }

                var player = GetCurrentPlayer();
                if (player == null || player.Queue == null)
                {
                    await channel.SendMessageAsync($"There's nothing to skip!");
                    return;
                }

                if (player.Track != null)
                {
                    await channel.SendMessageAsync($"Skipping - {player.Track.Title}");
                    if (player.Queue.Any())
                    {
                        var skip = await player.SkipAsync(TimeSpan.FromSeconds(0));
                    }
                    else
                    {
                      await  player.StopAsync();
                    }
                }
                else
                {
                    await channel.SendMessageAsync($"There's nothing to skip!");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
        }
        public async Task<PlayerState> ChangePauseState()
        {
            var player = GetCurrentPlayer();
            if (player == null) return PlayerState.None;

            if (player.PlayerState == PlayerState.Paused)
            {
                await player.ResumeAsync();
                return player.PlayerState;
            }
            if (player.PlayerState == PlayerState.Playing)
            {
                await player.PauseAsync();
                return player.PlayerState;
            }

            return player.PlayerState;
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
        }
        public bool BotIsInUseInOtherChannel(SocketCommandContext context) =>
           context.ToVoiceChannel()?.Id != default && 
           context.ToVoiceChannel()?.Id != GetCurrentPlayer()?.VoiceChannel.Id && 
           GetCurrentPlayer()?.PlayerState == PlayerState.Playing;
      
        private async Task ProcessTrack((SearchResponse response,string argumentsUsed) searchResponse, LavaPlayer? player, ITextChannel commandChannel)
        {
            if(!searchResponse.response.Tracks.Any())
            {
                return;
            }

            LavaTrack? track = null;
            var searchArguement = searchResponse.argumentsUsed.ToLower().Trim();

            if (searchArguement.Contains(".com/"))
            {
                //if we can't find an exact match, use a match that is like our arguments
                track = searchResponse.response.Tracks.FirstOrDefault(x=>x.Url.ToLower().Trim() == searchArguement) ??
                        searchResponse.response.Tracks.FirstOrDefault(x => x.Url.ToLower().Trim().Contains(searchArguement));
            }
            else
            {
                track = searchResponse.response.Tracks.FirstOrDefault(x => x.Title.ToLower().Trim() == searchArguement) ??
                        searchResponse.response.Tracks.FirstOrDefault(x => x.Title.ToLower().Trim().Contains(searchArguement));
            }
            
            if(track == null)
            {
                track = searchResponse.response.Tracks.FirstOrDefault();
                logger.LogInfo($"unable to find a match in search results for arguement {searchArguement}. Using first track in the list [{track?.Title}]");
                logger.LogInfo($"available tracks are {string.Join(Environment.NewLine,searchResponse.response.Tracks.Select(x=>$"{x.Title} | {x.Author} | {x.Url}"))}");
            }
             
            if (track == null || player == null) return;

            if (player.PlayerState == PlayerState.Playing)
            {
                player.Queue.Enqueue(track);
                await commandChannel.SendMessageAsync($"Track {track.Title} has been queued!");  
            }
            else
            {
                await player.PlayAsync(track);
            }
        }
        private async Task ProcessPlayList(SearchResponse searchResponse, LavaPlayer? player, ITextChannel commandChannel)
        {
            if (player == null || !searchResponse.Tracks.Any()) return;

            if(searchResponse.Playlist.SelectedTrack < searchResponse.Tracks.Count && searchResponse.Playlist.SelectedTrack >-1)
            {
                var firstTrack = searchResponse.Tracks.ElementAt(searchResponse.Playlist.SelectedTrack);

                var playlistTrackNames = string.Join(", ", searchResponse.Tracks.Select(x => x.Title).ToList());

                logger.LogInfo($"Search results are a play list. Will process the following tracks - {playlistTrackNames}");

                if (player.PlayerState == PlayerState.Playing)
                {
                    player.Queue.Enqueue(firstTrack);
                    await commandChannel.SendMessageAsync($"Queing playlist {searchResponse.Playlist.Name}");
                }
                else
                {
                    await player.PlayAsync(firstTrack);
                    await commandChannel.SendMessageAsync($"Play list {searchResponse.Playlist.Name} started");
                }

                foreach (var track in searchResponse.Tracks.Where(x=>x.Title != firstTrack.Title && x.Author != firstTrack.Author))
                {
                    player.Queue.Enqueue(track);
                }
            }
        }
        private async Task CheckForIdelDisconnect(LavaPlayer? lavaPlayer)
        {
            var disconnectTime = DateTime.UtcNow.AddMinutes(timeWaitUntilDisconnect.Minutes);
            while (lavaPlayer != null && lavaPlayer.VoiceChannel != null && lavaPlayer.PlayerState != PlayerState.Playing)
            {
                if (DateTime.UtcNow >= disconnectTime)
                {
                    logger.LogInformation("Idle time limit has been reached. Disconnecting");
                    await lavaNode.LeaveAsync(lavaPlayer.VoiceChannel);
                }
            }
        }
        private string ConvertTrackToTrackInfo(LavaTrack track) => $"{track.Title} - {track.Author} - {track.Duration - track.Position} - {track.Url}";

        private async Task<LavaPlayer?> JoinPlayer(IVoiceChannel voiceChannel, ITextChannel textChannel)
        {

            if (this.guildId.HasValue &&                 
                this.guildId.Value != voiceChannel.GuildId && 
                (GetCurrentPlayer()?.IsConnected ?? false))
            {
                await textChannel.SendMessageAsync("OuterHeaven bot can only play in one server at a time.");
                return null;
            }

            this.guildId = voiceChannel.GuildId;         
            var player = GetCurrentPlayer();
            if(player == null)
            {
                player = await lavaNode.JoinAsync(voiceChannel, textChannel) ?? throw new ArgumentNullException(nameof(lavaNode));
                logger.LogInfo($"Player not currently in a channel. Creating player for {player.VoiceChannel.Name}");
            } 
            return player;
        }

        private async Task<(SearchResponse response,string arguementUsed)> SearchRequest(string searchArgument)
        {
            var searchType = searchArgument.Contains("C:\\") && File.Exists(searchArgument) ? SearchType.Direct : SearchType.YouTube;

            logger.LogInformation($"Processing {searchArgument} for search type {searchType}");

            if (searchType == SearchType.Direct)
            {               
                return (await lavaNode.SearchAsync(searchType, searchArgument), searchArgument);
            }

            var urlRequest = Uri.TryCreate(searchArgument, UriKind.Absolute, out Uri? uriResult);
            
            var cleanedArgs = urlRequest && !string.IsNullOrWhiteSpace(uriResult?.Host ?? "") ? uriResult?.Host ?? ""
                                                                                                                       : searchArgument;
            if (urlRequest) 
            {
                var videoId = uriResult?.Query?.Split("&")[0];
                cleanedArgs = cleanedArgs + uriResult?.AbsolutePath + videoId;
                logger.LogInformation($"Url has been requested. Using absolute path {cleanedArgs}");
            }

            var response = await lavaNode.SearchAsync(searchType, cleanedArgs);
            //we only search youtube at the moment. If we don't find results we try youtube music aswell.
            if (!response.Tracks.Any())
            {
                logger.LogInformation("No results found from generic youtube search. Attempting to search youtubemusic");
                response = await lavaNode.SearchAsync(SearchType.YouTubeMusic, cleanedArgs);
            }

            return (response, cleanedArgs);
        }
        private LavaPlayer? GetCurrentPlayer()
        { 
            if (!this.guildId.HasValue)
            {
                return null;
            }

            LavaPlayer? player = null;
           
            //this doesn't work. It will say lava link is connected when it isn't/
            //if (!lavaNode.IsConnected)
            //{
            //  logger.LogInformation("Lavalink is not connected. Forcing reconnect");                
            //  lavaNode.ConnectAsync().GetAwaiter().GetResult();
            //}

            lavaNode?.TryGetPlayer(discordClient.GetGuild(guildId.Value) ?? default, out player);

            return player;
        } 
    }

}
