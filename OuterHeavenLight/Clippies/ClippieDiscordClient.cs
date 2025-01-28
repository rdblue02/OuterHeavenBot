using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using OuterHeaven.LavalinkLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenLight.Clippies
{
    public class ClippieDiscordClient:DiscordSocketClient 
    {
        private readonly ILogger logger;
        private readonly AppSettings botSettings; 
        public ClippieDiscordClient(ILogger<ClippieDiscordClient> logger,
                                    AppSettings botSettings ) :base (new DiscordSocketConfig()
                                    {
                                        LogLevel = LogSeverity.Verbose,
                                        GatewayIntents = GatewayIntents.All,
                                        MessageCacheSize = 100,
                                        LogGatewayIntentWarnings = false
                                    })
        {
            this.logger = logger;
            this.botSettings = botSettings; 
            this.Log += ClippieDiscordClient_Log; 
        }

        public async Task InitializeAsync()
        {
            
            await this.LoginAsync(TokenType.Bot, botSettings.ClippieBotSettings.DiscordToken);
            await this.SetGameAsync("| clippies", null, ActivityType.Playing);            
            await this.StartAsync(); 
        }

        private Task ClippieDiscordClient_Log(LogMessage arg)
        { 
            if (arg.Severity == LogSeverity.Error)
            {
                logger.LogError($"{arg.Message}\n{arg.Exception}");
            }
            else
            {
                logger.LogInformation($"{arg.Message}");
            }
            return Task.CompletedTask;
        }
    }
}
