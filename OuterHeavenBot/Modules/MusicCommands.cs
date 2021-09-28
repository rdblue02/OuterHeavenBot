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
            if (string.IsNullOrWhiteSpace(argument))
            {
                await ReplyAsync("Song name cannot be empty");
                return;
            }

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

            if(!string.IsNullOrWhiteSpace(searchResults.Playlist.Name))
            {
                await ReplyAsync($"Queuing Playlist {searchResults.Playlist.Name}");
                string playListMessage = "";
                int index = 1;
                foreach(var plTrack in searchResults.Tracks)
                {
                    playListMessage += $"{index}) {plTrack.Title} - {plTrack.Author} - {plTrack.Duration}";
                    await ReplyAsync(playListMessage);
                    await audioService.ProcessTrack(plTrack);
                    index++;
                }
                return;
            }

            LavaTrack track = null;
            if (argument.Contains("https://"))
            {
                Console.WriteLine($"search by link detected. Returned {searchResults.Tracks.Count} tracks");
                track = searchResults.Tracks.FirstOrDefault(x=>x.Url.ToLower().Trim() == argument.ToLower().Trim());
               
                if(track == null)
                {
                    track = searchResults.Tracks.FirstOrDefault();
                    Console.WriteLine($"Cannot find a matching track to url. Selecting {track.Title}");
                    
                }
                else
                {
                    Console.WriteLine($"Found a matching track to url. Selecting {track.Title}");
                }
            }
            else
            {
                track = searchResults.Tracks.FirstOrDefault();
                Console.WriteLine($"Searched by song name. Selecting {track.Title}");
            }
            var state = audioService.WillQueue ? "Queuing" : "Playing";

            await ReplyAsync($"{state} {track.Title} - {track.Author} - {track.Duration}");
            await audioService.ProcessTrack(track);
        }

        [Command("playlocal", RunMode = RunMode.Async)]
        [Alias("pl")]
        public async Task PlayLocal([Remainder] string argument)
        {
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
                await audioService.ProcessTrack(track);           
            }
        }

        [Command("pause", RunMode = RunMode.Async)]
        [Alias("pa")]
        public async Task Pause()
        { 
           if(await audioService.ChangePauseState())
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
            if (!string.IsNullOrWhiteSpace(audioService.CurrentTrackName))
            {
                await audioService.Skip();
                await ReplyAsync($"Skipping - {audioService.CurrentTrackName}"); 
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
           
            if (!audioService.activeLavaPlayer?.Queue.Any() ?? true)
            {
                await ReplyAsync("Que is currently empty!");
                return;
            }

            if (index.HasValue)
            {
                if (index.Value  > 0 && index.Value  < audioService.activeLavaPlayer?.Queue.Count-1)
                {
                    var trackToKill = audioService.activeLavaPlayer.Queue.ElementAt(index.Value - 1);
                    audioService.activeLavaPlayer.Queue.Remove(trackToKill);
                    await ReplyAsync($"Clearing {trackToKill.Title} from the queue");
                }
                else
                {
                    await ReplyAsync($"Invalid index. Please enter a value between 1 - {audioService.activeLavaPlayer?.Queue.Count - 1}");
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
            var track = audioService.activeLavaPlayer.Track;
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
            var quedSongs = this.audioService.activeLavaPlayer.Queue;

            if (quedSongs!=null && quedSongs.Any())
            {
                EmbedBuilder embedBuilder = new EmbedBuilder()
                {
                    Title = "Song Queue",
                    Color = Color.LighterGrey,
                    Fields = new List<EmbedFieldBuilder>()
                };

                var info = quedSongs.Select((x, y) => new { i = (y + 2).ToString(), t = x.Title }).ToList();

                var indexString = $"1{Environment.NewLine}{string.Join(Environment.NewLine, info.Select(x => x.i).ToList())}";
                var songString = $"{audioService.CurrentTrackName}{Environment.NewLine}{string.Join(Environment.NewLine, info.Select(x => x.t).ToList())}";

                embedBuilder.Fields.Add(new EmbedFieldBuilder() { IsInline = true, Name = "#", Value = indexString  });
                embedBuilder.Fields.Add(new EmbedFieldBuilder() { IsInline = true, Name = "Name", Value = songString });
                await ReplyAsync(null, false, embedBuilder.Build());
            }
            else
            {
                await ReplyAsync("No songs currently in queue");
            }
        }
    }
}
