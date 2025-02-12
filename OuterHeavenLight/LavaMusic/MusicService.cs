using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using OuterHeavenLight.Dev;
using OuterHeavenLight.Entities;
using OuterHeavenLight.Entities.Request;
using OuterHeavenLight.Entities.Response.Websocket;
using OuterHeavenLight.LavaConnection;
using OuterHeavenLight.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace OuterHeavenLight.Music
{
    public class MusicService
    {
        ILogger<MusicService> logger;
        Lava lava;
        ConcurrentQueue<LavaTrack> queuedTracks;
        MusicDiscordClient client;
        MusicCommandHandler musicCommandHandler; 
        DevCommandHandler devCommandHandler;
        
        public MusicService(ILogger<MusicService> logger,
                            Lava lava,
                            MusicDiscordClient client,
                            MusicCommandHandler musicCommandHandler,
                            DevCommandHandler devCommandHandler)
        {
            this.logger = logger;
            this.lava = lava;
            queuedTracks = new ConcurrentQueue<LavaTrack>();
            this.client = client;
            this.musicCommandHandler = musicCommandHandler;
            this.devCommandHandler = devCommandHandler;
            client.MessageReceived += async (messageParam) =>   
            {
               var message = messageParam as SocketUserMessage;

                if (message != null && musicCommandHandler.ShouldExecuteCommand(client, messageParam))
                {
                    await musicCommandHandler.HandleCommandAsync(client, message);
                    return;
                }
             
                if (message != null && devCommandHandler.ShouldExecuteCommand(client, messageParam))
                {
                    await devCommandHandler.HandleCommandAsync(client, message);
                    return;
                }              
            };

            this.lava.OnLavaTrackEndEvent += Lava_OnLavaTrackEndEvent;
            this.lava.OnLavaTrackStartEvent += Lava_OnLavaTrackStartEvent;
        }

        public async Task Initialize()
        {
            logger.LogInformation("Initializing music service");
            await Task.WhenAll(musicCommandHandler.InstallCommandsAsync(new List<Type>() { typeof(MusicCommands) }),
                     devCommandHandler.InstallCommandsAsync(new List<Type>() { typeof(DevCommands) }));

        }

        public LavaTrackInfo? GetCurrentTrackInfo()
        {
            if (!lava.IsPlaying)
            {
                logger.LogInformation($"Track info requested for player when {nameof(lava.IsPlaying)} set to {lava.IsPlaying}");
                return null;
            }

            return lava?.ActiveTrack?.info;
        }

        public string GetQeueueInfo()
        { 
            var tracks = new List<LavaTrack>();

            if (lava.ActiveTrack != null)
            {
                tracks.Add(lava.ActiveTrack);
            }

            tracks.AddRange(queuedTracks.ToList());

            return new QueueInfoMessageBuilder(tracks).Build();
        }

        public string ClearQueue(int? position = null)
        {
            var result = $"Queue is empty, nothing to clear.";
            if (queuedTracks.IsEmpty)
            {
                return result;
            }

            if (!position.HasValue)
            {
                result = $"Clearing {queuedTracks.Count} tracks from the queue";
                queuedTracks.Clear();
                return result;
            }

            //users enter a 1 base index instead of zero based          
            if (position.Value - 1 < 0 || position.Value > queuedTracks.Count - 1)
            {
                result = $"Invalid index {position.Value}. Select a song within queue range {queuedTracks.Count + 1}";
            }
            else
            {
                var temp = queuedTracks.ToList();
                temp.RemoveAt(position.Value - 1);
                queuedTracks = new ConcurrentQueue<LavaTrack>(temp);
            }

            return result;
        }
       
        public async Task Skip()
        {
            if (!lava.IsPlaying)
            {
                logger.LogError("Bot is not currently playing");
                return;
            }

            if (queuedTracks.TryDequeue(out var next))
            {
                await lava.UpdatePlayer(new UpdatePlayerTrack() { encoded = next.encoded });
            }
            else
            {
                await lava.StopPlayer();
            }
        }

        public async Task<string> PauseResume()
        {
            if (!lava.IsPlaying)
            {
                logger.LogError("Bot is not currently playing");
                return "Bot is not currently playing";
            }
    
            var result = await lava.PauseResume();

            if (result == null) 
            {
                logger.LogError("Cannot update null player");
                return "Error updating player";
            }
             
           return result.paused ? $"Pausing {result.track.info.title}" : $"Resuming {result.track.info.title}";
        }

        public async Task<string> PlayLocalFile(SocketCommandContext context, string path)
        {
            var commandUserVoice = (context.User as IVoiceState)?.VoiceChannel ?? (System.Diagnostics.Debugger.IsAttached ? context.Guild.Channels.OfType<IVoiceChannel>()
                                                                                                                                                .FirstOrDefault(x => x.Name == "audiotest") : null);
            if (commandUserVoice == null)
            {
                logger.LogError("Must be in a channel for this command");
                await context.Channel.SendMessageAsync("Must be in a channel for this command");
                return "Must be in a channel for this command";
            }

            FileInfo? musicFile = null;

            if (File.Exists(path))
            {
                musicFile = new FileInfo(path);
            }
            else
            {
                var musicDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), "music");

                if (!Directory.Exists(musicDirectoryPath))
                {
                    return $"Cannot find music directory {path}";
                }

                var musicDirectory = new DirectoryInfo(musicDirectoryPath);
                var musicFiles = musicDirectory.GetFiles();

                musicFile = musicFiles.FirstOrDefault(x => string.Equals(x.FullName, path, StringComparison.OrdinalIgnoreCase)) ??
                            musicFiles.FirstOrDefault(x => string.Equals(x.Name, path, StringComparison.OrdinalIgnoreCase)) ??
                            musicFiles.FirstOrDefault(x => x.Name.ToLower().Contains(path.ToLower()));
            }

            if (musicFile == null)
            {
                return $"Unable to find file {path}";
            }
         
            var searchResult = await lava.SearchForTracks(musicFile.FullName, LavalinkSearchType.Raw);
            var track = searchResult.LoadedTracks?.FirstOrDefault();
          
            if (track == null) 
            {
                return $"Error loading local track for {musicFile.FullName}";
            }

            track.info.title = musicFile.Name;

            if (lava.IsPlaying)
            {
                this.queuedTracks.Enqueue(track);
                return $"Local track {musicFile.Name} has been queued";
            }

            var botChannel = context.Guild.CurrentUser.VoiceChannel;

            if (botChannel == null || botChannel.Id != commandUserVoice.Id)
            {
                await commandUserVoice.ConnectAsync(true, true, true, true);
            }

            await this.lava.UpdatePlayer(new UpdatePlayerTrack() { encoded = track.encoded});
            return $"Loading local track {musicFile.Name}";
        }

        public async Task Query(SocketCommandContext context, string query)
        {
            var commandUserVoice = (context.User as IVoiceState)?.VoiceChannel ?? (System.Diagnostics.Debugger.IsAttached ? context.Guild.Channels.OfType<IVoiceChannel>()
                                                                                                                                                  .FirstOrDefault(x => x.Name == "audiotest") : null);
            if (commandUserVoice == null)
            {
                logger.LogError("Must be in a channel for this command");
                await context.Channel.SendMessageAsync("Must be in a channel for this command");
                return;
            }

            var searchType = query.ToLower().Contains("https") ? LavalinkSearchType.Raw : LavalinkSearchType.ytsearch;

            var result = await lava.SearchForTracks(query, searchType);

            if (result.LoadType == LavalinkLoadType.error)
            {
                logger.LogInformation($"Error playing {query}");
                await context.Channel.SendMessageAsync($"Error playing {query}");
                return;
            }

            if (result.LoadedTracks.Count == 0 ||
               result.LoadType == LavalinkLoadType.empty)
            {
                logger.LogInformation($"No matches found for {query}");

                await context.Channel.SendMessageAsync($"No matches found for {query}");
                return;
            }

            var botChannel = context.Guild.CurrentUser.VoiceChannel;

            if (botChannel == null || botChannel.Id != commandUserVoice.Id)
            {
                await commandUserVoice.ConnectAsync(true, true, true, true);
            }

            var userData = new Userdata() { Data = context.Channel.Id.ToString() };
            var firstTrack = result.LoadedTracks.First();
            firstTrack.UserData = userData;

            logger.LogInformation($"Found track {firstTrack.info.title} from source {firstTrack.info.uri}");

            if (queuedTracks.IsEmpty && !lava.IsPlaying)
            {
                logger.LogInformation($"Queue is empty. Processing {result.LoadType}.");
                await lava.UpdatePlayer(new UpdatePlayerTrack() { encoded = firstTrack.encoded, userData = userData });
            }
            else
            {
                logger.LogInformation($"Bot is active. Adding {result.LoadType} to music queue.");
                await context.Channel.SendMessageAsync($"Bot is active. Adding {result.LoadType} to music queue.");
                queuedTracks.Enqueue(firstTrack);
            }

            if (result.LoadType == LavalinkLoadType.playlist)
            {
                await context.Channel.SendMessageAsync($"Bot is active. Queuing playlist {result.PlaylistInfo?.Name}");
                foreach (var item in result.LoadedTracks.Skip(1))
                {
                    item.UserData = new Userdata() { Data = context.Channel.Id.ToString() };
                    queuedTracks.Enqueue(item);
                }
            }
        }

        private async Task Lava_OnLavaTrackStartEvent(TrackStartWebsocketEvent arg)
        { 
            var title = arg.Track.info.title;

            var isUnknown = arg.Track.info.title.ToLower().Contains("unknown");
            if (isUnknown && 
                Uri.TryCreate(arg.Track.info.identifier,UriKind.Absolute, out var uri) && uri != null && uri.IsFile)
            {
                title = uri.Segments.LastOrDefault() ?? "";
                if(this.lava.ActiveTrack !=null)
                this.lava.ActiveTrack.info.title = title;
            }

            logger.LogInformation($"Now playing {title}. Command channel {arg.IssuedCommandChannelId}");

            await SendMessageToExecutingCommandChannel(arg.Track?.UserData, $"Now playing track - {title ?? "error"}");
           
        }

        private async Task Lava_OnLavaTrackEndEvent(TrackEndWebsocketEvent arg)
        {
            logger.LogInformation($"Track {arg.Track?.info?.title} has ended. Reason [{arg.Reason}]");
           
            //handled in skip command logic
            if (arg.Reason == LavalinkTrackEndReason.replaced)
            { 
                return;
            }

            if (arg.Reason == LavalinkTrackEndReason.invalid)
            {
                await SendMessageToExecutingCommandChannel(arg.Track?.UserData, $"Error playing track title {arg?.Track?.info?.title ?? "error"}");
                return;
            }

            if (queuedTracks.TryDequeue(out var next))
            {
                await lava.UpdatePlayer(new UpdatePlayerTrack() { encoded = next.encoded, userData = next.UserData });
                return;
            } 
        }

        Task SendMessageToExecutingCommandChannel(Userdata? userdata, string message)
        {
            var channel = ulong.TryParse(userdata?.Data?.ToString(), out var channelId) ? client.GetChannel(channelId) as ITextChannel : null;

            if (channel == null)
                return Task.CompletedTask;

            return channel.SendMessageAsync(message);
        }
    }
}