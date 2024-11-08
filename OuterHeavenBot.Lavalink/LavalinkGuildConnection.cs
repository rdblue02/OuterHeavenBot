using OuterHeavenBot.Lavalink.EventArgs;
using Discord.Utils;
using Discord;
using OuterHeavenBot.Lavalink.Entities;
using Newtonsoft.Json;
using Microsoft.VisualBasic;
namespace OuterHeavenBot.Lavalink
{
    public class LavalinkGuildConnection
    {
        public event Func<LavalinkGuildConnection, PlaybackStartedEventArgs, Task> OnTrackStart;
        public event Func<LavalinkGuildConnection, PlaybackFinishedEventArgs,Task> OnTrackFinish;
        public event Func<LavalinkGuildConnection, PlaybackExceptionEventArgs, Task> OnTrackException;
        public event Func<LavalinkGuildConnection, PlaybackStuckEventArgs, Task> OnTrackStuck;
        public event Func<LavalinkGuildConnection, PlayerUpdateEventArgs, Task> OnPlayerUpdate;
        public event Func<LavalinkGuildConnection, PlayerWebsocketClosedEventArgs, Task> OnWebsocketClosed;
        public event Func<LavalinkGuildConnection, PlayerInternalError, Task> OnPlayerError;

        internal VoiceServerUpdateEventArgs VoiceServerUpdateEventArgs { get; set; }
        internal VoiceStateUpdateEventArgs VoiceStateUpdateEventArgs { get; set; }
        public IVoiceState VoiceState => VoiceStateUpdateEventArgs.Channel;
        public bool IsConnected => VoiceState != null;
        public LavalinkPlayerState State { get; set; }

        private LavalinkNode node;
        public LavalinkGuildConnection(LavalinkNode node,
                                       VoiceServerUpdateEventArgs voiceServerUpdate,
                                       VoiceStateUpdateEventArgs voiceStateUpdate)
        {
            this.VoiceServerUpdateEventArgs = voiceServerUpdate;
            this.VoiceStateUpdateEventArgs = voiceStateUpdate;
            this.node = node;
        }
        public async Task PlayAsync(LavalinkTrack track)
        {
            if (!node.IsReady || !IsConnected)
            {
                throw new InvalidOperationException("Node is not ready or not connected");
            }

            await node.Rest.UpdatePlayer(VoiceStateUpdateEventArgs.Guild.Id, new LavalinkPlayerUpdatePayload
            {
                EncodedTrack = track.Encoded
            });
        }

        public async Task StopAsync()
        {
            if (!node.IsReady || !IsConnected)
            {
                throw new InvalidOperationException("Node is not ready or not connected");
            }

            await node.Rest.UpdatePlayer(VoiceStateUpdateEventArgs.Guild.Id, new LavalinkPlayerUpdatePayload
            {
                EncodedTrack = null
            });
        }

        public async Task PauseAsync()
        {
            if (!node.IsReady || !IsConnected)
            {
                throw new InvalidOperationException("Node is not ready or not connected");
            }

            await node.Rest.UpdatePlayer(VoiceStateUpdateEventArgs.Guild.Id, new LavalinkPlayerUpdatePayload
            {
                Paused = true
            });
        }

        public async Task ResumeAsync()
        {
            if (!node.IsReady || !IsConnected)
            {
                throw new InvalidOperationException("Node is not ready or not connected");
            }

            await node.Rest.UpdatePlayer(VoiceStateUpdateEventArgs.Guild.Id, new LavalinkPlayerUpdatePayload
            {
                Paused = false
            });
        }

        public async Task SeekAsync(TimeSpan position)
        {
            if (!node.IsReady || !IsConnected)
            {
                throw new InvalidOperationException("Node is not ready or not connected");
            }

            await node.Rest.UpdatePlayer(VoiceStateUpdateEventArgs.Guild.Id, new LavalinkPlayerUpdatePayload
            {
                Position = position.Milliseconds
            });
        }

        public async Task SetVolumeAsync(int volume)
        {
            if (!node.IsReady || !IsConnected)
            {
                throw new InvalidOperationException("Node is not ready or not connected");
            }

            await node.Rest.UpdatePlayer(VoiceStateUpdateEventArgs.Guild.Id, new LavalinkPlayerUpdatePayload
            {
                Volume = volume
            });
        }

        public async Task SetFilterVolumeAsync(int volume)
        {
            if (!node.IsReady || !IsConnected)
            {
                throw new InvalidOperationException("Node is not ready or not connected");
            }

            await node.Rest.UpdatePlayer(VoiceStateUpdateEventArgs.Guild.Id, new LavalinkPlayerUpdatePayload
            {
                Filters = new()
                {
                    Volume = volume
                }
            });
        }

        public async Task DisconnectAsync()
        {
            if (!node.IsReady || !IsConnected)
            {
                throw new InvalidOperationException("Node is not ready or not connected");
            }

            //Disconnecting from the voice channel
            var vsd = new VoiceDispatch
            {
                OpCode = 4,
                Payload = new VoiceStateUpdatePayload
                {
                    GuildId = VoiceStateUpdateEventArgs.Guild.Id,
                    ChannelId = null,
                    Deafened = false,
                    Muted = false
                }
            };

            var vsdJson = JsonConvert.SerializeObject(vsd);
            node.DiscordWsSendAsync(vsdJson);

            //Removing the connection from the node
            await node.Rest.DestroyPlayer(VoiceStateUpdateEventArgs.Guild.Id);
        }

        public async Task HandleLavalinkEvent<T>(T eventPayload)
        {
            if (eventPayload is PlaybackStartedEventArgs)
            {
                await HandleEventAsync<PlaybackStartedEventArgs>(OnTrackStart.GetInvocationList(), eventPayload);
            }
            else if (eventPayload is PlaybackFinishedEventArgs)
            {
                await HandleEventAsync<PlaybackFinishedEventArgs>(OnTrackFinish.GetInvocationList(), eventPayload);
            }
            else if (eventPayload is PlaybackExceptionEventArgs)
            {
                await HandleEventAsync<PlaybackExceptionEventArgs>(OnTrackException.GetInvocationList(), eventPayload);
            }
            else if (eventPayload is PlayerUpdateEventArgs)
            {
                await HandleEventAsync<PlayerUpdateEventArgs>(OnPlayerUpdate.GetInvocationList(), eventPayload);
            }
            else if (eventPayload is PlayerWebsocketClosedEventArgs)
            {
                await HandleEventAsync<PlayerWebsocketClosedEventArgs>(OnWebsocketClosed.GetInvocationList(), eventPayload);
            }
            else
            {
                throw new InvalidOperationException($"invalid event type {typeof(T)}");
            } 
        }

       async Task HandleEventAsync<T>(Delegate[] delegates, object args)  
       {  
            var tasks = new Task[delegates.Length];
            for (int i = 0; i < tasks.Length; i++) 
            {
              var task = ((Func<object, T, Task>)delegates[i])(this, (T)args);
                tasks[i] = task;
            }

            await Task.WhenAll(tasks); 
        }
    }
}