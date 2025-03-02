using OuterHeavenLight.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OuterHeavenLight.LavaConnection
{
    public class LavaFileCache
    { 
        [JsonPropertyName("lava_process_id")]
        public int LavalinkProcessId { get; set; } = 0;
       
        [JsonPropertyName("lava_session_id")]
        public string LavalinkSessionId { get; set; } = string.Empty;
     
        [JsonPropertyName("discord_server_token")]
        public string DiscroderServerToken { get; set; } = string.Empty;

        [JsonPropertyName("discord_server_endpoint")]
        public string DiscordServerEndpoint { get; set; } = string.Empty;
         
        [JsonPropertyName("current_guild_id")]
        public string GuildId { get; set; } = string.Empty;
       
        [JsonPropertyName("current_channel_id")]
        public string ChannelId { get; set; } = string.Empty;

        private static string cacheLocation => Path.Combine(Directory.GetCurrentDirectory(), $"{nameof(LavaFileCache)}.json");
    
        private JsonSerializerOptions options = new()
        {
            WriteIndented = true
        };

        public void Save()
        { 
            var cache = JsonSerializer.Serialize(this, options);
            File.WriteAllText(cacheLocation, cache);
        }
         
        public void Load()
        {
            if (File.Exists(cacheLocation))
            {
                var data = File.Exists(cacheLocation) ? File.ReadAllText(cacheLocation) : "";
                var cache = JsonSerializer.Deserialize<LavaFileCache>(data) ?? new LavaFileCache();
                Set(cache);
            }
            else
            {
                Save();
            }           
        }  

        private void Set(LavaFileCache? fileCache)
        {
            this.LavalinkSessionId = fileCache?.LavalinkSessionId ?? "";
            this.LavalinkProcessId = fileCache?.LavalinkProcessId ?? default;
            this.GuildId = fileCache?.GuildId ?? "";
            this.ChannelId = fileCache?.ChannelId ?? ""; 
        }
    }
}
