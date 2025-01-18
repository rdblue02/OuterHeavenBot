using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using OuterHeaven.LavalinkLight;
using OuterHeavenLight.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenLight.Music
{
    [Name(CommandGroupName.Music)]
    public class MusicCommands : ModuleBase<SocketCommandContext>
    {
        ILogger<MusicCommands> logger;
        MusicService musicService;
        Lava lava;
        public MusicCommands(ILogger<MusicCommands> logger,
                             MusicService musicService,
                             Lava lava)
        {
            this.logger = logger;
            this.musicService = musicService;
            this.lava = lava;
        }

        [Command("play", RunMode = RunMode.Async)]
        [Alias("p")]
        public async Task Play([Remainder] string argument)
        {
            try
            {
                await musicService.Query(Context, argument);
            }
            catch (Exception e)
            {
                logger.LogError($"Error: {e}");
                await ReplyAsync(e.Message);
            }
        }

        [Command("skip", RunMode = RunMode.Async)]
        [Alias("sk")]
        public async Task Skip()
        {
            try
            {
                var trackInfo = musicService.GetCurrentTrackInfo();
                if (trackInfo == null)
                {
                    await ReplyAsync("Nothing to skip");
                }
                else
                {
                    await ReplyAsync($"Skipping track {trackInfo.title}");
                    await musicService.Skip();
                }
            }
            catch (Exception e)
            {
                logger.LogError($"Error: {e}");
                await ReplyAsync(e.Message);
            }
        }

        [Command("queue", RunMode = RunMode.Async)]
        [Alias("q")]
        public async Task QueueInfo()
        {
            try
            {
                var builder = musicService.GetQeueueInfo();
                var message = builder.Build();

                await ReplyAsync(message);
            }
            catch (Exception e)
            {
                logger.LogError($"Error: {e}");
                await ReplyAsync(e.Message);
            }
        }

        [Command("track", RunMode = RunMode.Async)]
        [Alias("t")]
        public async Task TrackInfo()
        {
            try
            {
                var trackInfo = musicService.GetCurrentTrackInfo();
                if (trackInfo == null)
                {
                    await ReplyAsync("Nothing is playing. Use ~p to play a track!");
                }
                else
                {
                    await ReplyAsync($"Current track title [{trackInfo.title}]");
                }
            }
            catch (Exception e)
            {
                logger.LogError($"Error: {e}");
                await ReplyAsync(e.Message);
            }
        }

        [Command("clearqueue", RunMode = RunMode.Async)]
        [Alias("cq")]
        public async Task ClearQueue([Remainder] int? position = null)
        {
            try
            {
                var result = musicService.ClearQueue(position);
                await ReplyAsync(result);
            }
            catch (Exception e)
            {
                logger.LogError($"Error: {e}");
                await ReplyAsync(e.Message);
            }
        }
    }
}
