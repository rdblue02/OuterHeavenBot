using System.Text.Json.Serialization;


namespace OuterHeavenLight.Entities.Response.Websocket
{
    public partial class TrackExceptionWebsocketEvent : LavaWebsocketEvent
    {
        [JsonPropertyName("exception")]
        public LavaTrackException Exception { get; set; }
    }
}
