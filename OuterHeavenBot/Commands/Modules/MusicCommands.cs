using Discord;
using Discord.Commands;
using Discord.WebSocket;
using OuterHeavenBot;
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
namespace OuterHeavenBot.Commands.Modules
{
    public class MusicCommands : ModuleBase<SocketCommandContext>
    {
        MusicService musicService;
        ILogger logger;
        public MusicCommands(MusicService musicService, ILogger<MusicCommands> logger)
        {
            this.musicService = musicService;
            this.logger = logger;
        }

        [Command("search", RunMode = RunMode.Async)]
        [Alias("sr")]
        public async Task Search([Remainder] string argument)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(argument))
                {
                    await ReplyAsync("Song name cannot be empty");
                    return;
                }

                var results = await musicService.RequestSearch(argument);
                await ReplyAsync(null, false, results);
            }
            catch (Exception e)
            {
                await ReplyAsync($"Error searching for {argument}");
                logger.LogError($"Error searching {argument}:\n{e}");
            }
        }

        [Command("ShowLocal", RunMode = RunMode.Async)]
        [Alias("sl")]
        public async Task SearchLocal()
        {
            try
            {
                
                var runningDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
                var musicDirectory = runningDirectory?.GetDirectories().FirstOrDefault(x => x.Name.ToLower().Contains("music"));
                var searchResponse = musicDirectory?.GetFiles();


                EmbedBuilder embedBuilder = new EmbedBuilder()
                {
                    Title = "Song Search",
                    Color = Color.LighterGrey,
                    Fields = new List<EmbedFieldBuilder>(),
                };

                if (!searchResponse?.Any() ?? true)
                {
                    logger.LogInformation($"No songs found");
                    embedBuilder.Fields.Add(new EmbedFieldBuilder() { IsInline = true, Name = "No Results", Value = "Local Directory" });
                }
                else
                {
                 

                    var info = searchResponse?.Select((track, y) => new { index = (y).ToString(), title = track.Name}).ToList();

                    var indexString = $"{Environment.NewLine}{string.Join(Environment.NewLine, info?.Select(x => x?.index)?.ToList())}";
                    var songString = $"{string.Join(Environment.NewLine, info.Select(x => x.title).ToList())}"; 
                    embedBuilder.Fields.Add(new EmbedFieldBuilder() { IsInline = true, Name = "#", Value = indexString });
                    embedBuilder.Fields.Add(new EmbedFieldBuilder() { IsInline = true, Name = "Name", Value = songString });  
                }

                await ReplyAsync(null, false, embedBuilder.Build());
            }
            catch (Exception e)
            {
                await ReplyAsync($"Error");
                logger.LogError($"Error searching:\n{e}");
            }
        }



        [Command("play", RunMode = RunMode.Async)]
        [Alias("p")]
        public async Task Play([Remainder] string argument)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(argument))
                {
                    await ReplyAsync("Song name cannot be empty");
                    return;
                }

                await musicService.RequestSong(argument,Context,false);
            }
            catch (Exception e)
            {
                await ReplyAsync($"Error playing {argument}");
                logger.LogError($"Error playing {argument}:\n{e}");
            }
        }

        [Command("playlocal", RunMode = RunMode.Async)]
        [Alias("pl")]
        public async Task PlayLocal([Remainder] string argument)
        {
            try
            {
                await ReplyAsync("Playing local");
                if (string.IsNullOrWhiteSpace(argument))
                {
                    await ReplyAsync("Song name cannot be empty");
                    return;
                }

                await musicService.RequestSong(argument, Context,true);
            }
            catch (Exception e)
            {
                await ReplyAsync($"Error playing {argument}");
                logger.LogError($"Error playing {argument}:\n{e}");
            }
        }

        [Command("pause", RunMode = RunMode.Async)]
        [Alias("pa")]
        public async Task Pause()
        {
            try
            {
               var result =  await musicService.ChangePauseState();

                await ReplyAsync($"Player is now {result}");
            }
            catch(Exception e)
            {
                await ReplyAsync($"Error pausing");
                logger.LogError($"Error pausing:\n{e}");
            }
        }

        [Command("skip", RunMode = RunMode.Async)]
        [Alias("sk")]
        public async Task Skip()
        {
            try
            { 
                await musicService.RequestSkip(Context);
            }
            catch(Exception e)
            {
                await ReplyAsync($"Error skipping track");
                logger.LogError($"Error skipping track:\n{e}");
            }        
        }

        [Command("clearqueue", RunMode = RunMode.Async)]
        [Alias("cq")]
        public async Task ClearQ([Remainder] int? index = null)
        {
            try
            {
                await musicService.RequestQueueClear(index, Context);
            }
            catch (Exception e)
            {
                await ReplyAsync($"Error clearing queue");
                logger.LogError($"Error clearing queue:\n{e}");
            }
        }

        [Command("fastforward", RunMode = RunMode.Async)]
        [Alias("ff")]
        public async Task FastForward(int seconds = 10)
        {
            try
            { 
                if (seconds < 0)
                {
                    await ReplyAsync($"Invalid fast forward duration {seconds}");
                    return;
                }

                await musicService.FastForward(seconds,Context);
            }
            catch (Exception e)
            {
                await ReplyAsync($"Error trying to fast forward track");
                logger.LogError($"Error trying to fast forward track:\n{e}");
            } 
        }

        [Command("rewind", RunMode = RunMode.Async)]
        [Alias("rw")]
        public async Task Rewind(int seconds = 10)
        {
            try
            { 
                if (seconds < 0)
                {
                    await ReplyAsync($"Invalid rewind duration {seconds}");
                    return;
                }
                await musicService.RequestRewind(seconds,Context);
            }
            catch (Exception e)
            {
                await ReplyAsync($"Error trying to rewind track");
                logger.LogError($"Error trying to rewind track:\n{e}");
            }
        }

        [Command("goto", RunMode = RunMode.Async)]
        [Alias("gt")]
        public async Task GoTo(string? time)
        {
            try
            {              
                if (!TimeSpan.TryParse(time, out TimeSpan timeResult))
                {
                    await ReplyAsync($"Invalid time entry. Make sure to use hh:mm:ss format.");
                    return;
                }

                await musicService.GoTo(timeResult, Context);
            }
            catch (Exception e)
            {
                await ReplyAsync($"Error going to position {time}");
                logger.LogError($"Error going to position {time}:\n{e}");
            }           
        }

        [Command("trackInfo", RunMode = RunMode.Async)]
        [Alias("t")]
        public async Task GetCurrentTrack()
        {
            try
            {
                var trackInfo = musicService.GetCurrentTrackInfo();
                await ReplyAsync(trackInfo);
            }
            catch (Exception e)
            { 
                await ReplyAsync($"Error getting track info");
                logger.LogError($"Error getting track info:\n{e}");
            } 
        }

        [Command("queue", RunMode = RunMode.Async)]
        [Alias("q")]
        public async Task Queue()
        {
            try
            {
                if ((Context.Channel as ITextChannel) == null)
                {
                    await ReplyAsync("You must be in the server for this command");
                    return;
                }

                var quedSongs = this.musicService.GetAllTracks();

                if (quedSongs.Any())
                {
                    EmbedBuilder embedBuilder = new EmbedBuilder()
                    {
                        Title = "Song Queue",
                        Color = Color.LighterGrey,
                        Fields = new List<EmbedFieldBuilder>(),
                    };

                    var info = quedSongs.Select((track, y) => new { index = (y).ToString(), title = Helpers.CleanSongTitle(track.Title, track.Author), duration = track.Duration }).ToList();

                    var indexString = $"Playing{Environment.NewLine}{string.Join(Environment.NewLine, info.Select(x => x.index).Skip(1).ToList())}";
                    var songString = $"{string.Join(Environment.NewLine, info.Select(x => x.title).ToList())}";
                    var durationString = $"{string.Join(Environment.NewLine, info.Select(x => x.duration).ToList())}";
                    embedBuilder.Fields.Add(new EmbedFieldBuilder() { IsInline = true, Name = "#", Value = indexString });
                    embedBuilder.Fields.Add(new EmbedFieldBuilder() { IsInline = true, Name = "Name", Value = songString });
                    embedBuilder.Fields.Add(new EmbedFieldBuilder() { IsInline = true, Name = "Duration", Value = durationString });

                    var footerText = quedSongs.Count > 1 ?
                        $" Current song remaining duration - {(quedSongs[0].Duration - quedSongs[0].Position).ToString("hh\\:mm\\:ss")}{Environment.NewLine}" +
                                                         $"Total Queue Duration - {info.Skip(1).Select(x => x.duration).ToList().Aggregate((x, y) => x + y).ToString("hh\\:mm\\:ss")}" :
                        $" Current song remaining duration - {(quedSongs[0].Duration - quedSongs[0].Position).ToString("hh\\:mm\\:ss")}";
                                                       
                    embedBuilder.WithFooter(new EmbedFooterBuilder()
                    {
                        Text = footerText
                    });
                    await ReplyAsync(null, false, embedBuilder.Build());
                }
                else
                {
                    await ReplyAsync("No songs currently in queue");
                }
            }
            catch (Exception e)
            {
                await ReplyAsync($"Error queing song(s)");
                logger.LogError($"Error queing song(s):\n{e}");
            } 
        }
         
    }
}
