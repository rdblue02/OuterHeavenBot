
namespace OuterHeavenLight.Dev
{
    public interface ISearch
    {
        string BaseDirectory { get; }

        DirectoryInfo? FindDirectory(string directoryName, int maxDirectoryHeight = 0 , int recursionDepth = 0);
        FileInfo? FindFile(string simpleFileName, int maxDirectoryHeight = 0, int recursionDepth = 0, string? baseDirectoryOverride = null);
    }
}