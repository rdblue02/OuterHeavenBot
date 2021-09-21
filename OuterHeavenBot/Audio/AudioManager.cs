using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OuterHeavenBot.Audio
{
    public class AudioManager
    {
        public bool Paused { get; private set; }
        public bool PlayingMusic => CurrentRequest != null && CurrentRequest.GetType() == typeof(MusicRequest);
        public IAudioRequest CurrentRequest { get; private set; } = null;
        ConcurrentQueue<IAudioRequest> soundQueue = new ConcurrentQueue<IAudioRequest>();
        IAudioClient audioClient;
        SocketCommandContext currentContext;
        private const int bufferLength = 200;
        CancellationTokenSource songCancellation;
        CancellationTokenSource audioCancellation;

        public List<(string, string)> GetQueue()
        {
            var songs = new List<(string, string)>();
            var qued = this.soundQueue.Select((x, index) => ((index + 1).ToString(), x.Name)).ToList();

            if (this.CurrentRequest != null)
            {
                songs.Add(("Playing", this.CurrentRequest?.Name));
                songs.AddRange(qued);
            }
            return songs;
        }
        public void RequestSkip()
        {
            // songCancellation?.Cancel();
            audioClient.StopAsync().Wait();
        }
        public void RequestStop()
        {
            this.audioCancellation?.Cancel();
            this.songCancellation?.Cancel();
        }
        public async Task QueueSound(IAudioRequest soundRequest)
        {
            soundQueue.Enqueue(soundRequest);
            if (CurrentRequest == null)
            {
                audioCancellation = new CancellationTokenSource();
                await Play(audioCancellation.Token);
            }
        }

        public async Task ConnectForAudio(SocketCommandContext context)
        {
            SocketGuildUser user = context.User as SocketGuildUser; // Get the user who executed the command
            IVoiceChannel channel = user.VoiceChannel;
            var clientUser = await context.Channel.GetUserAsync(context.Client.CurrentUser.Id); // Find the client's current user (I.e. this bot) in the channel the command was executed in
            bool shouldConnect = false;

            if (this.audioClient == null)
            {
                shouldConnect = true;
            }

            if (clientUser != null)
            {
                if (clientUser is IGuildUser bot) // Cast the client user so we can access the VoiceChannel property
                {
                    if (bot.VoiceChannel == null)
                    {
                        Console.WriteLine("Bot is not in any channels");
                        shouldConnect = true;
                    }
                    else if (bot.VoiceChannel.Id == channel.Id)
                    {
                        Console.WriteLine($"Bot is already in requested channel: {bot.VoiceChannel.Name}");
                    }
                    else
                    {
                        Console.WriteLine($"Bot is in channel: {bot.VoiceChannel.Name}");
                        shouldConnect = true;
                    }
                }
            }
            if (shouldConnect)
            {
                this.audioClient = await channel.ConnectAsync();
                this.currentContext = context;
            }
        }

        async Task Play(CancellationToken playerToken)
        {
            while (this.soundQueue.Any())
            {
                if (playerToken.IsCancellationRequested)
                {
                    await Disconnect();
                    break;
                }
                songCancellation = CancellationTokenSource.CreateLinkedTokenSource(audioCancellation.Token);    
                soundQueue.TryDequeue(out IAudioRequest audioRequest);
                this.CurrentRequest = audioRequest;

                using var discordOutStream = audioClient.CreatePCMStream(AudioApplication.Mixed, 98304, bufferLength);                
                try
                {
                    try
                    {
                      await ProcessAudio(songCancellation.Token, discordOutStream);                   
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                    finally
                    {
                        await discordOutStream.FlushAsync();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            await Disconnect();
        }
        private async Task Disconnect()
        {
            this.soundQueue.Clear();
            this.Paused = false;
            CurrentRequest = null;
            await (this.currentContext.User as SocketGuildUser)?.VoiceChannel?.DisconnectAsync();
        }
        private async Task ProcessAudio(CancellationToken token, AudioOutStream discord)
        {
            //var bytes =  await this.CurrentRequest.GetAudioBytes();           
            //int index = 0;

            //while (index < bytes.Length && !token.IsCancellationRequested)
            //{
            //    var partBytes = new byte[100];
            //    for (int i = 0; i < 100 && !token.IsCancellationRequested && index<bytes.Length; i++)
            //    {
            //        partBytes[i] = bytes[index++];
            //    }
            //    await discord.WriteAsync(partBytes);
            //}
            //

            await discord.WriteAsync(await this.CurrentRequest.GetAudioBytes());
        }
    }
}
