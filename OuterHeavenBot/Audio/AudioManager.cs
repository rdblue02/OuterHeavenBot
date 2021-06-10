using Discord;
using Discord.Audio;
using Discord.WebSocket;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OuterHeavenBot.Audio
{
    public class AudioManager
    {
           
        public bool Paused { get; private set; }
        public AudioInStream CurrentAudioStream { get; private set; } = null;
        public AudioRequest CurrentRequest { get; private set; } = null;
        ConcurrentQueue<AudioRequest> soundQueue = new ConcurrentQueue<AudioRequest>();
        IAudioClient audioClient;
        IVoiceChannel currentChannel;
        private DiscordSocketClient client;
        public AudioManager(DiscordSocketClient client)
        {
            this.client = client;
        }
        public void QueueSound(AudioRequest soundRequest)
        {
            soundQueue.Enqueue(soundRequest);
        }

        public async Task Listen()
        {
           var result = soundQueue.TryDequeue(out AudioRequest audio);
            if (result)
            {
                this.CurrentRequest = audio;
                await Play();
            }
            else
            {
                if (CurrentRequest != null)
                {
                    await CurrentRequest.RequestingChannel.DisconnectAsync();
                    this.CurrentRequest = null;
                }
                if (this.CurrentAudioStream != null)
                {
                    await this.CurrentAudioStream.FlushAsync();
                    await this.CurrentAudioStream.DisposeAsync();
                    this.CurrentAudioStream = null;
                }
                this.currentChannel = null;
                this.audioClient = null;
            }         
        }
        async Task Play()
        {
           if(currentChannel?.Name != CurrentRequest.RequestingChannel.Name)
           {
               audioClient = await CurrentRequest.RequestingChannel.ConnectAsync();
                currentChannel = CurrentRequest.RequestingChannel;
           }

            using (var fs = File.OpenRead(CurrentRequest.Path))
            using (var discordOutStream = audioClient.CreatePCMStream(AudioApplication.Mixed, 98304, 200))
            {
                try
                {  
                    await fs.CopyToAsync(discordOutStream);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                finally
                {
                    discordOutStream.Flush();
                    fs.Flush();
                }
            }
        }
    }
}
