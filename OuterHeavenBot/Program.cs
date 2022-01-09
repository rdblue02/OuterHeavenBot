using Discord.Commands;
using Discord.WebSocket;
using OuterHeavenBot.Command;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Threading.Tasks;
using Victoria;
using System.Diagnostics;
using OuterHeavenBot.Services;
using System.Threading;

namespace OuterHeavenBot
{
    class Program
    {
       
        public static void Main(string[] args)
         => new Program().MainAsync(args).GetAwaiter().GetResult();

        public async Task MainAsync(string[] args)
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            try
            {
               
                Console.WriteLine("Initializing Music Bot");
                using IHost musicHost = CreateMusicHostBuilder(args, cancellationTokenSource).Build();               
                await musicHost.Services.GetRequiredService<DiscordMusicBotInitializer>().Initialize();

                //not sure if it would create a second instance when registering it with clippies.
                //So I'm getting a reference here. Should find out though.
                var audioService = musicHost.Services.GetRequiredService<AudioService>();
                Console.WriteLine("Initializing Clippie Bot");
                using IHost clippieHost = CreateClippieHostBuilder(args, audioService).Build();
                await clippieHost.Services.GetRequiredService<DiscordClippieBotInitializer>().Initialize();

                await Task.WhenAny(musicHost.RunAsync(cancellationTokenSource.Token), clippieHost.RunAsync(cancellationTokenSource.Token));   
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }

            if (cancellationTokenSource.IsCancellationRequested)
            {
                await MainAsync(args);
            }
 
        }
        static IHostBuilder CreateMusicHostBuilder(string[] args,CancellationTokenSource tokenSource)
        {
            var builder = Host.CreateDefaultBuilder(args)
                 .ConfigureServices((_, services) =>
                 {
                     services.AddSingleton(tokenSource);
                     services.AddSingleton<AudioService>();
                     services.AddSingleton<CommandService>().Configure<CommandServiceConfig>(x => { x.LogLevel = Discord.LogSeverity.Verbose; });
                     services.AddSingleton<DiscordSocketClient>().Configure<DiscordSocketConfig>(x => { x.LogLevel = Discord.LogSeverity.Verbose; });
                     services.AddSingleton<DiscordMusicBotInitializer>();
                     services.AddSingleton<CommandHandler<DiscordSocketClient>>();                     
                     services.AddLavaNode(x => { x.LogSeverity = Discord.LogSeverity.Verbose; x.Authorization = "0_9_21_2021"; x.ReconnectDelay = TimeSpan.FromSeconds(2); x.ReconnectAttempts = 100; });
                 });

            return builder;
        }
        static IHostBuilder CreateClippieHostBuilder(string[] args, AudioService audioService)
        {
            var builder = Host.CreateDefaultBuilder(args)     
                 .ConfigureServices((_, services) => {
                     services.AddSingleton(audioService);
                     services.AddSingleton<DiscordClippieClient>().Configure<DiscordSocketConfig>(x => { x.LogLevel = Discord.LogSeverity.Verbose; });
                     services.AddSingleton<CommandHandler<DiscordClippieClient>>();
                     services.AddSingleton<CommandService>().Configure<CommandServiceConfig>(x => { x.LogLevel = Discord.LogSeverity.Verbose; });
                     services.AddSingleton<DiscordClippieBotInitializer>();
                     services.AddSingleton<AudioService>();
                     services.AddTransient(x => new Random((int)DateTime.Now.Ticks));        
                 });

            return builder;
        }
    }
}
