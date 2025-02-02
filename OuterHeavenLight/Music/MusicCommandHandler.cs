
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using OuterHeavenLight.Clippies;
using OuterHeavenLight.Constants;

namespace OuterHeavenLight.Music
{
    public class MusicCommandHandler
    {
        private readonly CommandService commandService;
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger logger;
        private readonly List<CommandInfo> commands;
        //todo this should be a setting
        private const char Prefix = '~';

        public MusicCommandHandler(CommandService commandService,
                                         IServiceProvider serviceProvider,
                                         ILogger<MusicCommandHandler> logger)
        {
            this.commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.commands = new List<CommandInfo>();
            commandService.Log += CommandService_Log;
        }

        public async Task InstallCommandsAsync(List<Type> types)
        {
            foreach (var type in types)
            {
                var module = await commandService.AddModuleAsync(type, serviceProvider);
                foreach (var command in module.Commands)
                {
                    if (!this.commands.Contains(command))
                    {
                        commands.Add(command);
                        logger.LogInformation($"Adding command {command.Name}");
                    }
                }
            }
             
            logger.LogInformation($"Initialization of {this.GetType().Name} complete");

        }

        public async Task HandleCommandAsync(DiscordSocketClient discordSocketClient, SocketUserMessage message)
        {
            try
            {
                var userMessage = message as SocketUserMessage;
                if (userMessage == null || message.Author.IsBot) return;
             
                var argPos = GetCommandArgPos(discordSocketClient.CurrentUser, message);

                if (argPos < 1)
                {
                    return;
                }

                var commandInfo = GetCommandInfoFromMessage(message);
                if (commandInfo == null)
                {
                    return;
                }

                var context = new SocketCommandContext(discordSocketClient, message);

                logger.LogInformation($"{discordSocketClient?.GetType()?.Name} Bot command received by user {message?.Author?.Username} in channel {message?.Channel?.Name}. Processing message \n\"{message?.Content}\"");
                await commandService.ExecuteAsync(
                context: context,
                argPos: argPos,
                services: serviceProvider);
            }
            catch (Exception e)
            {
                logger.LogError($"Unable to proccess command message {message?.ToString()}\n {e}");
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

            if (!userMessage.HasCharPrefix(Prefix, ref argPos) ||
                 userMessage.HasMentionPrefix(selfUser, ref argPos))
            {
                return 0;
            }

            return argPos;
        }

        private Task CommandService_Log(LogMessage arg)
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
        }
    }
}