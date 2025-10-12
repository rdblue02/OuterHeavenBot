using Discord;
using Discord.WebSocket;
using OuterHeaven.LavalinkLight;
using OuterHeavenLight.Constants;
using OuterHeavenLight.Core;
using System.Collections.Generic;
using System.Reflection;

namespace OuterHeavenLight.Dev
{
    public class DevService
    {
        private AppSettings appSettings;
        private ILogger<DevService> logger;
        private IDiscordFileSender fileSender; 
        private ISearch search;
        public DevService(ILogger<DevService> logger,
                          AppSettings appSettings,
                          ISearch search, 
                          IDiscordFileSender fileSender)
        {
            this.logger = logger;
            this.appSettings = appSettings;
            this.search = search;
            this.fileSender = fileSender; 
        }

        public async Task<CommandResult> GetLogsFromServerAsync(SocketUser user)
        {
            try
            {
                var logDirectoryInfo = search.FindDirectory(BotResourceName.DefaultLogDirectory, 5, 5);

                if (logDirectoryInfo == null)
                {
                    return CommandResult.FailureResult($"Cannot find directory {BotResourceName.DefaultLogDirectory}"); 
                }

                var logsToSend = new List<FileInfo>(); 
                var lavalog = this.search.FindFile(BotResourceName.LavalinkLogFileName, 0, 3, logDirectoryInfo.FullName);
                var botLog = this.search.FindFile(BotResourceName.BotLogFileName, 0, 3, logDirectoryInfo.FullName);

                AddFileToResult(logsToSend, lavalog);
                AddFileToResult(logsToSend, botLog);

                if (logsToSend.Count == 0)
                {
                    return CommandResult.FailureResult("No logs found");
                }

                var zipFile = this.fileSender.StageZipFile(logsToSend, "logs");
                if (zipFile == null)
                {
                    return CommandResult.FailureResult("Error sending log files");
                }

                await this.fileSender.SendZipFileAsync(user, zipFile);
                return CommandResult.SuccessResult("File send complete");
            }
            catch (Exception ex)
            { 
                return  CommandResult.FailureResult($"Error reading logs: {ex.Message}", ex);
            }
        }

        public async Task<CommandResult> GetSettingsFromServerAsync(SocketUser user)
        { 
            try
            { 
                var settings = new List<FileInfo>();
                var appsettingsFile = search.FindFile("appSettings.json", 0, 0);
                var lavaSettingsFile = search.FindFile("application.yml", 0, 5); 

                AddFileToResult(settings, appsettingsFile);
                AddFileToResult(settings, lavaSettingsFile);

                if (settings.Count == 0)
                {
                    return CommandResult.FailureResult("Unable to find setting files");
                }

                var zipFile = this.fileSender.StageZipFile(settings, "botsettings");
                if (zipFile == null)
                {
                    return CommandResult.FailureResult("Error staging setting files");
                }

                await fileSender.SendZipFileAsync(user, zipFile);
                return CommandResult.SuccessResult("File send complete");
            }
            catch(Exception ex)
            { 
                return CommandResult.FailureResult($"Error reading settings: {ex.Message}", ex);
            } 
        }
         
        private void AddFileToResult(List<FileInfo> results, FileInfo? fileResult)
        {
            if (fileResult != null)
            {
                logger.LogInformation($"Found file path {fileResult.FullName}");
                results.Add(fileResult);
            } 
        }
    }
}