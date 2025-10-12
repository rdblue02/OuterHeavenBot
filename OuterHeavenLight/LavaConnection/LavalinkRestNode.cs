using Discord;
using Discord.Commands;
using Microsoft.Extensions.Logging;
using OuterHeavenLight.Constants;
using OuterHeavenLight.Entities;
using OuterHeavenLight.Entities.Request;
using OuterHeavenLight.Entities.Response.Rest;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Web;


namespace OuterHeavenLight.LavaConnection
{
    public class LavalinkRestNode
    {
        private LavalinkEndpoint restEndpoint;
        private HttpClient _httpClient;
        private ILogger<LavalinkRestNode> logger;
        public LavalinkRestNode(ILogger<LavalinkRestNode> logger)
        {
            this.logger = logger;
            restEndpoint = LavalinkEndpoint.ToRestEndpoint;
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

        public async Task<LavaDataLoadResult> SearchForTracks(string queryRaw, LavalinkSearchType searchType, bool loadPlaylist)
        {
            var queryUrl = BuildSearchUrl(queryRaw, searchType);
            using (var res = await SendMessage(queryUrl))
            {
                if (!res.IsSuccessStatusCode)
                {
                    return FailResult($"Received Error http status code ({res.StatusCode})",
                                      await (res.Content?.ReadAsStringAsync() ?? Task.FromResult("")),
                                      $"Failed to resolve tracks: {res.StatusCode}");
                }

                var jsonString = await (res.Content?.ReadAsStringAsync() ?? Task.FromResult(""));
                var json = JsonNode.Parse(jsonString);

                if (json == null)
                {
                    return FailResult("Unkown",
                                      $"Unable to parse json content. Json:  {json ?? "null"})",
                                      "fault");
                }

                var loadTypeString = json["loadType"]?.GetValue<string>();

                if (!Enum.TryParse<LavalinkLoadType>(loadTypeString, out var loadType))
                {
                    return FailResult("Unkown",
                                      $"Unable to parse json content Invalid load type {loadTypeString ?? "null"}.Json:  {json ?? "null"})",
                                      "fault");
                }

                return BuildLoadResult(loadType, json, loadPlaylist);
            }
        }

        public async Task<LavaPlayer> UpdatePlayer(string guildId, string sessionId, PlayerUpdateRequest payload, bool noReplace = false)
        {
            var builder = new UriBuilder(new Uri(string.Format(LavalinkRestUrl.PLAYER, _httpClient.BaseAddress, sessionId, guildId)));
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["noReplace"] = noReplace.ToString().ToLower();

            builder.Query = query.ToString();
            var requestJson = JsonSerializer.Serialize(payload);

            using (var res = await SendMessage(builder.ToString(), "PATCH", requestJson))
            {
                var responseJson = await res.Content.ReadAsStringAsync();

                logger.LogInformation($"received the following json as a response {responseJson}");

                if (string.IsNullOrWhiteSpace(responseJson) || !res.IsSuccessStatusCode)
                {
                    throw new Exception("Failed to update player: No response");
                }

                return JsonSerializer.Deserialize<LavaPlayer>(responseJson) ?? throw new Exception($"Cannot deserialize player {responseJson}");
            }
        }

        internal async Task DestroyPlayer(string guildId, string sessionId)
        {
            using (var res = await SendMessage(string.Format(LavalinkRestUrl.PLAYER, _httpClient.BaseAddress, sessionId, guildId), "DELETE"))
            {
                if (!res.IsSuccessStatusCode)
                {
                    logger.LogError($"Error destroying player in guild {guildId} for session {sessionId}");
                }
            }
        }

        LavaDataLoadResult BuildLoadResult(LavalinkLoadType loadType, JsonNode json, bool loadPlaylist)
        {
            var result = new LavaDataLoadResult()
            {
                LoadType = loadType,
                LoadedTracks = [],
                PlaylistInfo = new PlaylistInfo(),
                TrackException = null
            };

            switch (loadType)
            {
                case LavalinkLoadType.track:
                    var track = json.Deserialize<TrackLoaded>()?.Track;
                    if (track != null)
                    {
                        result.LoadedTracks.Add(track);
                    }
                    break;
                case LavalinkLoadType.playlist:

                    var playList = json.Deserialize<PlaylistLoaded>()?.Playlist;
                    var playListTracks = playList?.Tracks?.ToList() ?? [];

                    if (!loadPlaylist &&
                        playListTracks.Count > 0)
                    {
                        var selectedTrack = playListTracks[playList?.Info?.SelectedTrackIndex ?? 0];
                        if (selectedTrack != null)
                        {
                            result.LoadedTracks.Add(selectedTrack);
                        }
                    }
                    else
                    {
                        result.LoadedTracks = playListTracks.Count > 20 ? [.. playListTracks.Take(20)] : playListTracks;
                        result.PlaylistInfo = playList?.Info ?? new PlaylistInfo();
                    }
                    break;
                case LavalinkLoadType.search:

                    var searchedTrack = json.Deserialize<SearchLoaded>()?.Tracks?.FirstOrDefault();
                    if (searchedTrack != null)
                    {
                        result.LoadedTracks.Add(searchedTrack);
                    }
                    break;
                case LavalinkLoadType.error:
                    result.TrackException = json.Deserialize<LoadError>()?.Exception ?? 
                                            FailResult("Unknown",
                                            $"Received and error response when loading tracks but cannot parse it. Json content {json ?? "null"})",
                                            "fault").TrackException; 
                    break;
            }

            return result;
        }

        LavaDataLoadResult FailResult(string cause, string message, string severity, string? logMessage = null)
        {
            if (!string.IsNullOrWhiteSpace(logMessage)) logger.LogError(logMessage);

            return new LavaDataLoadResult()
            {
                TrackException = new LavaTrackException()
                {
                    Cause = cause,
                    Message = message,
                    Severity = severity
                }
            };
        }

        private string BuildSearchUrl(string query, LavalinkSearchType searchType = LavalinkSearchType.ytsearch)
        {
            var prefix = searchType switch
            {
                LavalinkSearchType.ytsearch => "ytsearch:",
                LavalinkSearchType.scsearch => "scsearch:",
                LavalinkSearchType.Raw => "",
                _ => throw new ArgumentOutOfRangeException(nameof(searchType), searchType, null)
            };

            query = $"{prefix}{query}";
            var builder = new UriBuilder(new Uri(string.Format(LavalinkRestUrl.TRACK_RESOLVE, _httpClient.BaseAddress)));
            var queryBuilder = HttpUtility.ParseQueryString(builder.Query);

            queryBuilder["identifier"] = query;

            builder.Query = queryBuilder.ToString();
            return builder.ToString();
        }

        async Task<HttpResponseMessage> SendMessage(string url, string method = "GET", string? jsonContent = null)
        {
            var req = new HttpRequestMessage
            {
                Method = new HttpMethod(method),
                RequestUri = new Uri(url)
            };
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                logger.LogInformation($"Sending a message to {url}");
            }
            else
            {
                logger.LogInformation($"Sending a message to {url}\nMessage content:\n{jsonContent}");
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                req.Content = content;
            }

            var res = await _httpClient.SendAsync(req);
            return res != null ? res : new HttpResponseMessage(HttpStatusCode.NotFound);
        }
    }
}
