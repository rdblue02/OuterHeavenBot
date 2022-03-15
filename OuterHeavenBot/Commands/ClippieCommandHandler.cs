using Discord;
using Discord.Commands;
using Discord.WebSocket;
using OuterHeavenBot.Commands.Modules;
using OuterHeavenBot.Clients;
using OuterHeavenBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenBot.Commands
{

    public class ClippieCommandHandler:CommandHandlerBase<ClippieCommandHandler>
    {
        public ClippieCommandHandler(CommandService commandService,
                              IServiceProvider serviceProvider,                           
                              ILogger<ClippieCommandHandler> logger):base(commandService,serviceProvider,logger)
        {
          
        }

        public async Task ApplyCommands()=> await this.InstallCommandsAsync(new List<Type>() { (typeof(ClippieCommands)) });
    }
}
