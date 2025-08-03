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
using OuterHeavenLight.Extensions;
using OuterHeavenLight.LavaConnection;
using OuterHeavenLight.Music;
using OuterHeavenLight.Utilities;
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
                    x.AddLog4Net(); 
                    x.AddConsole();
                });

                ////todo - convert to options pattern
                //var fileCache = new LavaFileCache();
                //fileCache.Load();

                //builder.Services.AddSingleton(fileCache); 
                builder.Services.AddDevUtilities();
                builder.Services.AddLava();
                builder.Services.AddMusic();
                builder.Services.AddClips();
                 
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