using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OuterHeavenLight.Entities
{
    public class PlaylistInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("selectedTrack")]
        public int SelectedTrackIndex { get; set; }
    }
}
