﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OuterHeaven.LavalinkLight
{
    public class AppSettings
    {

        public string AppLogDirectory { get; set; } = "";
        public OuterHeavenBotSettings? OuterHeavenBotSettings { get; set; }

        public ClippieBotSettings? ClippieBotSettings { get; set; }
    }

    public class OuterHeavenBotSettings : BotSettings
    {
        public string PlayLocalDirectory { get; set; } = "";
        public string LavalinkDirectory { get; set; } = "";
    }

    public class ClippieBotSettings : BotSettings
    {
        public string SoundFileDirectory { get; set; } = "";
        public string[] ExcludedSubDirectories { get; set; } = [];

        [JsonIgnore]
        public string DefaultSoundFileDirectory => Path.Combine(Directory.GetCurrentDirectory(), SoundFileDirectory);
    }

    public class BotSettings
    {
        public bool Enabled { get; set; }
        public string DiscordToken { get; set; } = "";
    }
}
