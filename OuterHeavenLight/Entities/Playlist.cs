using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OuterHeavenLight.Entities
{
    public class Playlist
    {
        [JsonPropertyName("info")]
        public PlaylistInfo Info { get; set; }

        [JsonPropertyName("pluginInfo")]
        public Plugininfo PluginInfo { get; set; }

        [JsonPropertyName("tracks")]
        public List<LavaTrack> Tracks { get; set; }
    }
}
