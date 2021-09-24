using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenBot
{
    public static class Helpers
    {
        public static  Dictionary<string, List<FileInfo>> GetAudioFiles()
        {
            var directoryFileList = new Dictionary<string, List<FileInfo>>();
            var directories = new DirectoryInfo(Directory.GetCurrentDirectory() + "\\audio").GetDirectories();
            foreach (var directory in directories)
            {
                var fileNames = directory.GetFiles().ToList();
                directoryFileList.Add(directory.Name, fileNames);
            }
            return  directoryFileList;
        }

        public static string DecompressStringFromStream(Stream compressed)
        {
            
            byte[] gZipBuffer = new byte[(int)compressed.Length];

            using (var gZipStream = new GZipStream(compressed, CompressionMode.Decompress))
            {
                gZipStream.Read(gZipBuffer, 0, gZipBuffer.Length);
            }

            return Encoding.UTF8.GetString(gZipBuffer);
        }
    }
}
