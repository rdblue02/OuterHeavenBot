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
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Victoria;
using Victoria.Enums;

namespace OuterHeavenBot.Services
{
    public class DiscordMusicBotInitializer
    {
        DiscordSocketClient client;
        CommandHandler<DiscordSocketClient> commandHandler;
        IConfiguration config;
        LavaNode lavaNode;
        AudioService audioService;
        public DiscordMusicBotInitializer(DiscordSocketClient client,
                                         CommandHandler<DiscordSocketClient> commandHandler,
                                         IConfiguration config,
                                         LavaNode lavaNode,
                                         AudioService audioService)
        {
            this.commandHandler = commandHandler;
            this.client = client;
            this.lavaNode = lavaNode;
            client.Log += Log;
            lavaNode.OnLog += Log;
            this.config = config ?? throw new ArgumentNullException("misssing appsettings.config");
            if (config["discord_music_token"] == null)
            {
                throw new ArgumentNullException("No discord_music_token found in appsettings.json");
            }
            this.audioService = audioService;
            client.Ready += Client_Ready;
         
             
        }
        private async Task Client_Ready()
        {
            if (!lavaNode.IsConnected)
            {
                try{
                    await lavaNode.ConnectAsync();
                    lavaNode.OnTrackEnded += LavaNode_OnTrackEnded;
                    lavaNode.OnTrackException += LavaNode_OnTrackException;
                    lavaNode.OnWebSocketClosed += LavaNode_OnWebSocketClosed;
                    lavaNode.OnTrackStuck += LavaNode_OnTrackStuck;

                    //we can do this because only one server plans to use the player.
                    var clientGuild = client.Guilds.FirstOrDefault() as IGuild;
                    this.audioService.SetPlayer(lavaNode.GetPlayer(clientGuild));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);     
                }         
            }
        }

        private async Task LavaNode_OnTrackStuck(Victoria.EventArgs.TrackStuckEventArgs arg)
        {
           if(arg.Threshold > TimeSpan.FromSeconds(5))
           {
             await arg.Player.TextChannel.SendMessageAsync($"track {arg.Track.Title} is stuck. Skipping...");
                if (arg.Player.Queue.Any())
                {
                    await arg.Player.SkipAsync();
                }
                else
                {
                    await arg.Player.StopAsync();
                }
           }
        }

        private Task LavaNode_OnWebSocketClosed(Victoria.EventArgs.WebSocketClosedEventArgs arg)
        {
            Console.WriteLine($"Socket closed for {arg.Reason}.");

            return Task.CompletedTask;
        }

        private  Task LavaNode_OnTrackException(Victoria.EventArgs.TrackExceptionEventArgs arg)
        {
            Console.WriteLine($"player [{arg.Player}]\nTitle [{arg.Track?.Title}]\nhas error [{arg.ErrorMessage}]");
            return Task.CompletedTask;
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
            await client.LoginAsync(TokenType.Bot, config["discord_music_token"]);
            await client.StartAsync();
        }
         
        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
  
    }
}
