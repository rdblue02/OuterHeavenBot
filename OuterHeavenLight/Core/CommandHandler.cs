using Discord;
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
        List<CommandInfo> commands = [];
        CommandService commandService;
        IServiceProvider serviceProvider;
        ILogger logger;

        Dictionary<char, List<string>> eligiblePrefixes = new() {
            { '~', [CommandGroupName.Music, CommandGroupName.Clippies] },
            { '!', [CommandGroupName.Dev] }
        };

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
                commands.AddRange(module.Commands);
            }

            logger.LogInformation($"Registering commands {string.Join(Environment.NewLine, commands.Where(x => x.Module.Group != CommandGroupName.Dev).Select(x => x.Name))}");
        }

        public async Task HandleCommandAsync(string group, DiscordSocketClient client, SocketMessage message)
        {
            try
            {
                //ignore invalid prefix.
                if (string.IsNullOrWhiteSpace(message?.Content) ||
                    !this.eligiblePrefixes.ContainsKey(message.Content[0]) ||
                    message.Author.IsBot)
                    return;

                var command = GetCommandInfoFromMessage(message, group);

                if (command == null)
                {
                    //make sure this command is not intended for another group before logging as error
                    var otherGroups = eligiblePrefixes.Values.SelectMany(x => x).Where(x=> x != group).Any(x=>GetCommandInfoFromMessage(message,x) != null);
                    if (!otherGroups)
                    {
                        logger.LogError($"Invalid command {message.Content} received by user {message.Author.Username}");
                    }

                    return;
                }

                logger.LogInformation($"{client?.GetType()?.Name} Bot command received by user {message?.Author?.Username} in channel {message?.Channel?.Name}. Processing message \n\"{message?.Content}\"");
              
                var context = new SocketCommandContext(client, message as SocketUserMessage);
                var commandResult = await commandService.ExecuteAsync(
                context: context,
                argPos: 1,
                services: serviceProvider);

            }
            catch (Exception e)
            {
                logger.LogError($"Unable to proccess command message {message?.ToString()}\n {e}");
            }
        }

        private CommandInfo? GetCommandInfoFromMessage(SocketMessage message, string group)
        {
            var endOfCommand = message.Content.IndexOf(' ');
            var endOfCommandIndex = endOfCommand > 0 ? endOfCommand : message.Content.Length;

            var commandNameOrAlias = message.Content[..(endOfCommandIndex)]
                                            .Replace(message.Content[0], '\n')//remove the prefix
                                            .Trim();

            var commands = this.commands.Where(x => string.Equals(x.Name, commandNameOrAlias, StringComparison.OrdinalIgnoreCase) ||
                                        x.Aliases.Any(x => string.Equals(x, commandNameOrAlias, StringComparison.OrdinalIgnoreCase)));

            return commands.FirstOrDefault(x => x.Module.Name == group);
        }
    }
}
