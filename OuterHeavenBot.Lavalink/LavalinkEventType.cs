using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenBot.Lavalink
{
    public enum LavalinkEventType
    {
        /// <summary>
        /// Track started
        /// </summary>
        [EnumMember(Value = "TrackStartEvent")]
        TrackStartEvent,

        /// <summary>
        /// Track ended
        /// </summary>
        [EnumMember(Value = "TrackEndEvent")]
        TrackEndEvent,

        /// <summary>
        /// Track exception
        /// </summary>
        [EnumMember(Value = "TrackExceptionEvent")]
        TrackExceptionEvent,

        /// <summary>
        /// Track stuck
        /// </summary>
        [EnumMember(Value = "TrackStuckEvent")]
        TrackStuckEvent,

        /// <summary>
        /// Websocket closed
        /// </summary>
        [EnumMember(Value = "WebSocketClosedEvent")]
        WebSocketClosedEvent,
    }
}
