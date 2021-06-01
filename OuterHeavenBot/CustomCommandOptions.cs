using Discord.WebSocket;
using OuterHeavenBot.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenBot
{
    public class CustomCommandOptions : ICommandOptions
    {
        public Dictionary<string, List<string>> ArgumentAliasMap { get; set; }
        public char Prefix { get; set; }
        public List<Func<SocketUserMessage, bool>> RequirementsToExecute { get; set; }
        public CustomCommandOptions()
        {
            this.Prefix = '~';
            this.RequirementsToExecute = new List<Func<SocketUserMessage, bool>>();
        }
    }
}
