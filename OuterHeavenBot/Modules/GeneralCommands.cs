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
 

namespace OuterHeavenBot.Modules
{
    public class GeneralCommands : ModuleBase<SocketCommandContext>
    {
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

        [Command("options")]
        public async Task Options()
        {
             
        }               
    }


}
