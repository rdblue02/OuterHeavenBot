using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenLight.Extensions
{
    public static class LoggingExtensions
    {
        public static Task LogMessage(this ILogger logger, LogMessage message)
        {
            if (message.Severity == LogSeverity.Error) 
            {
                logger.LogError($"{message.Message}\n{message.Exception}\n{message.Source}");
            }
            else
            {
                logger.LogInformation(message.ToString());
            }

            return Task.CompletedTask;
        }
    }
}
