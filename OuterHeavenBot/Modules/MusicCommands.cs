using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Victoria;
using Victoria.Enums;
using Victoria.Responses.Search;
namespace OuterHeavenBot.Modules
{
    public class MusicCommands : ModuleBase<SocketCommandContext>
    {
        private LavaNode lavaNode;

        public MusicCommands(LavaNode lavaNode )
        {
            this.lavaNode = lavaNode;
        }

        [Command("play", RunMode = RunMode.Async)]
        [Alias("p")]
        public async Task Play([Remainder]string argument)
        { 
            if(!(await ValideToConnection()))
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(argument))
            {
                await ReplyAsync("Song name cannot be empty");
                return;
            }
            var player = await GetPlayer();

            var searchResults = await lavaNode.SearchYouTubeAsync(argument);
            if (searchResults.Status == SearchStatus.NoMatches)
            {
                await ReplyAsync("No matches found on youtube. Checking Sound Cloud");
                searchResults = await lavaNode.SearchSoundCloudAsync(argument);

                if (searchResults.Status == SearchStatus.NoMatches)
                {
                    await ReplyAsync($"No matches found for {argument}");
                    return;
                }
            }
            var track = searchResults.Tracks.ElementAt(0);
            if (player.PlayerState == PlayerState.Playing)
            {
                await ReplyAsync($"Adding {track.Title} - {track.Author} - {track.Duration} to the queue");
                player.Queue.Enqueue(track);
            }
            else
            {
                await PlayTrack(searchResults.Tracks.ElementAt(0), player);
            }

        }

        [Command("playlocal", RunMode = RunMode.Async)]
        [Alias("pl")]
        public async Task PlayLocal([Remainder] string argument)
        {
            if (!(await ValideToConnection()))
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(argument))
            {
                await ReplyAsync("Song name cannot be empty");
                return;
            }
 
            var filePath = "";
            if (argument.Contains("c://"))
            {
                filePath = argument;
            }
            else
            {
                var audioFiles = Helpers.GetAudioFiles();

                if (audioFiles.ContainsKey("music"))
                {
                    filePath = audioFiles["music"].FirstOrDefault(x => x.Name == argument)?.FullName;
                }
            }
            if (string.IsNullOrWhiteSpace(filePath))
            {
                await ReplyAsync($"Cannot find {argument}");
                return;
            }
            var searchResults = await lavaNode.SearchAsync(SearchType.Direct, filePath);
            if (searchResults.Status == SearchStatus.NoMatches || searchResults.Status == SearchStatus.LoadFailed)
            {
                await ReplyAsync($"Error playing {argument}");
                return;
            }
            await PlayTrack(searchResults.Tracks.FirstOrDefault(), await GetPlayer());           
        }

        [Command("pause", RunMode = RunMode.Async)]
        [Alias("pa")]
        public async Task Pause()
        {
            if (!(await ValideToConnection()))
            {
                return;
            }
            var player = await GetPlayer(false);

            if (player?.PlayerState == Victoria.Enums.PlayerState.Playing)
            {
                await player.PauseAsync();
                await ReplyAsync($"Player is now paused");
            }
            else if (player?.PlayerState == Victoria.Enums.PlayerState.Paused)
            {
                await player.ResumeAsync();
                await ReplyAsync($"Player is now unpaused");
            }
            else
            {
                await ReplyAsync($"There's nothing to pause!");
            }

        }
        [Command("skip", RunMode = RunMode.Async)]
        [Alias("sk")]
        public async Task Skip()
        {
            if (!(await ValideToConnection()))
            {
                return;
            }

            var player = await GetPlayer(false);
            if (player?.Queue.Any() ?? false)
            {
                var track = await player.SkipAsync();
                await ReplyAsync($"Skipping - {track.Skipped.Title}"); 
            }
            else
            {
                if (player?.Track != null)
                {
                    await player.StopAsync();
                }
                else
                {
                    await ReplyAsync($"There's nothing to skip!");
                }
            }

        }

        [Command("clearQ", RunMode = RunMode.Async)]
        [Alias("cq")]
        public async Task ClearQ([Remainder]string song = null)
        {
            if (!(await ValideToConnection()))
            {
                return;
            }
            var player = await GetPlayer(false);
            if (!player?.Queue.Any() ?? true)
            {
                await ReplyAsync("Que is currently empty!");
                return;
            }

            if (string.IsNullOrWhiteSpace(song))
            {
                await ReplyAsync($"Clearing {player.Queue.Count} songs from the queue");
                player.Queue.Clear();
            }
            else
            {
                var trackToKill = player.Queue.FirstOrDefault(x => x.Title.ToLower().Contains(song));
                player.Queue.Remove(trackToKill);
                await ReplyAsync($"Clearing {trackToKill.Title} from the queue");
            }

        }

        [Command("fastforward", RunMode = RunMode.Async)]
        [Alias("ff")]
        public async Task FastForward(int seconds)
        {
            if (!(await ValideToConnection()))
            {
                return;
            }
            var player = await GetPlayer(false);
            if(player?.Track == null)
            {
                await ReplyAsync($"Nothing is playing");
            }
            if (!player.Track.CanSeek || player.Track == null)
            {
                await ReplyAsync($"Cannot go to time in track");
                return;
            }
            if (seconds <= 0)
            {
                await ReplyAsync($"Invalid fast forward duration {seconds}");
                return;
            }
            await ReplyAsync($"Fast forwarding {TimeSpan.FromSeconds(seconds)}");
            if (player.Track.Position + TimeSpan.FromSeconds(seconds) < player.Track.Duration)
            {
                await player.SeekAsync(player.Track.Position + TimeSpan.FromSeconds(seconds));
            }
            else
            {
                await player.SeekAsync(player.Track.Duration - TimeSpan.FromMilliseconds(500));
            }

        }

        [Command("rewind", RunMode = RunMode.Async)]
        [Alias("r")]
        public async Task Rewind(int seconds)
        {
            if (!(await ValideToConnection()))
            {
                return;
            }
            var player = await GetPlayer(false);
            if (player?.Track == null)
            {
                await ReplyAsync($"Nothing is playing");
            }
            if (!player.Track.CanSeek || player.Track == null)
            {
                await ReplyAsync($"Cannot go to time in track");
                return;
            }
            await ReplyAsync($"Rewinding {TimeSpan.FromSeconds(seconds)}");
            if (player.Track.Position - TimeSpan.FromSeconds(seconds) > TimeSpan.FromSeconds(0))
            {
                await player.SeekAsync(player.Track.Position - TimeSpan.FromSeconds(seconds));
            }
            else
            {
                await player.SeekAsync(TimeSpan.FromSeconds(0));
            }
 
        }

        [Command("goto", RunMode = RunMode.Async)]
        [Alias("gt")]
        public async Task GoTo(string time)
        {
            if (!(await ValideToConnection()))
            {
                return;
            }
            var player = await GetPlayer(false);
            if (player?.Track == null)
            {
                await ReplyAsync($"Nothing is playing");
            }
            if (!TimeSpan.TryParse(time, out TimeSpan timeResult))
            {
                await ReplyAsync($"Invalid time entry. Make sure to use hh:mm:ss format.");
                return;
            }

            if (!player.Track.CanSeek || player.Track == null)
            {
                await ReplyAsync($"Cannot go to time in track");
                return;
            }
            if (timeResult > player.Track.Duration)
            {
                await ReplyAsync($"Seek time must be smaller than the track duration {player.Track.Duration}");
                return;
            }
            await ReplyAsync($"Seeking to {timeResult}");
            await player.SeekAsync(timeResult);

        }

        [Command("trackInfo", RunMode = RunMode.Async)]
        [Alias("t")]
        public async Task GetCurrentTrack()
        {
            var track = (await GetPlayer(false))?.Track;
            if (track != null)
            {
                await ReplyAsync($"Current track: {track.Title} - {track.Author} - {track.Duration} - {track.Url}");
            }
            else
            {
                await ReplyAsync($"Nothing is playing");
            }
        }

        [Command("queue", RunMode = RunMode.Async)]
        [Alias("q")]
        public async Task Queue()
        {
            var quedSongs = (await GetPlayer())?.Queue;

            if (quedSongs!=null && quedSongs.Any())
            {
                EmbedBuilder embedBuilder = new EmbedBuilder()
                {
                    Title = "Song Queue",
                    Color = Color.LighterGrey,
                    Fields = new List<EmbedFieldBuilder>()
                };

                var info = quedSongs.Select((x, y) => new { i = (y + 1).ToString(), t = x.Title }).ToList();

                embedBuilder.Fields.Add(new EmbedFieldBuilder() { IsInline = true, Name = "#", Value = string.Join(Environment.NewLine, info.Select(x => x.i).ToList()) });
                embedBuilder.Fields.Add(new EmbedFieldBuilder() { IsInline = true, Name = "Name", Value = string.Join(Environment.NewLine, info.Select(x => x.t).ToList()) });
                await ReplyAsync(null, false, embedBuilder.Build());
            }
            else
            {
                await ReplyAsync("No songs currently in queue");
            }
        }

        [Command("disconnect", RunMode = RunMode.Async)]
        [Alias("dc")]
        public async Task Disconnect()
        {
            await ReplyAsync("Stopping music bot");
           var player =  await GetPlayer(false);
            if (player != null)
            {
                await lavaNode.LeaveAsync(player.VoiceChannel);
            }
           await lavaNode.DisconnectAsync();
           
        }
        private async Task PlayTrack(LavaTrack track, LavaPlayer player)
        {
            await player.TextChannel.SendMessageAsync(
                   $"Now playing: {track.Title} - {track.Author} - {track.Duration}");

            await player.PlayAsync(track);
        }
      

        private async Task<LavaPlayer> GetPlayer(bool connect = true)
        {
            LavaPlayer player = null;
            if (lavaNode.HasPlayer(Context.Guild))
            {
                player = lavaNode.GetPlayer(Context.Guild);
            }
            else
            {
                if (connect)
                {
                    var voiceState = Context.User as IVoiceState;
                    player = await lavaNode.JoinAsync(voiceState.VoiceChannel, Context.Channel as ITextChannel);
                }
            }
            return player;
        }
        private async Task<bool> ValideToConnection()
        {
            if (!UserIsInVoice())
            {
                await ReplyAsync("You must be connected to a voice channel!");
                return false;
            }

            if (BotsInOtherChannel())
            {
                await ReplyAsync("Bot is currently playing in a different channel");
                return false;
            }
            return true;
        }
        private bool UserIsInVoice()
        {
            var voiceState = Context.User as IVoiceState;
            if (voiceState?.VoiceChannel == null)
            { 
                return false;
            }
            else
            {
                return true;
            }
        }
        private bool BotsInOtherChannel()
        {
            if (lavaNode.HasPlayer(Context.Guild))
            {
               var player = lavaNode.GetPlayer(Context.Guild);
                var userVoicChannel = (Context.User as IGuildUser)?.VoiceChannel;
                if ((player.PlayerState == PlayerState.Playing ||
                            player.PlayerState == PlayerState.Paused) &&
                            userVoicChannel.Name != player.VoiceChannel?.Name)
                {

                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
            
        }
    }
}
