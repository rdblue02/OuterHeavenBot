using System.Text.Json.Serialization;

namespace OuterHeavenLight.Entities
{
    public class LavaTrackInfo
    {
        [JsonPropertyName("identifier")]
        public string identifier { get; set; }

        [JsonPropertyName("isSeekable")]
        public bool isSeekable { get; set; }

        [JsonPropertyName("author")]
        public string author { get; set; }

        [JsonPropertyName("length")]
        public int length { get; set; }

        [JsonPropertyName("isStream")]
        public bool isStream { get; set; }

        [JsonPropertyName("position")]
        public int position { get; set; }

        [JsonPropertyName("title")]
        public string title { get; set; }

        [JsonPropertyName("uri")]
        public string uri { get; set; }

        [JsonPropertyName("artworkUrl")]
        public string artworkUrl { get; set; }

        [JsonPropertyName("isrc")]
        public object isrc { get; set; }

        [JsonPropertyName("sourceName")]
        public string sourceName { get; set; }
    }

}
