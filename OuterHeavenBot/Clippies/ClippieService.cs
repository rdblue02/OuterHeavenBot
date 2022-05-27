using Discord;
using Discord.Audio;
using OuterHeavenBot.Clients;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OuterHeavenBot.ClippieExtensions;
using Discord.Commands;
using OuterHeavenBot.Commands;
using Discord.WebSocket;

namespace OuterHeavenBot.Services
{
    public class ClippieService
    {
        ILogger logger;
        ClippieDiscordClient discordClient;
        ClippieCommandHandler clippieCommandHandler;
        ulong? currentChannelId = 0;
        ulong botUserId;

        public ClippliePlayerState clippliePlayerState { get; set; } = ClippliePlayerState.Available;

        public ClippieService(ILogger<ClippieService> logger,
                              ClippieDiscordClient clippieDiscordClient,
                              ClippieCommandHandler clippieCommandHandler)
        {
            this.logger = logger;
            this.discordClient = clippieDiscordClient;
            this.clippieCommandHandler = clippieCommandHandler;
            this.clippliePlayerState = ClippliePlayerState.Connecting;

            discordClient.Ready += DiscordClient_Ready;
            discordClient.MessageReceived += DiscordClient_MessageReceived;
            discordClient.Disconnected += DiscordClient_Disconnected;
            discordClient.Connected += DiscordClient_Connected;
        }

        private Task DiscordClient_Connected()
        {
            this.clippliePlayerState = ClippliePlayerState.Available;
            return Task.CompletedTask;
        }

        public async Task InitializeAsync() 
        {
            await this.discordClient.InitializeAsync();
            await clippieCommandHandler.ApplyCommands();
        }  

        private Task DiscordClient_Ready()
        {
            this.botUserId = discordClient.CurrentUser.Id;

            this.clippliePlayerState = ClippliePlayerState.Available;
            return Task.CompletedTask;
        }

        private Task DiscordClient_Disconnected(Exception arg)
        {
            logger.LogError(arg?.ToString() ?? "");
            this.clippliePlayerState = ClippliePlayerState.Disconnected;
            this.currentChannelId = null;
            return Task.CompletedTask;
        }

        private async Task DiscordClient_MessageReceived(SocketMessage messageParam)
        {
             var userMessage = messageParam as SocketUserMessage;
             if (userMessage == null) return;

             var requestedCommand = clippieCommandHandler.GetCommandInfoFromMessage(userMessage);
             if(requestedCommand == null) return;

            if (requestedCommand.Name.ToLower() == "clippie" &&
                clippliePlayerState != ClippliePlayerState.Available)
            { 

                logger.LogWarning($"Clippie bot is currently in {clippliePlayerState} state.");
                await userMessage.Channel.SendMessageAsync("Clippies are currently unavailable"); 
                return;
            }

             await clippieCommandHandler.HandleCommandAsync(discordClient, userMessage);
        }

        public async Task PlayClippie(string contentRequested, SocketCommandContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                //this is so I can test the clippie bot while hanging out in a different discord.
                //obviously you can't hear whats played but I can debug logic errors at least.
                //if (System.Diagnostics.Debugger.IsAttached && context.Channel is not IVoiceState)
                //{

                //    var voiceChannel = context.Guild.Channels.OfType<IVoiceChannel>().FirstOrDefault(x=>x.GuildId==context.Guild.Id);
                //    logger.LogWarning($"DEBUG MODE ENABLED! Clippies are being played in the first available voice channel.{voiceChannel?.Name}");
                  
                //    if (voiceChannel != null) await PlayClippie(contentRequested, voiceChannel, cancellationToken);
                  
                //    return;
                //}

                if (context.User is IVoiceState voice)
                {
                    await PlayClippie(contentRequested,context.Channel, voice.VoiceChannel, cancellationToken);
                    clippliePlayerState = ClippliePlayerState.Available;
                }
                else
                {
                    await context.Channel.SendMessageAsync("You must be in a voice channel to use this command");
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex.ToString());
                clippliePlayerState = ClippliePlayerState.Available;
            }
        }

        private async Task PlayClippie(string contentRequested, ISocketMessageChannel channel, IVoiceChannel voice, CancellationToken cancellationToken = default)
        {
            try
            {
                currentChannelId = voice.Id;
                clippliePlayerState = ClippliePlayerState.Connecting;

                var bytes = ClippieHelpers.ReadClippieFile(contentRequested);
               
                if(bytes?.Any() ?? false)
                {
                    var audioClient = await voice.ConnectAsync();
                    audioClient.Disconnected += AudioClient_Disconnected;
                    using (var discordOutStream = audioClient.CreatePCMStream(AudioApplication.Mixed, 98304, 20))
                    {
                        clippliePlayerState = ClippliePlayerState.Playing;
                        await discordOutStream.WriteAsync(bytes, cancellationToken);
                        await audioClient.StopAsync();
                    }

                    await Task.Delay(50);
                    await voice.DisconnectAsync();
                }
                else
                {
                    await channel.SendMessageAsync($"No files found for {contentRequested}");               
                }
            }

            catch (Exception e)
            {
                logger.LogError($"Error playing clippie. Current channel id: {currentChannelId} channel name: {voice?.Name}");
                await channel.SendMessageAsync($"Error playing clippie {contentRequested}");
                throw;
            }
        }
        private Task AudioClient_Disconnected(Exception arg)
        {
            logger.LogError(arg?.ToString() ??"");

            this.clippliePlayerState = ClippliePlayerState.Available;
            this.currentChannelId = null;

            return Task.CompletedTask;
        }

    }
}
