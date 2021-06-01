using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenBot.Command
{
    public interface ICommandOptions
    {
        public char Prefix { get; set; }
        public List<Func<SocketUserMessage, bool>> RequirementsToExecute { get; set; } 
    }
}
