using Microsoft.Extensions.Logging;

namespace OuterHeavenBot.Lavalink.Constants
{
    public static class LavalinkEventId
    {
        public static EventId Intents { get; } = new EventId(1000, "Intents");
        public static EventId Lavalink { get; set; } = new EventId(1001, "Lavalink");
    }
}
