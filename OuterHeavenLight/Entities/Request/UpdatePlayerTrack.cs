using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OuterHeavenLight.Entities.Request
{
    public class UpdatePlayerTrack
    { 
        [JsonPropertyName("encoded")]
        [DefaultValue("")]
        public string? encoded { get; set; }
        
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("identifier")]
        public string? identifier { get; set; }
      
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] 
        [JsonPropertyName("userData")]
        public Userdata? userData { get; set; }
    }
}
