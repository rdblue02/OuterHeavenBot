using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenBot.Lavalink.Entities
{
    public class LavalinkNodeReadyPayload
    {
        /// <summary>
        /// The session id of the node
        /// </summary>
        [JsonProperty("sessionId")] public string SessionId { get; set; }

        /// <summary>
        /// Whether the node has resumed
        /// </summary>
        [JsonProperty("resumed")] public bool Resumed { get; set; }
    }
}
