
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using OuterHeavenLight.Constants;

namespace OuterHeaven.LavalinkLight
{ 
    public class MusicCommandHandler
    {
        ILogger<MusicCommandHandler> logger;
        CommandService commandService;
        IServiceProvider serviceProvider;
         
        public MusicCommandHandler(ILogger<MusicCommandHandler> logger,
                              CommandService commandService,
                              IServiceProvider serviceProvider)
        {
            this.logger = logger;
            this.commandService = commandService;
            this.serviceProvider = serviceProvider;
            this.commandService.Log += (log) =>
            {
                if (log.Severity == LogSeverity.Error)
                {
                    logger.LogError(log.Message);
                }
                else
                {
                    logger.LogInformation(log.Message);
                }
                return Task.CompletedTask;
            }; 
        }

        public async Task Initialize(List<Type> commandModules)
        {
            foreach (var type in commandModules)
            {
               await commandService.AddModuleAsync(type, serviceProvider);
            }
        }

        public async Task HandleMessage(DiscordSocketClient client, SocketMessage arg)
        {
            try
            {
                var userMessage = arg as SocketUserMessage;
                if (userMessage == null || arg.Author.IsBot) return;

                var argPos = 0;
                var isBotCommand = userMessage.HasCharPrefix('~', ref argPos) ||
                                   userMessage.HasMentionPrefix(client.CurrentUser, ref argPos);

                if (isBotCommand && argPos < 1)
                {
                    logger.LogError($"Invalid message {arg?.Content} sent by {arg?.Author?.Username}");
                    return;
                } 

                var context = new SocketCommandContext(client, userMessage); 
 
                logger.LogInformation($"{client?.GetType()?.Name} Bot command received by user {arg?.Author?.Username} in channel {arg?.Channel?.Name}. Processing message \n\"{arg?.Content}\"");
                await commandService.ExecuteAsync(
                context: context,
                argPos: argPos,
                services: serviceProvider);

                logger.LogInformation($"{client?.GetType()?.Name} Bot command \n\"{arg?.Content}\" completed");
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
            }
        } 
    }
}
