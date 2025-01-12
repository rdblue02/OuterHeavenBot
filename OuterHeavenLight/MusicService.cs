using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using OuterHeaven.LavalinkLight; 
using OuterHeavenLight.Entities;
using OuterHeavenLight.Entities.Request;
using OuterHeavenLight.Entities.Response.Websocket;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenLight
{
    public class MusicService
    {
        ILogger<MusicService> logger;
        Lava lava;
        ConcurrentQueue<LavaTrack> queuedTracks;   
        DiscordSocketClient client;
        CommandHandler commandHandler;
        TimeSpan waitUntilDisconnect = TimeSpan.FromSeconds(10); 
        bool isPlaying = false;
        public MusicService(ILogger<MusicService> logger, 
                            Lava lava, 
                            DiscordSocketClient client,
                            CommandHandler commandHandler) 
        {
            this.logger = logger;
            this.lava = lava;  
            this.queuedTracks = new ConcurrentQueue<LavaTrack>();
            this.client = client;
            this.commandHandler = commandHandler;
            this.client.MessageReceived += async (arg) => { await commandHandler.HandleMessage(client, arg); };
            this.lava.OnLavaTrackEndEvent += Lava_OnLavaTrackEndEvent;
            this.lava.OnLavaTrackStartEvent += Lava_OnLavaTrackStartEvent;  
        }
  
        public async Task Initialize()
        {
            logger.LogInformation("Initializing music service");
            await commandHandler.Initialize(new List<Type>() { typeof(BotCommands) }); 
        }

        public LavaTrackInfo? GetCurrentTrackInfo()
        {
            if(!lava.IsPlaying)
            {
                logger.LogInformation($"Skip requested for player when {nameof(lava.IsPlaying)} set to {lava.IsPlaying}");
                return null;
            }

            return lava?.ActiveTrack?.info;
        }

        public QueueInfoMessageBuilder GetQeueueInfo()
        {
            return new QueueInfoMessageBuilder(this.queuedTracks.ToList());
        }

        public string ClearQueue(int? position = null)
        {
            var result = $"Queue is empty, nothing to clear.";
            if (!isPlaying || this.queuedTracks.IsEmpty)
            {
                return result ;
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
                result = $"Invalid index {position.Value}. Select a song within queue range {queuedTracks.Count +1}";
            }
            else
            {
                var temp = this.queuedTracks.ToList();
                temp.RemoveAt(position.Value - 1);
                this.queuedTracks = new ConcurrentQueue<LavaTrack>(temp); 
            }

            return result;
        }

        public async Task Skip()
        {
            if(!isPlaying)
            {
                logger.LogError("Bot is not currently playing");
                return;
            }
             
            if (this.queuedTracks.TryDequeue(out var next))
            {
                await this.lava.UpdatePlayer(new UpdatePlayerTrack() { encoded = next.encoded });
            }
            else
            {
                await this.lava.StopPlayer();
            }
        }
         
        public async Task Query(SocketCommandContext context, string query)
        {
            var commandUserVoice = context.User as IVoiceState;
         
            if (commandUserVoice?.VoiceChannel == null) 
            {
                logger.LogError("Must be in a channel for this command");
                return;
            }

            var searchType = LavalinkSearchType.Raw; // query.StartsWith("https") ? LavalinkSearchType.Raw : LavalinkSearchType.ytsearch;
        
            var result = await lava.SearchForTracks(query, searchType);

            if (result.LoadType == LavalinkLoadType.error)
            {
                logger.LogInformation($"Error playing {query}");
                return;
            }

            if (result.LoadedTracks.Count == 0 ||
               result.LoadType == LavalinkLoadType.empty)
            {
                logger.LogInformation($"No matches found for {query}");
                return;
            }

            var botChannel = context.Guild.CurrentUser.VoiceChannel;
             
            if (botChannel == null || botChannel.Id != commandUserVoice.VoiceChannel.Id)
            {
               await commandUserVoice.VoiceChannel.ConnectAsync(true, true, true, true);
            }

            var firstTrack = result.LoadedTracks.First();
            logger.LogInformation($"Found track {firstTrack.info.title} from source {firstTrack.info.uri}");

            if (this.queuedTracks.IsEmpty && !isPlaying)
            {
                logger.LogInformation($"Queue is empty. Processing {result.LoadType}.");
                await lava.UpdatePlayer(new UpdatePlayerTrack() { encoded = firstTrack.encoded});
            }
            else
            {
                logger.LogInformation($"Bot is active. Adding {result.LoadType} to music queue.");
                this.queuedTracks.Enqueue(firstTrack); 
            }
             
            if (result.LoadType == LavalinkLoadType.playlist)
            {
                foreach (var item in result.LoadedTracks.Skip(1))
                {
                    this.queuedTracks.Enqueue(item);
                }
            } 
        }
       
        private Task Lava_OnLavaTrackStartEvent(TrackStartWebsocketEvent arg)
        { 
            logger.LogInformation($"Now playing {arg.Track.info.title}"); 
            isPlaying = true;
            return Task.CompletedTask;
        }

        private async Task Lava_OnLavaTrackEndEvent(TrackEndWebsocketEvent arg)
        {
            logger.LogInformation($"Track {arg.Track?.info?.title} has ended. Reason [{arg.Reason}]");

            if (arg.Reason == LavalinkTrackEndReason.replaced ||
               arg.Reason == LavalinkTrackEndReason.invalid ||
               arg.Reason == LavalinkTrackEndReason.cleanup)
            {
                return;
            }

            if (arg.Reason == LavalinkTrackEndReason.stopped ||
                !this.queuedTracks.TryDequeue(out var next))
            {
                queuedTracks.Clear();
                this.isPlaying = false;

                await Task.Run(async () =>
                {
                    var disconnectTime = DateTime.UtcNow.Add(this.waitUntilDisconnect);

                    while (!isPlaying && DateTime.UtcNow < disconnectTime)
                    {
                        await Task.Delay(100);
                    }

                    if (!isPlaying)
                    {
                        logger.LogInformation("Idle timer has been reached. Disconnecting bot");
                        await lava.DisconnectFromChannel();
                    } 
                });
                return;
            }

            await this.lava.UpdatePlayer(new UpdatePlayerTrack() { encoded = next.encoded });
        } 
    }
}