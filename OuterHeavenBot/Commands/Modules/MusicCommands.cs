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

                if(!await ValidateVoiceCommand()) return;

                await musicService.RequestSong(argument, (this.Context.User as IVoiceState).VoiceChannel, Context.Channel as ITextChannel ?? throw new ArgumentNullException(nameof(Context.Channel)));
            }
            catch (Exception e)
            {
                await ReplyAsync($"Error playing {argument}");
                logger.LogError($"Error playing {argument}:\n{e.ToString()}");
            }
        }

        [Command("pause", RunMode = RunMode.Async)]
        [Alias("pa")]
        public async Task Pause()
        {
            try
            {
                if (!await ValidateVoiceCommand(musicService.MusicBotPlayerState != MusicBotPlayerState.Paused)) return;

                await musicService.ChangePauseState((Context.User as IVoiceState).VoiceChannel, Context.Channel as ITextChannel);

                await ReplyAsync($"Player is now {musicService.MusicBotPlayerState}");
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
                if (!await ValidateVoiceCommand(true)) return;
                await musicService.RequestSkip((Context.User as IVoiceState).VoiceChannel, Context.Channel as ITextChannel);
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
                if (!await ValidateVoiceCommand()) return;

                await musicService.RequestQueueClear(index, (Context.User as IVoiceState).VoiceChannel, Context.Channel as ITextChannel);
            }
            catch (Exception e)
            {
                await ReplyAsync($"Error clearing queue");
                logger.LogError($"Error clearing queue:\n{e}");
            }
        }

        [Command("fastforward", RunMode = RunMode.Async)]
        [Alias("ff")]
        public async Task FastForward(int seconds = 0)
        {
            try
            {
                if (!await ValidateVoiceCommand(true)) return;

                if (seconds <= 0)
                {
                    await ReplyAsync($"Invalid fast forward duration {seconds}");
                    return;
                }

                await musicService.FastForward(seconds, (Context.User as IVoiceState).VoiceChannel, Context.Channel as ITextChannel);
            }
            catch (Exception e)
            {
                await ReplyAsync($"Error trying to fast forward track");
                logger.LogError($"Error trying to fast forward track:\n{e}");
            } 
        }

        [Command("rewind", RunMode = RunMode.Async)]
        [Alias("rw")]
        public async Task Rewind(int seconds)
        {
            try
            {
                if (!await ValidateVoiceCommand(true)) return;

                if (seconds <= 0)
                {
                    await ReplyAsync($"Invalid rewind duration {seconds}");
                    return;
                }
                await musicService.RequestRewind(seconds, (Context.User as IVoiceState).VoiceChannel, Context.Channel as ITextChannel);
            }
            catch (Exception e)
            {
                await ReplyAsync($"Error trying to rewind track");
                logger.LogError($"Error trying to rewind track:\n{e}");
            }
        }

        [Command("goto", RunMode = RunMode.Async)]
        [Alias("gt")]
        public async Task GoTo(string time)
        {
            try
            {
                if (!await ValidateVoiceCommand(true)) return;

                if (!TimeSpan.TryParse(time, out TimeSpan timeResult))
                {
                    await ReplyAsync($"Invalid time entry. Make sure to use hh:mm:ss format.");
                    return;
                }

                await musicService.GoTo(timeResult, (Context.User as IVoiceState).VoiceChannel, Context.Channel as ITextChannel);
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
                var trackInfo = musicService.GetCurrentTrackInfo((Context.User as IVoiceState).VoiceChannel, Context.Channel as ITextChannel);
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

                var quedSongs = this.musicService.GetAllTracks((Context.User as IVoiceState).VoiceChannel, Context.Channel as ITextChannel);

                if (quedSongs.Any())
                {
                    EmbedBuilder embedBuilder = new EmbedBuilder()
                    {
                        Title = "Song Queue",
                        Color = Color.LighterGrey,
                        Fields = new List<EmbedFieldBuilder>(),
                    };

                    var info = quedSongs.Select((track, y) => new { index = (y + 1).ToString(), title = CleanSongTitle(track.Title, track.Author), duration = track.Duration }).ToList();

                    var indexString = $"Playing{Environment.NewLine}{string.Join(Environment.NewLine, info.Select(x => x.index).ToList())}";
                    var songString = $"{string.Join(Environment.NewLine, info.Select(x => x.title).ToList())}";
                    var durationString = $"{string.Join(Environment.NewLine, info.Select(x => x.duration).ToList())}";
                    embedBuilder.Fields.Add(new EmbedFieldBuilder() { IsInline = true, Name = "#", Value = indexString });
                    embedBuilder.Fields.Add(new EmbedFieldBuilder() { IsInline = true, Name = "Name", Value = songString });
                    embedBuilder.Fields.Add(new EmbedFieldBuilder() { IsInline = true, Name = "Duration", Value = durationString });

                    var footerText = quedSongs.Count > 1 ? $" Current song remaining duration - {quedSongs[0].Duration - quedSongs[0].Position}"
                                                       : $" Current song remaining duration - {quedSongs[0].Duration - quedSongs[0].Position}{Environment.NewLine}" +
                                                         $"Total Queue Duration - {info.Skip(1).Select(x => x.duration).ToList().Aggregate((x, y) => x + y)}";
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

        private string CleanSongTitle(string title, string author)
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
                    return cleanedTitle.Substring(0, 39) + "...";
                }
                else
                {
                    return cleanedTitle;
                }
            }
        }

        public async Task<bool> ValidateVoiceCommand(bool requireSound = false)
        {
            var user = Context.User as IVoiceState;
            bool valid = false;
            if (user == null || user?.VoiceChannel == null)
            {
                await ReplyAsync("You must be in a voice channel for this command");
            }
            else if (this.musicService.MusicBotPlayerState == MusicBotPlayerState.Playing && user?.VoiceChannel.Id != musicService.CurrentChannelId)
            {
                await ReplyAsync("You must be in the same channel as the bot for this command");
            }
            else if (requireSound && this.musicService.MusicBotPlayerState != MusicBotPlayerState.Playing)
            {
                await ReplyAsync($"The bot isn't playing anything!");
            }
            else
            {
                valid = true;
            }
            return valid;
        }
    }
}
