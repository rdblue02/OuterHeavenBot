using Discord.Commands;
using Discord.WebSocket;
using OuterHeavenLight.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenLight.Core
{
    public class CommandHandler
    {
        List<CommandInfo> commands = new List<CommandInfo>();
        CommandService commandService;
        IServiceProvider serviceProvider;
        ILogger logger;
        List<char> eligiblePrefixes = new List<char>() { '~','!'};

        public CommandHandler(ILogger<CommandHandler> logger, 
                              CommandService commandService,
                              IServiceProvider serviceProvider) 
        {
            this.logger = logger;
            this.commandService = commandService;
            this.serviceProvider = serviceProvider;
        }

        public async Task Initialize(List<Type> moduleTypes)  
        { 
            foreach (var type in moduleTypes)
            {
                var module = await commandService.AddModuleAsync(type, serviceProvider);
                foreach (var command in module.Commands)
                {
                    commands.Add(command);
                    if(command.Module.Name != CommandGroupName.Dev)
                    { 
                        logger.LogInformation($"Registering command {command.Name}");
                    }
                }
            }         
        }

        public async Task HandleCommandAsync(string group, DiscordSocketClient client, SocketMessage message)  
        {
            try
            { 
                var content = message.Content; 
                var prefix = message?.Content?.FirstOrDefault();

                //ignore invalid prefix.
                if(!prefix.HasValue || 
                   !this.eligiblePrefixes.Contains(prefix.Value))
                {
                    return;
                }
                
                var matchingCommands = GetCommandInfoFromMessage(message!.Content, prefix.Value.ToString());
                var command = matchingCommands.FirstOrDefault(x => x.Module.Name == group);

                if (command != null)
                {

                    logger.LogInformation($"{client?.GetType()?.Name} Bot command received by user {message?.Author?.Username} in channel {message?.Channel?.Name}. Processing message \n\"{message?.Content}\"");
                    var context = new SocketCommandContext(client, message as SocketUserMessage);

                    await commandService.ExecuteAsync(
                    context: context,
                    argPos: 1,
                    services: serviceProvider); 
                    return;
                } 

                if (!matchingCommands.Any())
                {
                    logger.LogError($"Invalid command {content} received by user {message.Author.Username}");
                }
            } 
            catch (Exception e)
            {
                logger.LogError($"Unable to proccess command message {message?.ToString()}\n {e}");
            }
        }

        private List<CommandInfo> GetCommandInfoFromMessage(string messageContent, string prefix)
        {
            if (string.IsNullOrWhiteSpace(messageContent)) return [];

            var endOfCommand = messageContent.IndexOf(' ');

            var content = messageContent.Substring(0, endOfCommand > 0 ? endOfCommand : messageContent.Length).Replace(prefix, "").Trim();

            return this.commands.Where(x => x.Name.ToLower() == content.ToLower() || 
                                            x.Aliases.Any(x => x.ToLower() == content.ToLower()))
                                .ToList();  
        }
    }
}
