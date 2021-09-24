using Discord;
using Discord.Audio;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using OuterHeavenBot;
using OuterHeavenBot.Command;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Victoria;
using Victoria.Enums;

namespace OuterHeavenBot
{
    public class DiscordBotInitializer
    {
        DiscordSocketClient client;
        CommandHandler commandHandler;
        IConfiguration config;
        LavaNode lavaNode;
        public DiscordBotInitializer(DiscordSocketClient client,
                                        CommandHandler commandHandler,
                                        IConfiguration config,
                                        LavaNode lavaNode)
        {
            this.commandHandler = commandHandler;
            this.client = client;
            this.lavaNode = lavaNode;
            client.Log += Log;
            lavaNode.OnLog += Log;
            this.config = config ?? throw new ArgumentNullException("misssing appsettings.config");
            if (config["discord_token"] == null)
            {
                throw new ArgumentNullException("No token found in appsettings.json");
            }
            client.Ready += Client_Ready;
        }

        private async Task Client_Ready()
        {
            if (!lavaNode.IsConnected)
            {
              await lavaNode.ConnectAsync();
                lavaNode.OnTrackEnded += LavaNode_OnTrackEnded;
            }
        }

        private async Task LavaNode_OnTrackEnded(Victoria.EventArgs.TrackEndedEventArgs arg)
        {
            if (arg.Reason != TrackEndReason.Stopped && arg.Reason != TrackEndReason.Replaced)
            {
                var player = arg.Player;
                if (player.Queue.Any())
                {
                    player.Queue.TryDequeue(out LavaTrack next);
                    await player.TextChannel.SendMessageAsync(
                    $"Now playing: {next.Title} - {next.Author} - {next.Duration}");
                    await player.PlayAsync(next);
                }
            }
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
            await client.LoginAsync(TokenType.Bot, config["discord_token"]);
            await client.StartAsync();
        }
         
        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
  
    }
}
