using Discord.Commands;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using OuterHeaven.LavalinkLight;
using OuterHeavenLight.Clippies;
using OuterHeavenLight.Dev;
using OuterHeavenLight.LavaConnection;
using OuterHeavenLight.Music;
using System.Reflection;

namespace OuterHeavenLight
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                var result = DisableConsoleQuickEdit.Disable();
              
                if (!result)
                {
                    Console.WriteLine("Error disabling console quick edit");
                } 

                var builder = Host.CreateApplicationBuilder(args);
                    builder.Configuration.AddEnvironmentVariables();
      
                var settings = builder.Configuration.Get<AppSettings>() ?? throw new ArgumentNullException(nameof(AppSettings));
                 
                builder.Services.AddSingleton(settings);
                builder.Services.AddLogging(x =>
                { 
                    var logDirectoryName = settings.AppLogDirectory ?? throw new ArgumentNullException(nameof(settings.AppLogDirectory));
 
                    x.AddLog4Net(); 
                    x.AddConsole();
                });

                builder.Services.AddSingleton<CommandService>();
                //dev
                builder.Services.AddSingleton<DevCommandHandler>();
                builder.Services.AddSingleton<DevCommands>();

                //lava
                builder.Services.AddSingleton<LavalinkEndpointProvider>();
                builder.Services.AddSingleton<LavalinkRestNode>();
                builder.Services.AddSingleton<Lava>();

                builder.Services.AddSingleton<MusicCommands>();
                builder.Services.AddSingleton<MusicCommandHandler>();
                builder.Services.AddSingleton<MusicDiscordClient>();
                builder.Services.AddSingleton<MusicService>();
                builder.Services.AddHostedService<MusicWorker>();

                //Clippie types 
                builder.Services.AddSingleton<ClippieCommands>();
                builder.Services.AddSingleton<ClippieCommandHandler>();
                builder.Services.AddSingleton<ClippieDiscordClient>();
                builder.Services.AddSingleton<ClippieService>();
                builder.Services.AddHostedService<ClippieWorker>();
                 
                var host = builder.Build();
                LogManager.GetLogger(typeof(Program)).Info("Starting application logger");
                host.Run();
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex.ToString());
                Console.ReadLine();
            }           
        }  
    }
}