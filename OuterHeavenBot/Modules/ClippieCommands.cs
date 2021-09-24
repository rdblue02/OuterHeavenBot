using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Victoria;

namespace OuterHeavenBot.Modules
{
    public class ClippieCommands:ModuleBase<SocketCommandContext>
    {
        private Random random;
        private LavaNode lavaNode;
        public ClippieCommands(Random random, LavaNode lavaNode)
        {
            this.random = random;
            this.lavaNode = lavaNode;
        }

        [Command("clippie", RunMode = RunMode.Async)]
        [Alias("c")]
        public async Task Clippie(string contentName = null)
        {
          if(!UserIsInVoice())
          {
                await ReplyAsync("You must be connected to a voice channel!");
                return;
          }
            var player = await GetPlayer(false);
            if (player?.PlayerState == Victoria.Enums.PlayerState.Playing)
            {
                await ReplyAsync("Can't play clippes during music. That would be rude!");
                return;
            }
            var fileDirectories =  Helpers.GetAudioFiles();
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
                var bytes = File.ReadAllBytes(pathToContent);
                var channel = (Context.User as IGuildUser)?.VoiceChannel;
                var client =await channel.ConnectAsync();
                using (var discordOutStream = client.CreatePCMStream(AudioApplication.Mixed, 98304, 200))
                {
                    try
                    {
                        discordOutStream.Write(bytes);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                    finally
                    {
                        discordOutStream.Flush();
                    }
                    
                }
               await channel.DisconnectAsync();
            }
        }
       
        [Command("sounds", RunMode = RunMode.Async)]
        [Alias("cs")]
        public async Task SendUserAvailableSounds(string category = null)
        {
            var directories =   Helpers.GetAudioFiles();
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

                    message.Append($"{fileName}{Environment.NewLine}");
                }
                await this.Context.User.SendMessageAsync(message.ToString().TrimEnd(','));
            }
            else
            {
                await ReplyAsync("Invalid sound option");
            }
        }
        private async Task<LavaPlayer> GetPlayer(bool connect = true)
        {
            LavaPlayer player = null;
            if (lavaNode.HasPlayer(Context.Guild))
            {
                player = lavaNode.GetPlayer(Context.Guild);
            }
            else
            {
                if (connect)
                {
                    var voiceState = Context.User as IVoiceState;
                    player = await lavaNode.JoinAsync(voiceState.VoiceChannel, Context.Channel as ITextChannel);
                }
            }
            return player;
        }
         
        private bool UserIsInVoice()
        {
            var voiceState = Context.User as IVoiceState;
            if (voiceState?.VoiceChannel == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        
    }
}
