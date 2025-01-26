using Discord.Commands;
using Discord.WebSocket;
using Discord;
using OuterHeaven.LavalinkLight;
using OuterHeavenLight.Clippies;
using OuterHeavenLight.Music;
using OuterHeavenLight.LavaConnection;

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

                var settings = CreateSettings(builder.Services);
                builder.Services.AddSingleton(settings);
                builder.Services.AddLogging(x =>
                {

                    var log4netPath = Path.Combine(Directory.GetCurrentDirectory(), "log4net.config");

                    x.AddLog4Net(log4netPath, true);
                    x.AddConsole();
                });

                builder.Services.AddSingleton<CommandService>();

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
                host.Run();
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex.ToString());
                Console.ReadLine();
            }           
        }

         static AppSettings CreateSettings(IServiceCollection services)
        {
            var configName = System.Diagnostics.Debugger.IsAttached ? "appsettings.Development.json" : "appsettings.json";
            IConfiguration config = new ConfigurationBuilder()
           .AddJsonFile(configName)
           .AddEnvironmentVariables()
           .Build();

            var settings = config.GetRequiredSection(nameof(AppSettings)).Get<AppSettings>() ?? throw new ArgumentNullException(nameof(AppSettings), $"Cannot resolve {nameof(AppSettings)}"); 
            return settings;
        }
    }

}