using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Web;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Logging;
using OuterHeavenLight.Constants;
using OuterHeavenLight.Entities;
using OuterHeavenLight.Entities.Request;
using OuterHeavenLight.Entities.Response.Rest;


namespace OuterHeaven.LavalinkLight
{
    public class LavalinkRestNode
    {
        private LavalinkEndpoint restEndpoint;
        private HttpClient _httpClient;
        private ILogger<LavalinkRestNode> logger;
        public LavalinkRestNode(LavalinkEndpointProvider endpointProvider, ILogger<LavalinkRestNode> logger)
        {
            this.logger = logger;
            restEndpoint = endpointProvider.RestEndpoint;
            _httpClient = new HttpClient();

            prepareHttpClient();
        }

        private void prepareHttpClient()
        {
            _httpClient.BaseAddress = restEndpoint.ToUri();
            _httpClient.DefaultRequestHeaders.Add("Authorization", restEndpoint.Password);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "DHCPCD9/Nomia");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public async Task<LavaPlayer?> GetPlayerOrDefaultAsync(string guildId, string hostSessionId)
        {
            var builder = new UriBuilder(new Uri(string.Format(LavalinkRestUrl.PLAYER, _httpClient.BaseAddress, hostSessionId, guildId)));
            using var req = new HttpRequestMessage
            {
                Method = new HttpMethod("GET"),
                RequestUri = new Uri(builder.ToString())
            };

            using var res = await _httpClient.SendAsync(req);

            if (!res.IsSuccessStatusCode)
            {
                logger.LogError($"Failed to resolve guild player: {res.StatusCode}");        
                return null;
            }
             
            var json = await res.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<LavaPlayer>(json);

        } 

        public async Task<LavaDataLoadResult> SearchForTracks(string queryRaw, LavalinkSearchType searchType = LavalinkSearchType.ytsearch)
        {
            var result = new LavaDataLoadResult();

            var query = AddQueryPrefix(queryRaw, searchType);

            var builder = new UriBuilder(new Uri(string.Format(LavalinkRestUrl.TRACK_RESOLVE, _httpClient.BaseAddress)));
            var queryBuilder = HttpUtility.ParseQueryString(builder.Query);

            queryBuilder["identifier"] = query;

            builder.Query = queryBuilder.ToString();
            using var req = new HttpRequestMessage
            {
                Method = new HttpMethod("GET"),
                RequestUri = new Uri(builder.ToString())
            };

            using var res = await _httpClient.SendAsync(req);

            var jsonString = await (res.Content?.ReadAsStringAsync() ?? Task.FromResult(""));

            if (!res.IsSuccessStatusCode)
            {
                logger.LogError($"Failed to resolve tracks: {res.StatusCode}");

                _ = Enum.TryParse<HttpStatusCode>(res.StatusCode.ToString(), out HttpStatusCode code);
                result.TrackException = new LavaTrackException()
                {
                    Cause = $"Received Error http status code {res.StatusCode} ({code})",
                    Message = jsonString,
                    Severity = "fault"
                };
                return result;
            }

            var json = JsonNode.Parse(jsonString);
            if(json == null)
            {
                result.TrackException = new LavaTrackException()
                {
                    Cause = $"Unkown",
                    Message = $"Unable to parse json content. Json:  {json ?? "null"})",
                    Severity = "fault"
                };
                return result;
            }
             
            var loadTypeString = json["loadType"]?.GetValue<string>();
            var validLoadType = Enum.TryParse<LavalinkLoadType>(loadTypeString, out var loadType); 

            if (!validLoadType)
            {
                result.TrackException = new LavaTrackException()
                {
                    Cause = $"Unkown",
                    Message = $"Unable to parse json content Invalid load type {loadTypeString ?? "null"}.Json:  {json ?? "null"})",
                    Severity = "fault"
                };

                return result;
            }

            result.LoadType = loadType;

            if (result.LoadType == LavalinkLoadType.track)
            {
                var track = json.Deserialize<TrackLoaded>()?.Track;
                if (track != null)
                {
                    result.LoadedTracks.Add(track);
                }
            }
            else if (result.LoadType == LavalinkLoadType.playlist)
            {
                var playList = json.Deserialize<PlaylistLoaded>()?.Playlist;
                result.LoadedTracks = playList?.Tracks?.ToList() ?? [];
                result.PlaylistInfo = playList?.Info ?? new PlaylistInfo();
            }
            else if (result.LoadType == LavalinkLoadType.search)
            {
                result.LoadedTracks = json.Deserialize<SearchLoaded>()?.Tracks ?? [];
            }
           
            else if (result.LoadType == LavalinkLoadType.error)
            {
                result.TrackException = json.Deserialize<LoadError>()?.Exception ?? new LavaTrackException() { Cause = " Unknown", Message = $"Received and error response when loading tracks but cannot parse it. Json content {json ?? "null"})"};
            }
            else
            {
                return result;
            }

            return result;
        }

  
        public async Task<LavaPlayer> UpdatePlayer(string guildId, string sessionId, PlayerUpdateRequest payload, bool noReplace = false)
        {
            var builder = new UriBuilder(new Uri(string.Format(LavalinkRestUrl.PLAYER, _httpClient.BaseAddress, sessionId, guildId)));
            var query = HttpUtility.ParseQueryString(builder.Query);
            if (noReplace)
            {
                query["noReplace"] = "true";
            }

            builder.Query = query.ToString();

            using var req = new HttpRequestMessage
            {
                Method = new HttpMethod("PATCH"),
                RequestUri = new Uri(builder.ToString())
            };

            var requestJson = JsonSerializer.Serialize(payload);

            logger.LogInformation($"Sending the following json to {builder.Query}\n{requestJson}");

            using var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            req.Content = content;

            using var res = await _httpClient.SendAsync(req);
                      
           var responseJson = await res.Content.ReadAsStringAsync();

            logger.LogInformation($"received the following json as a response {responseJson}");
            
            if (string.IsNullOrWhiteSpace(responseJson) || !res.IsSuccessStatusCode)
            {
                throw new Exception("Failed to update player: No response");
            }

             return JsonSerializer.Deserialize<LavaPlayer>(responseJson) ?? throw new Exception($"Cannot deserialize player {responseJson}");
        }

        internal async Task DestroyPlayer(string guildId, string sessionId)
        {
            var builder = new UriBuilder(new Uri(string.Format(LavalinkRestUrl.PLAYER, _httpClient.BaseAddress, sessionId, guildId)));
            var query = HttpUtility.ParseQueryString(builder.Query);

            using var req = new HttpRequestMessage
            {
                Method = new HttpMethod("DELETE"),
                RequestUri = new Uri(builder.ToString())
            };

            using var res = await _httpClient.SendAsync(req);

            if (!res.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to destroy player: {res.StatusCode}");
            }
        }


        private string AddQueryPrefix(string query, LavalinkSearchType searchType = LavalinkSearchType.ytsearch)
        {
            var prefix = searchType switch
            {
                LavalinkSearchType.ytsearch => "ytsearch:",
                LavalinkSearchType.scsearch => "scsearch:",
                LavalinkSearchType.Raw => "",
                _ => throw new ArgumentOutOfRangeException(nameof(searchType), searchType, null)
            };

            return $"{prefix}{query}";
        }

    }
}
