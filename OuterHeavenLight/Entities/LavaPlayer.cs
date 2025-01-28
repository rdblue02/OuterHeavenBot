using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OuterHeavenLight.Entities
{
    public class LavaPlayer
    {
        [JsonPropertyName("guildId")]
        public string guildId { get; set; }

        [JsonPropertyName("track")]
        public LavaTrack track { get; set; }

        [JsonPropertyName("volume")]
        public int volume { get; set; }

        [JsonPropertyName("paused")]
        public bool paused { get; set; }

        [JsonPropertyName("state")]
        public PlayerState state { get; set; }

        [JsonPropertyName("voice")]
        public VoiceState voice { get; set; }

        [JsonPropertyName("filters")]
        public Filters filters { get; set; }
    } 
}
