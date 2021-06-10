using OuterHeavenBot;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OuterHeavenBot.Audio;
using System.Threading;
using Discord.WebSocket;

namespace OuterHeavenBot
{
    public class StartUp
    {
        private readonly DiscordBotInitializer discordBotInitializer;
        private readonly AudioManager audioManager;
        public StartUp(DiscordBotInitializer discordBotInitializer,AudioManager audioManager)
        {
            this.discordBotInitializer = discordBotInitializer;
            this.audioManager = audioManager;
          
        }
        public async Task Start()
        {
             await this.discordBotInitializer.Initialize();

            var task = Task.Factory.StartNew(async () =>
            {

                while (true)
                {
                    await audioManager.Listen();
                   
                }

            }, TaskCreationOptions.LongRunning);
        }

       
    }
}
