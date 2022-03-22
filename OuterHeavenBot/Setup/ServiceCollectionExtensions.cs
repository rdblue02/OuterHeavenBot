using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using OuterHeavenBot.Logging;

namespace OuterHeavenBot.Setup
{
    public static class ServiceCollectionExtensions
    {
        public static ILoggingBuilder AddApplicationLogger(this ILoggingBuilder builder)
        {
            builder.ClearProviders();
            builder.AddConfiguration();
            builder.SetMinimumLevel(LogLevel.Debug);
            
            LoggerProviderOptions.RegisterProviderOptions<BotSettings,ApplicationLoggerProvider>(builder.Services);            
            builder.Services.AddSingleton<ILoggerProvider, ApplicationLoggerProvider>();       
            return builder;
        }
         
        public static IServiceCollection AddBotSettings(this IServiceCollection services)
        {
            var configName = System.Diagnostics.Debugger.IsAttached ? "appsettings.Development.json" : "appsettings.json";
            IConfiguration config = new ConfigurationBuilder()
           .AddJsonFile(configName)
           .AddEnvironmentVariables()
           .Build();

            var settings = config.GetRequiredSection(nameof(BotSettings)).Get<BotSettings>();
            services.AddSingleton(settings);
          
            return services;
        }
    }
   
}
