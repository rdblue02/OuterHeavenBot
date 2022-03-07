using OuterHeavenBot.Setup;
using OuterHeavenBot.Workers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenBot.Logging
{
    public class ApplicationLogger : ILogger
    {
        string name;
        Func<BotSettings> _getCurrentConfig;
        public static event EventHandler<ApplicationLogEvent>? OnLog;
        public ApplicationLogger(string name, Func<BotSettings> getCurrentConfig)
        {
            this.name = name;
            this._getCurrentConfig = getCurrentConfig;
        }

        public IDisposable BeginScope<TState>(TState state) => default!;

        public bool IsEnabled(LogLevel logLevel) =>
            logLevel >= _getCurrentConfig().LoggingConfiguration.Verbosity && logLevel < LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) 
        {
            if (IsEnabled(logLevel))
            {
                var logMessage = $"{DateTime.Now.ToString("T")}|{name}|{logLevel}| {formatter(state, exception)}";
                OnLog?.Invoke(this, new ApplicationLogEvent(logMessage));
            }    
        }                    
    } 
}
