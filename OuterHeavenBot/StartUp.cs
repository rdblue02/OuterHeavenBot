using OuterHeavenBot;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenBot
{
    public class StartUp
    {
        private readonly DiscordBotInitializer discordBotInitializer;
        public StartUp(DiscordBotInitializer discordBotInitializer)
        {
            this.discordBotInitializer = discordBotInitializer;
        }
        public async Task Start()
        {
            //   await  this.discordBotInitializer.Initialize();

            //folders for this have been removed to reduce size.
            //this needs to be in a different tool.
            await CompileAudioDirectories();
            Console.WriteLine("Done");
        }

        //this needs to be added to the build pipeline. Not ran in startup
        private async Task CompileAudioDirectories()
        {
            var directories = new DirectoryInfo(@"C:\Users\ryanb\source\repos\OuterHeavenBot\OuterHeavenBot\bin\Debug\netcoreapp3.1\pre-compiled-audio").GetDirectories();
            var directoryTasks = new List<Task>();
            foreach(var directory in directories)
            {
                directoryTasks.Add(CompileAudioFile(directory));
            }
            await Task.WhenAll(directoryTasks);
            Console.WriteLine("Compile Complete!");
        }
        private async Task CompileAudioFile(DirectoryInfo fromDirectory)
        {
            var toDirectory = @"C:\Users\ryanb\source\repos\OuterHeavenBot\OuterHeavenBot\bin\Debug\netcoreapp3.1\audio\" + fromDirectory.Name+"\\";
            if(!Directory.Exists(toDirectory))
            {
                Directory.CreateDirectory(toDirectory);
            }

            var files = fromDirectory.GetFiles();
            List<Task> fileCreateTasks = new List<Task>();

            foreach(var file in files)
            {
                var toFileName = file.Name.Substring(0, file.Name.LastIndexOf(".")) + ".b";
                if (!File.Exists(toDirectory + toFileName))
                {
                  fileCreateTasks.Add(Create(file.FullName,toDirectory+ toFileName));
                }              
            }
            await Task.WhenAll(fileCreateTasks);
            Console.WriteLine($"Finished compilling {fromDirectory.Name} directory");
        }
        private async Task Create(string fromPath,string toPath)
        {
           using ( var process = Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-hide_banner -loglevel panic -i \"{fromPath}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true
            }))
            {
                using(var output = process.StandardOutput.BaseStream)
                {
                    using (var fileStream = File.Create(toPath))
                    {
                        await output.CopyToAsync(fileStream);
                        await fileStream.FlushAsync();
                    }                 
                }
            }
            
          
        }
    }
}
