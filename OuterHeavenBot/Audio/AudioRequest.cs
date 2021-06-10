using Discord;
using OuterHeavenBot.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OuterHeavenBot.Audio
{
   public class AudioRequest
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public bool IsMusic { get; set; }
        public IVoiceChannel RequestingChannel { get; set; }

   }
}
