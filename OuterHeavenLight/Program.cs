using Discord.Commands;
using Discord.WebSocket;
using Discord;
using OuterHeaven.LavalinkLight;

namespace OuterHeavenLight
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
           
            AddSettings(builder.Services);
            var config = new DiscordSocketConfig()
            {
                LogLevel = LogSeverity.Verbose,
                GatewayIntents = GatewayIntents.All,
                MessageCacheSize = 100,
                LogGatewayIntentWarnings = false
            };

            builder.Services.AddLogging();
            builder.Services.AddSingleton(config);
            builder.Services.AddSingleton<DiscordSocketClient>();
            builder.Services.AddSingleton<CommandHandler>();
            builder.Services.AddSingleton<CommandService>();
            builder.Services.AddSingleton<BotCommands>();
            builder.Services.AddSingleton<MusicService>();
            builder.Services.AddSingleton<LavalinkEndpointProvider>();
            builder.Services.AddSingleton<LavalinkRestNode>(); 
            builder.Services.AddSingleton<Lava>();
            builder.Services.AddHostedService<Worker>();
            var host = builder.Build();
            host.Run();
        }
        public static IServiceCollection AddSettings(IServiceCollection services)
        {
            var configName = System.Diagnostics.Debugger.IsAttached ? "appsettings.Development.json" : "appsettings.json";
            IConfiguration config = new ConfigurationBuilder()
           .AddJsonFile(configName)
           .AddEnvironmentVariables()
           .Build();

            var settings = config.GetRequiredSection(nameof(AppSettings)).Get<AppSettings>() ?? throw new ArgumentNullException(nameof(AppSettings), $"Cannot resolve {nameof(AppSettings)}");
            services.AddSingleton(settings);
            return services;
        }
    }
}