using OuterHeavenBot;
using OuterHeavenBot.Logging;
using OuterHeavenBot.Workers;
using OuterHeavenBot.Setup;
using OuterHeavenBot.Services;

try
{
    IHost host = Host.CreateDefaultBuilder(args)

    .ConfigureServices(services =>
    {
        services.AddLogging(x => x.AddApplicationLogger());
        services.AddBotSettings();
        services.AddHostedService<ClippieBotWorker>();
        services.AddHostedService<OuterHeavenBotWorker>();
        services.AddHostedService<LoggingWorker>();
        services.AddDiscord();
    })
   .Build();

    await host.RunAsync();
}
catch (Exception ex)
{
    Console.WriteLine(ex);
    Console.Read();
}
