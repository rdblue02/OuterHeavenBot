using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenBot.Lavalink.EventArgs
{ 
    
     

    public class LavalinkClientExceptionEventArgs 
    { 
        public Exception Exception { get; }
        public LavalinkClientExceptionEventArgs(Exception exception)
        {
            Exception = exception;
        }
    }

    public class LavalinkNodeConnectedEventArgs 
    { 
        public LavalinkNode Node { get; }
        public LavalinkNodeConnectedEventArgs(LavalinkNode node)
        {
            Node = node;
        }
    }

    public class LavalinkNodeDisconnectedEventArgs  
    {
        public LavalinkNodeDisconnectedEventArgs()
        {
        }
    }

    public class LavalinkNodeReadyEventArgs  
    {
       
        public bool Resumed { get; } 
        public string SessionId { get; }

        public LavalinkNodeReadyEventArgs(bool resumed, string sessionId)
        {
            Resumed = resumed;
            SessionId = sessionId;
        }
    }
} 