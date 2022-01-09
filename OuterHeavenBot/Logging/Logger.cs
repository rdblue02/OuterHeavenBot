using System;
using System.Collections.Generic;
using System.Text;

namespace OuterHeavenBot.Logging
{
    class Logger<T>
    {
        string logPath;
        public Logger(string logPath)
        {
            this.logPath = logPath;
        }
     
        public void Log(object message)
        {

        }
    }
}
