using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using OuterHeavenBot.ClippieExtensions;
using OuterHeavenBot.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OuterHeavenBot.Commands.Modules
{
    public class ClippieCommands : ModuleBase<SocketCommandContext>
    {
        ILogger logger;
        ClippieService clippieService;
       
        public ClippieCommands(ILogger<ClippieCommands> logger,
                               ClippieService clippieService)
        {
            this.logger = logger;
            this.clippieService = clippieService;
        }

        [Command("clippie", RunMode = RunMode.Async)]
        [Alias("c")]
        public async Task Clippie(string contentName = "")
        {
            try
            {
                await clippieService.PlayClippie(contentName, Context);
            }
            catch (Exception ex)
            {
                await ReplyAsync("Error playing clippie");
                logger.LogError($"Error playing clippie:\n{ex.ToString()}");
            }
        }

        [Command("sounds", RunMode = RunMode.Async)]
        [Alias("s")]
        public async Task SendUserAvailableSounds(string? category = null)
        {
            if (category is null)
            {
                throw new ArgumentNullException(nameof(category));
            }

            try
            {
                var directories = ClippieHelpers.GetAudioFiles();
                StringBuilder message = new StringBuilder();
                if (string.IsNullOrWhiteSpace(category))
                {
                    message.Append("Available sounds types" + Environment.NewLine);
                    message.Append(string.Join(", ", directories.Keys));
                    await ReplyAsync(message.ToString());
                }
                else if (directories.ContainsKey(category.ToLower().Trim()))
                {
                    message.Append($"Available sounds for {category} below. Use ~p <filename> or ~play <filename> to play" + Environment.NewLine);

                    foreach (var file in directories[category])
                    {
                        var fileName = file.Name;
                        var extensionIndex = fileName.LastIndexOf('.');
                        fileName = fileName.Substring(0, extensionIndex);
                        fileName = fileName.Replace("-1", "").Trim();

                        message.Append($"{fileName}{Environment.NewLine}");
                    }
                    await this.Context.User.SendMessageAsync(message.ToString().TrimEnd(','));
                }
                else
                {
                    await ReplyAsync("Invalid sound option");
                }
            }
            catch(Exception ex)
            {
                await ReplyAsync("Error Sending sounds");
                logger.LogError($"Error sending sounds:\n{ex.ToString()}");
            }           
        }
    }
}
