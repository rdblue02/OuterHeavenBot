using OuterHeavenLight.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OuterHeavenLight.LavaConnection
{
    public class LavaFileCache
    {
        public int LavalinkProcessId { get; set; } = 0;
        public string LavalinkSessionId { get; set; } = string.Empty;
        public string GuildId { get; set; } = string.Empty;
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

        public static LavaFileCache Read()
        {
            LavaFileCache? cache = null;
            var data = File.Exists(cacheLocation) ? File.ReadAllTextAsync(cacheLocation).GetAwaiter().GetResult() : null;
            
            if(!string.IsNullOrWhiteSpace(data))
            {
                cache = JsonSerializer.Deserialize<LavaFileCache>(data) ?? throw new ArgumentNullException(nameof(LavaFileCache));
            }
           
            if(cache == null)
            {
                cache = new LavaFileCache();
                cache.Save();
            } 

            return cache;
        }
    }
}
