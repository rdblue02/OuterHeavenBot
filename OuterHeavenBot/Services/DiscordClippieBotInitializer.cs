using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using OuterHeavenBot.Command; 
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenBot.Services
{
    public class DiscordClippieBotInitializer
    {
        DiscordClippieClient client;
        CommandHandler<DiscordClippieClient> commandHandler;
        IConfiguration config;
        public DiscordClippieBotInitializer(DiscordClippieClient client,
                                           CommandHandler<DiscordClippieClient> commandHandler,
                                           AudioService audioService,
                                           IConfiguration config)
        {
        
            this.commandHandler = commandHandler;
            this.client = client;
           
            client.Log += Log; 
            this.config = config ?? throw new ArgumentNullException("misssing appsettings.config");
            if (config["discord_clippie_token"] == null)
            {
                throw new ArgumentNullException("No discord_clippie_token found in appsettings.json");
            }
         
        }
        public async Task<DiscordClippieClient> Initialize()
        {
            var startTasks = new List<Task>()
            {
                StartClient(),
                commandHandler.InstallCommandsAsync()
            };

            await Task.WhenAll(startTasks);
            return this.client;
        }
        private async Task StartClient()
        {
            await client.SetGameAsync("|~h for more info", null, ActivityType.Playing);
            await client.LoginAsync(TokenType.Bot, config["discord_clippie_token"]);
            await client.StartAsync();
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
