using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenBot.Client.Commands.Handlers
{

    public class CommandHandler
    {
        ILogger<CommandHandler> logger;
        CommandService commandService;
        IServiceProvider serviceProvider;
        public CommandHandler(ILogger<CommandHandler> logger,
                              CommandService commandService,
                              IServiceProvider serviceProvider)
        {
            this.logger = logger;
            this.commandService = commandService;
            this.serviceProvider = serviceProvider;
        }

        public async Task HandleMessage(DiscordSocketClient client, SocketMessage arg)
        {
            try
            {

                var userMessage = arg as SocketUserMessage;
                if (userMessage == null || arg.Author.IsBot) return;

                var argPos = 0;

                if (!(userMessage.HasCharPrefix('~', ref argPos) ||
                    userMessage.HasMentionPrefix(client.CurrentUser, ref argPos)))


                    if (argPos < 1)
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
