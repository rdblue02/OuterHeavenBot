using Discord.Commands;
using Discord.WebSocket;
using OuterHeavenBot;
using OuterHeavenBot.Command;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;
using OuterHeavenBot.Audio;
using System.Threading;
using YoutubeExplode;

namespace OuterHeavenBot
{
    class Program
    {
        public static void Main(string[] args)
         => new Program().MainAsync(args).GetAwaiter().GetResult();


        public async Task MainAsync(string[] args)
        {
            try
            {
                using IHost host = CreateHostBuilder(args).Build();
                await host.Services.GetRequiredService<DiscordBotInitializer>().Initialize();
                await host.RunAsync();
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }  
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
           Host.CreateDefaultBuilder(args)
               .ConfigureServices((_, services) => {
                   services.AddSingleton<CommandService>().Configure<CommandServiceConfig>(x => { x.LogLevel = Discord.LogSeverity.Verbose; });
                   services.AddSingleton<DiscordSocketClient>().Configure<DiscordSocketConfig>(x=>{ x.LogLevel = Discord.LogSeverity.Verbose; });
                   services.AddSingleton<DiscordBotInitializer>();
                   services.AddSingleton<CommandHandler>();
                   services.AddTransient(x=>new Random((int)DateTime.Now.Ticks));
                   services.AddSingleton<AudioManager>();
                   services.AddSingleton<YoutubeClient>();
               });
    }
}
