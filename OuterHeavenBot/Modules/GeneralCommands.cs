using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Victoria;

namespace OuterHeavenBot.Modules
{ 
    public class GeneralCommands : ModuleBase<SocketCommandContext>
    {
        private LavaNode lavaNode;
        public GeneralCommands(LavaNode lavaNode)
        {
            this.lavaNode = lavaNode;
        }

        [Summary("Lists available commands")]
        [Command("help")]
        [Alias("h")]
        public async Task Help()
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
                "clippesounds"
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
                "cs"
            };

            var commandArgs = new List<string>()
            {
                "none"                   ,
                "name | url"      ,
                "name | file path",
                "none"                   ,
                "none"                   ,
                "index"                  ,
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

            await ReplyAsync(null, false, embedBuilder.Build());
        }


        [Command("disconnect", RunMode = RunMode.Async)]
        [Alias("dc")]
        public async Task Disconnect()
        {
            await ReplyAsync("Stopping music bot");
            if (lavaNode.HasPlayer(Context.Guild))
            {
                var player =  lavaNode.GetPlayer(Context.Guild);
                if (player != null)
                {
                    await lavaNode.LeaveAsync(player.VoiceChannel);
                }
                await lavaNode.DisconnectAsync();
            }
        }

        [Command("options")]
        public async Task Options()
        {

        }
    }


}
