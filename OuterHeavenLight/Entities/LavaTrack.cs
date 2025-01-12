using OuterHeavenLight.Entities.Request;
using System.Text.Json.Serialization;

namespace OuterHeavenLight.Entities
{
    public class LavaTrack
    {
        [JsonPropertyName("encoded")]
        public string encoded { get; set; }

        [JsonPropertyName("info")]
        public LavaTrackInfo info { get; set; }

        [JsonPropertyName("pluginInfo")]
        public Plugininfo pluginInfo { get; set; }

        [JsonPropertyName("userData")]
        public Userdata? UserData { get; set; } 
    }
}