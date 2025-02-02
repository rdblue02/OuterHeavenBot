using Discord;
using Discord.Commands;
using Discord.WebSocket;
using OuterHeavenLight.Music;

namespace OuterHeavenLight.Dev
{
    public class DevCommandHandler
    {
        private readonly CommandService commandService;
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger logger;
        //todo this should be a setting
        private const string Prefix = "!";

        public DevCommandHandler(CommandService commandService,
                                         IServiceProvider serviceProvider,
                                         ILogger<DevCommandHandler> logger)
        {
            this.commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            commandService.Log += (arg) =>
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
            };

        }

        public bool IsDevCommandFor<TClient>(SocketUserMessage userMessage) where TClient : IDiscordClient
        {
            //clippie not suppported atm
            return userMessage?.Channel is SocketDMChannel && typeof(TClient) == typeof(MusicDiscordClient) &&
                   userMessage.Content.StartsWith(Prefix);
        }

        public async Task HandleCommandAsync(DiscordSocketClient discordSocketClient, SocketUserMessage message)
        {
            try
            {
                var context = new SocketCommandContext(discordSocketClient, message);

                logger.LogInformation($"{discordSocketClient?.GetType()?.Name} Bot command received by user {message?.Author?.Username} in channel {message?.Channel?.Name}. Processing message \n\"{message?.Content}\"");
                await commandService.ExecuteAsync(
                context: context,
                argPos: Prefix.Length,
                services: serviceProvider);
            }
            catch (Exception e)
            {
                logger.LogError($"Unable to proccess command message {message?.ToString()}\n {e}");
            }
        }
        public async Task InstallCommandsAsync()
        {
            await commandService.AddModuleAsync(typeof(DevCommands), serviceProvider); 
        }
    }
}
