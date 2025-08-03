using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenLight.Dev
{
    public interface IFileHandler
    {
        string? ZipSubDirectory(DirectoryInfo sourceDirectory, DirectoryInfo destinationDirectory, string zipFileName);
        DirectoryInfo ImportFiles(List<FileInfo> files, DirectoryInfo destinationDirectory);
        DirectoryInfo GetWorkingDirectory();
    }
}
