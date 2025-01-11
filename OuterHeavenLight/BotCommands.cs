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
            }
        }

        [Command("skip", RunMode = RunMode.Async)]
        [Alias("sk")]
        public async Task Skip()
        {
            try
            {  
                await musicService.Skip(); 
            }
            catch (Exception e)
            {
                logger.LogError($"Error: {e}");
            }
        }
         
        [Command("clearqueue", RunMode = RunMode.Async)]
        [Alias("cq")]
        public async Task ClearQ([Remainder] int? index = null)
        {
            try
            {
                await musicService.Skip();

            }
            catch (Exception e)
            {
                logger.LogError($"Error: {e}");
            }
        }   
    }
}
