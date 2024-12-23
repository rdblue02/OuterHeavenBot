using Discord;
using OuterHeavenBot.Client.Commands.Handlers;
using OuterHeavenBot.Core.Models;
using OuterHeavenBot.Lavalink;
using OuterHeavenBot.Lavalink.EventArgs;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenBot.Client.Services
{
    public class MusicService
    {
        private ILogger<MusicService> logger;
        private ConcurrentQueue<LavalinkTrack> tracksinQueue;
        private LavalinkNode node;
        public MusicService(ILogger<MusicService> logger,
                            LavalinkNode lavalinkNode)
        {
            this.logger = logger;
            tracksinQueue = new ConcurrentQueue<LavalinkTrack>();
            node = lavalinkNode;
        }
        public void Initialize()
        {

        }
        public async Task<CommandResult> QueueTrack(IVoiceChannel voiceChannel, LavalinkTrack track)
        {
            var message = $"{(tracksinQueue.IsEmpty ? "Now playing track" : "Now queuing track")}\n{track.Info.Title}";
            var result = await StartAndQueueTrack(voiceChannel, track);
            if (!result.Success)
            {
                return result;
            }

            result.Message = message;
            return result;
        }

        public async Task<CommandResult> QueuePlayList(IVoiceChannel voiceChannel, List<LavalinkTrack> tracks)
        {
            var firstTrack = tracks.FirstOrDefault();

            var message = "";

            var result = await StartAndQueueTrack(voiceChannel, firstTrack);

            if (!result.Success)
            {
                return result;
            }
            message += $"{(tracksinQueue.IsEmpty ? "Now playing tracks" : "Now queuing tracks")}\n{firstTrack.Info.Title}";
            foreach (var track in tracks.Skip(1))
            {
                result = await StartAndQueueTrack(voiceChannel, firstTrack);
                if (!result.Success)
                {
                    return result;
                }

                message += $"\n{track.Info.Title}";
            }

            return result;
        }

        public async Task<CommandResult> SkipTrackAsync()
        {
            var commandResult = new CommandResult() { Success = false };

            var connection = node.GetConntection();

            if (connection != null &&
                QueuedTrackTitles().Count > 0)
            {
                commandResult.Message = $"Skipping track {tracksinQueue.ElementAt(0)}";
                await connection.RemoveActiveTrackAsync();
                commandResult.Success = true;
                return commandResult;
            }

            commandResult.Message = "There's nothing to skip";
            return commandResult;
        }

        private async Task<CommandResult> StartAndQueueTrack(IVoiceChannel voiceChannel, LavalinkTrack track)
        {
            var commandResult = await GetOrConnectPlayer(voiceChannel);
            if (!commandResult.Success || commandResult.ResultData == null)
            {
                return commandResult;
            }

            var connection = commandResult.ResultData;

            if (tracksinQueue.IsEmpty)
            {
                RegisterPlayerEvents(connection);
                await connection.PlayTrackAsync(track);
            }

            tracksinQueue.Enqueue(track);
            commandResult.Success = true;

            return commandResult;
        }

        private async Task<CommandResult<LavalinkGuildConnection>> GetOrConnectPlayer(IVoiceChannel voiceChannel)
        {
            var commandResult = new CommandResult<LavalinkGuildConnection>() { Success = false };
            var registerEvents = node.GetConntection() == null;

            var connection = await node.ConnectPlayerAsync(voiceChannel);

            if (connection != null && connection.IsConnected)
            {
                RegisterPlayerEvents(connection);
                commandResult.ResultData = connection;
                commandResult.Success = true;
            }
            else
            {
                commandResult.Message = "Lavalink must be connected";
            }

            return commandResult;
        }

        public List<string> QueuedTrackTitles()
        {
            return tracksinQueue.Select(x => x.Info.Title).ToList();
        }
        public async Task<CommandResult> ClearQueue(int? index)
        {
            var commandResult = new CommandResult() { Success = false };
            try
            {
                var connnection = node.GetConntection();

                if (tracksinQueue.Count == 0 || connnection == null || !connnection.IsConnected)
                {
                    commandResult.Message = "No songs in queue";
                }
                else if (index.HasValue && (index < 1 || index > tracksinQueue.Count))
                {
                    commandResult.Message = "Invalid selection.";
                }
                //todo - there is likely a better way to do this. 
                //didn't see a .RemoveAt index in a concurrent queue.
                else if (index.HasValue && index > 1)
                {
                    var temp = new List<LavalinkTrack>();
                    int i = 0;
                    while (i < index.Value)
                    {
                        tracksinQueue.TryDequeue(out var track);
                        temp.Add(track);
                        i++;
                    }
                    tracksinQueue.TryDequeue(out _);
                    foreach (var track in temp)
                    {
                        tracksinQueue.Enqueue(track);
                    }
                }
                else
                {
                    tracksinQueue.Clear();
                    //events will be cleaned up by the track finished event.
                    await connnection.RemoveActiveTrackAsync();
                }
            }
            catch (Exception ex)
            {
                commandResult.Message = ex.Message;
            }

            return commandResult;
        }
        private  Task CurrentConnections_OnTrackStart(LavalinkGuildConnection connection, PlaybackStartedEventArgs arg2)
        {
            return Task.CompletedTask;
        }

        private async Task CurrentConnections_OnTrackStuck(LavalinkGuildConnection connection, PlaybackStuckEventArgs arg2)
        {
            //todo skip to next or attempt to replay from position?
            logger.LogError($"Track {arg2?.Track?.Info.Title} is stuck. Skipping track");
            tracksinQueue.TryDequeue(out _);

            if (tracksinQueue.TryPeek(out var nextTrack))
            {
                await connection.PlayTrackAsync(nextTrack);
            }
        }

        private async Task CurrentConnections_OnTrackException(LavalinkGuildConnection arg1, PlaybackExceptionEventArgs arg2)
        {
            logger.LogError($"Error playing track {arg2?.Track?.Info.Title}\nError: {arg2?.Exception?.Message.ToString()}\nCause: {arg2?.Exception?.Cause}");
            await arg1.LeaveAsync();
            tracksinQueue.Clear();
            DeregisterPlayerEvents(arg1);
        }

        private Task CurrentConnections_OnPlayerUpdate(LavalinkGuildConnection arg1, PlayerUpdateEventArgs arg2)
        {
            logger.LogInformation($"Player updated Time: {arg2.State.Time} | Connected: {arg2.State.Connected} | Ping: {arg2.State.Ping} | Position: {arg2.State.Position}");
            return Task.CompletedTask;
        }

        private async Task CurrentConnections_OnTrackFinish(LavalinkGuildConnection connection, PlaybackFinishedEventArgs playbackFinishedArgs)
        {
            tracksinQueue.TryDequeue(out _);

            if ((playbackFinishedArgs.EndReason == LavalinkTrackEndReason.Stopped ||
                 playbackFinishedArgs.EndReason == LavalinkTrackEndReason.Replaced ||
                 playbackFinishedArgs.EndReason == LavalinkTrackEndReason.Finished) &&
                 tracksinQueue.TryPeek(out var nextTrack) && connection.IsConnected)
            {
                await connection.PlayTrackAsync(nextTrack);
            }
            else
            {
                DeregisterPlayerEvents(connection);
            }
        }

        private void RegisterPlayerEvents(LavalinkGuildConnection lavaConnection)
        {
            lavaConnection.OnTrackFinish += CurrentConnections_OnTrackFinish;
            lavaConnection.OnPlayerUpdate += CurrentConnections_OnPlayerUpdate;
            lavaConnection.OnTrackException += CurrentConnections_OnTrackException;
            lavaConnection.OnTrackStuck += CurrentConnections_OnTrackStuck;
            lavaConnection.OnTrackStart += CurrentConnections_OnTrackStart;
        }
        private void DeregisterPlayerEvents(LavalinkGuildConnection lavaConnection)
        {
            lavaConnection.OnTrackFinish -= CurrentConnections_OnTrackFinish;
            lavaConnection.OnPlayerUpdate -= CurrentConnections_OnPlayerUpdate;
            lavaConnection.OnTrackException -= CurrentConnections_OnTrackException;
            lavaConnection.OnTrackStuck -= CurrentConnections_OnTrackStuck;
            lavaConnection.OnTrackStart -= CurrentConnections_OnTrackStart;
        }
    }
}
