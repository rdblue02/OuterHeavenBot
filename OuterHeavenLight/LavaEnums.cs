using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeaven.LavalinkLight
{
    public enum LavalinkSearchType
    {

        ytsearch,
        scsearch,
        Raw
    }

    public enum LavalinkLoadType
    {
        track,
        playlist,
        empty,
        error,
        search
    }

    public enum LavalinkTrackEndReason
    {
        invalid,
        finished,
        loadFailed,
        stopped,
        replaced,
        cleanup
    }

    public enum LavalinkWebsocketEventType
    {
        TrackStartEvent,
        TrackEndEvent,
        TrackExceptionEvent,
        TrackStuckEvent,
        WebSocketClosedEvent,
        Invalid
    }

 
}
