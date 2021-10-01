using Discord;
using Discord.Commands;
using Discord.WebSocket;
using OuterHeavenBot.Services;
using System;
using System.Collections.Generic;
using System.IO;
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
        private AudioService audioService;
        public MusicCommands(LavaNode lavaNode,AudioService audioService)
        {
            this.lavaNode = lavaNode;

            this.audioService = audioService;
        }

        [Command("play", RunMode = RunMode.Async)]
        [Alias("p")]
        public async Task Play([Remainder]string argument)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(argument))
                {
                    await ReplyAsync("Song name cannot be empty");
                    return;
                }
                var user = Context.User as IVoiceState;

                if (user == null || user?.VoiceChannel == null)
                {
                    await ReplyAsync("You must be in a voice channel for this command");
                    return;
                }

                if (audioService.IsPlayerInAnotherChannel(user))
                {
                    await ReplyAsync("You must be in the same channel as the bot for this command");
                    return;
                }
                var searchType = argument.Contains("https://") ? SearchType.Direct : SearchType.YouTubeMusic;

                SearchResponse searchResponse = await lavaNode.SearchAsync(searchType, argument);

                if (searchType == SearchType.YouTubeMusic && (searchResponse.Status == SearchStatus.NoMatches ||
                                                              searchResponse.Status == SearchStatus.LoadFailed ||
                                                              searchResponse.Tracks.Count == 0))
                {
                    searchResponse = await lavaNode.SearchAsync(SearchType.YouTube, argument);
                }

                if (searchResponse.Status == SearchStatus.NoMatches || searchResponse.Tracks.Count == 0)
                {
                    await ReplyAsync($"No matches foundfor {argument}");
                    return;
                }

                if (searchResponse.Status == SearchStatus.LoadFailed)
                {
                    await ReplyAsync($"Error playing {argument}");
                    return;
                }

                if (!string.IsNullOrWhiteSpace(searchResponse.Playlist.Name))
                {
                    await ReplyAsync($"Queuing Playlist {searchResponse.Playlist.Name}");                   
                    foreach (var plTrack in searchResponse.Tracks)
                    {                
                        await audioService.ProcessTrack(plTrack, user, Context.Channel as ITextChannel);
                     
                    }
                    await Queue();
                }
                else
                {
                    var track = searchResponse.Tracks.FirstOrDefault();
                    var state = audioService.WillQueue ? "Queuing" : "Playing";

                    await ReplyAsync($"{state} {track.Title} - {track.Author} - {track.Duration}");
                    await audioService.ProcessTrack(track, user, Context.Channel as ITextChannel);
                }
            }
            catch(Exception e)
            {
                await ReplyAsync($"Error playing {argument}");
                Console.WriteLine(e);
            }            
        }

        [Command("playlocal", RunMode = RunMode.Async)]
        [Alias("pl")]
        public async Task PlayLocal([Remainder] string argument)
        {
            var user = Context.User as IVoiceState;

            if (user == null || user?.VoiceChannel == null)
            {
                await ReplyAsync("You must be in a voice channel for this command");
                return;
            }

            if (audioService.IsPlayerInAnotherChannel(user))
            {
                await ReplyAsync("You must be in the same channel as the bot for this command");
                return;
            }

            if (string.IsNullOrWhiteSpace(argument))
            {
                await ReplyAsync("Song name cannot be empty");
                return;
            }
 
            var filePath = "";
            var audioFiles = new DirectoryInfo(Directory.GetCurrentDirectory()+"\\music").GetFiles();

            Console.WriteLine($"Found {audioFiles.Count()} files");
            filePath = audioFiles.FirstOrDefault(x => x.Name.ToLower().Trim().Contains(argument.ToLower().Trim()))?.FullName;
            
            if (string.IsNullOrWhiteSpace(filePath))
            {
                await ReplyAsync($"Cannot find {argument}");
                return;
            }

            var searchResults = await lavaNode.SearchAsync(SearchType.Direct, filePath);
            if (searchResults.Status == SearchStatus.NoMatches )
            {
                await ReplyAsync($"Cannot find {argument}");
                return;
            }
            else if(searchResults.Status == SearchStatus.LoadFailed)
            {
                await ReplyAsync($"Error playing {argument}");
                return;
            }
            else
            {
                var track = searchResults.Tracks.FirstOrDefault();
                var state = audioService.WillQueue ? "Queuing" : "Playing";

                await ReplyAsync($"{state} {track.Title} - {track.Author} - {track.Duration}");
                await audioService.ProcessTrack(track,user, Context.Channel as ITextChannel);           
            }
        }

        [Command("pause", RunMode = RunMode.Async)]
        [Alias("pa")]
        public async Task Pause()
        {
            var user = Context.User as IVoiceState;

            if (user == null || user?.VoiceChannel == null)
            {
                await ReplyAsync("You must be in a voice channel for this command");
                return;
            }

            if (audioService.IsPlayerInAnotherChannel(user))
            {
                await ReplyAsync("You must be in the same channel as the bot for this command");
                return;
            }

            var state = await audioService.ChangePauseState();                
            if (state == PlayerState.Paused || state == PlayerState.Playing)
            {
                await ReplyAsync($"Player is now {audioService.CurrentState}");
            }
            else
            {
                await ReplyAsync($"Player isn't playing!");
            }
        }

        [Command("skip", RunMode = RunMode.Async)]
        [Alias("sk")]
        public async Task Skip()
        {
            var user = Context.User as IVoiceState;

            if (user == null || user?.VoiceChannel == null)
            {
                await ReplyAsync("You must be in a voice channel for this command");
                return;
            }

            if (audioService.IsPlayerInAnotherChannel(user))
            {
                await ReplyAsync("You must be in the same channel as the bot for this command");
                return;
            }

            if (!string.IsNullOrWhiteSpace(audioService.CurrentTrackName))
            {
                await ReplyAsync($"Skipping - {audioService.CurrentTrackName}");
                await audioService.Skip();                
            }
            else
            {
                await ReplyAsync($"There's nothing to skip!");
            }
        }

        [Command("clearqueue", RunMode = RunMode.Async)]
        [Alias("cq")]
        public async Task ClearQ([Remainder]int? index = null)
        {
            var user = Context.User as IVoiceState;

            if (user == null || user?.VoiceChannel == null)
            {
                await ReplyAsync("You must be in a voice channel for this command");
                return;
            }

            if (audioService.IsPlayerInAnotherChannel(user))
            {
                await ReplyAsync("You must be in the same channel as the bot for this command");
                return;
            }

            if (!audioService.activeLavaPlayer?.Queue.Any() ?? true)
            {
                await ReplyAsync("Que is currently empty!");
                return;
            }

            if (index.HasValue)
            {
                if (index.Value   > 0 && index.Value -1  < audioService.activeLavaPlayer?.Queue.Count)
                {
                    var trackToKill = audioService.activeLavaPlayer.Queue.ElementAt(index.Value - 1);
                    audioService.activeLavaPlayer.Queue.Remove(trackToKill);
                    await ReplyAsync($"Clearing {trackToKill.Title} from the queue");
                }
                else
                {
                    await ReplyAsync($"Invalid index. Please enter a value between 1 - {audioService.activeLavaPlayer?.Queue.Count}");
                }
            }
            else
            {
                await ReplyAsync($"Clearing {audioService.activeLavaPlayer.Queue.Count} songs from the queue");
                audioService.activeLavaPlayer.Queue.Clear();
            }

        }

        [Command("fastforward", RunMode = RunMode.Async)]
        [Alias("ff")]
        public async Task FastForward(int seconds)
        {
            var user = Context.User as IVoiceState;

            if (user == null || user?.VoiceChannel == null)
            {
                await ReplyAsync("You must be in a voice channel for this command");
                return;
            }

            if (audioService.IsPlayerInAnotherChannel(user))
            {
                await ReplyAsync("You must be in the same channel as the bot for this command");
                return;
            }

            var player = audioService.activeLavaPlayer;
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
        [Alias("rw")]
        public async Task Rewind(int seconds)
        {
            var user = Context.User as IVoiceState;

            if (user == null || user?.VoiceChannel == null)
            {
                await ReplyAsync("You must be in a voice channel for this command");
                return;
            }

            if (audioService.IsPlayerInAnotherChannel(user))
            {
                await ReplyAsync("You must be in the same channel as the bot for this command");
                return;
            }
            var player = audioService.activeLavaPlayer;
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
            var user = Context.User as IVoiceState;

            if (user == null || user?.VoiceChannel == null)
            {
                await ReplyAsync("You must be in a voice channel for this command");
                return;
            }

            if (audioService.IsPlayerInAnotherChannel(user))
            {
                await ReplyAsync("You must be in the same channel as the bot for this command");
                return;
            }
            var player = audioService.activeLavaPlayer;
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
            if ((Context.Channel as ITextChannel) == null)
            {
                await ReplyAsync("You must be in the server for this command");
                return;
            }
            var track = audioService.activeLavaPlayer.Track;
            if (track != null)
            {
                await ReplyAsync($"Current track: {track.Title} - {track.Author} - {audioService.CurrentTrackTimeRemaining} - {track.Url}");
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
            if ((Context.Channel as ITextChannel) == null)
            {
                await ReplyAsync("You must be in the server for this command");
                return;
            }

            var quedSongs = this.audioService.activeLavaPlayer.Queue;

            if (quedSongs!=null && quedSongs.Any())
            {
                EmbedBuilder embedBuilder = new EmbedBuilder()
                {
                    Title = "Song Queue",
                    Color = Color.LighterGrey,
                    Fields = new List<EmbedFieldBuilder>(),                   
                };

                var info = quedSongs.Select((x, y) => new { i = (y + 1).ToString(), t = CleanSongTitle(x.Title,x.Author), duration = x.Duration }).ToList();

                var indexString = $"Playing{Environment.NewLine}{string.Join(Environment.NewLine, info.Select(x => x.i).ToList())}";
                var songString = $"{audioService?.CurrentTrackName}{Environment.NewLine}{string.Join(Environment.NewLine, info.Select(x => x.t).ToList())}";
                var durationString = $"{audioService?.activeLavaPlayer?.Track?.Duration}{Environment.NewLine}{string.Join(Environment.NewLine, info.Select(x => x.duration).ToList())}";
                embedBuilder.Fields.Add(new EmbedFieldBuilder() { IsInline = true, Name = "#", Value = indexString  });
                embedBuilder.Fields.Add(new EmbedFieldBuilder() { IsInline = true, Name = "Name", Value = songString });
                embedBuilder.Fields.Add(new EmbedFieldBuilder() { IsInline = true, Name = "Duration", Value = durationString });

                 embedBuilder.WithFooter(new EmbedFooterBuilder() { 
                 Text = $" Current song remaining duration - {audioService.CurrentTrackTimeRemaining}"+"" +
                 $"{Environment.NewLine}Total Queue Duration - {info.Select(x=>x.duration).ToList().Aggregate((x,y)=>x+y)}"
                });
                await ReplyAsync(null, false, embedBuilder.Build());
            }
            else
            {
                await ReplyAsync("No songs currently in queue");
            }
        }

        private string CleanSongTitle(string title,string author)
        {
            if (title.Length < 42)
            {
                return title;
            }
            else
            {
               var cleanedTitle = title.Replace(author, "")
                                    .Replace("|", " ")
                                    .Replace("-", " ")
                                    .Replace(",", " ")
                                    .Replace("(", " ")
                                    .Replace(")", " ")
                                    .Replace("  ", " ");  
                if (cleanedTitle.Length > 42)
                {
                    return cleanedTitle.Substring(0,39) + "...";
                }
                else
                {
                    return cleanedTitle;
                }
            }             
        }
    }
}
