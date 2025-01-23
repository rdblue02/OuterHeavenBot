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
        public int LavalinkProcessId { get; set; } 
        public string LavaSessionId { get; set; } = "";  
        public ulong LastChannel { get; set; } = 0;
        private string cacheLocation => Path.Combine(Directory.GetCurrentDirectory(), $"\\{nameof(LavaFileCache)}.json");

        public void Save()
        {
            var cache = JsonSerializer.Serialize(this);
            File.WriteAllText(cacheLocation, cache);
        }

        public void Read()
        {
            var data = File.ReadAllTextAsync(cacheLocation).GetAwaiter().GetResult() ?? throw new ArgumentNullException(nameof(LavaFileCache));
            var result= JsonSerializer.Deserialize<LavaFileCache>(data) ?? throw new ArgumentNullException(nameof(LavaFileCache));           
        }
    }
}
