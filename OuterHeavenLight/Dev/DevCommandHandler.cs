using Discord;
using Discord.Commands;
using Discord.WebSocket;
using OuterHeavenLight.Music;
using System.Net.Sockets;

namespace OuterHeavenLight.Dev
{
    public class DevCommandHandler : CommandHandlerBase<MusicDiscordClient>
    {  
        private readonly ILogger<DevCommandHandler> logger;

        public DevCommandHandler(CommandService commandService,
                                         IServiceProvider serviceProvider,
                                         ILogger<DevCommandHandler> logger) : base(logger, commandService, serviceProvider)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(this.logger));
            this.Prefix = "!";
        }

        public override bool ShouldExecuteCommand(MusicDiscordClient discordSocketClient, SocketMessage message)
        {  
            if(message?.Channel is SocketDMChannel)
            {
                return base.ShouldExecuteCommand(discordSocketClient, message);
            }
                
            return false; 
        }  
    }
}
