using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenBot.Core.Extensions
{
    public static class CommandContextHelpers
    {
        public static IVoiceChannel? GetUserVoiceChannel(this SocketCommandContext context)
        {
            if (context?.User is IVoiceState voiceState) 
            {
                return voiceState.VoiceChannel;
            }

            return null;
        }

        public static IVoiceChannel? GetBotVoiceChannel(this SocketCommandContext context)
        {
            if (context?.Guild?.CurrentUser is IVoiceState voiceState)
            {
                return voiceState.VoiceChannel;
            }

            return null;
        }

        public static bool UserIsInBotChannel(this SocketCommandContext context)
        {
            var userChannel = context.GetUserVoiceChannel();
            var botChannel = context.GetBotVoiceChannel();

            return userChannel != null &&
                   botChannel != null &&
                   userChannel.Id == botChannel.Id;
        }
    }
}
