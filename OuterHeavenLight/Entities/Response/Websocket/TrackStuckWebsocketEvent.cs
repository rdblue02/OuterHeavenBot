using System.Text.Json.Serialization;


namespace OuterHeavenLight.Entities.Response.Websocket
{
    public class TrackStuckWebsocketEvent : LavaWebsocketEvent
    {
        [JsonPropertyName("thresholdMs")]
        public long ThresholdMs { get; set; }
    }
}
