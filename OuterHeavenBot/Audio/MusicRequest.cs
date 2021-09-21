using CliWrap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using YoutubeExplode.Search;

namespace OuterHeavenBot.Audio
{
    class MusicRequest : IAudioRequest
    {
        public string Name { get; set; }
        public Stream MusicStream { get; set; }
        public async Task<byte[]> GetAudioBytes()
        {
            if (MusicStream == null)
            {
                throw new ArgumentNullException(nameof(MusicStream));
            }
            using (MusicStream)
            {
                var ms = new MemoryStream();
                await Cli.Wrap("ffmpeg")
                    .WithArguments(" -hide_banner -loglevel panic -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1")
                    .WithStandardInputPipe(PipeSource.FromStream(MusicStream))
                    .WithStandardOutputPipe(PipeTarget.ToStream(ms))
                    .ExecuteAsync();
                return ms.ToArray();
            }
        } 
    }
}
