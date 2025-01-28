using OuterHeaven.LavalinkLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OuterHeavenLight.Entities.Response.Rest
{
    public class LoadError
    {
        [JsonPropertyName("loadType")]
        public string LoadType { get; set; }

        [JsonPropertyName("data")]
        public LavaTrackException Exception { get; set; }
    }
}
