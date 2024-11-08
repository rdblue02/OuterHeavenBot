using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenBot.Lavalink.EventArgs
{
    public class PlaybackExceptionEventArgs  
    {
        /// <summary>
        /// The track that caused the exception
        /// </summary>
        public LavalinkTrack Track { get; }

        /// <summary>
        /// The exception that was thrown
        /// </summary>
        public LavalinkException Exception { get; }

        public PlaybackExceptionEventArgs(LavalinkTrack track, LavalinkException exception)
        {
            Track = track;
            Exception = exception;
        }
    }
    public class PlaybackFinishedEventArgs  
    {
        /// <summary>
        /// The track that finished playing
        /// </summary>
        public LavalinkTrack Track { get; }

        /// <summary>
        /// The reason the track ended
        /// </summary>
        public LavalinkTrackEndReason EndReason { get; }

        public PlaybackFinishedEventArgs(LavalinkTrack track, LavalinkTrackEndReason endReason)
        {
            Track = track;
            EndReason = endReason;
        }
    }

    public class PlaybackStartedEventArgs  
    {
        /// <summary>
        /// The track that started playing
        /// </summary>
        public LavalinkTrack Track { get; }

        public PlaybackStartedEventArgs(LavalinkTrack track)
        {
            Track = track;
        }
    }

    public class PlaybackStuckEventArgs  
    {
        /// <summary>
        /// The track that got stuck
        /// </summary>
        public LavalinkTrack Track { get; }

        /// <summary>
        /// The threshold in milliseconds
        /// </summary>
        public int ThresholdMs { get; }

        public PlaybackStuckEventArgs(LavalinkTrack track, int thresholdMs)
        {
            Track = track;
            ThresholdMs = thresholdMs;
        }
    }

    public class PlayerInternalError  
    {
        /// <summary>
        /// Exception that was thrown
        /// </summary>
        public Exception Exception { get; }

        public PlayerInternalError(Exception exception)
        {
            Exception = exception;
        }
    }

    public class PlayerUpdateEventArgs  
    {
        /// <summary>
        /// The player state
        /// </summary>
        public object State { get; }

        public PlayerUpdateEventArgs(LavalinkTrack state)
        {
            State = state;
        }
    }
    public class PlayerWebsocketClosedEventArgs  
    {
        /// <summary>
        /// The close code
        /// </summary>
        public int Code { get; }

        /// <summary>
        /// The close reason
        /// </summary>
        public string Reason { get; }
        /// <summary>
        /// Whether the connection was closed by DiscordClient
        /// </summary>
        public bool ByRemote { get; }

        public PlayerWebsocketClosedEventArgs(int code, string reason, bool byRemote)
        {
            Code = code;
            Reason = reason;
            ByRemote = byRemote;
        }
    }
}
