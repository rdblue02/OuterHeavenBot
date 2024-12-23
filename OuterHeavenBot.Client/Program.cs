using OuterHeavenBot.Setup;
using OuterHeavenBot.Lavalink;
using OuterHeavenBot.Client.Commands.Handlers;
using OuterHeavenBot.Core;
using OuterHeavenBot.Client.Services;
using OuterHeavenBot.Core.Models;
namespace OuterHeavenBot.Client
{
    public class Program
    {
        public static void Main(string[] args)
        { 
            //removes quick edit from console so our logging does not freeze during Console.WriteLine();
            var success = DisableConsoleQuickEdit.Disable();
            if (!success)
            {
                Console.WriteLine("Unable to disable quick edit. If logging freezes in the console, press the escape key to resume it.");
            }

            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddSingleton<MusicService>();
            builder.Services.AddSingleton<ClippieService>();
            builder.Services.AddSettings();
            builder.Services.AddDiscordDotNet();
            builder.Services.AddLavalink();
            builder.Services.AddSingleton<CommandHandler>();
            builder.Services.AddSingleton<ISearchHandler<ClippieFileData>,ClippieSearchHandler>();
            builder.Services.AddHostedService<MusicBotWorker>();
            builder.Services.AddHostedService<ClippieWorker>();

            var host = builder.Build();

          
            host.Run();
        }
    }
}