using Discord;
using Discord.Commands;
using Discord.WebSocket; 
using OuterHeavenBot.Client.Commands.Handlers;
using OuterHeavenBot.Client.Commands.Modules;
using OuterHeavenBot.Core;
using OuterHeavenBot.Lavalink; 

namespace OuterHeavenBot.Client.Services
{
    public class MusicBotWorker : BackgroundService
    {
        private readonly ILogger<MusicBotWorker> logger;
        private readonly DiscordSocketClient client;
        private readonly LavalinkNode lavalinkNode;
        private readonly AppSettings appSettings;
        private readonly CommandHandler commandHandler;
        private readonly CommandService commandService;
        private readonly IServiceProvider serviceProvider;
        private CancellationToken cancellationToken;
        private readonly MusicService musicService;
        public MusicBotWorker(ILogger<MusicBotWorker> logger,
                              DiscordClientProvider clientProvider,
                              LavalinkNode lavalinkNode,
                              CommandHandler commandHandler,
                              CommandService commandService,
                              AppSettings appSettings,
                              MusicService musicService,
                              IServiceProvider serviceProvider)
        {
            this.logger = logger;
            this.lavalinkNode = lavalinkNode;
            this.appSettings = appSettings;
            this.commandService = commandService;
            this.commandHandler = commandHandler;
            this.serviceProvider = serviceProvider;
            this.musicService = musicService;
            client = clientProvider.GetClient(DiscordClientProvider.MusicClientName);

            this.commandService.Log += Log;
            this.lavalinkNode.OnReady += LavalinkNode_OnReady;
            client.Log += Log;
            client.Ready += Client_Ready;
            client.MessageReceived += Client_MessageReceived;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!appSettings.OuterHeavenBotSettings.Enabled)
            {
                logger.LogInformation($"Outerheaven music bot is disabled.");
                return;
            }

            musicService.Initialize();
            cancellationToken = stoppingToken;

            logger.LogInformation("Attempting to connect to discord client");
            await client.LoginAsync(TokenType.Bot, appSettings.OuterHeavenBotSettings.DiscordToken);
            await client.SetGameAsync("|~h for more info", null, ActivityType.Playing);
            await client.StartAsync();
            logger.LogInformation("Bot is now running");
            await commandService.AddModuleAsync(typeof(MusicCommands), serviceProvider);
            await Task.Delay(-1, stoppingToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                await client.DisposeAsync().ConfigureAwait(false);
                lavalinkNode.Dispose();
            }
            catch (Exception e)
            {
                logger.LogError("Error: {0}", e.Message);
            }
            finally
            {
                await base.StopAsync(cancellationToken);
            }
        }

        private Task Log(LogMessage arg)
        {
            if (arg.Severity == LogSeverity.Error)
            {
                logger.LogError($"Error:{arg.Message}\nException{arg.Exception?.Message}");
            }
            else
            {
                logger.LogInformation(arg.Message);
            }

            return Task.CompletedTask;
        }

        private Task LavalinkNode_OnReady(LavalinkNode arg1, Lavalink.EventArgs.LavalinkNodeReadyEventArgs arg2)
        {
            logger.LogInformation("Lavalink has been connected");
            return Task.CompletedTask;
        }

        private async Task Client_MessageReceived(SocketMessage arg)
        {
            await commandHandler.HandleMessage(client, arg);
        }

        private async Task Client_Ready()
        {
            logger.LogInformation("Bot is ready");
            var restEndpoint = new LavalinkEndpoint("localhost", 2333, "youshallnotpass", "/v4");
            var wsEndpoint = new LavalinkEndpoint("localhost", 2333, "youshallnotpass", "/v4/websocket");
            await lavalinkNode.Initialize(client, null, cancellationToken);
            logger.LogInformation("Lava node initialized");
        }
    }
}
