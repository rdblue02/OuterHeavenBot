using Discord;
using Discord.WebSocket;
using OuterHeavenLight.Constants;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenLight.Dev
{
    public class DiscordFileSender : IDiscordFileSender
    { 
        private readonly ILogger<IDiscordFileSender> logger;
        private readonly IFileHandler fileHandler; 
        public DiscordFileSender(IFileHandler fileHandler, 
                                 ILogger<IDiscordFileSender> logger)
        {
            this.logger = logger;
            this.fileHandler = fileHandler; 
        }

        public FileInfo? StageZipFile(List<FileInfo> filesToSend, string zipFileName)
        {
            logger.LogInformation($"Staging {filesToSend.Count} files");

            var workingDirectory = fileHandler.GetWorkingDirectory();

            var tempDirectory = this.fileHandler.ImportFiles(filesToSend, new(Path.Combine(workingDirectory.FullName, BotResourceName.TempDirectory)));

            zipFileName = ensureZipFileExtension(zipFileName);
            var zipFilePath = this.fileHandler.ZipSubDirectory(tempDirectory, workingDirectory, zipFileName);

            if (string.IsNullOrWhiteSpace(zipFilePath) ||
                !File.Exists(zipFilePath))  
            {
                logger.LogError($"Error zipping file {zipFileName}");
                return null;
            }

            logger.LogInformation($"Zip file {zipFilePath} created successfully. Deleting temp directory.");
            tempDirectory.Delete(true);
            return new FileInfo(zipFilePath);
        }

        public async Task SendZipFileAsync(SocketUser user, FileInfo zipFileInfo)
        {
            await user.SendFileAsync(zipFileInfo.FullName);
            //Delay for a short time to ensure the handle for the zip file has been released by windows.
            //Not sure if this is needed.
            await Task.Delay(500);

            logger.LogInformation($"File send complete. Deleting zip file {zipFileInfo.FullName}");
            zipFileInfo.Delete();
        }

        private string ensureZipFileExtension(string name)
        { 
            if(!name.EndsWith(".zip"))
            {
                return name + ".zip";
            } 
            return name;
        }
    }
}
