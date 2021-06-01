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
        /// <summary>
        /// Decompresses the string.
        /// </summary>
        /// <param name="compressedText">The compressed text.</param>
        /// <returns></returns>
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
