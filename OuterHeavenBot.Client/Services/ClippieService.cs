using Discord;
using Discord.Audio;
using Discord.Rest;
using Discord.WebSocket;
using OuterHeavenBot.Core;
using OuterHeavenBot.Core.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OuterHeavenBot.Client.Services
{
    public class ClippieService
    {
        ILogger<ClippieService> logger;
        static int isBusy = 0;
        AppSettings appSettings;
        DiscordSocketClient discordClient;
        public ClippieService(ILogger<ClippieService> logger,
                              AppSettings appSettings,
                              DiscordClientProvider clientProvider)
        {
            this.logger = logger;
            this.appSettings = appSettings;
            discordClient = clientProvider.GetClient(DiscordClientProvider.ClippieClientName);
        }
        public void Initialize()
        {
            //todo load the clips into memory so we can read them faster.
        }
        public async Task<CommandResult> RequestClippie(ClippieFileData clippieFile)
        {
            var result = new CommandResult() { Success = false };
            //Cant seem to manage the discordOutStream over multiple requests. It starts cutting off the clip after playing 2-3 requests. So we only let one
            //clippie at a time for now. Maybe one day we can set up an concurrent queue that will let users queue more than one clip.
            if (Interlocked.Exchange(ref isBusy, 1) == 0)
            {
                try
                {
                    logger.LogInformation($"Now playing clippie {clippieFile.Name}");
                    using var audioClient = await clippieFile.RequestingChannel.ConnectAsync(false, false, false, true);
                    using var discordOutStream = audioClient.CreatePCMStream(AudioApplication.Mixed, 98304, 20);

                    //let the sound of connecting finish before we start playing
                    await Task.Delay(300);
                    await discordOutStream.WriteAsync(clippieFile.Data);
                    await discordOutStream.FlushAsync();
                    await clippieFile.RequestingChannel.DisconnectAsync();
                    //let the sound of disconnect finish before we allow a new clip to be queued.
                    await Task.Delay(300);

                    Interlocked.Exchange(ref isBusy, 0);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message.ToString());
                    result.Message = ex.Message;
                    return result;
                }

                result.Success = true;
                return result;
            }
            else
            {
                result.Message = "Clippie bot is busy. Try again in a second!";
                return result;
            }
        }
    }
}
