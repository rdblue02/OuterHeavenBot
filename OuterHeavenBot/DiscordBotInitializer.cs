using Discord;
using Discord.Audio;
using Discord.WebSocket;
using OuterHeavenBot;
using OuterHeavenBot.Command;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenBot
{
    public class DiscordBotInitializer
    {
        DiscordSocketClient client;
        CommandHandler commandHandler;
        DiscordBotConfig options;
 
        public DiscordBotInitializer(DiscordSocketClient client,
                                        CommandHandler commandHandler,                                        
                                        DiscordBotConfig options)
        {
            this.client = client;
            client.Log += Log;
            this.commandHandler = commandHandler;
            this.options = options;
         
        }
        public async Task<DiscordSocketClient> Initialize()
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
            await client.SetGameAsync("|~h for more info",null,ActivityType.Playing);
            await client.LoginAsync(TokenType.Bot, options.Token);
            await client.StartAsync();
        }
         
        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
  
    }
}
