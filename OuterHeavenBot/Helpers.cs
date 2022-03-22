using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenBot
{
    public static class Helpers
    {
        public static void LogInfo(this ILogger logger, object logStatement)
        {
            logger.Log(LogLevel.Information, logStatement?.ToString() ?? "null");
        }
        public static void LogError(this ILogger logger, object logStatement)
        {
            logger.Log(LogLevel.Error, logStatement?.ToString() ?? "null");
        }

        public static LogLevel ToMicrosoftLogLevel(LogSeverity discordLogLevel)
        {
            switch (discordLogLevel)
            {
                case LogSeverity.Critical:
                    return LogLevel.Critical;
                case LogSeverity.Error:
                    return LogLevel.Error;
                case LogSeverity.Warning:
                    return LogLevel.Warning;
                case LogSeverity.Info:
                    return LogLevel.Information;
                case LogSeverity.Verbose:
                    return LogLevel.Debug;
                case LogSeverity.Debug:
                    return LogLevel.Trace;
                default:
                    return LogLevel.None;
            }
        }
        public static LogSeverity ToDiscordLogLevel(LogLevel miscrosoftLogLevel)
        {
            switch (miscrosoftLogLevel)
            {
                case LogLevel.Critical:
                    return LogSeverity.Critical;
                case LogLevel.Error:
                    return LogSeverity.Error;
                case LogLevel.Warning:
                    return LogSeverity.Warning;
                case LogLevel.Information:
                    return LogSeverity.Info;
                case LogLevel.Debug:
                    return LogSeverity.Verbose;
                case LogLevel.Trace:
                    return LogSeverity.Debug;
                default:
                    return LogSeverity.Critical;
            }
        }
         
        public static IVoiceChannel? ToVoiceChannel(this SocketCommandContext? socketContext)=>
             (socketContext?.User as IVoiceState)?.VoiceChannel;
        public static ITextChannel? ToTextChannel(this SocketCommandContext? socketContext) =>
             (socketContext?.Channel as ITextChannel);
        public static bool InVoiceChannel(this SocketCommandContext context) 
        {
            var voiceState = (context?.User as IVoiceState);
            return (voiceState != null && voiceState.VoiceChannel != null && !voiceState.VoiceChannel.Name.ToLower().Contains("afk"));         
        }
    }
}
