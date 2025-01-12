using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using OuterHeavenLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeaven.LavalinkLight
{
    public class BotCommands : ModuleBase<SocketCommandContext>
    {
        ILogger<BotCommands> logger;
        MusicService musicService;
        public BotCommands(ILogger<BotCommands> logger, MusicService musicService) 
        {
            this.logger = logger;
            this.musicService = musicService;

        }
         
        [Command("play", RunMode = RunMode.Async)]
        [Alias("p")]
        public async Task Play([Remainder] string argument)
        {
            try
            {
              await musicService.Query(this.Context, argument); 
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
                    return;
                }
                await musicService.Skip(); 
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
