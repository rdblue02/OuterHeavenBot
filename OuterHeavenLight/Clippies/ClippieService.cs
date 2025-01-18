
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Channels;

namespace OuterHeavenLight.Clippies
{
    public class ClippieService
    {
        ILogger logger;
        ClippieDiscordClient discordClient;
        ClippieCommandHandler clippieCommandHandler; 
 
        public ClippliePlayerState clippliePlayerState { get; private set; } = ClippliePlayerState.Disconnected;

        public ClippieService(ILogger<ClippieService> logger,
                              ClippieDiscordClient client,
                              ClippieCommandHandler clippieCommandHandler)
        {
            this.logger = logger;
            this.discordClient = client;
            this.clippieCommandHandler = clippieCommandHandler; 

            discordClient.Ready += DiscordClient_Ready;
            
            discordClient.MessageReceived += DiscordClient_MessageReceived;
            discordClient.Disconnected += DiscordClient_Disconnected; 
        }  

        public async Task InitializeAsync() 
        {
            await this.discordClient.InitializeAsync();
            await clippieCommandHandler.InstallCommandsAsync(new List<Type>() { (typeof(ClippieCommands)) });
        }  

        private Task DiscordClient_Ready()
        { 
            this.clippliePlayerState = ClippliePlayerState.Available;
            return Task.CompletedTask;
        }
 
        private Task DiscordClient_Disconnected(Exception arg)
        {
            this.clippliePlayerState = ClippliePlayerState.Disconnected;
            logger.LogError(arg?.ToString() ?? "");  
            return Task.CompletedTask;
        }

        private async Task DiscordClient_MessageReceived(SocketMessage messageParam)
        {
            var userMessage = messageParam as SocketUserMessage;
            if (userMessage == null) return;

            await clippieCommandHandler.HandleCommandAsync(discordClient, userMessage);  
        }

        public async Task PlayClippie(string contentRequested, SocketCommandContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                if (clippliePlayerState != ClippliePlayerState.Available)
                {
                    logger.LogWarning($"Clippie bot is currently in {clippliePlayerState} state.");
                    await context.Channel.SendMessageAsync("Clippies are currently unavailable");
                    return;
                }

                if (context.User is IVoiceState voice && voice.VoiceChannel != null)
                { 
                    clippliePlayerState = ClippliePlayerState.Playing; 
                    await PlayClippie(contentRequested,context.Channel, voice.VoiceChannel, cancellationToken); 
                }
                else
                {
                    await context.Channel.SendMessageAsync("You must be in a voice channel to use this command");
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex.ToString()); 
            } 
        }

        private async Task PlayClippie(string contentRequested, ISocketMessageChannel channel, IVoiceChannel voice, CancellationToken cancellationToken = default)
        {
            try
            {   
                var bytes = ClippieHelpers.ReadClippieFile(contentRequested);
               
                if(bytes?.Any() ?? false)
                { 
                    var audioClient = await voice.ConnectAsync();
                    await Task.Delay(200);
                    var discordOutStream = audioClient.CreatePCMStream(AudioApplication.Mixed, 98304, 20);
                        await discordOutStream.WriteAsync(bytes, cancellationToken); 
                              discordOutStream.Flush();
                    logger.LogInformation("Clippie finished");
                    audioClient.Dispose();
                    discordOutStream.Dispose();  
                    await voice.DisconnectAsync();
                    await Task.Delay(300);
                    this.clippliePlayerState = ClippliePlayerState.Available;
                }
                else
                {
                    await channel.SendMessageAsync($"No files found for {contentRequested}");               
                }
            }

            catch (Exception e)
            {
                logger.LogError($"Error playing clippie in channel name: {voice?.Name} Error:\n{e}");
                await channel.SendMessageAsync($"Error playing clippie {contentRequested}");
                await Task.Delay(500);
                this.clippliePlayerState = ClippliePlayerState.Available;
            }
        }  
    }
}
