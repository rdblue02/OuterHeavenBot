using Discord.Commands;
using OuterHeavenLight.Music;
using System.Reflection;
using System.Linq;
using Discord;
using System.IO.Compression;
using OuterHeaven.LavalinkLight;
using System.Runtime.Serialization;

namespace OuterHeavenLight.Dev
{
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
            var logFiles = new List<FileInfo?>();
            var parentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory())?.Parent ?? new DirectoryInfo(Directory.GetCurrentDirectory());
         
            var lavalog = parentDirectory?.GetDirectories(appsettings.AppLogDirectory, SearchOption.AllDirectories)
                                          .FirstOrDefault()?
                                          .GetFiles(lavalinkLogName)?
                                          .OrderByDescending(x => x.CreationTime)?
                                          .FirstOrDefault();

            var applog = parentDirectory?.GetDirectories(appsettings.AppLogDirectory, SearchOption.AllDirectories)?
                                         .FirstOrDefault()?
                                         .GetFiles(appLogName)?
                                         .OrderByDescending(x => x.CreationTime)?
                                         .FirstOrDefault();

            if (lavalog != null) 
            {
                logFiles.Add(lavalog);
            }

            if (applog != null) 
            {
               logFiles.Add(applog);
            }

            return logFiles;
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
