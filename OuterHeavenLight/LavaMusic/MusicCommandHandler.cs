
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using OuterHeavenLight.Clippies;
using OuterHeavenLight.Constants;
using OuterHeavenLight.Core;
using OuterHeavenLight.Extensions;

namespace OuterHeavenLight.Music
{
    public class MusicCommandHandler : CommandHandlerBase<MusicDiscordClient>
    { 
        private readonly ILogger<MusicCommandHandler> logger;  
        public MusicCommandHandler(CommandService commandService,
                                         IServiceProvider serviceProvider,
                                         ILogger<MusicCommandHandler> logger) : base(logger, commandService, serviceProvider) 
        { 
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger)); 
        }  
    }
}