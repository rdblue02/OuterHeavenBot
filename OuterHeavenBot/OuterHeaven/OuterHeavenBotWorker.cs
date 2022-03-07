using OuterHeavenBot.Clients;
using OuterHeavenBot.Commands;
using OuterHeavenBot.Logging;
using OuterHeavenBot.Services;
using OuterHeavenBot.Setup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Victoria;

namespace OuterHeavenBot.Workers
{
    public class OuterHeavenBotWorker : BackgroundService
    {
        private readonly ILogger<OuterHeavenBotWorker> logger;
        private readonly MusicService musicService;
        public OuterHeavenBotWorker(ILogger<OuterHeavenBotWorker> logger,                               
                                MusicService musicService)
        {
            this.logger = logger;
            this.musicService = musicService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInfo("Executeing OuterHeaven Bot Worker");
            await musicService.InitializeAsync();
            await Task.Delay(-1, stoppingToken);
        }
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInfo("Starting OuterHeaven Bot Worker");
            return base.StartAsync(cancellationToken);
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInfo("Stopping OuterHeaven Bot Worker");
            return base.StopAsync(cancellationToken);
        }
    }
}
