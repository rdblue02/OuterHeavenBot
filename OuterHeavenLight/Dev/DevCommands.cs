using Discord.Commands;
using OuterHeavenLight.Music;
using System.Reflection;
using System.Linq;
using Discord;
using System.IO.Compression;

namespace OuterHeavenLight.Dev
{
    public class DevCommands : ModuleBase<SocketCommandContext>
    {
        ILogger<DevCommands> logger;
        public DevCommands(ILogger<DevCommands> logger)
        {
            this.logger = logger;
        }

        [Command("logs", RunMode = RunMode.Async)]
        public async Task GetLogs()
        {
            var executingFile = Assembly.GetEntryAssembly()?.Location;

            var workingDirectory = executingFile != null ? new FileInfo(executingFile)?.DirectoryName ?? Directory.GetCurrentDirectory() : Directory.GetCurrentDirectory();

            var logGroups = new DirectoryInfo(workingDirectory).GetFiles("*log");
           
        }

        private async Task SendApplicationLogFile(string currentDirectory) 
        {

            var logDirectory = new DirectoryInfo(currentDirectory).GetDirectories().FirstOrDefault(x => x.Name == "logs");
            if (logDirectory == null) { return; }

            var logFiles = logDirectory.GetFiles().OrderByDescending(x => x.CreationTime).ToList();
            var file = logFiles.FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(file?.Name))
            {
                var zipDirectoryName = file.DirectoryName + "\\log_files";
                DirectoryInfo zipDirectory = !Directory.Exists(zipDirectoryName) ? Directory.CreateDirectory(zipDirectoryName) :
                                                                                   new DirectoryInfo(zipDirectoryName);

                var zipFileName = $"{file.DirectoryName}\\{zipDirectory.Name}.zip";
                File.Copy(file.FullName, $"{zipDirectory.FullName}\\{file.Name}", true);

                ZipFile.CreateFromDirectory(zipDirectory.FullName, zipFileName);

                await Context.User.SendFileAsync(zipFileName, "logs");
                await Task.Delay(100);

                File.Delete($"{zipDirectory.FullName}\\{file.Name}");
                File.Delete(zipFileName);
                Directory.Delete(zipDirectoryName);
            }
        }

        private async Task SendLavalinkLogFile(string currentDirectory)
        {
            if (!Directory.Exists(currentDirectory)) { return ; }

            var directoryInfo = new DirectoryInfo(currentDirectory);

            while (directoryInfo != null &&
                  !directoryInfo.GetDirectories().Any(x => x.Name.ToLower().Trim() == "lavalink"))
            {
                directoryInfo = directoryInfo.Parent;
            }

            if (directoryInfo == null)
            {
                return;
            }
            var logDirectory = directoryInfo.GetDirectories().FirstOrDefault(x => x.Name == "logs");
            if (logDirectory == null) { return; }

            var file = logDirectory.GetFiles().OrderByDescending(x => x.CreationTime).FirstOrDefault();
            if(!string.IsNullOrWhiteSpace(file?.Name))
            {
                var zipDirectoryName = file.DirectoryName + "\\log_files";
                DirectoryInfo zipDirectory = !Directory.Exists(zipDirectoryName) ? Directory.CreateDirectory(zipDirectoryName) :
                                                                                   new DirectoryInfo(zipDirectoryName);

                var zipFileName = $"{file.DirectoryName}\\{zipDirectory.Name}.zip";
                File.Copy(file.FullName, $"{zipDirectory.FullName}\\{file.Name}", true);

                ZipFile.CreateFromDirectory(zipDirectory.FullName, zipFileName);

                await Context.User.SendFileAsync(zipFileName, "logs");
                await Task.Delay(100);

                File.Delete($"{zipDirectory.FullName}\\{file.Name}");
                File.Delete(zipFileName);
                Directory.Delete(zipDirectoryName);
            } 
        }
    }
}
