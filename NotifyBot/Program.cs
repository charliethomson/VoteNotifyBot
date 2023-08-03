using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NotifyBot;
using NotifyBot.Database;
using NotifyBot.Secrets;
using NotifyBot.Services;
using Serilog;
using Serilog.Core;
using Serilog.Events;


Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().MinimumLevel
    .Override("Microsoft", LogEventLevel.Information).Enrich.FromLogContext().WriteTo.Console().CreateLogger();

using var host = Host.CreateDefaultBuilder(args)
    .UseSerilog(Log.Logger)
    .ConfigureServices(services =>
    {
        services.AddSerilog();
        services.AddSingleton<IDopplerService, DopplerService>();
        services.AddSingleton<IDatabaseClient, DatabaseClient>();
        services.AddSingleton<IExpiredVotesService, ExpiredVotesService>();
        services.AddSingleton<INotifyService, NotifyService>();
        services.AddHostedService<App>();
    }).ConfigureAppConfiguration((hostingContext, configuration) =>
    {
        configuration.Sources.Clear();
        var env = hostingContext.HostingEnvironment;
        configuration
            .AddJsonFile(@"C:\Users\c\git\NotifyBot\NotifyBot\appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, true);
    }).Build();

await host.RunAsync();