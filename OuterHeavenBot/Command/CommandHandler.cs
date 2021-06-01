using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenBot.Command
{
    
    public class CommandHandler
    {
        private readonly DiscordSocketClient client;
        private readonly CommandService commands;
        private readonly IServiceProvider serviceProvider;
        private readonly ICommandOptions commandOptions;
        public CommandHandler(DiscordSocketClient client, 
                             CommandService commands, 
                             IServiceProvider serviceProvider,
                             ICommandOptions options)
        { 
            this.commands = commands;
            this.client = client;
            this.serviceProvider = serviceProvider;
            this.commandOptions = options ?? new DefaultCommandOptions();
           
        }
        public async Task InstallCommandsAsync()
        {
            // Hook the MessageReceived event into our command handler
            client.MessageReceived += HandleCommandAsync;
         
            await commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(),
                                            services: serviceProvider);
        }
        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            // Don't process the command if it was a system message
            var message = messageParam as SocketUserMessage;
            if (message == null) return;

            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;

            // Determine if the message is a command based on the prefix and make sure no bots trigger commands
            if (!(message.HasCharPrefix(commandOptions.Prefix, ref argPos) ||
                message.HasMentionPrefix(client.CurrentUser, ref argPos)) ||
                message.Author.IsBot || commandOptions.RequirementsToExecute.Any(x=>!x.Invoke(message)))
                return;

            // Create a WebSocket-based command context based on the message
            var context = new SocketCommandContext(client, message);

            // Execute the command with the command context we just
            // created, along with the service provider for precondition checks.
            try
            {
               await commands.ExecuteAsync(
               context: context,
               argPos: argPos,
               services: serviceProvider);
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }           
    }
}
