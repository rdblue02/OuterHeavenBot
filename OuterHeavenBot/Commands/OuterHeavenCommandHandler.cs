using Discord;
using Discord.Commands;
using Discord.WebSocket;
using OuterHeavenBot.Commands.Modules;
using OuterHeavenBot.Clients;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenBot.Commands
{
    public class OuterHeavenCommandHandler:CommandHandlerBase<OuterHeavenCommandHandler>
    {
       
        public OuterHeavenCommandHandler(CommandService commandService,
                              IServiceProvider serviceProvider,
                              ILogger<OuterHeavenCommandHandler> logger):base(commandService, serviceProvider, logger)
        {
        
        }
         
        public async Task ApplyCommands() => await this.InstallCommandsAsync(new List<Type>() { typeof(MusicCommands),typeof(GeneralCommands) });
    }
}
