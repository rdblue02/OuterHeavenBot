using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenBot.Command
{
    public class DefaultCommandOptions : ICommandOptions
    {
        public char Prefix { get; set; }
        public List<Func<SocketUserMessage, bool>> RequirementsToExecute { get; set; }

        public DefaultCommandOptions()
        {           
            Prefix = '!';
            RequirementsToExecute = new List<Func<SocketUserMessage, bool>>();
        }
    }
}
