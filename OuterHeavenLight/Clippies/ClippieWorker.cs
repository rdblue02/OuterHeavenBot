  
namespace OuterHeavenLight.Clippies
{
   
    public class ClippieWorker : BackgroundService
    {
        ClippieService clippieService;
        private readonly ILogger<ClippieWorker>  logger;
 
        public ClippieWorker(ILogger<ClippieWorker> logger,
                             ClippieService clippieService)
        {
             this.logger = logger;
            this.clippieService = clippieService;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Executeing Clippie Bot Worker");
            await clippieService.InitializeAsync();
            await Task.Delay(-1, stoppingToken);
        }
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Starting Clippie Bot Worker");
            return base.StartAsync(cancellationToken);
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Stopping Clippie Bot Worker");
            return base.StopAsync(cancellationToken);
        }
    }
}
