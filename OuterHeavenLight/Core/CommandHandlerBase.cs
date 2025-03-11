using Discord;
using Discord.Commands;
using Discord.WebSocket;
using OuterHeavenLight.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenLight.Core
{
    public abstract class CommandHandlerBase<TDiscordClient> where TDiscordClient : DiscordSocketClient
    {
        public string Prefix { get; protected set; } = "~";
        private ILogger logger;
        private readonly CommandService commandService;
        private readonly IServiceProvider serviceProvider;
        private readonly List<CommandInfo> commands;
        private Type clientType;
        public CommandHandlerBase(ILogger logger,
                                  CommandService commandService,
                                  IServiceProvider serviceProvider)
        {
            this.logger = logger;
            this.commandService = commandService;
            this.serviceProvider = serviceProvider;
            commands = [];
            commandService.Log += logger.LogMessage;
        }

        public async Task InstallCommandsAsync(List<Type> types)
        {
            foreach (var type in types)
            {
                var module = await commandService.AddModuleAsync(type, serviceProvider);
                foreach (var command in module.Commands)
                {
                    if (!commands.Contains(command))
                    {
                        logger.LogInformation($"Adding command {command.Name} for {type.Name}");
                        commands.Add(command);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Command {command.Name} has already been registered");
                    }
                }
            }
        }

        public virtual bool ShouldExecuteCommand(TDiscordClient discordSocketClient, SocketMessage message)
        {
            if (string.IsNullOrWhiteSpace(message?.Content) ||
               !message.Content.StartsWith(Prefix) ||
                message.Author.IsBot &&
                discordSocketClient.GetType() == typeof(TDiscordClient))
                return false;

            var commandInfo = GetCommandInfoFromMessage(message.CleanContent);
            if (commandInfo == null)
            {
                logger.LogError($"No command found for  {message.CleanContent}");
                return false;
            }

            return true;
        }

        public async Task HandleCommandAsync(TDiscordClient discordSocketClient, SocketMessage message)
        {
            try
            {
                var context = new SocketCommandContext(discordSocketClient, message as SocketUserMessage);

                logger.LogInformation($"{discordSocketClient?.GetType()?.Name} Bot command received by user {message?.Author?.Username} in channel {message?.Channel?.Name}. Processing message \n\"{message?.Content}\"");
                await commandService.ExecuteAsync(
                context: context,
                argPos: 1,
                services: serviceProvider);
            }
            catch (Exception e)
            {
                logger.LogError($"Unable to proccess command message {message?.ToString()}\n {e}");
            }
        }

        private CommandInfo? GetCommandInfoFromMessage(string messageContent)
        {
            if (string.IsNullOrWhiteSpace(messageContent)) return null;
            var endOfCommand = messageContent.IndexOf(' ');

            var content = messageContent.Substring(0, endOfCommand > 0 ? endOfCommand : messageContent.Length).Replace(Prefix, "").Trim();
            var info = commands.FirstOrDefault(x => x.Name.ToLower() == content.ToLower() || x.Aliases.Any(x => x.ToLower() == content.ToLower()));
            return info;
        }
    }
}