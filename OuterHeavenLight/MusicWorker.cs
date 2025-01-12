using OuterHeaven.LavalinkLight;

namespace OuterHeavenLight
{
    public class MusicWorker : BackgroundService
    {
        private readonly ILogger<MusicWorker> _logger;
        private readonly Lava lava;
        private readonly MusicService musicService;
        public MusicWorker(ILogger<MusicWorker> logger, Lava lava,
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
