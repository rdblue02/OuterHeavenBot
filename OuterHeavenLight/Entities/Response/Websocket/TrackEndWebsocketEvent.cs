using OuterHeaven.LavalinkLight;
using System.Text.Json.Serialization;


namespace OuterHeavenLight.Entities.Response.Websocket
{
    public class TrackEndWebsocketEvent : LavaWebsocketEvent
    {
      
        public LavalinkTrackEndReason Reason => Enum.TryParse<LavalinkTrackEndReason>(ReasonRaw, out var reason) ? reason : LavalinkTrackEndReason.invalid;

        [JsonPropertyName("reason")]
        public string ReasonRaw { get; set; }
    }
}
