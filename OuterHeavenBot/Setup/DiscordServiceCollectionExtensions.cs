using Discord.Commands;
using Discord.WebSocket;
using OuterHeavenBot.Clients;
using OuterHeavenBot.Commands;
using OuterHeavenBot.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Victoria;

namespace OuterHeavenBot.Setup
{
    public static class DiscordServiceCollectionExtensions
    {

        public static IServiceCollection AddDiscord(this IServiceCollection services)
        {

            services.AddSingleton<CommandService>().Configure<CommandServiceConfig>(x => { x.LogLevel = Discord.LogSeverity.Verbose; });

            services.AddSingleton<ClippieService>();
            services.AddSingleton<ClippieCommandHandler>();
            services.AddSingleton<ClippieDiscordClient>().Configure<DiscordSocketConfig>(x => { x.LogLevel = Discord.LogSeverity.Verbose; });

            services.AddSingleton<MusicService>();
            services.AddSingleton<OuterHeavenCommandHandler>();
            services.AddSingleton<OuterHeavenDiscordClient>().Configure<DiscordSocketConfig>(x => { x.LogLevel = Discord.LogSeverity.Verbose; });

            services.AddSingleton(new LavaConfig()
            {
                LogSeverity = Discord.LogSeverity.Verbose,
                Authorization = "0_9_21_2021",
                ReconnectDelay = TimeSpan.FromSeconds(5),
                ReconnectAttempts = 100,
                EnableResume = true,
                Port = 50223,
                Hostname = "127.0.0.1",
                SelfDeaf = true
            });

            services.AddSingleton(x => new LavaNode(x.GetRequiredService<OuterHeavenDiscordClient>(), x.GetRequiredService<LavaConfig>()));
            return services;
        }        
    }
}
