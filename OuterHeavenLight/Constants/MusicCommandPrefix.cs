using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenLight.Constants
{
    public static class MusicCommandPrefix
    {
        public const string Local = "l";
        public const string Youtube = "yt";
        public const string SoundCloud = "sc";
        public const string Spotify = "sp";

        public static bool IsValidPrefix(string prefix)
        {
            return prefix == Local || prefix == Youtube || prefix == SoundCloud || prefix == Spotify;
        }
    }
}
