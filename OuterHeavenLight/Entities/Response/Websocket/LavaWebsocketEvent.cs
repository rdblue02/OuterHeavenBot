using System.Text.Json.Serialization;


namespace OuterHeavenLight.Entities.Response.Websocket
{ 
    public class LavaWebsocketEvent
    {
        [JsonPropertyName("op")]
        public string OP { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("guildId")]
        public string GuildId { get; set; }

        [JsonPropertyName("track")]
        public LavaTrack Track { get; set; }

        [JsonIgnore]
        public ulong? IssuedCommandChannelId { get; set; }
    } 
}
