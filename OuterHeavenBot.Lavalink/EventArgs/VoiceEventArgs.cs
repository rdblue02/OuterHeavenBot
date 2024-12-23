using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
namespace OuterHeavenBot.Lavalink.EventArgs
{
    public class VoiceStateUpdateEventArgs
    {
        public IGuild Guild { get; set; }
        public IVoiceState Channel { get; set; }
        public VoiceStateUpdateEventArgs()
        {
            
        }
       
    }

    public class VoiceServerUpdateEventArgs
    {
        public string Endpoint { get; set; }
        public string VoiceToken { get; set; }
    }
}
