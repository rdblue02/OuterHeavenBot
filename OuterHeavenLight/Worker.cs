using OuterHeaven.LavalinkLight;

namespace OuterHeavenLight
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly Lava lava;
        private readonly MusicService musicService;
        public Worker(ILogger<Worker> logger, Lava lava,
                      MusicService musicService)
        {
            _logger = logger;
            this.musicService = musicService;
            this.lava = lava;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await musicService.Initialize();
            await lava.Initialize();
             
            await Task.Delay(-1, stoppingToken);
        }
    }
}
