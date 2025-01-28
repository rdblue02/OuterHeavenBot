using System.Text.Json.Serialization;

namespace OuterHeavenLight.Entities
{
    public class PlayerState
    {
        [JsonPropertyName("time")]
        public long time { get; set; }

        [JsonPropertyName("position")]
        public long position { get; set; }

        [JsonPropertyName("connected")]
        public bool connected { get; set; }

        [JsonPropertyName("ping")]
        public long ping { get; set; }
    }
}
