using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenLight.Core
{
    public struct CommandResult
    {
        public bool Success { get; set; } = false;
        public string Message { get; set; } = "";
        public Exception? Exception { get; set; } = null;
 
        public CommandResult()
        {

        }
    }
}
