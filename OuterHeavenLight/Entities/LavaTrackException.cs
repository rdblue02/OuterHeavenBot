using System.Text.Json.Serialization;


namespace OuterHeavenLight.Entities
{
    public class LavaTrackException
    {
        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("severity")]
        public string Severity { get; set; }

        [JsonPropertyName("cause")]
        public string Cause { get; set; }
    }
}
