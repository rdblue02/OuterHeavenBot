using OuterHeavenBot.Core;
using OuterHeavenBot.Lavalink;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenBot.Client.Commands.Handlers
{
    public class LavalinkSearchHandler : ISearchHandler<LavalinkTrack>
    {
        private LavalinkNode lavalinkNode;
        private ILogger<LavalinkSearchHandler> logger;

        public LavalinkSearchHandler(LavalinkNode lavalinkNode, ILogger<LavalinkSearchHandler> logger)
        {
            this.lavalinkNode = lavalinkNode;
            this.logger = logger;
        }

        public async Task<IEnumerable<LavalinkTrack>> SearchAsync(string query)
        { 
            var searchType = GetSearchType(query);

            var result = await this.lavalinkNode.LoadTrackAsync(query, searchType);
             
            if (result is LavalinkLoadFailedType failed)
            {
                logger.LogError($"Error: {failed.Data.Message} | Cause: {failed.Data.Cause}");
                return [];
            }

            if (result is LavalinkSearchLoadedType searchResult)
            { 
                var bestTrack = FindBestTrack(query, searchResult.Tracks);

                return bestTrack != null ? [bestTrack] : []; 
            }

            if (result is LavalinkTrackLoadedType loadedResult)
            {
                return [loadedResult.Data]; 
            }

            if (result is LavalinkPlaylistLoadedData plLoaded)
            {
                return plLoaded.Tracks;
            }

            return [];
        }

        public IEnumerable<LavalinkTrack> Search(string query)
        {
           return SearchAsync(query).GetAwaiter().GetResult();
        }

        LavalinkSearchType GetSearchType(string query)
        {

            var isUrl = Uri.TryCreate(query, UriKind.Absolute, out var uri) || query.Trim().ToLower().StartsWith("http");

            if (isUrl)
            {
                return LavalinkSearchType.Raw;
            }

            if (query.ToLower().Contains("soundcloud"))
                return LavalinkSearchType.Soundcloud;

            return LavalinkSearchType.Youtube;
        }

        //todo - find best track for multiple results
        LavalinkTrack? FindBestTrack(string query, IEnumerable<LavalinkTrack> tracks)
        {
            return tracks.FirstOrDefault();
           // return tracks?.OrderByDescending(x => AssignPriority(query, x)).FirstOrDefault();
        }
 
    }
}
