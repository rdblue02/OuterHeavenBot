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
    public class ClippieCommandHandler : CommandHandlerBase<ClippieDiscordClient>
    {  
        private readonly ILogger<ClippieCommandHandler> logger; 
        public ClippieCommandHandler(CommandService commandService,
                                     IServiceProvider serviceProvider,
                                     ILogger<ClippieCommandHandler> logger):base(logger,commandService,serviceProvider)
        {
            
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));  
        }
        public override bool ShouldExecuteCommand(ClippieDiscordClient discordSocketClient, SocketMessage message)
        {
            return base.ShouldExecuteCommand(discordSocketClient, message);
        }
    }
}
