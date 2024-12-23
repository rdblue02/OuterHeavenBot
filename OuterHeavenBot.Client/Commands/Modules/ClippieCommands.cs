using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using OuterHeavenBot.Client.Services;
using OuterHeavenBot.Core;
using OuterHeavenBot.Core.Extensions;
using OuterHeavenBot.Core.Models;
using OuterHeavenBot.Lavalink;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OuterHeavenBot.Client.Commands.Modules
{
    public class ClippieCommands : ModuleBase<SocketCommandContext>
    {
        ILogger<ClippieCommands> logger;   
        ClippieService clippieService;
        ISearchHandler<ClippieFileData> searchHandler;
        public ClippieCommands(ILogger<ClippieCommands> logger,  
                                ClippieService clippieService,
                                ISearchHandler<ClippieFileData> searchHandler)
        {
            this.logger = logger;  
            this.clippieService = clippieService;
            this.searchHandler = searchHandler;
        }

        [Command("clippie", RunMode = RunMode.Async)]
        [Alias("c")]
        public async Task Clippie(string contentName = "")
        {
            try
            {
                var voice = Context.GetUserVoiceChannel();
                if (voice == null)
                {
                    await ReplyAsync("You must be in a voice channel to use this command");
                    return;
                }


                var matches = await searchHandler.SearchAsync(contentName);
                var match = matches.FirstOrDefault();

                if (match == null ||
                    match.Data == null ||
                    match.Data.Length == 0)
                {
                    if (string.IsNullOrWhiteSpace(contentName))
                    {
                        await ReplyAsync($"Cannot find clippies!");
                    }
                    else
                    {
                        await ReplyAsync($"Unable to find a match for {contentName}");
                    }
                    return;
                }

                match.RequestingChannel = voice;
                var result = await clippieService.RequestClippie(match);
                if (!string.IsNullOrWhiteSpace(result.Message))
                {
                    await ReplyAsync($"{result.Message}");
                }
            }
            catch (Exception ex)
            {
                await ReplyAsync("Error playing clippie");
                logger.LogError($"Error playing clippie:\n{ex.ToString()}");
            }
        }  
    }
}
