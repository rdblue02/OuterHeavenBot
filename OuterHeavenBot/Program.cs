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
   
    //removes quick edit from console so our logging does not freeze during Console.WriteLine();
    var success = DisableConsoleQuickEdit.Disable();
    if (!success)
    {
        Console.WriteLine("Unable to disable quick edit. If logging freezes in the console, press the escape key to resume it.");
    }
    await host.RunAsync();
}
catch (Exception ex)
{
    Console.WriteLine(ex);
    Console.Read();
}
