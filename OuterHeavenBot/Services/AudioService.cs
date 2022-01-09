using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Victoria;
using Victoria.Enums;
namespace OuterHeavenBot.Services
{
    public class AudioService : IDisposable
    {
        public LavaPlayer activeLavaPlayer { get; private set; }
        public TimeSpan LavaPlayerIdelTime = TimeSpan.FromSeconds(0);
        public bool ClippiePlaying { get; set; }
        public bool BlockClippies { get; private set; }
        public bool WillQueue => activeLavaPlayer != null && (activeLavaPlayer?.PlayerState == PlayerState.Playing ||
                activeLavaPlayer?.PlayerState == PlayerState.Paused);
        public PlayerState CurrentState => activeLavaPlayer?.PlayerState ?? PlayerState.None;
        public string CurrentTrackName => activeLavaPlayer?.Track?.Title;
        public string CurrentTrackTimeRemaining => $"{activeLavaPlayer?.Track?.Position} | {activeLavaPlayer?.Track?.Duration}";
        private LavaNode lavaNode;
        private Task connectingTask = null;
        private bool isDisposing = false;
        public void Initialize(LavaNode lavaNode)
        {
            this.lavaNode = lavaNode;
        }

        public async Task<AudioActionResult> ProcessTrack(LavaTrack lavaTrack, IVoiceState requester, ITextChannel textChannel)
        {
            if (!lavaNode.IsConnected)
            {
                try
                {
                    if (connectingTask != null)
                    {
                        await connectingTask;
                    }
                    else
                    {
                        connectingTask = Task.Delay(2100);
                        await connectingTask;
                    }

                    if (!lavaNode.IsConnected)
                    {
                        await lavaNode.ConnectAsync();

                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return AudioActionResult.Error;
                }
            }

            if (requester?.VoiceChannel != null && (this.activeLavaPlayer == null ||
              this.activeLavaPlayer?.VoiceChannel?.Name != requester.VoiceChannel.Name))
            {
                this.activeLavaPlayer = await lavaNode.JoinAsync(requester.VoiceChannel, textChannel);
            }
            this.activeLavaPlayer = lavaNode.GetPlayer(requester.VoiceChannel.Guild);
            if (WillQueue)
            {
                this.activeLavaPlayer.Queue.Enqueue(lavaTrack);
                return AudioActionResult.Queing;
            }
            else
            {
                await activeLavaPlayer?.PlayAsync(lavaTrack);
            }
            return AudioActionResult.Playing;
        }

        public bool IsPlayerInAnotherChannel(IVoiceState context)
        {
            return activeLavaPlayer != null &&

                   activeLavaPlayer?.VoiceChannel?.Name != context?.VoiceChannel?.Name &&
                   (activeLavaPlayer?.Track != null || activeLavaPlayer?.PlayerState == PlayerState.Playing);
        }

        public async Task<PlayerState> ChangePauseState()
        {
            if (CurrentState == PlayerState.Paused)
            {
                await this.activeLavaPlayer.ResumeAsync();
                return this.activeLavaPlayer.PlayerState;
            }
            else if (CurrentState == PlayerState.Playing)
            {
                await this.activeLavaPlayer.PauseAsync();
                return this.activeLavaPlayer.PlayerState;
            }
            else
            {
                return this.activeLavaPlayer.PlayerState;
            }
        }
        public async Task<string> Skip()
        {
            string current = "";
            if (this.activeLavaPlayer.Queue.Any())
            {
                current = (await this.activeLavaPlayer.SkipAsync()).Current?.Title;
            }
            else
            {
                await this.activeLavaPlayer.StopAsync();
            }
            return current;
        }

        public void Dispose()
        {
            try
            {
                if (this.isDisposing)
                {
                    return;
                }

                this.isDisposing = true;
                if (lavaNode != null)
                {
                    this.lavaNode.DisposeAsync().GetAwaiter().GetResult();
                }
                if (this.activeLavaPlayer != null)
                {
                    this.activeLavaPlayer.DisposeAsync().GetAwaiter().GetResult();
                }
            }
            finally { }
        }
    }
}
