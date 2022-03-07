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
            builder.AddConfiguration();
            builder.SetMinimumLevel(LogLevel.Debug);
            LoggerProviderOptions.RegisterProviderOptions<BotSettings,ApplicationLoggerProvider>(builder.Services);
            builder.Services.AddSingleton<ILoggerProvider, ApplicationLoggerProvider>();       
            return builder;
        }
         
        public static IServiceCollection AddBotSettings(this IServiceCollection services)
        {
            IConfiguration config = new ConfigurationBuilder()
           .AddJsonFile("appsettings.json")
           .AddEnvironmentVariables()
           .Build();

            var settings = config.GetRequiredSection(nameof(BotSettings)).Get<BotSettings>();
            services.AddSingleton(settings);
          
            return services;
        }
    }
   
}
