using Discord.Commands; 
using OuterHeaven.LavalinkLight;
using OuterHeavenLight.Constants;
using OuterHeavenLight.Core;

namespace OuterHeavenLight.Dev
{
    [Name(CommandGroupName.Dev)]
    public class DevCommands : ModuleBase<SocketCommandContext>
    {  
        private DevService devService;
        public DevCommands(AppSettings appSettings,
                           DevService devService)
        { 
            this.devService = devService;
        }

        [Command("logs", RunMode = RunMode.Async)]
        public async Task RequestLogs()
        {
            var result = await devService.GetLogsFromServerAsync(this.Context.User);
            await Respond(result);
        }
  
        [Command("settings", RunMode = RunMode.Async)]
        public async Task GetSettings()
        {
            var result = await devService.GetSettingsFromServerAsync(this.Context.User);
            await Respond(result);
        }

        private async Task Respond(CommandResult result) => await (!string.IsNullOrWhiteSpace(result.UserMessage) ? ReplyAsync(result.UserMessage) : Task.CompletedTask);
    } 
} 