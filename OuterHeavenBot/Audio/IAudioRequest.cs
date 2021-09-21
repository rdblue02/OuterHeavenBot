using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenBot.Audio
{
    public interface IAudioRequest
    {
        public string Name { get; set; }
        Task<byte[]> GetAudioBytes();   
    }
}
