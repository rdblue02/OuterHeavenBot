using Discord;
using Discord.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenLight.Clippies
{
    public static class ClippieHelpers
    {
       public readonly static Random ClippieRandomizer = new Random((int)DateTime.Now.Ticks);

        public static byte[] ReadClippieFile(string contentName, string clippieDirectory, List<string> exclusions)
        {
            var fileDirectories = GetAudioFiles(clippieDirectory, exclusions);
            if(!fileDirectories.Any())
            {
                return [];
            }

            string? clippie = "";
           
            //no category or content specified. Play anything
            contentName = contentName?.ToLower()?.Trim() ?? "";

            if (string.IsNullOrEmpty(contentName))
            {
                var allAudio = fileDirectories.SelectMany(x => x.Value).ToList();
                clippie = allAudio[ClippieRandomizer.Next(0, allAudio.Count - 1)].FullName;
            }
            else if (fileDirectories.ContainsKey(contentName))
            {
                clippie = fileDirectories[contentName][ClippieRandomizer.Next(0, fileDirectories[contentName].Count - 1)].FullName;
            }
            else 
            {
                var allAudio = fileDirectories.SelectMany(x => x.Value).ToList();
              
                clippie = allAudio.FirstOrDefault(x=>x.Name == contentName || 
                                                     x.FullName.ToLower() == contentName || 
                                                     x.Name.ToLower().Replace(x.Extension,"") == contentName ||
                                                     x.Name.ToLower().Contains(contentName))?.FullName;
            }
            return string.IsNullOrEmpty(clippie) ? Array.Empty<byte>() : File.ReadAllBytes(clippie);
        }        
       
        public static Dictionary<string, List<FileInfo>> GetAudioFiles(string clippieDirectory, List<string> exclusions)
        { 
            if(string.IsNullOrWhiteSpace(clippieDirectory))
            { 
                clippieDirectory = Directory.GetCurrentDirectory() + "\\clips";
            }

            if(!Directory.Exists(clippieDirectory))
            {
                Directory.CreateDirectory(clippieDirectory);
            }

            var directoryFileList = new Dictionary<string, List<FileInfo>>();
            var directories = new DirectoryInfo(clippieDirectory).GetDirectories().Where(x => exclusions
                                                                                  .All(y=> !y.Trim().Equals(x.Name, StringComparison.CurrentCultureIgnoreCase)));
            foreach (var directory in directories)
            {
                var fileNames = directory.GetFiles().ToList();
                directoryFileList.Add(directory.Name.ToLower(), fileNames);
            }

            return directoryFileList;
        }
    }
}
