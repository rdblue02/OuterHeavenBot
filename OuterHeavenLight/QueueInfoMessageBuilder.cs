using OuterHeavenLight.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenLight
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
            if(tracks.Count == 0)
            {
                return "Queue is empty";
            }

            var message = "Queue\n";
                message += $"| {Clean("#", 5)} | {Clean("Title", 20)} | {Clean("Author", 20)} | {Clean("Source", 20)} |";
           
            for (int i = 0; i < tracks.Count; i++)
            {
                message += $"| {Clean($"{i})", 5)} | {Clean(tracks[i].info.title, 20)} | {Clean(tracks[i].info.author, 20)} | {Clean(tracks[i].info.sourceName, 20)} |\n";
            }

            return message.TrimEnd();
        }

        private string Clean(string message, int columnWidth)
        {
           message = message.Trim();
           message = message.Length > columnWidth ? message.Substring(0, columnWidth) + "..." : message.PadRight(columnWidth + 3, ' ');
           
            return message;
        }
    }
}
