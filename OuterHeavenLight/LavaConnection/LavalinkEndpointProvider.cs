using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenLight.LavaConnection
{
    public class LavalinkEndpointProvider
    {
        public LavalinkEndpoint RestEndpoint { get; } = new LavalinkEndpoint("localhost", 2333, "youshallnotpass", "/v4");
        public LavalinkEndpoint WebSocketEndpoint { get; } = new LavalinkEndpoint("localhost", 2333, "youshallnotpass", "/v4/websocket");
    }
}
