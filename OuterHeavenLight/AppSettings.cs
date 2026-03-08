using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OuterHeaven.LavalinkLight
{
    public class AppSettings
    {
        [JsonPropertyName("AppLogDirectoryPath")]
        public string? AppLogDirectoryPath { get; set; }
     
        [JsonPropertyName("OuterHeavenBotSettings")]
        public OuterHeavenBotSettings? OuterHeavenBotSettings { get; set; }
       
        [JsonPropertyName("ClippieBotSettings")]
        public ClippieBotSettings? ClippieBotSettings { get; set; }
    }

    public class OuterHeavenBotSettings : BotSettings
    {
        [JsonPropertyName("PlayLocalDirectory")]
        public string PlayLocalDirectory { get; set; } = "";

        [JsonPropertyName("LavalinkDirectory")]
        public string LavalinkDirectory { get; set; } = "";
    }

    public class ClippieBotSettings : BotSettings
    {
        [JsonPropertyName("SoundFileDirectory")]
        public string SoundFileDirectory { get; set; } = "";
       
        [JsonPropertyName("ExcludedSubDirectories")]
        public string[] ExcludedSubDirectories { get; set; } = [];

        [JsonIgnore]
        public string DefaultSoundFileDirectory => Path.Combine(Directory.GetCurrentDirectory(), SoundFileDirectory);
    }

    public class BotSettings
    {
        [JsonPropertyName("Enabled")]
        public bool Enabled { get; set; }
        [JsonPropertyName("DiscordToken")]
        public string DiscordToken { get; set; } = "";
    }
} 