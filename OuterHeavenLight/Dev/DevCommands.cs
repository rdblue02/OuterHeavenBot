using Discord.Commands;
using OuterHeavenLight.Music;
using System.Reflection;
using System.Linq;
using Discord;
using System.IO.Compression;
using OuterHeaven.LavalinkLight;
using System.Runtime.Serialization;
using OuterHeavenLight.Constants;

namespace OuterHeavenLight.Dev
{
    [Name(CommandGroupName.Dev)]
    public class DevCommands : ModuleBase<SocketCommandContext>
    {
        ILogger<DevCommands> logger;
        private AppSettings appsettings;
        private string lavalinkLogName = "lava_log.txt";
        private string appLogName = "bot_log.txt";
        private string tempDirectorName = "temp";
        public DevCommands(ILogger<DevCommands> logger,
                           AppSettings appSettings)
        {
            this.logger = logger;
            this.appsettings = appSettings;
        }

        [Command("logs", RunMode = RunMode.Async)]
        public async Task GetLogs()
        {
            try
            {
                var logFiles = GetLogFiles();
               
                if (logFiles.Count == 0)
                {
                    await ReplyAsync("No log files found.");
                    return;
                }

                await SendApplicationLogFiles(logFiles);
            }
            catch (Exception ex)
            {
                await ReplyAsync($"Error zipping log files\n{ex}");
            } 
         }

        private List<FileInfo?> GetLogFiles()
        { 
            var parentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory())?.Parent ?? new DirectoryInfo(Directory.GetCurrentDirectory());

            var logDirectory = parentDirectory.GetDirectories(appsettings.AppLogDirectory, SearchOption.AllDirectories)
                                              .FirstOrDefault();
            if(logDirectory == null)
            {
                logger.LogError($"Cannot find directory {appsettings.AppLogDirectory}");
                return [];
            }

            var logFiles = logDirectory.GetFiles().OrderByDescending(x => x.CreationTime); 
            var lavalog =  logFiles.FirstOrDefault(x => string.Equals(lavalinkLogName, x?.Name,StringComparison.OrdinalIgnoreCase));
            var applog = logFiles.FirstOrDefault(x => string.Equals(appLogName, x?.Name, StringComparison.OrdinalIgnoreCase));

            var logsToSend = new List<FileInfo?>();
            if (lavalog != null) 
            {
                logsToSend.Add(lavalog);
            }

            if (applog != null) 
            {
                logsToSend.Add(applog);
            }

            return logsToSend;
        }
 
        private async Task SendApplicationLogFiles(List<FileInfo?> files) 
        {
            var zipDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), tempDirectorName);
           
            if (!Directory.Exists(zipDirectoryPath))
            {
                Directory.CreateDirectory(zipDirectoryPath);
            } 

            foreach (var file in files) 
            {
                if (!string.IsNullOrWhiteSpace(file?.Name))
                {
                    var destinationFilePath = $"{zipDirectoryPath}\\{file.Name}";
                    File.Copy(file.FullName, destinationFilePath, true); 
                }
            }
             
            var zipFileName = $"{Directory.GetCurrentDirectory()}\\logs.zip";

            if(File.Exists(zipFileName))
            {
                File.Delete(zipFileName); 
            }

            await Task.Delay(100);
            ZipFile.CreateFromDirectory(tempDirectorName, zipFileName,CompressionLevel.SmallestSize, true);
            await Task.Delay(100);
            await Context.User.SendFileAsync(zipFileName, "logs");
            await Task.Delay(100);
          
            foreach (var file in new DirectoryInfo(zipDirectoryPath).GetFiles())
            {
                File.Delete(file.FullName);
            }
              
            Directory.Delete(zipDirectoryPath);
            File.Delete(zipFileName);
        } 
    }
}
