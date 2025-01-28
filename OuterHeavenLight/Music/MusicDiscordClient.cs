using Discord;
using Discord.WebSocket;
using OuterHeaven.LavalinkLight;
using OuterHeavenLight.Clippies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenLight.Music
{
    public class MusicDiscordClient:DiscordSocketClient
    {
        private readonly ILogger<MusicDiscordClient> logger;
        private readonly AppSettings botSettings;

        public MusicDiscordClient(ILogger<MusicDiscordClient> logger,
                                    AppSettings botSettings) : base(new DiscordSocketConfig()
                                    {
                                        LogLevel = LogSeverity.Verbose,
                                        GatewayIntents = GatewayIntents.All,
                                        MessageCacheSize = 100,
                                        LogGatewayIntentWarnings = false
                                    })
        {
            this.logger = logger;
            this.botSettings = botSettings;
            this.Log += MusicDiscordClient_Log;
        }
       

        public async Task InitializeAsync()
        { 
            await this.LoginAsync(TokenType.Bot, botSettings.OuterHeavenBotSettings.DiscordToken);
            await this.SetGameAsync("| music", null, ActivityType.Playing);
            await this.StartAsync();
        }

        private Task MusicDiscordClient_Log(LogMessage log)
        {
            if (log.Severity == LogSeverity.Error)
            {
                logger.LogError(log.ToString());
            }
            else
            {
                logger.LogInformation(log.ToString());
            }
            return Task.CompletedTask;
        }
    }
}
