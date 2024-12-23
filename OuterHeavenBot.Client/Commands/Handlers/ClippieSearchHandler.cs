using OuterHeavenBot.Core;
using OuterHeavenBot.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenBot.Client.Commands.Handlers
{
    public class ClippieSearchHandler : ISearchHandler<ClippieFileData>
    {
        private readonly ILogger<ClippieSearchHandler> logger;
        private readonly List<string> excludedDirectories;
        private readonly AppSettings appSettings;
        private static readonly Random random = new Random(DateTime.Now.Second);
        private string clippieDirecotryPath;
        public ClippieSearchHandler(ILogger<ClippieSearchHandler> logger,
                                    AppSettings appSettings)
        {
            this.logger = logger;
            this.appSettings = appSettings;
            var clippieDirecotryName = appSettings.ClippieBotSettings?.SoundFileDirectory?.Trim('\\')?.Trim() ?? throw new ArgumentNullException(nameof(AppSettings.ClippieBotSettings.SoundFileDirectory));
            this.clippieDirecotryPath = Path.Combine(Directory.GetCurrentDirectory(), clippieDirecotryName);
            this.excludedDirectories = [.. appSettings.ClippieBotSettings?.ExcludedSubDirectories];

            logger.LogInformation($"Using local path {clippieDirecotryPath} to search for clippies");

            if (!Directory.Exists(clippieDirecotryPath))
            {
                logger.LogInformation($"Clippie path [{clippieDirecotryPath}] does not exist. Creating empty directory");
                Directory.CreateDirectory(clippieDirecotryPath);
            }
        }

        public IEnumerable<ClippieFileData> Search(string query)
        {
            logger.LogInformation($"Using local path {clippieDirecotryPath} to search for clippies");

            if (!Directory.Exists(clippieDirecotryPath))
            {
                logger.LogInformation($"Clippie path [{clippieDirecotryPath}] does not exist. Creating empty directory");
                Directory.CreateDirectory(clippieDirecotryPath);
            }

            var files = new DirectoryInfo(clippieDirecotryPath).GetDirectories()
                                                              .Where(directory => !excludedDirectories.Any(excludedDirectoryName => string.Equals(directory.Name.Trim(),
                                                                                                                                                  excludedDirectoryName.Trim(),
                                                                                                                                                  StringComparison.InvariantCultureIgnoreCase)))
                                                              .SelectMany(x => x.GetFiles())
                                                              .Select(x => new ClippieFileData() { FullName = x.FullName, Name = x.Name.Replace(x.Extension, "") })
                                                              .ToList();

            var matches = FindClosestMatch(files, query);

            if (matches.Count > 0)
            {
                matches[0].Data = File.ReadAllBytes(matches[0].FullName);
            }

            return matches;
        }

        public async Task<IEnumerable<ClippieFileData>> SearchAsync(string query)
        {
            var files = new DirectoryInfo(clippieDirecotryPath).GetDirectories()
                                                               .Where(directory => !excludedDirectories.Any(excludedDirectoryName => string.Equals(directory.Name.Trim(),
                                                                                                                                                  excludedDirectoryName.Trim(),
                                                                                                                                                  StringComparison.InvariantCultureIgnoreCase)))
                                                               .SelectMany(x => x.GetFiles())
                                                               .Select(x => new ClippieFileData() { FullName = x.FullName, Name = x.Name.Replace(x.Extension, "") })
                                                               .ToList();

            var matches = FindClosestMatch(files, query);

            if (matches.Count > 0)
            {
                matches[0].Data = await File.ReadAllBytesAsync(matches[0].FullName);
            }

            return matches;
        }

        private List<ClippieFileData> FindClosestMatch(List<ClippieFileData> clippieFiles, string query)
        {
            if (clippieFiles.Count == 0) return clippieFiles;

            if (string.IsNullOrEmpty(query))
            {
                return [clippieFiles[random.Next(0, clippieFiles.Count - 1)]];
            }

            var match = clippieFiles.FirstOrDefault(x => string.Equals(x.Name, query, StringComparison.OrdinalIgnoreCase));

            if (match == null)
            {
                match = clippieFiles.FirstOrDefault(x => x.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase));
            }

            if (match != null)
            {
                match.Data = File.ReadAllBytes(query);
            }

            return [match];
        }

    }
}
