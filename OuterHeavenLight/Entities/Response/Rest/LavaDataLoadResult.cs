using OuterHeaven.LavalinkLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenLight.Entities.Response.Rest
{
    public class LavaDataLoadResult
    {
        public LavalinkLoadType LoadType { get; set; }
        public List<LavaTrack> LoadedTracks { get; set; } = new List<LavaTrack>();
        public PlaylistInfo PlaylistInfo { get; set; } = new PlaylistInfo();
        public LavaTrackException? TrackException { get; set; }
    }
}
