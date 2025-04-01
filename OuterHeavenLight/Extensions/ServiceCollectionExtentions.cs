using Discord.Commands;
using OuterHeavenLight.Clippies;
using OuterHeavenLight.Core;
using OuterHeavenLight.Dev;
using OuterHeavenLight.LavaConnection;
using OuterHeavenLight.Music;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenLight.Extensions
{
    public static class ServiceCollectionExtentions
    {
        public static IServiceCollection AddLava(this IServiceCollection services)
        {
            services.AddSingleton<CommandHandler>();
            services.AddSingleton<CommandService>();
            services.AddSingleton<LavalinkEndpointProvider>();
            services.AddSingleton<LavalinkRestNode>();
            services.AddSingleton<Lava>();
            return services;
        }

        public static IServiceCollection AddClips(this IServiceCollection services)
        {
           services.AddSingleton<ClippieCommands>(); 
           services.AddSingleton<ClippieDiscordClient>();
           services.AddSingleton<ClippieService>();
           services.AddHostedService<ClippieWorker>(); 
            return services;
        }

        public static IServiceCollection AddMusic(this IServiceCollection services)
        { 
           services.AddSingleton<MusicCommands>(); 
           services.AddSingleton<MusicDiscordClient>();
           services.AddSingleton<MusicService>();
           services.AddHostedService<MusicWorker>(); 
            return services;
        }

        public static IServiceCollection AddDevUtilities(this IServiceCollection services)
        { 
            services.AddSingleton<DevCommands>();
            return services;
        }
    }
}
