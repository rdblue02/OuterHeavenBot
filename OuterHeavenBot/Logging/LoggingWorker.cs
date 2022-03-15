using Microsoft.Extensions.Options;
using OuterHeavenBot.Setup;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenBot.Logging
{
    public class LoggingWorker : BackgroundService
    {
        
        private LoggingConfiguration _currentConfig;  
        private ConcurrentQueue<string> _loggersQueue;
        public LoggingWorker(IOptionsMonitor<BotSettings> config)
        {
            _currentConfig = config.CurrentValue.LoggingConfiguration;
             config.OnChange(updatedConfig => _currentConfig = updatedConfig.LoggingConfiguration);
            _loggersQueue = new ConcurrentQueue<string>();
           
            ApplicationLogger.OnLog += _applicationLogger_OnLog;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!Directory.Exists(_currentConfig.LogDirectory))
            {
                Directory.CreateDirectory(_currentConfig.LogDirectory);
            }
            await base.StartAsync(cancellationToken);
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await WriteLogsAsync(stoppingToken);
                await Task.Delay(_currentConfig.PollingMilliseconds);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_loggersQueue.Any())
            {
               await WriteLogsAsync(cancellationToken);
            }

            await base.StopAsync(cancellationToken);
        }

        private void _applicationLogger_OnLog(object? sender, ApplicationLogEvent e)
        {
            _loggersQueue.Enqueue(e.LogMessage);
        }

        private async Task WriteLogsAsync(CancellationToken stoppingToken)
        {
            var logTextBuilder = new StringBuilder();
            try
            {
                var todaysDate = DateTime.Now;
                ClearOutdatedLogs(todaysDate);

                while (_loggersQueue.TryDequeue(out string? logMessage) && !stoppingToken.IsCancellationRequested)
                {
                    logTextBuilder.Append(logMessage + Environment.NewLine);
                }

                var logFiles = new DirectoryInfo(_currentConfig.LogDirectory).GetFiles();
                var currentLogFile = logFiles.FirstOrDefault(x => x.CreationTime.Date == todaysDate.Date);

                var logTask = currentLogFile != null ?
                    File.AppendAllTextAsync(currentLogFile.FullName, logTextBuilder.ToString(), stoppingToken) :
                    File.WriteAllTextAsync(_currentConfig.LogDirectory + $"{todaysDate.ToShortDateString().Replace("/", "_")}_logs.txt", logTextBuilder.ToString(), stoppingToken);

                await logTask; 
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Erroring writing logs while executing {ex.TargetSite}.\nError: {ex}\nLogMessage: {logTextBuilder}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private void ClearOutdatedLogs(DateTime todaysDate)
        {
            var logFiles = new DirectoryInfo(_currentConfig.LogDirectory).GetFiles().Where(x => x.CreationTime.Date < todaysDate.Date).OrderBy(x => x.CreationTime).ToList();
            
            if (_currentConfig.MaxNumberOfLogFiles > 0 && logFiles.Count > logFiles.Count)
            {
                foreach (var fileToDelete in logFiles.Skip(logFiles.Count - _currentConfig.MaxNumberOfLogFiles).ToList())
                {
                    logFiles.Remove(fileToDelete);
                    File.Delete(fileToDelete.FullName);
                }
            }

            if (_currentConfig.MaxDaysToSaveLogs > 0)
            {
                foreach (var fileToDelete in logFiles.Where(x=>x.CreationTime.Date.AddDays(_currentConfig.MaxDaysToSaveLogs)<todaysDate.Date).ToList())
                {
                    logFiles.Remove(fileToDelete);
                    File.Delete(fileToDelete.FullName);
                }
            } 
        }
    }
}
