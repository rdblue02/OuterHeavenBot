using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenBot.Audio
{
    class ClippieRequest : IAudioRequest
    {
        public string Name { get; set; }

        public string ContentPath { get; set; }

        public async Task<byte[]> GetAudioBytes()
        {
            if (string.IsNullOrWhiteSpace(ContentPath))
            {
                throw new ArgumentNullException(nameof(ContentPath));
            }
            var fileBites = await File.ReadAllBytesAsync(ContentPath);
            using(var ms = new MemoryStream())
            {
                await ms.WriteAsync(fileBites);
                return ms.ToArray();
            }
        }
    }
}
