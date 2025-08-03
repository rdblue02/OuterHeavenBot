using OuterHeaven.LavalinkLight;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenLight.Dev
{
    public class FileHandler : IFileHandler
    {
        public DirectoryInfo GetWorkingDirectory() => new(workingDirectory.FullName);
        private DirectoryInfo workingDirectory;

        public FileHandler(AppSettings appSettings)
        {
            this.workingDirectory = !string.IsNullOrWhiteSpace(appSettings.AppLogDirectoryPath) ?
                                     new DirectoryInfo(appSettings.AppLogDirectoryPath) : new(getDefaultWorkingDirectory());
        }

        public FileHandler()
        {
            this.workingDirectory = new(getDefaultWorkingDirectory());
        }

        public DirectoryInfo ImportFiles(List<FileInfo> files, DirectoryInfo destinationDirectory)
        {  
            if(!destinationDirectory.Exists)
            {
               destinationDirectory.Create();
            }

            foreach (var file in files)
            {
                File.Copy(file.FullName, Path.Combine(destinationDirectory.FullName, file.Name), true);
            }

            return destinationDirectory;
        }

        public string? ZipSubDirectory(DirectoryInfo sourceDirectory, DirectoryInfo destinationDirectory, string zipFileName)
        { 
            var zipFileFullName = GetZipFileFullName(zipFileName, destinationDirectory.FullName);

            if (File.Exists(zipFileFullName))
            {
                File.Delete(zipFileFullName);
            }

            ZipFile.CreateFromDirectory(sourceDirectory.FullName, zipFileFullName, CompressionLevel.SmallestSize, false);
            return zipFileFullName;
        }
         
        private string GetZipFileFullName(string zipResultFileName, string destinationDirectory)
        {
            if (!zipResultFileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                zipResultFileName = zipResultFileName + ".zip";
            }

            var zipFileFullName = Path.Combine(destinationDirectory, zipResultFileName);

            return zipFileFullName;
        }

        string getDefaultWorkingDirectory() =>
         Path.GetDirectoryName(this.GetType().Assembly?.Location) ?? Directory.GetCurrentDirectory();
    }
}
