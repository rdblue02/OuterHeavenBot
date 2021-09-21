using CliWrap;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using OuterHeavenBot.Audio;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Search;
using YoutubeExplode.Videos.Streams;

namespace OuterHeavenBot.Modules
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        private Random random;
        private AudioManager audioManager;
        private YoutubeClient youtubeClient;
        public Commands(Random random, AudioManager audioManager, YoutubeClient youtubeClient)
        {
            this.random = random;
            this.audioManager = audioManager;
            this.youtubeClient = youtubeClient;
        }

        [Summary("Lists available commands")]
        [Command("help")]
        [Alias("h")]
        public async Task Help()
        {
            var commandList = new StringBuilder();
            var aliasList = new StringBuilder();
            var descriptionList = new StringBuilder();

            commandList.Append("help" + Environment.NewLine);
            commandList.Append("sounds" + Environment.NewLine);
            commandList.Append("sounds <category>" + Environment.NewLine);
            commandList.Append("play <file name>" + Environment.NewLine);
            commandList.Append("play <category>" + Environment.NewLine);
            commandList.Append("play" + Environment.NewLine);

            aliasList.Append("h" + Environment.NewLine);
            aliasList.Append("s" + Environment.NewLine);
            aliasList.Append("s <category>" + Environment.NewLine);
            aliasList.Append("p <file name>" + Environment.NewLine);
            aliasList.Append("p <category>" + Environment.NewLine);
            aliasList.Append("p" + Environment.NewLine);

            descriptionList.Append("Display help info" + Environment.NewLine);
            descriptionList.Append("Display sound categories" + Environment.NewLine);
            descriptionList.Append("Get all file names for a category" + Environment.NewLine);
            descriptionList.Append("play a file" + Environment.NewLine);
            descriptionList.Append("play a random file" + Environment.NewLine);
            descriptionList.Append("play a random file within a category" + Environment.NewLine);

            EmbedBuilder embedBuilder = new EmbedBuilder()
            {
                Title = "Outer Heaven Bot Help Info",
                Color = Color.LighterGrey,
                Fields = new List<EmbedFieldBuilder>() {
              new EmbedFieldBuilder(){ IsInline= true, Name = "Command", Value= commandList },
              new EmbedFieldBuilder(){ IsInline= true, Name = "Alias",Value = aliasList },
              new EmbedFieldBuilder(){ IsInline= true, Name = "Description",Value= descriptionList },
             },
            };

            await ReplyAsync(null, false, embedBuilder.Build());
        }

        [Command("play", RunMode = RunMode.Async)]
        [Alias("p")]
        public async Task Play(string argument)
        {
            var searchResults = await youtubeClient.Search.GetVideosAsync(argument).FirstOrDefaultAsync();
            if (searchResults != null)
            {
                await audioManager.ConnectForAudio(Context);
                if (audioManager.CurrentRequest == null)
                {
                    await ReplyAsync($"Playing: {searchResults.Title} - {searchResults.Author} - {searchResults.Duration}");
                }
                else
                {
                    await ReplyAsync($"Queued: {searchResults.Title} - {searchResults.Author} - {searchResults.Duration}");
                }

                var streamInfo = (await youtubeClient.Videos.Streams.GetManifestAsync(searchResults.Id)).GetAudioOnlyStreams().GetWithHighestBitrate();
                var stream = await youtubeClient.Videos.Streams.GetAsync(streamInfo);
                var audioRequest = new MusicRequest()
                {
                    Name = searchResults.Title,
                    MusicStream = stream
                };
                await audioManager.QueueSound(audioRequest);
            }
        } 

        [Command("queue", RunMode = RunMode.Async)]
        [Alias("q")]
        public async Task Queue()
        {
            var quedSongs = audioManager.GetQueue();
          
            if (quedSongs.Any())
            {
                EmbedBuilder embedBuilder = new EmbedBuilder()
                {
                    Title = "Song Queue",
                    Color = Color.LighterGrey,
                    Fields = new List<EmbedFieldBuilder>()
                };

                embedBuilder.Fields.Add(new EmbedFieldBuilder() { IsInline = true, Name = "#", Value = string.Join(Environment.NewLine,quedSongs.Select(x => x.Item1).ToList()) });
                embedBuilder.Fields.Add(new EmbedFieldBuilder() { IsInline = true, Name = "Name", Value = string.Join(Environment.NewLine, quedSongs.Select(x => x.Item2).ToList()) });
                await ReplyAsync(null, false, embedBuilder.Build());
            }
            else
            {
                await ReplyAsync("No songs currently in queue");
            }
        }

        [Command("clippie", RunMode = RunMode.Async)]
        [Alias("c")]
        public async Task Clippie(string contentName = null)
        {
            var channel = (Context.User as IGuildUser)?.VoiceChannel;
            if (channel == null) { await Context.Channel.SendMessageAsync("User must be in a voice channel."); return; }

            if (audioManager.PlayingMusic)
            {
                await ReplyAsync("Can't play clippes during music. That would be rude!");
                return;
            }
            var fileDirectories = await GetAudioFiles();
            var availableFiles = fileDirectories.SelectMany(x => x.Value).Select(x => x.FullName).ToList();

            //variable used so we don't mutate the origional argument value
            var pathToContent = "";
            if (string.IsNullOrEmpty(contentName))
            {
                availableFiles = availableFiles.Where(x => !x.Contains("\\music\\")).ToList();
                var index = random.Next(0, availableFiles.Count);
                pathToContent = availableFiles[index];
            }
            else if (fileDirectories.Keys.Contains(contentName.ToLower().Trim()))
            {
                var index = random.Next(0, fileDirectories[contentName].Count);
                pathToContent = fileDirectories[contentName][index].FullName;
            }
            else
            {
                pathToContent = contentName.ToLower().Trim();

                bool matchFound = false;

                foreach (var fileName in availableFiles)
                {
                    //we have an exact match including extension. No need for further checks.
                    if (fileName.ToLower() == pathToContent)
                    {
                        pathToContent = fileName;
                        matchFound = true;
                        break;
                    }

                    if (fileName.LastIndexOf('.') > 0 || fileName.LastIndexOf('\\') > 0)
                    {
                        var friendlyName = fileName.Substring(fileName.LastIndexOf('\\') + 1);
                        var extensionIndex = friendlyName.LastIndexOf('.');
                        friendlyName = friendlyName.Substring(0, extensionIndex).ToLower().Trim();

                        if (friendlyName == pathToContent || friendlyName.Replace("-1", "").Trim() == pathToContent)
                        {
                            pathToContent = fileName;
                            matchFound = true;
                            break;
                        }
                    }
                }
                //no exact match found for literal or friendly file name. Take the next closest one.
                if (!matchFound)
                {
                    pathToContent = availableFiles.FirstOrDefault(x => x.ToLower().Contains(pathToContent));
                }
            }

            if (string.IsNullOrEmpty(pathToContent))
            {
                await ReplyAsync($"No files found for {contentName}");
            }
            else
            {
                await audioManager.ConnectForAudio(Context);
                var clippie = new ClippieRequest()
                {
                    Name = contentName,
                    ContentPath = pathToContent
                };
                await audioManager.QueueSound(clippie);
            }
        }


        [Command("skip", RunMode = RunMode.Async)]
        [Alias("sk")]
        public async Task Skip()
        {
            await ReplyAsync($"Skipping - {audioManager.CurrentRequest.Name}");
            audioManager.RequestSkip();
        }
        //[Command("pause", RunMode = RunMode.Async)]
        //[Alias("pa")]
        //public async Task Pause()
        //{
        //    if (audioManager.Paused)
        //    {
        //        audioManager.UnPause();
        //    }
        //    else
        //    {
        //        audioManager.Pause();
        //    }
        //    var status = audioManager.Paused ? "paused" : "unpaused";
        //    await ReplyAsync($"Player is now {status}");
        //}

        [Command("sounds", RunMode = RunMode.Async)]
        [Alias("s")]
        public async Task SendUserAvailableSounds(string category = null)
        {
            var directories = await GetAudioFiles();
            StringBuilder message = new StringBuilder();
            if (string.IsNullOrWhiteSpace(category))
            {
                message.Append("Available sounds types" + Environment.NewLine);
                message.Append(string.Join(", ", directories.Keys));
                await ReplyAsync(message.ToString());
            }
            else if (directories.ContainsKey(category.ToLower().Trim()))
            {
                message.Append($"Available sounds for {category} below. Use ~p <filename> or ~play <filename> to play" + Environment.NewLine);

                foreach (var file in directories[category])
                {
                    var fileName = file.Name;
                    var extensionIndex = fileName.LastIndexOf('.');
                    fileName = fileName.Substring(0, extensionIndex);
                    fileName = fileName.Replace("-1", "").Trim();

                    message.Append($"   {fileName}{Environment.NewLine}");
                }
                await this.Context.User.SendMessageAsync(message.ToString().TrimEnd(','));
            }
            else
            {
                await ReplyAsync("Invalid sound option");
            }
        }

        private Task<Dictionary<string, List<FileInfo>>> GetAudioFiles()
        {
            var directoryFileList = new Dictionary<string, List<FileInfo>>();
            var directories = new DirectoryInfo(Directory.GetCurrentDirectory() + "\\audio").GetDirectories();
            foreach (var directory in directories)
            {
                var fileNames = directory.GetFiles().ToList();
                directoryFileList.Add(directory.Name, fileNames);
            }
            return Task.FromResult(directoryFileList);
        }
    }


}
