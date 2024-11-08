using Discord;
using Discord.WebSocket;
using OuterHeavenBot.Lavalink.EventArgs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenBot.Lavalink
{
    public class LavaExtension : IDisposable
    {
        public List<LavalinkNode> Nodes { get; }  
        public event Func<LavaExtension, LavalinkNodeConnectedEventArgs, Task>? LavalinkNodeConnected;

        public DiscordSocketClient  Client;
        public LavaExtension(DiscordSocketClient client) 
        {
           this.Nodes = new List<LavalinkNode>();
           this.Client = client;
        } 
 
        public void ConnectNode(LavalinkNode node)
        {
            if (node is null) throw new ArgumentNullException(nameof(node));
            Nodes.Add(node); 
        }
 
        public void DisconnectNode(LavalinkNode node)
        {
            if (node is null) throw new ArgumentNullException(nameof(node));
            node.Disconnect();
        }
 
        public async Task ConnectAllNodes()
        {
            foreach (var node in Nodes)
            {
                await node.ConnectNodeAsync(Client).ConfigureAwait(false);
            }
        }
 
        public void AddNode(LavalinkNode node)
        {
            if (node is null) throw new ArgumentNullException(nameof(node));
            Nodes.Add(node);
        }
 
        public LavalinkNode GetNode()
        {
            if (!Nodes.Any()) throw new InvalidOperationException("No nodes are connected.");

            return Nodes.Where(c => c.IsReady).OrderBy(x => x.Stats.PlayingPlayers).First();
        }

        internal void RemoveNode(LavalinkNode nomiaNode)
        {
            Nodes.Remove(nomiaNode);
        }

        public void Dispose()
        {
            foreach (var node in Nodes)
            {
                node.Dispose();
            }
        }


    } 
}