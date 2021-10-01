using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenBot
{
    public enum FileExtensions
    {
        txt,
        xml,
        mp3,
        ogg,
        binary
    }

    public enum AudioActionResult
    {
        ChannelJoined,
        Playing,
        Queing,
        InAnotherChannel,
        NotConnected,
        Error
    }
}
