using Discord;
using Discord.Commands;
using Discord.WebSocket;
using OuterHeavenBot.Commands.Modules;
using OuterHeavenBot.Commands;
using OuterHeavenBot.Setup;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace OuterHeavenBot.Clients
{
    public class OuterHeavenDiscordClient : DiscordSocketClient,IDisposable
    {
        private readonly ILogger logger;
        private readonly BotSettings botSettings; 
        private readonly List<string> requiredLavaLinkFiles;
        private const string lavalinkProcessName = "javaw";
        private const string lavalinkStartFile = "Lavalink.jar";
        public OuterHeavenDiscordClient(ILogger<OuterHeavenDiscordClient> logger,
                                        BotSettings botSettings)
        {
            this.requiredLavaLinkFiles= new List<string>() { "Lavalink.jar", "application.yml" }; 
            this.logger = logger;
            this.botSettings = botSettings;  
            this.Log += OuterHeavenDiscordClient_Log;             
        }
         
        public async Task InitializeAsync()
        {
            await this.LoginAsync(TokenType.Bot, botSettings.OuterHeavenBotToken);
            await this.SetGameAsync("|~h for more info", null, ActivityType.Playing);

          // await StartLavaLinkAsync();
            await this.StartAsync();

        }
        private Task OuterHeavenDiscordClient_Log(LogMessage arg)
        {
            logger.Log(Helpers.ToMicrosoftLogLevel(arg.Severity), $"{arg.Message}{arg.Exception}");
            return Task.CompletedTask;
        }

        //cant get this to work properly. Removing for now.
        private async Task StartLavaLinkAsync()
        {
            var directory = GetExecutingDirectory();
            var lavaLinkFiles = directory.GetFiles("*", SearchOption.AllDirectories)
                                                                                  .Where(file => requiredLavaLinkFiles.Any(requiredFile => file.Name.Contains(requiredFile)))
                                                                                  .ToList(); 
            foreach (var requiredFileName in requiredLavaLinkFiles)
            {
                //need lavalink to run music bot. This means we are missing a file.
                if (!lavaLinkFiles.Any(x => x.FullName.Contains(requiredFileName)))
                {
                    throw new InvalidOperationException($"Missing required file {requiredFileName}");
                }

            }
           
            //make sure there are no current laval link processes running.
            KillLavalink();
             
            logger.LogInformation("Starting lavalink process");
            var lavalinkFile = lavaLinkFiles.FirstOrDefault(x => x.FullName.Contains(lavalinkStartFile));
            if (string.IsNullOrEmpty(lavalinkFile?.FullName))
            {
                throw new InvalidOperationException($"Missing required file {lavalinkStartFile}");
            }
            else
            {
                logger.LogInformation($"Using lavalink file at the following path: \n{lavalinkFile.FullName}");
            }

            var lavaLinkProcess = new Process();
            lavaLinkProcess.StartInfo.UseShellExecute = true;
            lavaLinkProcess.StartInfo.CreateNoWindow = false;
            lavaLinkProcess.StartInfo.WorkingDirectory = lavalinkFile.DirectoryName;
            lavaLinkProcess.StartInfo.FileName = lavalinkProcessName;
            lavaLinkProcess.StartInfo.Arguments = $"-jar {lavalinkStartFile}";
            lavaLinkProcess.Start();

            //give lavalink a second to start
            await Task.Delay(1000);
            logger.LogInformation($"lavalink started with process id {lavaLinkProcess?.Id}, name {lavaLinkProcess?.ProcessName}");       

        }
        private void KillLavalink()
        {
            var lavaLinkProcess = Process.GetProcesses().FirstOrDefault(x => x.ProcessName.ToLower().Contains(lavalinkProcessName));
            if (lavaLinkProcess != null)
            {
                logger.LogInfo("Killing any existing lavalink processes");
                try
                {
                    lavaLinkProcess.Kill();
                }
                finally { }
            }
        }
        public override Task StopAsync()
        { 
            //KillLavalink();
            return base.StopAsync();
        }

        private DirectoryInfo GetExecutingDirectory()
        {
            var dllPath = GetType().Assembly.Location;
            var directoryPath = dllPath.Substring(0, dllPath.LastIndexOf('\\'));
            return new DirectoryInfo(directoryPath);
        }
    }
}
