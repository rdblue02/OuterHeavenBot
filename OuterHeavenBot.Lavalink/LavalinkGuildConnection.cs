using OuterHeavenBot.Lavalink.EventArgs;
using Discord.Utils;
using Discord;
using OuterHeavenBot.Lavalink.Entities;
using Newtonsoft.Json;
using Microsoft.VisualBasic;
using Discord.WebSocket;
namespace OuterHeavenBot.Lavalink
{
    public class LavalinkGuildConnection
    {
        public event Func<LavalinkGuildConnection, PlaybackStartedEventArgs, Task>? OnTrackStart;
        public event Func<LavalinkGuildConnection, PlaybackFinishedEventArgs, Task>? OnTrackFinish;
        public event Func<LavalinkGuildConnection, PlaybackExceptionEventArgs, Task>? OnTrackException;
        public event Func<LavalinkGuildConnection, PlaybackStuckEventArgs, Task>? OnTrackStuck;
        public event Func<LavalinkGuildConnection, PlayerUpdateEventArgs, Task>? OnPlayerUpdate;
        public event Func<LavalinkGuildConnection, PlayerWebsocketClosedEventArgs, Task>? OnWebsocketClosed; 

        public IVoiceState? VoiceState { get; private set; }
        public bool IsConnected => VoiceState != null && 
                    VoiceState.VoiceChannel != null;
        public LavalinkPlayerState? State { get; internal set; } 
        private readonly LavalinkNode node;
        

        public LavalinkGuildConnection(LavalinkNode node, 
                                       IVoiceState voiceState)
        {
            this.node = node; 
            this.VoiceState = voiceState;
        } 

        public async Task PlayTrackAsync(LavalinkTrack track)
        {
            ThrowOnNotReady();
            await node.Rest.UpdatePlayer(VoiceState?.VoiceChannel?.GuildId ?? 0, new LavalinkPlayerUpdatePayload
            {
                EncodedTrack = track.Encoded
            });
        }
      
        public async Task LeaveAsync()
        {
            ThrowOnNotReady();
            await node.Rest.DestroyPlayer(VoiceState.VoiceChannel.GuildId);
            await VoiceState.VoiceChannel.DisconnectAsync();
        }

        public async Task RemoveActiveTrackAsync()
        {
            ThrowOnNotReady();
            await node.Rest.UpdatePlayer(VoiceState?.VoiceChannel?.GuildId ?? 0, new LavalinkPlayerUpdatePayload
            {
                EncodedTrack = null
            });
        }

        public async Task PauseAsync()
        {
            ThrowOnNotReady();
            await node.Rest.UpdatePlayer(VoiceState?.VoiceChannel?.GuildId ?? 0, new LavalinkPlayerUpdatePayload
            {
                Paused = true
            });
        }

        public async Task ResumeAsync()
        {
            ThrowOnNotReady();
            await node.Rest.UpdatePlayer(VoiceState?.VoiceChannel?.GuildId ?? 0, new LavalinkPlayerUpdatePayload
            {
                Paused = false
            });
        }

        public async Task SeekAsync(TimeSpan position)
        {
            ThrowOnNotReady();
            await node.Rest.UpdatePlayer(VoiceState?.VoiceChannel?.GuildId ?? 0, new LavalinkPlayerUpdatePayload
            {
                Position = position.Milliseconds
            });
        }

        public async Task SetVolumeAsync(int volume)
        {
            ThrowOnNotReady();
            await node.Rest.UpdatePlayer(VoiceState?.VoiceChannel?.GuildId ?? 0, new LavalinkPlayerUpdatePayload
            {
                Volume = volume
            });
        }

        public async Task SetFilterVolumeAsync(int volume)
        {
            ThrowOnNotReady();
            await node.Rest.UpdatePlayer(VoiceState?.VoiceChannel?.GuildId ?? 0, new LavalinkPlayerUpdatePayload
            {
                Filters = new()
                {
                    Volume = volume
                }
            });
        }
         
        public async Task HandleLavalinkEvent<T>(T eventPayload)
        {
            if (eventPayload is PlaybackStartedEventArgs pbevent)
            {
                await OnTrackStart.FireEventAsync(this, pbevent); 
            }
            else if (eventPayload is PlaybackFinishedEventArgs pfevent)
            {
                await OnTrackFinish.FireEventAsync(this, pfevent);                
            }
            else if (eventPayload is PlaybackExceptionEventArgs peevent)
            {
                await OnTrackException.FireEventAsync(this, peevent); 
            }
            else if (eventPayload is PlayerUpdateEventArgs puevent)
            {
                await OnPlayerUpdate.FireEventAsync(this, puevent); 
            }
            else if (eventPayload is PlayerWebsocketClosedEventArgs pwcevent)
            {
                await OnWebsocketClosed.FireEventAsync(this, pwcevent); 
            }
            else
            {
                throw new InvalidOperationException($"invalid event type {typeof(T)}");
            }
        } 
        void ThrowOnNotReady()
        {
            if (!node.IsReady || !IsConnected)
            {
                throw new InvalidOperationException("Node is not ready or not connected");
            }
        }
    }
}