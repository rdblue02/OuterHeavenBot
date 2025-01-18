
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using OuterHeavenLight.Constants;

namespace OuterHeavenLight.Music
{
    public class MusicCommandHandler
    { 
        protected readonly CommandService commandService;
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger<MusicCommandHandler> logger;
        private readonly List<CommandInfo> commands; 
        private const char Prefix = '~';

        public MusicCommandHandler(ILogger<MusicCommandHandler> logger,
                                  CommandService commandService,
                                  IServiceProvider serviceProvider)
        {
            this.logger = logger;
            this.commandService = commandService;
            this.serviceProvider = serviceProvider;
            commands = new List<CommandInfo>();
            this.commandService.Log += (log) =>
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
            };
        }

        public async Task InstallCommandsAsync(List<Type> types)
        {
            foreach (var type in types)
            {
                var module = await commandService.AddModuleAsync(type, serviceProvider);

                this.commands.AddRange(module.Commands);
            }
            foreach (var command in this.commands)
            {
                logger.LogInformation($"Adding command {command.Name}");
            }

            logger.LogInformation($"Initialization of {this.GetType().Name} complete"); 
        } 

        public async Task HandleMessage(DiscordSocketClient client, SocketMessage message)
        {
            try
            {
                
                var userMessage = message as SocketUserMessage;
                if (userMessage == null || message.Author.IsBot) return;

                var argPos = GetCommandArgPos(client.CurrentUser, userMessage);

                if (argPos < 1)
                {
                    logger.LogError($"Invalid message {message?.Content} sent by {message?.Author?.Username}");
                    return;
                }

                var commandInfo = GetCommandInfoFromMessage(userMessage);
                if (commandInfo == null)
                {
                    return;
                }

                var context = new SocketCommandContext(client, userMessage);

                logger.LogInformation($"{client?.GetType()?.Name} Bot command received by user {message?.Author?.Username} in channel {message?.Channel?.Name}. Processing message \n\"{message?.Content}\"");
                await commandService.ExecuteAsync(
                context: context,
                argPos: argPos,
                services: serviceProvider);

              
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
            }
        }

        public CommandInfo? GetCommandInfoFromMessage(SocketUserMessage message)
        {
            if (string.IsNullOrWhiteSpace(message?.CleanContent)) return null;
            var endOfCommand = message.CleanContent.IndexOf(' ');

            var content = message.CleanContent.Substring(0, endOfCommand > 0 ? endOfCommand : message.CleanContent.Length).Replace(Prefix, '\0').Trim();
            var info = commands.FirstOrDefault(x => x.Name.ToLower() == content.ToLower() || x.Aliases.Any(x => x.ToLower() == content.ToLower()));
            return info;
        }
        private int GetCommandArgPos(SocketSelfUser selfUser, SocketUserMessage userMessage)
        {
            var argPos = 0;

            if (!(userMessage.HasCharPrefix(Prefix, ref argPos) ||
                userMessage.HasMentionPrefix(selfUser, ref argPos))) return 0;

            return argPos;
        }
    }
}
