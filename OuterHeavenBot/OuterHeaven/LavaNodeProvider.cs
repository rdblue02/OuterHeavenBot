using OuterHeavenBot.Clients;
using Victoria;
using Victoria.Node;

namespace OuterHeavenBot.OuterHeaven
{
    public class LavaNodeProvider
    {
        private LavaNode lavaNode;
        IServiceProvider serviceProvider;
        public LavaNodeProvider(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(IServiceProvider));
            this.lavaNode = new LavaNode(serviceProvider.GetRequiredService<OuterHeavenDiscordClient>(), serviceProvider.GetRequiredService<NodeConfiguration>(),serviceProvider.GetRequiredService<ILogger<LavaNode>>());
        }

        public LavaNode GetLavaNode() =>
                        lavaNode == null ? 
                        new LavaNode(serviceProvider.GetRequiredService<OuterHeavenDiscordClient>(), serviceProvider.GetRequiredService<NodeConfiguration>(), serviceProvider.GetRequiredService<ILogger<LavaNode>>()) : lavaNode;
    }
}
