using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenBot.Lavalink.Entities
{
    public class LavalinkPlayerUpdatePayload
    {
        /// <summary>
        /// The encoded track base64 to play. null stops the current track
        /// </summary>
        [JsonProperty("encodedTrack", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue("")]
        public string? EncodedTrack { get; set; } = "";

        /// <summary>
        /// The track identifier to play
        /// </summary>
        [JsonProperty("identifier", NullValueHandling = NullValueHandling.Ignore)]
        public string? Identifier { get; set; }

        /// <summary>
        /// The track position in milliseconds
        /// </summary>
        [JsonProperty("position", NullValueHandling = NullValueHandling.Ignore)]
        public long? Position { get; set; }

        /// <summary>
        /// The track end time in milliseconds
        /// </summary>
        [JsonProperty("endTime", NullValueHandling = NullValueHandling.Ignore)]
        public long? EndTime { get; set; }

        /// <summary>
        /// The player volume from 0 to 1000
        /// </summary>
        [JsonProperty("volume", NullValueHandling = NullValueHandling.Ignore)]
        public int? Volume { get; set; }

        /// <summary>
        /// Whether the player is paused
        /// </summary>
        [JsonProperty("paused", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Paused { get; set; }

        /// <summary>
        /// The new filters to apply. This will override all previously applied filters
        /// </summary>
        [JsonProperty("filters", NullValueHandling = NullValueHandling.Ignore)]
        public LavalinkFilters? Filters { get; set; }

        /// <summary>
        /// Information required for connecting to DiscordClient, without connected or ping
        /// </summary>
        [JsonProperty("voice", NullValueHandling = NullValueHandling.Ignore)]
        public LavalinkVoiceState? VoiceState { get; set; }
    }
}
