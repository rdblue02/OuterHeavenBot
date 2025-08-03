using Discord.Commands;
using OuterHeaven.LavalinkLight;
using OuterHeavenLight.Constants;

namespace OuterHeavenLight.Dev
{
    [Name(CommandGroupName.Dev)]
    public class DevCommands : ModuleBase<SocketCommandContext>
    { 
        private ILogger<DevCommands> logger;
        private DevService devService;
        public DevCommands(ILogger<DevCommands> logger,
                           AppSettings appSettings,
                           DevService devService)
        {
            this.logger = logger; 
            this.devService = devService;
        }

        [Command("logs", RunMode = RunMode.Async)]
        public async Task RequestLogs()
        {
            var result = await devService.GetLogsFromServerAsync(this.Context.User);
            if (!string.IsNullOrWhiteSpace(result))
            {
                await ReplyAsync(result);
            } 
        }
  
        [Command("settings", RunMode = RunMode.Async)]
        public async Task GetSettings()
        {
            try
            {
                var response = await devService.GetSettingsFromServerAsync(this.Context.User);
                await ReplyAsync(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
                await ReplyAsync($"Error locating bot settings\n {ex.Message}");
            }
        } 
    } 
} 