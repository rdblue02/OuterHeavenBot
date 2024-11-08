using Microsoft.Extensions.Logging;

namespace OuterHeavenBot.Lavalink
{
    public static class LavalinkEvent
    {
        public static EventId Intents { get; } = new EventId(1000, "Intents");
        public static EventId Lavalink { get; set; } = new EventId(1001, "Lavalink");
    } 
}
