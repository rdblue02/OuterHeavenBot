using OuterHeavenLight.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenLight.Utilities
{
    public class QueueInfoMessageBuilder
    {
        private List<LavaTrack> tracks;

        public QueueInfoMessageBuilder(List<LavaTrack> tracks)
        {
            this.tracks = tracks ?? [];
        }

        public string Build()
        {
            if (tracks.Count == 0)
            {
                return "Queue is empty";
            }

            var message = "Queue\n";
            var totalTime = TimeSpan.Zero;

            for (int i = 0; i < tracks.Count; i++)
            {
                var trackDuration = TimeSpan.FromMilliseconds(tracks[i].info.length);

                if (i == 0)
                {
                    var timeRemaining =  tracks[i].info.length - tracks[i].info.position; 
                    trackDuration = TimeSpan.FromMilliseconds(timeRemaining);
                    message += $"Current - {Clean(tracks[i].info.title, 3)} - {Clean(tracks[i].info.author, 30)} - {Clean(tracks[i].info.sourceName, 20)} - {Clean(trackDuration.ToString((@"hh\:mm\:ss")), 10)}\n\n";
                }
                else
                { 
                    message += $"{Clean($"#{i}", 3)} - {Clean(tracks[i].info.title, 30)} - {Clean(tracks[i].info.author, 30)} - {Clean(trackDuration.ToString((@"hh\:mm\:ss")), 10)}\n";
                }

                totalTime += trackDuration;
            }
            message += $"Total Time Remaining - {totalTime:hh\\:mm\\:ss}";

            return message.TrimEnd();
        }

        private string Clean(string message, int columnWidth)
        {
            message = message.Trim(); 
            message = message.Length > columnWidth ? message.Substring(0, columnWidth) + "..." : message.PadRight(columnWidth + 1, ' ');
            message = " " + message;
            return message;
        }
    }
}
