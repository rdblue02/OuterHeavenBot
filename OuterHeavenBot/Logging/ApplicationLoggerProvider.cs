using Microsoft.Extensions.Options;
using OuterHeavenBot.Setup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenBot.Logging
{
    public class ApplicationLoggerProvider : ILoggerProvider
    {

        private readonly IDisposable _onChangeToken;
        private BotSettings _currentConfig;
        private ILogger? _logger;   
        public ApplicationLoggerProvider(IOptionsMonitor<BotSettings> config)
        {
            _currentConfig = config.CurrentValue;
            _onChangeToken = config.OnChange(updatedConfig => _currentConfig = updatedConfig);
            
        }
        public ILogger CreateLogger(string categoryName) 
        {
            if(_logger == null)
            {
              _logger = new ApplicationLogger(categoryName, () => _currentConfig);
            }
            return _logger; 
        }
        public void Dispose() => _onChangeToken?.Dispose();
    }
}
