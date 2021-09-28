using Discord.Commands;
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
    public class AudioService
    {
        private Stopwatch applicationTimer = new Stopwatch();


        public LavaPlayer activeLavaPlayer { get; private set; } 
        public TimeSpan LavaPlayerIdelTime = TimeSpan.FromSeconds(0);
        public bool ClippiePlaying { get; set; }
        public bool BlockClippies { get; private set; }
        public bool WillQueue => activeLavaPlayer != null && (activeLavaPlayer?.PlayerState == PlayerState.Playing ||
                activeLavaPlayer?.PlayerState == PlayerState.Paused);
        public PlayerState CurrentState => activeLavaPlayer?.PlayerState ?? PlayerState.None;
        public string CurrentTrackName => activeLavaPlayer?.Track?.Title;
        public void SetPlayer(LavaPlayer player)
        {
            if(player != null)
            {
                this.activeLavaPlayer = player;
            }
        }
        public TimeSpan GetElapsedTime()
        {
            return applicationTimer.Elapsed;
        }

        public async Task ProcessTrack(LavaTrack lavaTrack)
        {         
            if(WillQueue)
            {
                this.activeLavaPlayer.Queue.Enqueue(lavaTrack);
            }
            else
            {
              await activeLavaPlayer?.PlayAsync(lavaTrack);
            }
        }    
    
        public async Task<bool> ChangePauseState()
        {
            if(CurrentState == PlayerState.Paused)
            {
             await  this.activeLavaPlayer.PauseAsync();
                return true;
            }
            else if(CurrentState == PlayerState.Playing)
            {
                await this.activeLavaPlayer.ResumeAsync();
                return true;
            }
            else
            {
                return false;
            }
        }
        public async Task Skip()
        {
            if (this.activeLavaPlayer.Queue.Any())
            {
              await  this.activeLavaPlayer.SkipAsync();
            }
            else
            {
              await this.activeLavaPlayer.StopAsync();
            }
        }        
    
    }
}
