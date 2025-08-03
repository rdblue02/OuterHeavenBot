using Discord;
using Discord.WebSocket;
using OuterHeaven.LavalinkLight;
using OuterHeavenLight.Constants;
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

        public async Task<string> GetLogsFromServerAsync(SocketUser user)
        {
            try
            {
                var logFiles = ReadLogFiles();

                if (logFiles.Count == 0)
                {
                    return "No logs found";
                }

                var zipFile = this.fileSender.StageZipFile(logFiles,"logs");
                if (zipFile == null)
                {
                    return "Error sending log files";
                }

                await this.fileSender.SendZipFileAsync(user, zipFile);

                return "File send complete";
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
                return ex.Message;
            }
        }

        public async Task<string> GetSettingsFromServerAsync(SocketUser user)
        { 
            try
            {
                var settings = ReadSettingFiles();
                if (settings.Count == 0)
                {
                    return "Unable to find setting files";
                }

                var zipFile = this.fileSender.StageZipFile(settings, "botsettings");
                if (zipFile == null)
                {
                    return "Error sending setting files";
                }

                await fileSender.SendZipFileAsync(user, zipFile);
                return "File send complete";
            }
            catch(Exception ex)
            {
                logger.LogError(ex.ToString());
                return ex.Message;
            } 
        }

        private List<FileInfo> ReadSettingFiles()
        {
            var appsettingsFile = search.FindFile($"{appSettings}.json", 0, 0);
            var lavaSettings = search.FindFile("application.yml", 0, 5);

            var settings = new List<FileInfo>();

            AddFileToResult(settings, appsettingsFile);
            AddFileToResult(settings, lavaSettings);
            return settings;
        }

        private List<FileInfo> ReadLogFiles()
        { 
            var logDirectoryInfo = search.FindDirectory(BotResourceName.DefaultLogDirectory, 5, 5);

            if (logDirectoryInfo == null)
            {
                logger.LogError($"Cannot find directory {BotResourceName.DefaultLogDirectory}");
                return [];
            }

            var logsToSend = new List<FileInfo>();

            var lavalog = this.search.FindFile(BotResourceName.LavalinkLogFileName, 0, 3, logDirectoryInfo.FullName);
            var botLog = this.search.FindFile(BotResourceName.BotLogFileName, 0, 3, logDirectoryInfo.FullName);
          
            AddFileToResult(logsToSend, lavalog);
            AddFileToResult(logsToSend, botLog);
           
            return logsToSend;
        }

        private void AddFileToResult(List<FileInfo> results, FileInfo? fileResult)
        {
            if (fileResult != null)
            {
                logger.LogInformation($"Found appsettings file path {fileResult.FullName}");
                results.Add(fileResult);
            }
        }
    }
}