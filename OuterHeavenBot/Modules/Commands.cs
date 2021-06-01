using Discord;
using Discord.Audio;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenBot.Modules
{
   public class Commands: ModuleBase<SocketCommandContext>
    {
        private Random random;
        public Commands(Random random)
        {
            this.random = random;
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

            EmbedBuilder embedBuilder = new EmbedBuilder() { 
             Title = "Outer Heaven Bot Help Info",
             Color = Color.LighterGrey,
             Fields = new List<EmbedFieldBuilder>() {
              new EmbedFieldBuilder(){ IsInline= true, Name = "Command", Value= commandList },
              new EmbedFieldBuilder(){ IsInline= true, Name = "Alias",Value = aliasList },
              new EmbedFieldBuilder(){ IsInline= true, Name = "Description",Value= descriptionList },
             }, 
            };
        
            await ReplyAsync(null,false, embedBuilder.Build());
        }

        //todo fix variable names for argument/argument mutations.
        //we can be more specific no that we know what this command will do.
        [Command("play", RunMode = RunMode.Async)]
        [Alias("p")]
        public async Task Play(string argument = null)
        {
            var channel = (Context.User as IGuildUser)?.VoiceChannel;
            if (channel == null) { await Context.Channel.SendMessageAsync("User must be in a voice channel, or a voice channel must be passed as an argument."); return; }

            var fileDirectories = await GetAudioFiles();
            var availableFiles = fileDirectories.SelectMany(x => x.Value).Select(x => x.FullName).ToList();

            //variable used so we don't mutate the origional argument value
            var argumentMutation = "";
            if (string.IsNullOrEmpty(argument))
            {
                int retry = 0;
                do
                {
                    var index = random.Next(0, availableFiles.Count);
                    argumentMutation = availableFiles[index];
                    retry++;
                } while (argument.Contains("\\music\\") && retry<20);
            }
            else if (fileDirectories.Keys.Contains(argument.ToLower().Trim()))
            {
                var index = random.Next(0, fileDirectories[argument].Count);
                argumentMutation = fileDirectories[argument][index].FullName;
            }
            else
            {
                argumentMutation = argument.ToLower().Trim();

                bool matchFound = false;

                foreach(var fileName in availableFiles)
                {
                    //we have an exact match including extension. No need for further checks.
                    if(fileName.ToLower() == argumentMutation)
                    {
                        argumentMutation = fileName;
                        matchFound = true;
                        break;
                    }

                    if(fileName.LastIndexOf('.') > 0 || fileName.LastIndexOf('\\') > 0)
                    {
                        var friendlyName = fileName.Substring(fileName.LastIndexOf('\\') + 1);
                        var extensionIndex = friendlyName.LastIndexOf('.');
                        friendlyName = friendlyName.Substring(0, extensionIndex).ToLower().Trim();
                       
                        if (friendlyName == argumentMutation || friendlyName.Replace("-1", "").Trim() == argumentMutation)
                        {
                            argumentMutation = fileName;
                            matchFound = true;
                            break;
                        }
                    } 
                }
                //no exact match found for literal or friendly file name. Take the next closest one.
                if (!matchFound)
                {
                    argumentMutation = availableFiles.FirstOrDefault(x => x.ToLower().Contains(argumentMutation));
                }                              
            }

            if(string.IsNullOrEmpty(argumentMutation))
            {
                await ReplyAsync($"No files found for {argument}");
            }
            else
            {
                // For the next step with transmitting audio, you would want to pass this Audio Client in to a service.
                var audioClient = await channel.ConnectAsync();
                await SendAsync(audioClient, argumentMutation);
            }
           
            await channel.DisconnectAsync();

        }
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
            else if(directories.ContainsKey(category.ToLower().Trim()))
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

        private async Task SendAsync(IAudioClient client, string path)
        {
            // Create FFmpeg using the previous example
            using(var fs = File.OpenRead(path))
            using (var discord = client.CreatePCMStream(AudioApplication.Mixed, 98304, 200))
            {
                try
                {
                    await fs.CopyToAsync(discord);
                }
                finally
                {
                    await discord.FlushAsync();
                }
            }
        }
        private Task<Dictionary<string,List<FileInfo>>> GetAudioFiles()
        {
            var directoryFileList = new Dictionary<string, List<FileInfo>>();
           var directories = new DirectoryInfo(Directory.GetCurrentDirectory() + "\\audio").GetDirectories();
            foreach(var directory in directories)
            {
                var fileNames = directory.GetFiles().ToList();
                directoryFileList.Add(directory.Name, fileNames);
            }
            return Task.FromResult(directoryFileList);
        }
    }
}
