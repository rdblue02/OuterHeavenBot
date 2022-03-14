using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Victoria;

namespace OuterHeavenBot.Setup
{
    public class BotSettings
    {
        //lavanode settings
        //discord client settings

        //discord_music_token
        public string OuterHeavenBotToken { get; set; } = "";

        //discord_clippie_token
        public string ClippieBotToken { get; set; } = "";

        public LoggingConfiguration LoggingConfiguration { get;set;} = new LoggingConfiguration();
    }
    public class LoggingConfiguration
    {
        public int PollingMilliseconds { get; set; } = 1000;
        public int MaxDaysToSaveLogs { get; set; } = 10;
        public int MaxNumberOfLogFiles { get; set; } = 10;
        public string LogDirectory { get; set; } = Directory.GetCurrentDirectory() + "\\BotLogs\\";
        public LogLevel Verbosity { get; set; } = LogLevel.Debug;
        public bool NotifyDev { get; set; }
    }
}
