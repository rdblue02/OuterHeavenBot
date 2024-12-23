using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenBot.Lavalink.EventArgs
{
    public class WebsocketConnectedEventArgs  
    {
        public WebsocketConnectedEventArgs()
        {

        }
    }
    public class WebsocketDisconnectedEventArgs 
    {
        public string Reason { get; }
        public WebsocketDisconnectedEventArgs(string reason)
        {
            Reason = reason;
        }
    }
    public class WebsocketMessageEventArgs  
    {
        public bool IsBinary { get; }

        public string Message { get; }

        public WebsocketMessageEventArgs(bool isBinary, string message)
        {
            IsBinary = isBinary;
            Message = message;
        }
    }
}
