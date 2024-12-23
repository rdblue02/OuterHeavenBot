using Discord;
using Discord.Commands;
using Discord.WebSocket;
using OuterHeavenBot.Client.Commands.Handlers;
using OuterHeavenBot.Client.Commands.Modules;
using OuterHeavenBot.Core;
using OuterHeavenBot.Lavalink;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenBot.Client.Services
{
    public class ClippieWorker : BackgroundService
    {
        private readonly ILogger<ClippieWorker> logger;
        private readonly DiscordSocketClient client;
        private readonly AppSettings appSettings;
        private readonly CommandHandler commandHandler;
        private readonly CommandService commandService;
        private readonly ClippieService clippieService;
        private readonly IServiceProvider serviceProvider;
        public ClippieWorker(ILogger<ClippieWorker> logger,
                              DiscordClientProvider clientProvider,
                              ClippieService clippieService,
                              CommandHandler commandHandle,
                              CommandService commandService,
                              AppSettings appSettings,
                              IServiceProvider serviceProvider)
        {
            this.logger = logger;
            this.appSettings = appSettings;
            client = clientProvider.GetClient(DiscordClientProvider.ClippieClientName);
            this.clippieService = clippieService;

            this.commandService = commandService;
            commandHandler = commandHandle;
            this.serviceProvider = serviceProvider;
            this.commandService.Log += Log;
            client.Log += Log;
            client.Ready += Client_Ready;
            client.MessageReceived += Client_MessageReceived;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!appSettings.ClippieBotSettings.Enabled)
            {
                logger.LogInformation("Clippie client is disabled");
                return;
            }

            logger.LogInformation("Attempting to connect to clippie client");
            clippieService.Initialize();
            await client.LoginAsync(TokenType.Bot, appSettings.ClippieBotSettings.DiscordToken);
            await client.StartAsync();
            await commandService.AddModuleAsync(typeof(ClippieCommands), serviceProvider);

            await Task.Delay(-1, stoppingToken);
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


        private async Task Client_MessageReceived(SocketMessage arg)
        {
            await commandHandler.HandleMessage(client, arg);
        }

        private Task Client_Ready()
        {
            logger.LogInformation("Clippie Bot is ready");
            return Task.CompletedTask;
        }
    }
}
