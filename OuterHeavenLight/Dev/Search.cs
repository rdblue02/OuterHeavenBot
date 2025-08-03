using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenLight.Dev
{
    public class Search : ISearch
    {
        //directory to start the search from
        public string BaseDirectory => Path.GetDirectoryName(Assembly.GetCallingAssembly()?.Location) ?? Environment.CurrentDirectory;

        public DirectoryInfo? FindDirectory(string directoryName, int maxDirectoryHeight = 0, int recursionDepth = 0)
        {
            // skip lookup logic if the directory name is an absolute path
            if (Path.IsPathFullyQualified(directoryName))
            {
                return Directory.Exists(directoryName) ? new(directoryName) : null;
            }

            var baseDirectoryInfo = new DirectoryInfo(BaseDirectory);

            //quick check for an exact match.     
            if (Directory.Exists(Path.Combine(baseDirectoryInfo.FullName, directoryName)))
            {
                return new(Path.Combine(baseDirectoryInfo.FullName, directoryName));
            }

            var searchOptions = new EnumerationOptions()
            {
                IgnoreInaccessible = true,
                MatchType = MatchType.Simple,
                MatchCasing = MatchCasing.PlatformDefault,
                RecurseSubdirectories = true,
                ReturnSpecialDirectories = false,
                MaxRecursionDepth = recursionDepth
            };

            int maxAttempts = maxDirectoryHeight;
            var match = baseDirectoryInfo.GetDirectories($"*{directoryName}*", searchOptions)
                                                    .FirstOrDefault(x => x.Name == directoryName);
            while (match == null &&
                   maxAttempts > 0 &&
                   baseDirectoryInfo?.Parent != null)
            {
                baseDirectoryInfo = baseDirectoryInfo.Parent;
                maxAttempts--;

                match = baseDirectoryInfo.GetDirectories($"*{directoryName}*", searchOptions)
                                               .FirstOrDefault(x => x.Name == directoryName);
            }

            return match;
        }

        public FileInfo? FindFile(string simpleFileName, int maxDirectoryHeight = 0, int recursionDepth = 0, string? baseDirectoryOverride = null)
        {
            // skip lookup logic if the file name is an absolute path
            if (Path.IsPathFullyQualified(simpleFileName))
            {
                return File.Exists(simpleFileName) ? new(simpleFileName) : null;
            }

            var baseDirectoryInfo = !string.IsNullOrWhiteSpace(baseDirectoryOverride) ? new DirectoryInfo(baseDirectoryOverride) : new DirectoryInfo(BaseDirectory);

            if(!baseDirectoryInfo.Exists)
            {
                throw new InvalidOperationException($"Invalid base directory {baseDirectoryOverride}");
            }

            //quick check for an exact match.     
            if (File.Exists(Path.Combine(baseDirectoryInfo.FullName, simpleFileName)))
            {
                return new(Path.Combine(baseDirectoryInfo.FullName, simpleFileName));
            }

            var searchOptions = new EnumerationOptions()
            {
                IgnoreInaccessible = true,
                MatchType = MatchType.Simple,
                MatchCasing = MatchCasing.PlatformDefault,
                RecurseSubdirectories = true,
                ReturnSpecialDirectories = false,
                MaxRecursionDepth = recursionDepth
            };

            int maxAttempts = maxDirectoryHeight;
            var match = baseDirectoryInfo.GetFiles($"*{simpleFileName}*", searchOptions)
                                          .FirstOrDefault(x => x.Name == simpleFileName);
            while (match == null &&
                   maxAttempts > 0 &&
                   baseDirectoryInfo?.Parent != null)
            {
                baseDirectoryInfo = baseDirectoryInfo.Parent;
                maxAttempts--;

                match = baseDirectoryInfo.GetFiles($"*{simpleFileName}*", searchOptions)
                                         .FirstOrDefault(x => x.Name == simpleFileName);
            }

            return match;
        }
    }
}
