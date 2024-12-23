using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenBot.Core
{
    public class DiscordClientProvider
    {
        public const string MusicClientName = "MusicClient";
        public const string ClippieClientName = "ClippieClient";

        private ILogger<DiscordClientProvider> logger;
        private Dictionary<string, DiscordSocketClient> clients = [];

        public DiscordClientProvider(ILogger<DiscordClientProvider> logger,  
                                     DiscordSocketConfig discordConfig) 
        {
            this.logger = logger;
            clients = new Dictionary<string, DiscordSocketClient>()
            {
                {MusicClientName, new DiscordSocketClient(discordConfig)},
                {ClippieClientName,new DiscordSocketClient(discordConfig)}
            }; 
        }

        public DiscordSocketClient GetClient(string name) 
        {
            if(clients.TryGetValue(name, out DiscordSocketClient? value)) 
                return value ?? throw new ArgumentNullException(nameof(DiscordSocketClient));

            throw new Exception($"Invalid client name {name}");
        }
    }
}
