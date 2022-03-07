using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenBot.Logging
{
    public class ApplicationLogEvent:EventArgs
    {
        public string LogMessage { get; }
        public ApplicationLogEvent(string logMessage)
        {
            LogMessage = logMessage;
        }
    }
}
