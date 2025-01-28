using System.Text.Json.Serialization;

namespace OuterHeavenLight.Entities.Response.Websocket
{
    public class ClosedWebsocketEvent:LavaWebsocketEvent
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("reason")]
        public string Reason { get; set; }

        [JsonPropertyName("byRemote")]
        public bool ByRemote { get; set; }
    }  
}
