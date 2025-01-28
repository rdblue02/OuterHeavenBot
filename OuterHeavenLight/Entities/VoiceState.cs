using Newtonsoft.Json.Linq;
using System.Text.Json.Serialization;

namespace OuterHeavenLight.Entities
{
    public class VoiceState
    {
        [JsonPropertyName("token")]
        public string Token { get; set; }

        [JsonPropertyName("endpoint")]
        public string Endpoint { get; set; }

        [JsonPropertyName("sessionId")]
        public string DiscordVoiceSessionId { get; set; }

        [JsonIgnore]
        public string LavaSessionId { get; set; }

        [JsonIgnore]
        public string ChannelId { get; set; }

        [JsonIgnore]
        public string GuildId { get; set; }

        public bool DiscordServerLoaded() => !string.IsNullOrEmpty(Token) &&
                                             !string.IsNullOrEmpty(Endpoint);
        public bool DiscordVoiceLoaded() => !string.IsNullOrEmpty(DiscordVoiceSessionId) && 
                                            !string.IsNullOrEmpty(ChannelId) &&
                                            !string.IsNullOrEmpty(GuildId);
        public bool VoiceLoaded() =>  DiscordVoiceLoaded() && 
                                      DiscordServerLoaded() &&
                                      !string.IsNullOrEmpty(LavaSessionId);

        public override string ToString()
        {
            return 
                $"{nameof(Token)} | {Token}" +
                $"{nameof(Endpoint)} | {Endpoint}" +
                $"{nameof(DiscordVoiceSessionId)} | {DiscordVoiceSessionId}" +
                $"{nameof(ChannelId)} |  {ChannelId}" +
                $"{nameof(GuildId)} | {GuildId}";
        }
    } 
}