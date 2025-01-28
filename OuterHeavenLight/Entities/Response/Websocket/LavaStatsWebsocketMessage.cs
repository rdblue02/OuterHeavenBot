using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OuterHeavenLight.Entities.Response.Websocket
{
    public class LavaStatsWebsocketMessage
    {   
        [JsonPropertyName("op")] 
        public string op { get; set; }

        [JsonPropertyName("players")]
        public int players { get; set; }

        [JsonPropertyName("playingPlayers")]
        public int playingPlayers { get; set; }

        [JsonPropertyName("uptime")]
        public int uptime { get; set; }

        [JsonPropertyName("memory")]
        public Memory memory { get; set; }

        [JsonPropertyName("cpu")]
        public Cpu cpu { get; set; }

        [JsonPropertyName("frameStats")]
        public Framestats frameStats { get; set; }


        public class Memory
        {
            [JsonPropertyName("free")]
            public ulong free { get; set; }

            [JsonPropertyName("used")]
            public ulong used { get; set; }

            [JsonPropertyName("allocated")]
            public ulong allocated { get; set; }

            [JsonPropertyName("reservable")]
            public ulong reservable { get; set; }
        }

        public class Cpu
        {
            [JsonPropertyName("cores")]
            public int cores { get; set; }

            [JsonPropertyName("systemLoad")]
            public float systemLoad { get; set; }

            [JsonPropertyName("lavalinkLoad")]
            public float lavalinkLoad { get; set; }
        }

        public class Framestats
        {
            [JsonPropertyName("sent")]
            public ulong sent { get; set; }

            [JsonPropertyName("nulled")]
            public ulong nulled { get; set; }

            [JsonPropertyName("deficit")]
            public int deficit { get; set; }
        }
    }
   
}
