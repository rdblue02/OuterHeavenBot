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

namespace OuterHeavenBot.Workers
{
    public class ClippieBotWorker : BackgroundService
    {
        private readonly ILogger<ClippieBotWorker> logger;
        private readonly ClippieService clippieService;
        public ClippieBotWorker(ILogger<ClippieBotWorker> logger,  
                                ClippieService clippieService)
        {
            this.logger = logger; 
            this.clippieService = clippieService;           
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInfo("Executeing Clippie Bot Worker");
            await clippieService.InitializeAsync();
            await Task.Delay(-1, stoppingToken);
        }
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInfo("Starting Clippie Bot Worker");
            return base.StartAsync(cancellationToken);
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInfo("Stopping Clippie Bot Worker");
            return base.StopAsync(cancellationToken);
        }
    } 
}
