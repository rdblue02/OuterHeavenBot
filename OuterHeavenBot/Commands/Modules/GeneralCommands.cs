using Discord;
using Discord.Commands;
using OuterHeavenBot.Services;
using OuterHeavenBot.Setup;
using System.Text;
using System.IO.Compression;
using Microsoft.Extensions.Options;

namespace OuterHeavenBot.Commands.Modules
{
    public class GeneralCommands : ModuleBase<SocketCommandContext>
    {
        private ILogger logger;
        private MusicService musicService;
        private BotSettings botSettings;
        public GeneralCommands(ILogger<GeneralCommands> logger,
                               MusicService musicService,
                              IOptionsMonitor<BotSettings> config)
        {
            this.musicService = musicService;
            this.logger = logger;  
            this.botSettings = config.CurrentValue;
            config.OnChange(updatedConfig => botSettings = updatedConfig);
        }

        [Summary("Lists available commands")]
        [Command("help")]
        [Alias("h")]
        public async Task Help()
        {
            try
            {
                var embedBuilder = GetHelpMessage();
                await ReplyAsync(null, false, embedBuilder.Build());
            }
            catch(Exception ex)
            {
                await ReplyAsync("Error getting help message");
                logger.LogError($"Error getting help message:\n{ex.ToString()}");
            }         
        }


        [Command("disconnect", RunMode = RunMode.Async)]
        [Alias("dc")]
        public async Task Disconnect()
        {
            try
            {
                await ReplyAsync("Stopping music bot");
                await musicService.RequestDisconnect();
            }
            catch (Exception ex)
            {
                await ReplyAsync("Error stopping music bot");
                logger.LogError($"Error stopping music bot:\n{ex.ToString()}");
            }
        }

        #region devcommands

        //todo do I really want to run this async? 
        //it might make sense to block for this to avoid issues.
        [Command("requestlogs", RunMode = RunMode.Async)]
        public async Task RequestLogs()
        {           
            try
            {
             var currentLogFile = new DirectoryInfo(botSettings.LoggingConfiguration.LogDirectory).GetFiles()
                                                                                                  .OrderByDescending(x=>x.CreationTime)
                                                                                                  .FirstOrDefault();
                if(currentLogFile == null)
                {
                    await Context.User.SendMessageAsync("Error finding log file");
                }
                else
                {
                    var zipDirectoryName = currentLogFile.DirectoryName + "\\log_files";
                    DirectoryInfo zipDirectory = !Directory.Exists(zipDirectoryName) ? Directory.CreateDirectory(zipDirectoryName):
                                                                                       new DirectoryInfo(zipDirectoryName);                 

                    var zipFileName = $"{currentLogFile.DirectoryName}\\{zipDirectory.Name}.zip";
                    File.Copy(currentLogFile.FullName, $"{zipDirectory.FullName}\\{currentLogFile.Name}",true);

                    ZipFile.CreateFromDirectory(zipDirectory.FullName,zipFileName);
                                        
                    await Context.User.SendFileAsync(zipFileName,"logs");
                    await Task.Delay(100);

                    File.Delete($"{zipDirectory.FullName}\\{currentLogFile.Name}");                   
                    File.Delete(zipFileName);
                    Directory.Delete(zipDirectoryName);
                    logger.LogInformation($"Logs requested by user {Context.User.Username}. Sending log file {currentLogFile.Name}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error retrieving logs:\n{ex.ToString()}");
                await Context.User.SendMessageAsync($"Error retrieving logs:\n{ex}");
            }
        }
       
        #endregion

        private EmbedBuilder GetHelpMessage()
        {
            var commandList = new StringBuilder();
            var aliasList = new StringBuilder();
            var commandArgsList = new StringBuilder();
            var descriptionList = new StringBuilder();
            var commandNames = new List<string>()
            {
                "help"         ,
                "play"         ,
                "playlocal"    ,
                "pause"        ,
                "skip"         ,
                "clearqueue"   ,
                "fastforward"  ,
                "rewind"       ,
                "goto"         ,
                "trackInfo"    ,
                "queue"        ,
                "disconnect"   ,
                "clippie"      ,
                "sounds"
            };

            var aliases = new List<string>()
            {
                "h",
                "p",
                "pl" ,
                "pa" ,
                "sk" ,
                "cq" ,
                "ff" ,
                "rw" ,
                "gt" ,
                "t"  ,
                "q"  ,
                "dc" ,
                "c"  ,
                "s"
            };

            var commandArgs = new List<string>()
            {
                "none"                   ,
                "name | url"      ,
                "name | file path",
                "none"                   ,
                "none"                   ,
                "none"                   ,
                "seconds"                ,
                "seconds"                ,
                "hh:mm:ss"               ,
                "none"                   ,
                "none"                   ,
                "none"                   ,
                "name | category" ,
                "category"
            };

            var descriptions = new List<string>()
            {
                "Displays help info",
                "Play a song"       ,
                "Play from local directory",
                "Pause or unpause current song"   ,
                "Skips current song"  ,
                "Clear queue or song in queue"   ,
                "Fast forward current song" ,
                "Rewind current song",
                "Go to time stamp in song" ,
                "Get info about current song",
                "List songs in queue",
                "Disconnect the bot",
                "Play a clippe",
                "Get available clippes" ,
            };

            for (int i = 0; i < commandNames.Count; i++)
            {
                commandList.Append(commandNames[i] + Environment.NewLine);
                aliasList.Append(aliases[i] + Environment.NewLine);
                commandArgsList.Append(commandArgs[i] + Environment.NewLine);
                descriptionList.Append(descriptions[i] + Environment.NewLine);
            }

            EmbedBuilder embedBuilder = new EmbedBuilder()
            {
                Title = "Outer Heaven Bot Help Info",
                Color = Color.LighterGrey,

                Fields = new List<EmbedFieldBuilder>() {
              new EmbedFieldBuilder(){ IsInline= true, Name = "Command", Value= commandList },
              new EmbedFieldBuilder(){ IsInline= true, Name = "Args",Value = commandArgsList },
              new EmbedFieldBuilder(){ IsInline= true, Name = "Description",Value= descriptionList },
              new EmbedFieldBuilder(){ IsInline= true, Name = "Alias",Value = aliasList },
              new EmbedFieldBuilder(){ IsInline= true, Name = "Args",Value = commandArgsList },
              new EmbedFieldBuilder(){ IsInline= true, Name = "Description",Value= descriptionList },

             },
            };

            return embedBuilder;
        }
    } 
}
