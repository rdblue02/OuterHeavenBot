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

namespace OuterHeavenBot.Clients
{
    public class OuterHeavenDiscordClient : DiscordSocketClient,IDisposable
    {
        private readonly ILogger logger;
        private readonly BotSettings botSettings; 
        private readonly List<string> requiredLavaLinkFiles;
        private const string lavalinkProcessName = "java";
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
           // var lavaLinkTask =  StartLavaLinkAsync();
            await this.LoginAsync(TokenType.Bot, botSettings.OuterHeavenBotToken);
            await this.SetGameAsync("|~h for more info", null, ActivityType.Playing);
          
          //  await lavaLinkTask;
            await this.StartAsync();

        }
        private Task OuterHeavenDiscordClient_Log(LogMessage arg)
        {
            logger.Log(Helpers.ToMicrosoftLogLevel(arg.Severity), $"{arg.Message}\n{arg.Exception}");
            return Task.CompletedTask;
        }
        private async Task StartLavaLinkAsync()
        {
            var lavaLinkFiles = new DirectoryInfo(Directory.GetCurrentDirectory()).GetFiles("*",SearchOption.AllDirectories)
                                                                                  .Where(file => requiredLavaLinkFiles.Any(requiredFile => file.Name.Contains(requiredFile)))
                                                                                  .ToList();
            //need lavalink to run music bot. This means we are missing a file.
            if (lavaLinkFiles.Count != this.requiredLavaLinkFiles.Count)
            {
                throw new InvalidOperationException($"Missing required files {string.Join(" ,", lavaLinkFiles.Where(x => !requiredLavaLinkFiles.Any(y => x.Name.Contains(y))).Select(x => x.Name))}");
            }

            //make sure there are no current laval link processes running.
            KillLavalink();
             
            logger.LogInformation("Starting lavalink process");
            var lavalinkFilePath = lavaLinkFiles.FirstOrDefault(x => x.FullName.Contains(lavalinkStartFile))?.FullName;
            var lavaLinkProcess = new Process();
            lavaLinkProcess.StartInfo.UseShellExecute = true;
            lavaLinkProcess.StartInfo.CreateNoWindow = false;
            lavaLinkProcess.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
            lavaLinkProcess.StartInfo.FileName = @"C:\Program Files\Microsoft\jdk-11.0.12.7-hotspot\bin\javaw";
            lavaLinkProcess.StartInfo.Arguments = $"-jar {lavalinkFilePath}";
            lavaLinkProcess.Start();
            logger.LogInformation($"lavalink started with process id {lavaLinkProcess?.Id}, name {lavaLinkProcess?.ProcessName}");
            lavaLinkProcess.WaitForExit(5000);
            TimeSpan timeWaited = TimeSpan.Zero;
            bool lavaLinkIsRunning = Process.GetProcesses().Any(x => x.ProcessName.ToLower().Contains(lavalinkProcessName));
            if (!lavaLinkIsRunning)
            {
                throw new InvalidOperationException($"Unable to start lavalink");
            }
           
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
            KillLavalink();
            return base.StopAsync();
        }
    }
}
