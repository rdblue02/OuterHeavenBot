using Discord.Commands;
using Discord;
using Discord.WebSocket;
using OuterHeavenBot.Lavalink;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OuterHeavenBot.Client.Commands.Handlers;
using OuterHeavenBot.Core;

namespace OuterHeavenBot.Client
{
    public static class SetupExtensions
    {
        public static IServiceCollection AddLavalink(this IServiceCollection services)
        {
            services.AddSingleton<LavalinkNode>();
            services.AddSingleton<LavalinkWebsocket>();
            services.AddSingleton<ISearchHandler<LavalinkTrack>,LavalinkSearchHandler>(); 
            return services;
        }

        public static IServiceCollection AddDiscordDotNet(this IServiceCollection services) 
        {
            var config = new DiscordSocketConfig()
            {
                LogLevel = LogSeverity.Verbose,
                GatewayIntents = GatewayIntents.All,
                MessageCacheSize = 100,
                LogGatewayIntentWarnings = false
            };

            services.AddSingleton(config);
            services.AddSingleton<CommandHandler>();
            services.AddSingleton<CommandService>();
           

            services.AddSingleton<DiscordClientProvider>();  
           
            return services;
        }

        public static IServiceCollection AddSettings(this IServiceCollection services) 
        {
            var configName = System.Diagnostics.Debugger.IsAttached ? "appsettings.Development.json" : "appsettings.json";
            IConfiguration config = new ConfigurationBuilder()
           .AddJsonFile(configName)
           .AddEnvironmentVariables()
           .Build();
        

            var settings = config.GetRequiredSection(nameof(AppSettings)).Get<AppSettings>() ?? throw new ArgumentNullException(nameof(AppSettings),$"Cannot resolve {nameof(AppSettings)}");
            services.AddSingleton(settings);
            return services;
        }
    }
}