using Discord.WebSocket;
using OuterHeaven.LavalinkLight;
using OuterHeavenLight.Clippies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenLight
{
    public class DiscordClientProvider
    {
        public const string ClippieClientName = "CLIP";
        public const string MusicClientName = "MUSIC"; 
 
        private Dictionary<string, DiscordSocketClient> clients = new Dictionary<string, DiscordSocketClient>()
        {
            {ClippieClientName, null },
            {MusicClientName, null},
        };

        public DiscordClientProvider(ILogger<ClippieDiscordClient> clipLogger,DiscordSocketConfig config, AppSettings appSettings)
        {
            clients[ClippieClientName] = new ClippieDiscordClient(clipLogger,appSettings, config);
            clients[MusicClientName] = new DiscordSocketClient(config);
        }
        
        public ClippieDiscordClient? GetClipClient()
        {
            return clients[ClippieClientName] as ClippieDiscordClient;
        }
        public DiscordSocketClient? GetMusicClient()
        {
            return clients[MusicClientName];
        }
    }
}
