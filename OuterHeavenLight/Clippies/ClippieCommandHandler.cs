using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OuterHeavenLight.Clippies;
using OuterHeavenLight.Constants;
namespace OuterHeavenLight.Clippies
{
    public class ClippieCommandHandler 
    {
        public bool IsInitialized { get; private set; } = false;
        protected readonly CommandService commandService;
     
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger logger;
        private readonly List<CommandInfo> commands;
        //todo this should be a setting
        private const char Prefix = '~';
        

        public ClippieCommandHandler(CommandService commandService,
                                         IServiceProvider serviceProvider,
                                         ILogger<ClippieCommandHandler> logger)
        {
            this.commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.commands = new List<CommandInfo>();
            commandService.Log += CommandService_Log;
 
        }

        public async Task InstallCommandsAsync(List<Type> types)
        {
            foreach(var type in types)
            {
                var module = await commandService.AddModuleAsync(type, serviceProvider);
                
                this.commands.AddRange(module.Commands);
            }
            foreach (var command in this.commands)
            {
                logger.LogInformation($"Adding command {command.Name}");
            }

            logger.LogInformation($"Initialization of {this.GetType().Name} complete");
          
            this.IsInitialized = true;
        }
         
        public async Task HandleCommandAsync(DiscordSocketClient discordSocketClient, SocketUserMessage message)
        {
            try
            {
                if (!IsInitialized)
                {
                    throw new InvalidOperationException("Cannot accept command before being initialized");
                }

                var userMessage = message as SocketUserMessage;
                if (userMessage == null || message.Author.IsBot) return;

                var argPos = GetCommandArgPos(discordSocketClient.CurrentUser, message);

                if (argPos < 1) 
                {
                    logger.LogError($"Invalid message {message?.Content} sent by {message?.Author?.Username}");
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

            var content = message.CleanContent.Substring(0,endOfCommand>0? endOfCommand: message.CleanContent.Length).Replace(Prefix, '\0').Trim();
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
