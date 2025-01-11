using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OuterHeavenLight.Entities.Response.Websocket
{ 
    public class ReadyOPWebsocketMessage
    {
        [JsonPropertyName("op")]
        public string op { get; set; }

        [JsonPropertyName("resumed")]
        public bool resumed { get; set; }
        
        [JsonPropertyName("sessionId")]
        public string sessionId { get; set; }
    } 
}
