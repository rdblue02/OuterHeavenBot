using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OuterHeavenLight.Entities.Response.Websocket
{
    public partial class PlayerUpdateWebsocketMessage
    {
        [JsonPropertyName("op")]
        public string op { get; set; }

        [JsonPropertyName("guildId")]
        public string guildId { get; set; }

        [JsonPropertyName("state")]
        public PlayerState state { get; set; }
    }
  
}
