using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using OuterHeavenBot.Commands.Modules;
using OuterHeavenBot.Commands;
using OuterHeavenBot.Services;
using OuterHeavenBot.Setup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenBot.Clients
{
    public class ClippieDiscordClient:DiscordSocketClient 
    {
        private readonly ILogger logger;
        private readonly BotSettings botSettings;
        public ClippieDiscordClient(ILogger<ClippieDiscordClient> logger,
                                    BotSettings botSettings)
        {
            this.logger = logger;
            this.botSettings = botSettings;
            this.Log += ClippieDiscordClient_Log;
              
        }

        public async Task InitializeAsync()
        {
            
            await this.LoginAsync(TokenType.Bot, botSettings.ClippieBotToken);
            await this.SetGameAsync("| clippies", null, ActivityType.Playing);            
            await this.StartAsync(); 
        }

        private Task ClippieDiscordClient_Log(LogMessage arg)
        {
            logger.Log(Helpers.ToMicrosoftLogLevel(arg.Severity), $"{arg.Message}{arg.Exception}");
            return Task.CompletedTask;
        }
    }
}
