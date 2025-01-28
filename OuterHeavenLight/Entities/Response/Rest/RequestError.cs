using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OuterHeavenLight.Entities.Response.Rest
{  
    public class RequestError
    {
        [JsonPropertyName("timestamp")]
        public long timestamp { get; set; }
         
        [JsonPropertyName("status")]
        public int status { get; set; }

        [JsonPropertyName("error")]
        public string error { get; set; }

        [JsonPropertyName("trace")]
        public string trace { get; set; }

        [JsonPropertyName("message")]
        public string message { get; set; }

        [JsonPropertyName("path")]
        public string path { get; set; }
    }
}
