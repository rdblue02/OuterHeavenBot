using Discord;
using Discord.Commands;
using OuterHeavenBot.Core.CommandValidation;
using OuterHeavenBot.Core;
using OuterHeavenBot.Core.Extensions;
using OuterHeavenBot.Lavalink;
using OuterHeavenBot.Client.Services;

namespace OuterHeavenBot.Client.Commands.Modules
{
    public class MusicCommands : ModuleBase<SocketCommandContext>
    {
        private ILogger<MusicCommands> logger;
        private LavalinkNode lavalinkNode;
        private MusicService musicService;
        private ISearchHandler<LavalinkTrack> searchHandler;
        public MusicCommands(ILogger<MusicCommands> logger,
                             LavalinkNode lavalinkNode,
                             MusicService musicService,
                             ISearchHandler<LavalinkTrack> searchHandler) 
        {
            this.logger = logger;
            this.lavalinkNode = lavalinkNode;
            this.musicService = musicService;
            this.searchHandler = searchHandler;
        }

        [Command("play", RunMode = RunMode.Async)]
        [Alias("p")]
        public async Task Play([Remainder] string argument)
        {
            try
            {
                if (await IsInvalidVoiceChannel(false)) return;

                if (string.IsNullOrWhiteSpace(argument))
                {
                    await ReplyAsync("A song name is required");
                    return;
                }

                var voice = this.Context.GetUserVoiceChannel() ?? throw new ArgumentNullException(nameof(IVoiceChannel));

                var tracks = (await searchHandler.SearchAsync(argument)).ToList();
                
                if (tracks.Count == 0)
                {
                    await ReplyAsync($"unable to find a match for {argument}");
                    return;
                }

                var result = tracks.Count == 1 ? await musicService.QueueTrack(voice, tracks.First()) :
                                                 await musicService.QueuePlayList(voice, tracks); 

                await ReplyAsync(result.Message);
            }
            catch (Exception e)
            {
                await ReplyAsync($"Error playing {argument}");
                logger.LogError($"Error playing {argument}:\n{e}");
            }
        }

        [Command("skip", RunMode = RunMode.Async)]
        [Alias("sk")]
        public async Task Skip()
        {
            try
            {
                if (await IsInvalidVoiceChannel(true)) return;

                var skipResult = await musicService.SkipTrackAsync();
                await ReplyAsync(skipResult.Message);
            }
            catch (Exception e)
            {
                await ReplyAsync($"Error skipping track");
                logger.LogError($"Error skipping track:\n{e}");
            }
        }

        [Command("clearqueue", RunMode = RunMode.Async)]
        [Alias("cq")]
        public async Task ClearQ([Remainder] int? index = null)
        {
            try
            { 
                if (await IsInvalidVoiceChannel(true)) return;
              
                await musicService.ClearQueue(index);
            }
            catch (Exception e)
            {
                await ReplyAsync($"Error clearing queue");
                logger.LogError($"Error clearing queue:\n{e}");
            }
        }
    
        async Task<bool> IsInvalidVoiceChannel(bool requireBotChannel) 
        {  
            if (Context.GetUserVoiceChannel() == null)
            {
                await ReplyAsync("You must be in a voice channel to use this command");
                return true;
            }

            if (requireBotChannel && !Context.UserIsInBotChannel()) 
            {
                await ReplyAsync("You and the bot must be in the same channel to use this command");
                return true;
            }

            return false;
        }
    }
}
