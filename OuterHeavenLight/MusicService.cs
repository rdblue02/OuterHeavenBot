﻿using Discord;
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
        MusicCommandHandler commandHandler;
        TimeSpan waitUntilDisconnect = TimeSpan.FromSeconds(30); 
        bool isPlaying = false; 

        public MusicService(ILogger<MusicService> logger, 
                            Lava lava, 
                            DiscordClientProvider clientProvider,
                            MusicCommandHandler commandHandler) 
        {
            this.logger = logger;
            this.lava = lava;  
            this.queuedTracks = new ConcurrentQueue<LavaTrack>();
            this.client = clientProvider.GetMusicClient() ?? throw new ArgumentNullException(nameof(DiscordSocketClient));  
            this.commandHandler = commandHandler;
            this.client.MessageReceived += async (arg) => { await commandHandler.HandleMessage(client, arg); };
            this.lava.OnLavaTrackEndEvent += Lava_OnLavaTrackEndEvent;
            this.lava.OnLavaTrackStartEvent += Lava_OnLavaTrackStartEvent;  
        }
  
        public async Task Initialize()
        {
            logger.LogInformation("Initializing music service");
            await commandHandler.Initialize(new List<Type>() { typeof(MusicCommands) }); 
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

            if (this.queuedTracks.IsEmpty && !isPlaying)
            {
                logger.LogInformation($"Queue is empty. Processing {result.LoadType}.");
                await lava.UpdatePlayer(new UpdatePlayerTrack() { encoded = firstTrack.encoded, userData = userData });
            }
            else
            {
                logger.LogInformation($"Bot is active. Adding {result.LoadType} to music queue.");
                await context.Channel.SendMessageAsync($"Bot is active. Adding {result.LoadType} to music queue.");
                this.queuedTracks.Enqueue(firstTrack);
            }
             
            if (result.LoadType == LavalinkLoadType.playlist)
            {
                await context.Channel.SendMessageAsync($"Bot is active. Queuing playlist {result.PlaylistInfo?.Name}");
                foreach (var item in result.LoadedTracks.Skip(1))
                {
                    item.UserData = new Userdata() { Data = context.Channel.Id.ToString() };
                    this.queuedTracks.Enqueue(item);
                }
            } 
        }
       
        private async Task Lava_OnLavaTrackStartEvent(TrackStartWebsocketEvent arg)
        { 
            logger.LogInformation($"Now playing {arg.Track.info.title}. Command channel {arg.IssuedCommandChannelId}");
            isPlaying = true;
           
            await SendMessageToExecutingCommandChannel(arg.Track?.UserData, $"Now playing track - {arg?.Track?.info?.title ?? "error"}"); 
        }

        private async Task Lava_OnLavaTrackEndEvent(TrackEndWebsocketEvent arg)
        {
            logger.LogInformation($"Track {arg.Track?.info?.title} has ended. Reason [{arg.Reason}]");
             
            if (arg.Reason == LavalinkTrackEndReason.replaced)
            {
                //handled in skip command logic
                return;
            } 

            if (arg.Reason == LavalinkTrackEndReason.invalid ||
                arg.Reason == LavalinkTrackEndReason.cleanup)
            {
                await SendMessageToExecutingCommandChannel(arg.Track?.UserData, $"Error playing track title {arg?.Track?.info?.title ?? "error"}");

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

            await this.lava.UpdatePlayer(new UpdatePlayerTrack() { encoded = next.encoded, userData = next.UserData});
        }

        Task SendMessageToExecutingCommandChannel(Userdata? userdata, string message)
        {
            var channel = ulong.TryParse(userdata?.Data?.ToString(), out var channelId) ? this.client.GetChannel(channelId) as ITextChannel : null;

            if (channel == null)
                return Task.CompletedTask;

            return channel.SendMessageAsync(message);
        }
    }
}